# Pokepad Lambda — Search API

ASP.NET Core minimal API hosted on AWS Lambda via `Amazon.Lambda.AspNetCoreServer.Hosting`. Accepts natural language questions, converts them to SQL using OpenAI, executes them against Athena, and returns tabular results.

## Endpoints

The OpenAPI spec is served at `GET /openapi/v1.json` when running locally.

### `GET /v1/health`

No auth required. Returns `{"status":"healthy"}` with HTTP 200.

---

### `POST /v1/search`

**Auth required.** Synchronous search — blocks until the Athena query completes (up to the 30 s Lambda timeout).

**Request body**
```json
{ "question": "Which product categories generated the most revenue last month?" }
```

**Response `200 OK`**
```json
{
  "sql": "SELECT Category, SUM(oi.Subtotal) AS revenue ...",
  "columns": ["Category", "revenue"],
  "rows": [["Electronics", "142300.50"], ["Clothing", "98100.00"]]
}
```

**When to use vs `/v1/query/start`**: Use `/v1/search` for simple queries where the answer is expected in a few seconds. Use the async pair for queries that may take longer than the API client's timeout.

---

### `POST /v1/query/start`

**Auth required.** Starts an Athena query and returns immediately with an `executionId`.

**Request body** — same as `/v1/search`.

**Response `202 Accepted`**
```json
{ "executionId": "abc-123-..." }
```

The `Location` header is set to `/v1/query/{executionId}/status`.

---

### `GET /v1/query/{id}/status`

**Auth required.** Returns the current state of an Athena query execution.

**Response `200 OK`**
```json
{ "executionId": "abc-123-...", "status": "RUNNING" }
```

Possible `status` values: `QUEUED`, `RUNNING`, `SUCCEEDED`, `FAILED`, `CANCELLED`.

Returns `404` if the `executionId` is unknown or expired (24-hour TTL). Returns `403` if the caller is not the user who started the query.

---

### `GET /v1/query/{id}/results`

**Auth required.** Fetches results for a completed query.

Returns `409 Conflict` with the current status if the query has not yet `SUCCEEDED`. Returns `404`/`403` under the same conditions as `/status`.

**Response `200 OK`** — same shape as `/v1/search`.

## Services

### OpenAiService

Calls the OpenAI API to translate a natural language question into an Athena SQL `SELECT` statement. The Glue schema is injected into the system prompt.

Uses `gpt-4o`. Max tokens: 1024.

### GlueSchemaService

Fetches table definitions from the Glue Data Catalog at query time and formats them as a schema string for the OpenAI system prompt. The catalog has four tables: `customers`, `products`, `orders`, `order_items`.

### AthenaService

Wraps the Athena SDK. Provides both a blocking `ExecuteAsync` (poll-until-done) and a non-blocking `StartAsync`/`GetExecutionAsync`/`FetchResultsAsync` split used by the async endpoints. All queries run in the `pokepad` workgroup against the `ecommerce_gold` database.

### SqlValidator

Runs before every Athena query. Verifies the generated SQL:

1. Must start with `SELECT` (case-insensitive).
2. Must not contain any of: `DROP`, `DELETE`, `INSERT`, `UPDATE`, `CREATE`, `ALTER`, `TRUNCATE`, `EXEC`, `EXECUTE`, `MERGE`, `--`, `/*`.

This is a defence-in-depth guard — the model is already instructed to write only `SELECT` queries, but the validator catches any prompt-injection or model misbehaviour before it reaches Athena.

### QueryTrackingService

Writes `executionId → userId` mappings to DynamoDB with a 24-hour TTL. The async status and results endpoints check this table to enforce that a user can only see their own queries.

## Authentication design

API Gateway validates the Cognito JWT before the request reaches Lambda. The Lambda itself does **not** call the JWKS endpoint — it only base64-decodes the already-trusted payload to extract the `sub` claim (the user's unique ID).

This avoids an outbound JWKS fetch on every cold start. The trade-off is that Lambda trusts the authorizer; revoked tokens will be honoured until the API Gateway cache expires (or until the authorizer rejects them at the gateway level).

## Environment variables

| Variable | Source | Description |
|----------|--------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Lambda config | Set to `Production` in deployed Lambda |
| `API_KEY_PARAM` | Lambda config | SSM parameter path for the OpenAI key |
| `API_KEY` | Set at startup from SSM | Consumed by `OpenAiService` |
| `ATHENA_OUTPUT_LOCATION` | Lambda config | S3 URI for Athena result output |
| `DYNAMODB_TABLE_NAME` | Lambda config | DynamoDB table for query tracking |

In `Development` mode the SSM fetch is skipped. Set `API_KEY` directly in your shell instead.
