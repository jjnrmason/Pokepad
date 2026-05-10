using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.AthenaStack.WhenWorkingWithTheAthenaConstruct;

public class AthenaConstructTestBase
{
    protected AthenaConstruct AthenaConstruct { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        var dataLake = new DataLakeConstruct(stack, "DataLake");
        this.AthenaConstruct = new AthenaConstruct(stack, "Athena", dataLake.AthenaResults);
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }
}
