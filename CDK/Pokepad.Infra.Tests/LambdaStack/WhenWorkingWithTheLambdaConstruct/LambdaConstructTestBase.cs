using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.LambdaStack.WhenWorkingWithTheLambdaConstruct;

public class LambdaConstructTestBase
{
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        var dataLake = new DataLakeConstruct(stack, "DataLake");
        var glueCatalog = new GlueCatalogConstruct(stack, "GlueCatalog", dataLake.Gold);
        var cognito = new CognitoConstruct(stack, "Cognito");
        var dynamo = new DynamoConstruct(stack, "Dynamo");
        var vectorStore = new VectorStoreConstruct(stack, "VectorStore");
        _ = new LambdaConstruct(stack, "Lambda", dataLake, glueCatalog, cognito, dynamo, vectorStore);
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }

    protected IDictionary<string, object>? FindFunctionByName(string name)
    {
        return this.Template.FindResources("AWS::Lambda::Function").Values
            .FirstOrDefault(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props.TryGetValue("FunctionName", out var n) && n?.ToString() == name;
            });
    }

    protected bool AnyPolicyStatementContainsAction(string action) =>
        this.Template.FindResources("AWS::IAM::Policy").Values.Any(resource =>
        {
            var props = (IDictionary<string, object>)resource["Properties"];
            var doc = (IDictionary<string, object>)props["PolicyDocument"];
            var statements = (IList<object>)doc["Statement"];
            return statements.Cast<IDictionary<string, object>>().Any(stmt =>
            {
                if (!stmt.TryGetValue("Action", out var rawActions)) return false;
                return rawActions is string s ? s == action
                    : rawActions is IList<object> list && list.Any(a => a?.ToString() == action);
            });
        });
}
