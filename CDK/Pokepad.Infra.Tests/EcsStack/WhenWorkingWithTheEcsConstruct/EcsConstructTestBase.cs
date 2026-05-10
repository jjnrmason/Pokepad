using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.EcsStack.WhenWorkingWithTheEcsConstruct;

public class EcsConstructTestBase
{
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        var dataLake = new DataLakeConstruct(stack, "DataLake");
        var vectorStore = new VectorStoreConstruct(stack, "VectorStore");
        _ = new EcsConstruct(stack, "Ecs", dataLake, vectorStore);
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }
}
