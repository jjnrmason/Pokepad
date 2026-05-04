using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;

namespace Pokepad.Infra.Tests.DataLakeStack.WhenWorkingWithTheDataLakeStack;

public class DataLakeStackTestBase
{
    protected Pokepad.Infra.DataLakeStack DataLakeStack { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        this.DataLakeStack = new Pokepad.Infra.DataLakeStack(app, "TestStack");
        this.Template = Template.FromStack(this.DataLakeStack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }
}
