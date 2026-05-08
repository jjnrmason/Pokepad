using Amazon.CDK;
using Amazon.CDK.AWS.Athena;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Pokepad.Infra;

public sealed class AthenaStack : Stack
{
    public AthenaStack(Construct scope, string id, Bucket athenaResultsBucket, IStackProps? props = null)
        : base(scope, id, props)
    {
        new CfnWorkGroup(this, "pokepad-workgroup", new CfnWorkGroupProps
        {
            Name = "pokepad",
            Description = "Pokepad analytics workgroup",
            State = "ENABLED",
            WorkGroupConfiguration = new CfnWorkGroup.WorkGroupConfigurationProperty
            {
                ResultConfiguration = new CfnWorkGroup.ResultConfigurationProperty
                {
                    OutputLocation = $"s3://{athenaResultsBucket.BucketName}/results/"
                },
                EnforceWorkGroupConfiguration = true,
                PublishCloudWatchMetricsEnabled = true,
                BytesScannedCutoffPerQuery = 1_073_741_824 // 1 GB cost guard
            }
        });
    }
}
