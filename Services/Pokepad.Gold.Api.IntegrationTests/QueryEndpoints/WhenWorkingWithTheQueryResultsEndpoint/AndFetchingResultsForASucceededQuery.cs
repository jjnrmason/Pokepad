using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryResultsEndpoint;

public partial class WhenWorkingWithTheQueryResultsEndpoint
{
    public class AndFetchingResultsForASucceededQuery : QueryResultsEndpointTestBase
    {
        private HttpResponseMessage Response { get; set; } = null!;
        private JsonDocument Body { get; set; } = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            var executionId = await this.StartQueryAsync(this.PrimaryUserClient, ValidQuestion);
            var status = await this.WaitForTerminalStatusAsync(this.PrimaryUserClient, executionId);
            if (status != "SUCCEEDED")
            {
                Assert.Fail($"Expected the query to succeed but it finished with status {status}.");
            }

            this.Response = await this.PrimaryUserClient.GetAsync($"v1/query/{executionId}/results");
            this.Body = await ReadJsonAsync(this.Response);
        }

        [OneTimeTearDown]
        public void TearDownScenario()
        {
            this.Body.Dispose();
            this.Response.Dispose();
        }

        [Test]
        public void ThenItReturns200()
        {
            Assert.That(this.Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void ThenTheGeneratedSqlIsReturned()
        {
            Assert.That(this.Body.RootElement.GetProperty("sql").GetString(), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ThenTheColumnsAreReturned()
        {
            Assert.That(this.Body.RootElement.GetProperty("columns").GetArrayLength(), Is.GreaterThan(0));
        }

        [Test]
        public void ThenTheRowsAreReturned()
        {
            Assert.That(this.Body.RootElement.GetProperty("rows").GetArrayLength(), Is.GreaterThan(0));
        }
    }
}
