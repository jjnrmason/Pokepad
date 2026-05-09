# Pokepad Infrastructure (CDK)

All AWS resources are defined here as a single CDK app in C#. The app deploys one stack (`PokepadBuilderStack`) composed of several constructs.

## Architecture

```
PokepadBuilderStack
├── DataLakeConstruct      — four S3 buckets (medallion layers)
├── GlueCatalogConstruct   — Glue database + four external tables over Gold
├── AthenaConstruct        — Athena workgroup "pokepad"
├── CognitoConstruct       — User Pool + app client for API auth
├── DynamoConstruct        — DynamoDB table for async query ownership
├── IamConstruct           — scoped IAM roles (ingestion, analyst, crawler)
└── LambdaConstruct        — Lambda function + HTTP API + CloudWatch alarm
```

## Constructs

### DataLakeConstruct

Four S3 buckets following the **medallion architecture**:

| Bucket | Purpose |
|--------|---------|
| Bronze | Raw data as received — immutable, never transformed in-place |
| Silver | Cleaned and deduplicated |
| Gold   | Analytics-ready Parquet; the source of truth for all queries |
| Athena Results | Athena writes query output here; objects expire after 7 days |

All buckets are encrypted (S3-managed), versioned, block all public access, and enforce SSL. Bucket names follow the pattern `pokepad-<tier>-<env>-<account>-<region>` to guarantee global uniqueness.

**Why medallion?** It keeps raw data untouched (Bronze) so ingestion bugs can be replayed, isolates transformation failures (Silver), and gives the query layer (Gold) a stable, clean surface to sit on. Athena only ever reads Gold.

### GlueCatalogConstruct

Creates the `ecommerce_gold` Glue database with four external tables that point at Parquet files in the Gold bucket:

| Table | Key columns |
|-------|-------------|
| `customers` | `CustomerId`, `Email`, `City`, `Country`, `CreatedAt` |
| `products` | `ProductId`, `Name`, `Category`, `Price`, `StockQuantity` |
| `orders` | `OrderId`, `CustomerId`, `OrderDate`, `Status`, `TotalAmount` |
| `order_items` | `OrderItemId`, `OrderId`, `ProductId`, `Quantity`, `UnitPrice`, `Subtotal` |

The Lambda reads this schema at query time and injects it into the Claude prompt so the model knows the exact column names and types.

### AthenaConstruct

Configures the `pokepad` Athena workgroup with:

- A 1 GB per-query data scan cap (`BytesScannedCutoffPerQuery`) — this is the primary cost guard. A query that would scan more than 1 GB is cancelled automatically.
- CloudWatch metrics publishing enabled.
- Result output directed at the Athena Results bucket.

**Why a dedicated workgroup?** It isolates cost and config from the default workgroup, allows per-workgroup IAM policies, and lets the Lambda role be scoped to only this workgroup's ARN.

### CognitoConstruct

Creates a Cognito User Pool (`pokepad-users`) and an app client (`pokepad-app-client`):

- Self-sign-up is disabled — users must be provisioned by an admin.
- Sign-in by email.
- Password policy: min 8 chars, upper + lower + digit required.
- A test user (`test@pokepad.dev`) is provisioned on stack creation with email verification suppressed.
- The app client uses the `USER_PASSWORD_AUTH` flow (no client secret, suitable for CLI/server-side use).

The API Gateway JWT authorizer is pointed at this User Pool's issuer URL and validates every authenticated request before it reaches Lambda.

### DynamoConstruct

A single DynamoDB table (`pokepad-query-executions`) that maps Athena `executionId → userId`. Used by the async query endpoints to enforce that only the user who started a query can poll its status or fetch its results.

- Partition key: `executionId` (string)
- TTL attribute: `ttl` — items automatically expire 24 hours after creation
- Billing: on-demand (PAY_PER_REQUEST)

### IamConstruct

Three scoped IAM roles for use outside the Lambda:

| Role | Principal | Access |
|------|-----------|--------|
| `pokepad-data-ingestion` | `lambda.amazonaws.com` | PutObject/GetObject on Bronze |
| `pokepad-data-analyst` | Account root | Read Gold + Athena + Glue (for human analysts) |
| `pokepad-glue-crawler` | `glue.amazonaws.com` | GetObject on Bronze/Silver/Gold (for Glue crawlers) |

The Lambda's own execution role is created inside `LambdaConstruct` and is scoped more tightly (no analyst-level access; only the specific workgroup and tables it needs).

### LambdaConstruct

Wires everything together:

- **Lambda function** (`pokepad-search`): .NET 10 self-contained binary compiled in Docker during `cdk deploy` using the `mcr.microsoft.com/dotnet/sdk:10.0` image. Renamed to `bootstrap` for the `provided.al2023` runtime.
- **HTTP API** (`pokepad-search-api`): API Gateway HTTP API with CORS enabled (all origins). Routes are defined per-endpoint; all except `/v1/health` use the Cognito JWT authorizer.
- **Rate limiting**: burst 10, rate 10 req/s at the default route level.
- **Access logs**: structured JSON logged to CloudWatch (1-month retention).
- **CloudWatch alarm** (`pokepad-lambda-error-rate`): alerts when error rate ≥ 5 % over a 5-minute window. Treats missing data as non-breaching (e.g. zero traffic periods).
- **X-Ray tracing**: active tracing enabled on the function.

## CDK commands

```bash
cd CDK

# Synthesise CloudFormation templates (no deployment)
cdk synth

# Show diff against deployed stack
cdk diff --context env=dev

# Deploy everything
cdk deploy --all --context env=dev

# Deploy a single stack
cdk deploy PokepadBuilderStack --context env=dev

# Destroy (note: S3 objects must be emptied manually if AutoDeleteObjects is false)
cdk destroy --all
```

The `env` context key is used in bucket names to avoid collisions between environments (e.g. `dev`, `staging`, `prod`).
