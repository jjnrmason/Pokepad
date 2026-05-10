using Amazon.Athena.Model;
using NSubstitute;
using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.AthenaService.WhenWorkingWithTheAthenaService;

public partial class WhenWorkingWithTheAthenaService
{
    public class AndStartingAQuery : AthenaServiceTestBase
    {
        private string _result = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.Athena.StartQueryExecutionAsync(
                    Arg.Any<StartQueryExecutionRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new StartQueryExecutionResponse { QueryExecutionId = "exec-abc-123" });

            _result = await this.AthenaService.StartAsync("SELECT product_id FROM products LIMIT 10");
        }

        [Test]
        public void ThenItReturnsTheExecutionId()
        {
            Assert.That(_result, Is.EqualTo("exec-abc-123"));
        }
    }
}
