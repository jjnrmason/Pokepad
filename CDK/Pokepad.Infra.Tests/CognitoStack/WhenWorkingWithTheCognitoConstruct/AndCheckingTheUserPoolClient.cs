using NUnit.Framework;

namespace Pokepad.Infra.Tests.CognitoStack.WhenWorkingWithTheCognitoConstruct;

public partial class WhenWorkingWithTheCognitoConstruct
{
    public class AndCheckingTheUserPoolClient : CognitoConstructTestBase
    {
        [Test]
        public void ThenItExposesTheUserPoolClientAsAPublicProperty()
        {
            Assert.That(this.CognitoConstruct.UserPoolClient, Is.Not.Null);
        }

        [Test]
        public void ThenItCreatesOneUserPoolClient()
        {
            Assert.That(this.Template.FindResources("AWS::Cognito::UserPoolClient"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheClientNameIsPokepadAppClient()
        {
            var clients = this.Template.FindResources("AWS::Cognito::UserPoolClient");
            var props = (IDictionary<string, object>)clients.Values.Single()["Properties"];
            Assert.That(props["ClientName"]?.ToString(), Is.EqualTo("pokepad-app-client"));
        }

        [Test]
        public void ThenUserPasswordAuthFlowIsEnabled()
        {
            var clients = this.Template.FindResources("AWS::Cognito::UserPoolClient");
            var props = (IDictionary<string, object>)clients.Values.Single()["Properties"];
            var flows = (IList<object>)props["ExplicitAuthFlows"];
            Assert.That(flows.Any(f => f?.ToString()?.Contains("USER_PASSWORD") == true), Is.True);
        }

        [Test]
        public void ThenClientSecretGenerationIsDisabled()
        {
            var clients = this.Template.FindResources("AWS::Cognito::UserPoolClient");
            var props = (IDictionary<string, object>)clients.Values.Single()["Properties"];
            Assert.That(props.ContainsKey("GenerateSecret") && props["GenerateSecret"] is bool b && b, Is.False);
        }
    }
}
