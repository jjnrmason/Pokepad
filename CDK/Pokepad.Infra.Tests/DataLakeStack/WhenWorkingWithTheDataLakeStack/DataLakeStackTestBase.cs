using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public class DataLakeStackTestBase
{
    protected DataLakeConstruct DataLakeConstruct { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        this.DataLakeConstruct = new DataLakeConstruct(stack, "DataLake");
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }
}
