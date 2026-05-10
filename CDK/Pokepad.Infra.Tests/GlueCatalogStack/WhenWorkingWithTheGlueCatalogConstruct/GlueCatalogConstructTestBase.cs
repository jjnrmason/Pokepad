using Amazon.CDK;
using Amazon.CDK.Assertions;
using NUnit.Framework;
using Pokepad.Infra.Constructs;

namespace Pokepad.Infra.Tests.GlueCatalogStack.WhenWorkingWithTheGlueCatalogConstruct;

public class GlueCatalogConstructTestBase
{
    protected GlueCatalogConstruct GlueCatalogConstruct { get; private set; } = null!;
    protected Template Template { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        var app = new App();
        var stack = new Stack(app, "TestStack");
        var dataLake = new DataLakeConstruct(stack, "DataLake");
        this.GlueCatalogConstruct = new GlueCatalogConstruct(stack, "GlueCatalog", dataLake.Gold);
        this.Template = Template.FromStack(stack);
    }

    [SetUp]
    public virtual void SetUp()
    {
    }

    protected IDictionary<string, object> GetTableByName(string tableName)
    {
        var tables = this.Template.FindResources("AWS::Glue::Table");
        return tables.Values
            .Select(r => (IDictionary<string, object>)r["Properties"])
            .First(props =>
            {
                var input = (IDictionary<string, object>)props["TableInput"];
                return input["Name"]?.ToString() == tableName;
            });
    }
}
