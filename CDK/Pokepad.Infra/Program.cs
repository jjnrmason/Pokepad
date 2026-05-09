using Amazon.CDK;
using Pokepad.Infra;

var app = new App();
var env = app.Node.TryGetContext("env")?.ToString() ?? "dev";

var tags = new Dictionary<string, string>
{
    { "project", "pokepad" },
    { "environment", env }
};

_ = new BuilderStack(app, "Pokepad", new StackProps { Tags = tags });

app.Synth();
