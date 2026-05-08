using NUnit.Framework;

namespace Pokepad.Infra.Tests.IamStack.WhenWorkingWithTheIamStack;

public partial class WhenWorkingWithTheIamStack
{
    public class AndCheckingTheRoleCount : IamStackTestBase
    {
        [Test]
        public void ThenItCreatesThreeIamRoles()
        {
            Assert.That(this.Template.FindResources("AWS::IAM::Role"), Has.Count.EqualTo(3));
        }

        [Test]
        public void ThenItCreatesThreeIamPolicies()
        {
            Assert.That(this.Template.FindResources("AWS::IAM::Policy"), Has.Count.EqualTo(3));
        }
    }
}
