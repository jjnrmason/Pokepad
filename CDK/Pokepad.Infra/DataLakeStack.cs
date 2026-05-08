using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Pokepad.Infra;

public sealed class DataLakeStack : Stack
{
    public DataLakeStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        var env = Node.TryGetContext("env")?.ToString() ?? "dev";

        Bronze = CreateMedallionBucket("bronze", env);
        Silver = CreateMedallionBucket("silver", env);
        Gold = CreateMedallionBucket("gold", env);
        AthenaResults = CreateMedallionBucket("athena-results", env, queryResultRetentionDays: 7);
    }

    public Bucket Bronze { get; }
    public Bucket Silver { get; }
    public Bucket Gold { get; }
    public Bucket AthenaResults { get; }

    private Bucket CreateMedallionBucket(string tier, string env, int? queryResultRetentionDays = null)
    {
        var lifecycleRules = queryResultRetentionDays is { } days
            ? new LifecycleRule[]
            {
                new()
                {
                    Id = "expire-query-results",
                    Enabled = true,
                    Expiration = Duration.Days(days)
                }
            }
            : null;

        return new Bucket(this, $"{tier}-bucket", new BucketProps
        {
            BucketName = $"pokepad-{tier}-{env}-{Account}-{Region}",
            Versioned = true,
            Encryption = BucketEncryption.S3_MANAGED,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.RETAIN,
            AutoDeleteObjects = false,
            LifecycleRules = lifecycleRules
        });
    }
}
