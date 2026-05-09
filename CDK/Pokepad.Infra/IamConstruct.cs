using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Pokepad.Infra;

public sealed class IamConstruct : Construct
{
    public IamConstruct(Construct scope, string id, DataLakeConstruct dataLake, GlueCatalogConstruct glueCatalog)
        : base(scope, id)
    {
        DataIngestionRole = CreateDataIngestionRole(dataLake.Bronze);
        DataAnalystRole = CreateDataAnalystRole(dataLake.Gold, dataLake.AthenaResults, glueCatalog.DatabaseName);
        GlueCrawlerRole = CreateGlueCrawlerRole(dataLake.Bronze, dataLake.Silver, dataLake.Gold);
    }

    public Role DataIngestionRole { get; }
    public Role DataAnalystRole { get; }
    public Role GlueCrawlerRole { get; }

    private Role CreateDataIngestionRole(Bucket bronze)
    {
        var role = new Role(this, "data-ingestion-role", new RoleProps
        {
            RoleName = "pokepad-data-ingestion",
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            Description = "Allows ingestion services to write raw data into the Bronze S3 bucket"
        });

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:PutObject", "s3:GetObject"],
            Resources = [bronze.ArnForObjects("*")]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
            Resources = [bronze.BucketArn]
        }));

        return role;
    }

    private Role CreateDataAnalystRole(Bucket gold, Bucket athenaResults, string databaseName)
    {
        var stack = Stack.Of(this);

        var role = new Role(this, "data-analyst-role", new RoleProps
        {
            RoleName = "pokepad-data-analyst",
            AssumedBy = new AccountPrincipal(stack.Account),
            Description = "Allows analysts to query Gold layer data via Athena"
        });

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:GetObject"],
            Resources = [gold.ArnForObjects("*")]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
            Resources = [gold.BucketArn]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:PutObject", "s3:GetObject", "s3:AbortMultipartUpload", "s3:ListMultipartUploadParts"],
            Resources = [athenaResults.ArnForObjects("*")]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:ListBucket", "s3:GetBucketLocation"],
            Resources = [athenaResults.BucketArn]
        }));

        var catalogArn = Arn.Format(new ArnComponents { Service = "glue", Resource = "catalog" }, stack);
        var databaseArn = Arn.Format(new ArnComponents { Service = "glue", Resource = "database", ResourceName = databaseName }, stack);
        var tableArn = Arn.Format(new ArnComponents { Service = "glue", Resource = "table", ResourceName = $"{databaseName}/*" }, stack);

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["glue:GetDatabase", "glue:GetTable", "glue:GetTables", "glue:GetPartition", "glue:GetPartitions"],
            Resources = [catalogArn, databaseArn, tableArn]
        }));

        var workgroupArn = Arn.Format(new ArnComponents { Service = "athena", Resource = "workgroup", ResourceName = "pokepad" }, stack);

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["athena:StartQueryExecution", "athena:GetQueryExecution", "athena:GetQueryResults", "athena:GetWorkGroup", "athena:StopQueryExecution"],
            Resources = [workgroupArn]
        }));

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["athena:ListWorkGroups"],
            Resources = ["*"]
        }));

        return role;
    }

    private Role CreateGlueCrawlerRole(Bucket bronze, Bucket silver, Bucket gold)
    {
        var role = new Role(this, "glue-crawler-role", new RoleProps
        {
            RoleName = "pokepad-glue-crawler",
            AssumedBy = new ServicePrincipal("glue.amazonaws.com"),
            Description = "Allows AWS Glue crawlers to discover schema from the medallion S3 buckets",
            ManagedPolicies = [ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSGlueServiceRole")]
        });

        role.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = ["s3:GetObject", "s3:ListBucket"],
            Resources =
            [
                bronze.BucketArn, bronze.ArnForObjects("*"),
                silver.BucketArn, silver.ArnForObjects("*"),
                gold.BucketArn, gold.ArnForObjects("*")
            ]
        }));

        return role;
    }
}
