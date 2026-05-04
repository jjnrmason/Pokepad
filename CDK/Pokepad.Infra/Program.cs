using Amazon.CDK;
using Pokepad.Infra;

var app = new App();

_ = new DataLakeStack(app, "PokepadDataLake", new StackProps
{
    Tags = new Dictionary<string, string>
    {
        { "project", "pokepad" },
        { "environment", app.Node.TryGetContext("env")?.ToString() ?? "dev" }
    }
});

app.Synth();
