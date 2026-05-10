using Amazon.Athena;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Tests.AthenaService.WhenWorkingWithTheAthenaService;

public class AthenaServiceTestBase
{
    protected IAmazonAthena Athena { get; private set; } = null!;
    protected Services.AthenaService AthenaService { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.Athena = Substitute.For<IAmazonAthena>();

        var config = Substitute.For<IConfiguration>();
        config["ATHENA_OUTPUT_LOCATION"].Returns("s3://test-bucket/output/");

        this.AthenaService = new Services.AthenaService(
            this.Athena,
            config,
            Substitute.For<ILogger<Services.AthenaService>>());
    }

    [SetUp]
    public virtual void SetUp()
    {
        this.Athena.ClearReceivedCalls();
    }
}
