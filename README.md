# Pokepad

Pokepad is a natural language search API over e-commerce data with two complementary search paths:

- **SQL search** — You ask a question in plain English; GPT-4o translates it to SQL, Athena runs it against the data lake, and the results come back as structured JSON.
- **Semantic search** — Product names, descriptions, and categories are embedded with `text-embedding-3-small` and stored in PostgreSQL with pgvector. Queries are embedded at runtime and matched by cosine similarity.

**Data lake — medallion layers**

| Layer  | S3 bucket prefix | Contents |
|--------|-----------------|----------|
| Bronze | `pokepad-bronze-*` | Raw ingestion (unmodified source data) |
| Silver | `pokepad-silver-*` | Cleaned, deduplicated |
| Gold   | `pokepad-gold-*`   | Analytics-ready Parquet; what the API queries |

The Glue catalog (`ecommerce_gold` database) exposes four tables over the Gold layer: `customers`, `products`, `orders`, `order_items`.

## Local setup

**Prerequisites**

- .NET 10 SDK
- Docker (required for CDK asset bundling — Lambda is compiled inside a container)
- AWS CLI configured (`aws configure`) with credentials that have access to the deployed stack
- Node.js (for CDK CLI): `npm install -g aws-cdk`

**Deploy infrastructure**

```bash
cd CDK

# First deploy — bootstraps CDK assets bucket once per account/region
cdk bootstrap

# Deploy everything
cdk deploy --all
```

After deployment, CDK prints the API Gateway endpoint URL and Cognito User Pool / Client IDs.

**Store the OpenAI API key**

The stack reads the key from SSM at `/pokepad/ai-api-key`. Create it once after the first deploy:

```bash
aws ssm put-parameter \
  --name "/pokepad/ai-api-key" \
  --value "sk-..." \
  --type SecureString
```

Both the search Lambda and the embedding indexer ECS task read this parameter at startup.

**Generate and upload test data**

```bash
cd "Data Generation/Pokepad.DataGeneration"

# Write Parquet files to ./output/ only (no AWS credentials needed)
dotnet run

# Write and upload to S3 (triggers embedding indexer automatically)
dotnet run -- --gold-bucket-name pokepad-gold-<account>-<region>

# Custom record counts
dotnet run -- --customers 1000 --products 5000 --orders 10000
```

Uploading Parquet to `gold/products/` fires an EventBridge rule that enqueues an SQS message. The ECS embedding indexer scales up from zero to process it.

**Run the API locally**

The Lambda runs inside the VPC and connects to RDS directly. Running locally, semantic search is unavailable — SQL search still works against the live Athena/Glue stack.

```bash
cd Services/Pokepad.Gold.Api

# SSM fetch is skipped in Development mode; set the key directly
export API_KEY=sk-...
export ATHENA_OUTPUT_LOCATION=s3://pokepad-athena-results-<account>-<region>/results/
export DYNAMODB_TABLE_NAME=pokepad-query-executions

dotnet run
# API listens on http://localhost:5000
```

Browse the OpenAPI spec at `http://localhost:5000/openapi/v1.json` or Scalar UI at `http://localhost:5000/scalar/v1`.

## Embedding indexer

The `Pokepad.EmbeddingIndexer` ECS Fargate service keeps the vector store in sync with the Gold layer automatically:

1. A Parquet file lands in `s3://pokepad-gold-*/gold/products/`
2. EventBridge fires an `Object Created` event and routes the S3 detail (bucket + key) to an SQS queue
3. Application Auto Scaling detects messages in the queue and scales the ECS service from 0 to 1–5 workers
4. Each worker long-polls the queue, downloads the Parquet file, embeds all products in batches of 25, and upserts into `products_embeddings` in RDS
5. On success the message is deleted; on failure it is left for SQS to re-deliver (up to 3 attempts, then dead-letter queue)
6. Workers receive SIGTERM on scale-in and finish their current message before exiting

**To trigger a full re-index**, re-upload the products Parquet files to S3. The data generation project does this automatically when given `--gold-bucket-name`.

## Connecting to the database locally

The stack provisions a `t4g.nano` bastion EC2 in the VPC accessible via SSM Session Manager — no inbound ports, no SSH keys, no key pair.

**One-time: install the Session Manager plugin**

```bash
brew install --cask session-manager-plugin
```

**Open a tunnel**

```bash
# 1. Get the bastion instance ID and RDS hostname from CDK outputs
aws cloudformation describe-stacks \
  --stack-name BuilderStack \
  --query 'Stacks[0].Outputs'

# 2. Open the tunnel (keep this terminal running)
aws ssm start-session \
  --target <BastionInstanceId> \
  --document-name AWS-StartPortForwardingSessionToRemoteHost \
  --parameters '{"host":["<RdsEndpoint>"],"portNumber":["5432"],"localPortNumber":["5433"]}'
```

**Connect with DataGrip / psql**

