using Microsoft.Extensions.Configuration;

namespace Pokepad.Gold.Api.PerformanceTests;

public static class PerformanceTestEnvironment
{
    private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();

    public static string BaseUrl => Require("ApiBaseUrl", "POKEPAD_API_BASE_URL").TrimEnd('/') + "/";

    public static string UserToken => Require("UserToken", "POKEPAD_PRIMARY_USER_TOKEN");

    private static string Require(string configKey, string environmentVariable)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Configuration[configKey];
        }

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException(
                $"Set the '{environmentVariable}' environment variable or '{configKey}' in appsettings.Development.json to run the performance tests against a deployed environment.");
    }
}
