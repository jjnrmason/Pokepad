using NUnit.Framework;

namespace Pokepad.Infra.Tests.CognitoStack.WhenWorkingWithTheCognitoConstruct;

public partial class WhenWorkingWithTheCognitoConstruct
{
    public class AndCheckingTheUserPool : CognitoConstructTestBase
    {
        [Test]
        public void ThenItExposesTheUserPoolAsAPublicProperty()
        {
            Assert.That(this.CognitoConstruct.UserPool, Is.Not.Null);
        }

        [Test]
        public void ThenItCreatesOneUserPool()
        {
            Assert.That(this.Template.FindResources("AWS::Cognito::UserPool"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenSelfSignUpIsDisabled()
        {
            var props = this.GetUserPoolProps();
            Assert.That(props["AdminCreateUserConfig"], Is.Not.Null);
            var adminConfig = (IDictionary<string, object>)props["AdminCreateUserConfig"];
            Assert.That(adminConfig["AllowAdminCreateUserOnly"], Is.True);
        }

        [Test]
        public void ThenEmailIsTheSignInAlias()
        {
            var props = this.GetUserPoolProps();
            Assert.That(props.ContainsKey("UsernameAttributes"), Is.True);
            var aliases = (IList<object>)props["UsernameAttributes"];
            Assert.That(aliases.Any(a => a?.ToString() == "email"), Is.True);
        }

        [Test]
        public void ThenTheTestUserIsCreated()
        {
            var users = this.Template.FindResources("AWS::Cognito::UserPoolUser");
            Assert.That(users, Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheTestUserEmailIsSet()
        {
            var users = this.Template.FindResources("AWS::Cognito::UserPoolUser");
            var props = (IDictionary<string, object>)users.Values.Single()["Properties"];
            var username = props["Username"]?.ToString();
            Assert.That(username, Is.EqualTo("test@pokepad.dev"));
        }

        [Test]
        public void ThenTheCfnOutputsAreCreated()
        {
            var outputs = this.Template.FindOutputs("*");
            var hasUserPoolId = outputs.Keys.Any(k => k.Contains("UserPoolId"));
            var hasClientId = outputs.Keys.Any(k => k.Contains("UserPoolClientId"));
            Assert.That(hasUserPoolId, Is.True);
            Assert.That(hasClientId, Is.True);
        }
    }
}
