using Microsoft.Extensions.Configuration;

namespace Pokepad.Gold.Api.IntegrationTests;

public static class IntegrationTestEnvironment
{
    private static readonly IConfigurationRoot Configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .Build();

    public static string BaseUrl => Require("ApiBaseUrl", "POKEPAD_API_BASE_URL").TrimEnd('/') + "/";

    public static string PrimaryUserToken => Require("PrimaryUserToken", "POKEPAD_PRIMARY_USER_TOKEN");

    public static string SecondaryUserToken => Require("SecondaryUserToken", "POKEPAD_SECONDARY_USER_TOKEN");

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
                $"Set the '{environmentVariable}' environment variable or '{configKey}' in appsettings.local.json to run the integration tests against a deployed environment.");
    }
}
