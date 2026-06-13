using Microsoft.Extensions.DependencyInjection;
using Pacer.Hosting;
using Pokepad.Gold.Api.PerformanceTests;
using Pokepad.Gold.Api.PerformanceTests.Scenarios;

// A Pacer console app: register a shared API client plus the scenarios, then hand the command-line
// arguments to Pacer. Try:
//   dotnet run -- list
//   dotnet run -- run --scenario search --out ./reports
//   dotnet run -- run --group storefront --profile load --users 5 --duration 1
//   dotnet run -- run --scenario rate-limit
//   dotnet run -- run --scenario cold-start
return await PacerApplication.RunAsync(args, builder =>
{
    builder.Services.AddSingleton<ApiClient>();

    builder.Services.AddPacer()
        .AddScenario(PokepadScenarios.Health())
        .AddScenario(PokepadScenarios.Search())
        .AddScenario(PokepadScenarios.SemanticSearch())
        .AddScenario(PokepadScenarios.AsyncQueryFlow())
        .AddScenario(PokepadScenarios.RateLimit())
        .AddScenario(PokepadScenarios.ColdStart());
});