Point your client at `localhost:5433`. Fetch credentials from Secrets Manager:

```bash
aws secretsmanager get-secret-value \
  --secret-id pokepad/vector-db-credentials \
  --query SecretString --output text | jq '{user:.username, pass:.password}'
```

| Field    | Value                      |
|----------|----------------------------|
| Host     | `localhost`                |
| Port     | `5433`                     |
| Database | `pokepad`                  |
| User     | `pokepad`                  |
| Password | from Secrets Manager above |

## Authentication

All endpoints except `GET /v1/health` require a Cognito JWT in the `Authorization: Bearer <token>` header.

The JWT is validated by API Gateway before the request ever reaches Lambda, so the Lambda itself only decodes the token to extract the `sub` claim (user ID) — no re-validation needed.

**Get a token (AWS CLI)**

```bash
aws cognito-idp admin-set-user-password \
    --user-pool-id $PoolId \
    --username test@pokepad.dev \
    --password Pokepad1! \
    --permanent
    
aws cognito-idp initiate-auth \
  --auth-flow USER_PASSWORD_AUTH \
  --client-id $UserPoolClientId \
  --auth-parameters USERNAME=test@pokepad.dev,PASSWORD=Pokepad1!
```

The `IdToken` in the response is your Bearer token. It is valid for 1 hour.

**Example request**

```bash
curl -X POST https://<api-id>.execute-api.<region>.amazonaws.com/v1/search \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"question": "Which product categories generated the most revenue last month?"}'
```

**Semantic search request**

```bash
curl -X POST https://<api-id>.execute-api.<region>.amazonaws.com/v1/semantic-search \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"question": "Show me waterproof hiking boots under £100", "topK": 10, "synthesise": false}'
```

The response returns the nearest product matches from pgvector:

```json
{
  "results": [
    {
      "productId": "product-123",
      "metadata": "{\"Name\":\"Trail Boot\",\"Category\":\"Footwear\",\"Price\":89.99}",
      "similarityScore": 0.92
    }
  ],
  "answer": null
}
```

Set `"synthesise": true` to include a concise plain-English answer generated from the top-K matches.

## Performance tests

k6 performance tests live in `Services/Pokepad.Gold.Api.PerformanceTests`.

```bash
cd Services/Pokepad.Gold.Api.PerformanceTests

BASE_URL=https://<api-id>.execute-api.<region>.amazonaws.com \
TOKEN=<id-token> \
./run-perf-tests.sh
```

The scripts cover baseline latency, the async query flow under load, API Gateway rate-limit probing, and a manual cold-start measurement.

## Service costs to run

**Per-request costs** (SQL search, typical low-to-medium volume):

| Service | Cost driver | Estimate |
|---------|-------------|----------|
| **OpenAI (GPT-4o)** | $2.50/M input tokens, $10/M output tokens | ~$0.003–$0.007 per search |
| **Athena** | $5/TB scanned; 1 GB/query cap enforced by workgroup | < $0.005 per query; often < $0.001 on small datasets |
| **Lambda** | $0.0000166667/GB-second + $0.20/M requests. 512 MB × avg 5 s = 2.5 GB-s | ~$0.00004 per invocation |
| **API Gateway (HTTP API)** | $1.00/M requests | ~$0.000001 per request |
| **Cognito** | Free up to 50,000 MAU | $0 at low volume |
| **DynamoDB** | On-demand. $0.25/M writes, $0.25/M reads | Negligible; < $0.001/month at low volume |
| **S3** | $0.023/GB/month storage. Athena results auto-expire after 7 days | Depends on data size |
| **Glue Data Catalog** | First 1 M objects free | $0 |
| **CloudWatch Logs** | $0.50/GB ingested; $0.03/GB stored (30-day retention) | Negligible |
| **SSM Parameter Store** | Standard parameters free | $0 |

**Typical all-in cost per search: ~$0.004–$0.013**

**Fixed monthly costs** (always-on infrastructure):

| Service | Cost driver | Estimate |
|---------|-------------|----------|
| **RDS PostgreSQL** (db.t3.micro) | $0.017/hour × 730 hours | ~$12/month |
| **NAT instance** (t4g.nano) | $0.0042/hour × 730 hours — replaces NAT Gateway | ~$3/month |
| **Bastion** (t4g.nano) | $0.0042/hour × 730 hours — SSM access to RDS | ~$3/month |
| **ECS Fargate** | Only billed when embedding tasks run. 0.25 vCPU + 0.5 GB | ~$0.012/hour per worker; scale-to-zero when queue is empty |
| **SQS** | First 1 M requests/month free | $0 at low volume |

**Total fixed: ~$18/month** — down from ~$87/month with a NAT Gateway and interface VPC endpoints.

If you only need SQL search and want to cut costs further, the vector store infrastructure (RDS, NAT instance, bastion) can be removed from the stack entirely.
