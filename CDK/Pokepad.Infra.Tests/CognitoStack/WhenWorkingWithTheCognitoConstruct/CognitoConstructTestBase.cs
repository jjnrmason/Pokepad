using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.CognitoStack.WhenWorkingWithTheCognitoConstruct;

public class CognitoConstructTestBase
{
    protected CognitoConstruct CognitoConstruct { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        this.CognitoConstruct = new CognitoConstruct(stack, "Cognito");
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }

    protected IDictionary<string, object> GetUserPoolProps()
    {
        var pools = this.Template.FindResources("AWS::Cognito::UserPool");
        return (IDictionary<string, object>)pools.Values.Single()["Properties"];
    }
}
