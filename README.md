# Pokepad

Pokepad is a natural language search API over e-commerce data. You ask a question in plain English; Claude translates it to SQL, Athena runs it against a data lake, and the results come back as structured JSON.

## Architecture

```
Client
  │
  ▼
API Gateway (HTTP API)   ← rate limited: 10 req/s burst
  │  JWT verified by Cognito authorizer
  ▼
Lambda (ASP.NET Core, 512 MB, 30 s)
  ├── ClaudeService  ────────► Anthropic API (SQL generation)
  ├── GlueSchemaService ─────► Glue Data Catalog (schema lookup)
  ├── AthenaService  ────────► Athena workgroup "pokepad"
  │                                   │
  │                            S3 Gold bucket (Parquet data)
  │                            S3 Athena results bucket
  │
  ├── QueryTrackingService ──► DynamoDB (async query ownership, 24 h TTL)
  └── SSM Parameter Store  ──► /pokepad/anthropic-api-key (encrypted)
```

**Data lake — medallion layers**

| Layer  | S3 bucket prefix | Contents |
|--------|-----------------|----------|
| Bronze | `pokepad-bronze-*` | Raw ingestion (unmodified source data) |
| Silver | `pokepad-silver-*` | Cleaned, deduplicated |
| Gold   | `pokepad-gold-*`   | Analytics-ready Parquet; what the API queries |

The Glue catalog (`ecommerce_gold` database) exposes four tables over the Gold layer: `customers`, `products`, `orders`, `order_items`.

## Projects

| Directory | Description |
|-----------|-------------|
| `CDK/` | AWS CDK infrastructure (C#). Defines all cloud resources. |
| `Lambda/` | ASP.NET Core Lambda — the search API. |
| `Shared Libraries/` | Shared model types (`Customer`, `Product`, `Order`, `OrderItem`). |
| `Data Generation/` | CLI tool to generate synthetic e-commerce data and upload to S3. |

See the README in each project for more detail.

## Local setup

**Prerequisites**

- .NET 10 SDK
- AWS CLI configured (`aws configure`) with credentials that have access to the deployed stack
- Node.js (for CDK CLI): `npm install -g aws-cdk`

**Run the API locally**

```bash
cd Lambda/Pokepad.Lambda

# Set the Anthropic key directly when running in Development mode
# (SSM fetch is skipped when ASPNETCORE_ENVIRONMENT=Development)
export ANTHROPIC_API_KEY=sk-ant-...
export ATHENA_OUTPUT_LOCATION=s3://pokepad-athena-results-<env>-<account>-<region>/results/
export DYNAMODB_TABLE_NAME=pokepad-query-executions

dotnet run
# API listens on https://localhost:5001
```

Browse the OpenAPI spec at `http://localhost:5000/openapi/v1.json` or point Swagger UI at it.

**Generate and upload test data**

```bash
cd "Data Generation/Pokepad.DataGeneration"

# Write Parquet files to ./output/ only
dotnet run

# Write and upload to S3
dotnet run -- --gold-bucket-name pokepad-gold-<env>-<account>-<region>

# Custom counts
dotnet run -- --customers 1000 --products 5000 --orders 10000
```

**Deploy infrastructure**

```bash
cd CDK

# First deploy (bootstrapping required once per account/region)
cdk bootstrap

# Deploy all stacks
cdk deploy --all --context env=dev
```

After deployment, CDK outputs the API endpoint URL and Cognito pool/client IDs.

## Authentication

All endpoints except `GET /v1/health` require a Cognito JWT in the `Authorization: Bearer <token>` header.

The JWT is validated by API Gateway before the request ever reaches Lambda, so the Lambda itself only decodes the token to extract the `sub` claim (user ID) — no re-validation needed.

**Get a token (AWS CLI)**

```bash
aws cognito-idp initiate-auth \
  --auth-flow USER_PASSWORD_AUTH \
  --client-id <UserPoolClientId> \
  --auth-parameters USERNAME=test@pokepad.dev,PASSWORD=<password>
```

The `IdToken` in the response is your Bearer token. It is valid for 1 hour.

**Example request**

```bash
curl -X POST https://<api-id>.execute-api.<region>.amazonaws.com/v1/search \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"question": "Which product categories generated the most revenue last month?"}'
```

## AWS service costs

Costs are per-request at typical low-to-medium volumes. All services have a free tier.

| Service | Cost driver | Estimate |
|---------|-------------|----------|
| **Anthropic (Claude Sonnet 4.6)** | $3/M input tokens, $15/M output tokens. Schema is prompt-cached (90 % cheaper on cache hits). | ~$0.003–$0.008 per search |
| **Athena** | $5 per TB scanned. Capped at 1 GB per query by the workgroup. | < $0.005 per query; often < $0.001 on small datasets |
| **Lambda** | $0.0000166667 per GB-second + $0.20/M requests. 512 MB × avg 5 s = 2.5 GB-s. | ~$0.00004 per invocation |
| **API Gateway (HTTP API)** | $1.00 per million requests | ~$0.000001 per request |
| **Cognito** | Free up to 50,000 MAU | $0 at low volume |
| **DynamoDB** | On-demand. $0.25/M writes, $0.25/M reads | Negligible; < $0.001/month at low volume |
| **S3** | $0.023/GB/month storage + negligible request costs. Athena results auto-expire after 7 days. | Depends on data size |
| **Glue Data Catalog** | First 1 M objects free | $0 |
| **CloudWatch Logs** | $0.50/GB ingested; $0.03/GB stored (30-day retention) | Negligible |
| **SSM Parameter Store** | Standard parameters are free | $0 |

**Typical all-in cost per search: ~$0.004–$0.013**

The largest variable cost is Anthropic — particularly the first call before the schema is cached. Athena cost scales with data volume scanned, but the 1 GB/query cap prevents runaway charges.
