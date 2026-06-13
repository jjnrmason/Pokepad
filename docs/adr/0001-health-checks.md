# ADR-0001: Health checks for the Pokepad Gold API

**Status:** Accepted
**Date:** 2026-06-12
**Deciders:** John Mason
**Related:** [#27 — What should health checks look like?](https://github.com/jjnrmason/pokepad/issues/27)

## Context

`GET /v1/health` exists today as an unauthenticated route that returns `{ "status": "healthy" }` without touching any dependency. It answers "is the Lambda process alive behind API Gateway?" and nothing more.

The API now depends on Glue (schema), OpenAI (SQL generation, embeddings, moderation), Athena + S3 (query execution and results), DynamoDB (async query ownership tracking with 24 h TTL), RDS pgvector (semantic search), SSM/Secrets Manager (startup configuration in production), and Cognito (auth at the API Gateway layer). A green liveness check tells us nothing about whether search traffic can actually be served, and during an incident there is currently no way to ask the service *which* dependency is broken without reading CloudWatch logs.

Forces at play:

- Liveness must stay cheap (< 50 ms warm) and safe to expose publicly — API Gateway and uptime monitors hit it frequently.
- Dependency checks have real cost and blast radius: OpenAI calls are paid and rate-limited; Athena queries scan S3; an over-eager readiness probe can become its own load problem.
- The response is operator-facing and must support dashboards/incident debugging without leaking infrastructure detail (no ARNs, connection strings, secret names, or raw exception messages).
- Lambda is not a long-running process with a separate readiness gate — "readiness" here is a diagnostic endpoint, not a Kubernetes-style traffic gate.

## Decision

Keep two levels of health:

1. **`GET /v1/health` stays public, shallow liveness.** No auth, no external calls. Extend the body to identify the service and API version.
2. **Add `GET /v1/health/dependencies` as an authenticated readiness/diagnostic endpoint.** It probes Glue, Athena, DynamoDB, the vector store, and configuration presence in parallel with a per-check timeout, and never calls OpenAI.

OpenAI availability is explicitly out of scope for health checks; it is covered by the performance-test canaries and CloudWatch alarms instead.

## Options Considered

### Option A: Keep `/v1/health` only (status quo)

| Dimension | Assessment |
|-----------|------------|
| Complexity | None |
| Cost | None |
| Incident value | Low — cannot distinguish "alive" from "able to serve" |
| Risk | None |

**Pros:** Zero work, zero new attack surface.
**Cons:** Every dependency outage looks identical from the outside; operators must correlate logs manually; no dashboardable readiness signal.

### Option B: Public liveness + authenticated `/v1/health/dependencies` (chosen)

| Dimension | Assessment |
|-----------|------------|
| Complexity | Low–medium — one endpoint, five cheap probes, one CDK route |
| Cost | Negligible — free-tier control-plane calls and a `SELECT 1` |
| Incident value | High — single request answers "what is broken?" |
| Risk | Low — auth required, no secrets in response, per-check timeouts cap latency |

**Pros:** Clear liveness/readiness separation; cheap probes only; response shape feeds dashboards directly; auth keeps infrastructure topology private.
**Cons:** Slight duplication of dependency knowledge in one more place; probes test reachability, not full workflow correctness (a passing check does not guarantee OpenAI can generate SQL).

### Option C: Deep readiness including OpenAI round-trips

| Dimension | Assessment |
|-----------|------------|
| Complexity | Medium |
| Cost | Recurring paid API calls from routine monitoring |
| Incident value | High but noisy |
| Risk | Medium–high — rate-limit coupling, cost scales with polling frequency |

**Pros:** Closest to "can a real search succeed end to end".
**Cons:** Health pollers would consume OpenAI quota and money; an OpenAI brownout would flap the whole readiness signal even though Athena-only workflows still work; latency of the check becomes unbounded by external SLAs. Rejected for routine checks — end-to-end coverage belongs in scheduled canaries (integration/performance test runs), not in a poll-every-minute endpoint.

## The dependency checks

| Check | Probe | Criticality | Notes |
|---|---|---|---|
| `glue` | `GetDatabase` for `ecommerce_gold` | **Critical** — SQL search cannot build prompts without schema | Control-plane call, no scan |
| `athena` | `GetWorkGroup` for `pokepad` | **Critical** — all SQL execution | Does not start a query, so no S3 scan cost |
| `dynamodb` | `DescribeTable` on `DYNAMODB_TABLE_NAME` | **Critical** — async query ownership/`403` enforcement | Control-plane; avoids consuming read capacity |
| `vectorStore` | `SELECT 1`, then `SELECT EXISTS (SELECT 1 FROM products_embeddings)` | **Degraded-only** — semantic search is a secondary workflow | Connection failure ⇒ this check is `unhealthy` but overall status caps at `degraded`; empty table ⇒ `degraded` with message `"products_embeddings is empty"` |
| `configuration` | Presence (not values) of required settings: AI key parameter, vector DB connection parameter/secret ARN, `DYNAMODB_TABLE_NAME` | **Critical** | Names of *missing* variables may be listed; values never |

Probes run concurrently with a 2-second timeout each; a timeout marks the check `unhealthy` with message `"timed out"`. Worst-case endpoint latency is therefore ~2 s, typical warm latency well under 500 ms.

Status roll-up:

- `healthy` — every check passes.
- `degraded` — all critical checks pass but `vectorStore` fails or reports an empty embeddings table.
- `unhealthy` — any critical check (`glue`, `athena`, `dynamodb`, `configuration`) fails.

## Response contract

`GET /v1/health` (public, no dependency calls, always `200` while the process can respond):

```json
{
  "status": "healthy",
  "service": "pokepad-gold-api",
  "version": "v1"
}
```

`GET /v1/health/dependencies` (Cognito-authenticated):

```json
{
  "status": "degraded",
  "checks": {
    "glue":          { "status": "healthy",  "latencyMs": 24 },
    "athena":        { "status": "healthy",  "latencyMs": 31 },
    "dynamodb":      { "status": "healthy",  "latencyMs": 12 },
    "vectorStore":   { "status": "degraded", "latencyMs": 210, "message": "products_embeddings is empty" },
    "configuration": { "status": "healthy" }
  }
}
```

- `message` is optional, human-readable, and sanitised — fixed strings chosen by the check, never raw exception text.
- HTTP mapping: `200` for `healthy` **and** `degraded` (the service still serves its core workflows; monitors read the JSON status for the distinction), `503` for `unhealthy`. This keeps load-balancer/synthetic-monitor semantics simple: 5xx means "page someone".

### Auth model

`/v1/health/dependencies` requires the same Cognito JWT as the rest of the API (`RequireAuthorization()` in the app, JWT authorizer on the route in CDK). Rationale: the response enumerates infrastructure components and their failure states — useful reconnaissance if public. Operators and dashboards already hold machine credentials (the integration-test users prove the flow). No separate scope/group is needed at current team size; revisit if external consumers ever get tokens.

## Trade-off Analysis

- **Control-plane probes vs real workloads.** `DescribeTable`/`GetWorkGroup` prove reachability and IAM, not data-path health. That is the right trade: the data path is exercised continuously by real traffic and by the scheduled integration/performance runs, while the readiness endpoint must stay safe to poll every 30–60 s.
- **`degraded` over `unhealthy` for the vector store.** Semantic search failing should not cause a 503 that pages on-call at 3 a.m. while SQL search is fine. The empty-table case specifically catches "deployed before the embedding indexer ran" — a data-readiness problem, not an outage.
- **200-for-degraded.** Returning 503 for degraded would let dumb monitors catch it, but couples paging to a non-critical subsystem. Dashboards that care about degraded parse the body.
- **No caching of check results initially.** At expected polling rates the probes are cheap enough; add a 10–30 s in-memory cache only if CloudWatch shows the endpoint generating meaningful Glue/Athena API traffic.

## Consequences

- **Easier:** incident triage (one authenticated GET shows which dependency is down), post-deploy verification (readiness check in the deploy pipeline), dashboarding (stable JSON contract).
- **Harder:** the endpoint encodes a dependency list that must be kept in sync as services are added (e.g. if semantic search gains a cache layer, the check list needs updating).
- **Revisit when:** OpenAI becomes checkable via a free/zero-cost ping; an external consumer needs readiness without full API credentials; polling volume justifies response caching.

## CloudWatch alarm recommendations

Health endpoints complement, not replace, alarms. Recommended set (CDK `MonitoringConstruct`, future ticket):

| Alarm | Metric | Suggested threshold |
|---|---|---|
| Lambda errors | `AWS/Lambda Errors` | > 0 for 2 consecutive 5-min periods |
| Lambda duration | `AWS/Lambda Duration p95` | > 80 % of timeout |
| API 5xx | `AWS/ApiGateway 5xx` | > 1 % of requests over 5 min |
| API 429 | `AWS/ApiGateway 4xx` with route filtering (or access-log metric filter on status 429) | sustained > 5/min |
| Athena query failures | `AWS/Athena QueryState=FAILED` (EventBridge rule → metric) | > 0 over 15 min |
| RDS connection failures | Log metric filter on Lambda logs for Npgsql connection exceptions | > 0 over 5 min |
| DynamoDB throttles | `AWS/DynamoDB ThrottledRequests` | > 0 |
| OpenAI failures | Log metric filter on OpenAI error responses | sustained > 3/5 min |

## Action Items

1. [ ] Accept this ADR (or adjust criticality/auth decisions) on issue #27.
2. [ ] [#30](https://github.com/jjnrmason/pokepad/issues/30) — extend `/v1/health` body (`service`, `version`); add `/v1/health/dependencies` endpoint with the five parallel checks, per-check 2 s timeout, and status roll-up above (`Services/Pokepad.Gold.Api/Endpoints/V1/HealthEndpoints.cs` + a `HealthCheckService`).
3. [ ] [#31](https://github.com/jjnrmason/pokepad/issues/31) — CDK: add the authenticated `GET /v1/health/dependencies` route to the HTTP API (`LambdaConstruct`), and grant the Lambda role `glue:GetDatabase`, `athena:GetWorkGroup`, `dynamodb:DescribeTable` if not already present.
4. [ ] [#32](https://github.com/jjnrmason/pokepad/issues/32) — CDK: `MonitoringConstruct` implementing the alarm table above with an SNS topic for notifications.
5. [ ] [#33](https://github.com/jjnrmason/pokepad/issues/33) — integration tests for both endpoints in `Pokepad.Gold.Api.IntegrationTests` (200 public liveness, 401 unauthenticated dependencies call, 200 + contract shape when authenticated).
6. [ ] Point uptime monitoring at `/v1/health` and the deploy pipeline's post-deploy verification at `/v1/health/dependencies`.
