using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.VectorStoreStack.WhenWorkingWithTheVectorStoreConstruct;

public class VectorStoreConstructTestBase
{
    protected VectorStoreConstruct VectorStoreConstruct { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        this.VectorStoreConstruct = new VectorStoreConstruct(stack, "VectorStore");
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }

    protected IDictionary<string, object>? FindSecurityGroupByName(string name)
    {
        return this.Template.FindResources("AWS::EC2::SecurityGroup").Values
            .FirstOrDefault(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props.TryGetValue("GroupName", out var n) && n?.ToString() == name;
            });
    }
}
