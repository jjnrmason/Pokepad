using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Pokepad.Infra.Constructs;

public sealed class DataLakeConstruct : Construct
{
    public DataLakeConstruct(Construct scope, string id) : base(scope, id)
    {
        var env = Node.TryGetContext("env")?.ToString() ?? "dev";
        var stack = Stack.Of(this);

        Bronze = CreateMedallionBucket("bronze", env, stack);
        Silver = CreateMedallionBucket("silver", env, stack);
        Gold = CreateMedallionBucket("gold", env, stack);
        AthenaResults = CreateMedallionBucket("athena-results", env, stack, queryResultRetentionDays: 7);
        
        _ = new CfnOutput(this, "GoldBucketName", new CfnOutputProps
        {
            Value = Gold.BucketName,
            Description = "Bucket name of the gold bucket"
        });
        _ = new CfnOutput(this, "AthenaBucketResultsName", new CfnOutputProps
        {
            Value = AthenaResults.BucketName,
            Description = "Bucket name of the athena results bucket"
        });
    }

    public Bucket Bronze { get; }
    public Bucket Silver { get; }
    public Bucket Gold { get; }
    public Bucket AthenaResults { get; }

    private Bucket CreateMedallionBucket(string tier, string env, Stack stack, int? queryResultRetentionDays = null)
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
            BucketName = $"pokepad-{tier}-{env}-{stack.Account}-{stack.Region}",
            Versioned = true,
            Encryption = BucketEncryption.S3_MANAGED,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            EnforceSSL = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = false,
            LifecycleRules = lifecycleRules
        });
    }
}
