using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.DynamoStack.WhenWorkingWithTheDynamoConstruct;

public class DynamoConstructTestBase
{
    protected DynamoConstruct DynamoConstruct { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        this.DynamoConstruct = new DynamoConstruct(stack, "Dynamo");
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }

    protected IDictionary<string, object> GetTableProps()
    {
        var tables = this.Template.FindResources("AWS::DynamoDB::Table");
        return (IDictionary<string, object>)tables.Values.Single()["Properties"];
    }
}
