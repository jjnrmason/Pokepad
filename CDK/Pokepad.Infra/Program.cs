using Amazon.CDK;
using Pokepad.Infra;

var app = new App();
var env = app.Node.TryGetContext("env")?.ToString() ?? "dev";

var tags = new Dictionary<string, string>
{
    { "project", "pokepad" },
    { "environment", env }
};

var dataLakeStack = new DataLakeStack(app, "PokepadDataLake", new StackProps { Tags = tags });

_ = new GlueCatalogStack(app, "PokepadGlueCatalog", dataLakeStack.Gold, new StackProps { Tags = tags });

_ = new AthenaStack(app, "PokepadAthena", dataLakeStack.AthenaResults, new StackProps { Tags = tags });

app.Synth();
