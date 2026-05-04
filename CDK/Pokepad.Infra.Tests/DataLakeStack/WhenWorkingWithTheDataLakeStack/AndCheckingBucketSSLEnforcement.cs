using Amazon.CDK.Assertions;
using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public partial class WhenWorkingWithTheDataLakeStack
{
    public class AndCheckingBucketSSLEnforcement : DataLakeStackTestBase
    {
        [Test]
        public void ThenEachBucketHasADedicatedBucketPolicy()
        {
            Assert.That(this.Template.FindResources("AWS::S3::BucketPolicy"), Has.Count.EqualTo(4));
        }

        [Test]
        public void ThenAllBucketPoliciesDenyNonSSLTraffic()
        {
            this.Template.HasResourceProperties("AWS::S3::BucketPolicy", new Dictionary<string, object>
            {
                ["PolicyDocument"] = new Dictionary<string, object>
                {
                    ["Statement"] = Match.ArrayWith(new object[]
                    {
                        Match.ObjectLike(new Dictionary<string, object>
                        {
                            ["Action"] = "s3:*",
                            ["Effect"] = "Deny",
                            ["Condition"] = new Dictionary<string, object>
                            {
                                ["Bool"] = new Dictionary<string, object>
                                {
                                    ["aws:SecureTransport"] = "false"
                                }
                            }
                        })
                    })
                }
            });
        }
    }
}
