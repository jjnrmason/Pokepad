using BenchmarkDotNet.Running;
using Pokepad.Gold.Api.PerformanceTests.Scenarios;

if (args.Length > 0 && args[0].Equals("rate-limit", StringComparison.OrdinalIgnoreCase))
{
    await RateLimitProbe.RunAsync();
    return;
}

if (args.Length > 0 && args[0].Equals("cold-start", StringComparison.OrdinalIgnoreCase))
{
    await ColdStartProbe.RunAsync();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
