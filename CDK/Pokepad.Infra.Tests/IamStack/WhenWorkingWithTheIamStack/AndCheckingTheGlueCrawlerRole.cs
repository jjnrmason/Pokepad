using NUnit.Framework;

namespace Pokepad.Infra.Tests.IamStack.WhenWorkingWithTheIamStack;

public partial class WhenWorkingWithTheIamStack
{
    public class AndCheckingTheGlueCrawlerRole : IamStackTestBase
    {
        [Test]
        public void ThenItExposesTheGlueCrawlerRoleAsAPublicProperty()
        {
            Assert.That(this.IamStack.GlueCrawlerRole, Is.Not.Null);
        }

        [Test]
        public void ThenTheRoleNameIsPokepadGlueCrawler()
        {
            Assert.That(this.FindRoleByName("pokepad-glue-crawler"), Is.Not.Null);
        }

        [Test]
        public void ThenItTrustsTheGlueService()
        {
            var role = this.FindRoleByName("pokepad-glue-crawler");
            Assert.That(this.GetServicePrincipal(role!), Is.EqualTo("glue.amazonaws.com"));
        }

        [Test]
        public void ThenItHasTheAWSGlueServiceRoleManagedPolicy()
        {
            var role = this.FindRoleByName("pokepad-glue-crawler");
            var props = (IDictionary<string, object>)role!["Properties"];
            var managedPolicies = (IList<object>)props["ManagedPolicyArns"];

            var hasGlueServiceRole = managedPolicies.Any(p =>
            {
                if (p is IDictionary<string, object> dict && dict.TryGetValue("Fn::Join", out var join))
                {
                    var parts = (IList<object>)((IList<object>)join)[1];
                    return parts.Any(part => part?.ToString()?.Contains("AWSGlueServiceRole") == true);
                }
                return p?.ToString()?.Contains("AWSGlueServiceRole") == true;
            });

            Assert.That(hasGlueServiceRole, Is.True);
        }

        [Test]
        public void ThenItAllowsGetObjectOnMedallionBuckets()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("s3:GetObject"), Is.True);
        }
    }
}
