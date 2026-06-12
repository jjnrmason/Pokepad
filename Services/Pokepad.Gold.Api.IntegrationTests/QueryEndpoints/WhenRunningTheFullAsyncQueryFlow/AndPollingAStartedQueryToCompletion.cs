using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenRunningTheFullAsyncQueryFlow;

public partial class WhenRunningTheFullAsyncQueryFlow
{
    public class AndPollingAStartedQueryToCompletion : FullAsyncQueryFlowTestBase
    {
        private string TerminalStatus { get; set; } = null!;
        private HttpResponseMessage ResultsResponse { get; set; } = null!;
        private JsonDocument ResultsBody { get; set; } = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            var executionId = await this.StartQueryAsync(this.PrimaryUserClient, ValidQuestion);
            this.TerminalStatus = await this.WaitForTerminalStatusAsync(this.PrimaryUserClient, executionId);
            this.ResultsResponse = await this.PrimaryUserClient.GetAsync($"v1/query/{executionId}/results");
            this.ResultsBody = await ReadJsonAsync(this.ResultsResponse);
        }

        [OneTimeTearDown]
        public void TearDownScenario()
        {
            this.ResultsBody.Dispose();
            this.ResultsResponse.Dispose();
        }

        [Test]
        public void ThenTheQuerySucceeds()
        {
            Assert.That(this.TerminalStatus, Is.EqualTo("SUCCEEDED"));
        }

        [Test]
        public void ThenTheResultsAreReturned()
        {
            Assert.That(this.ResultsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void ThenTheSqlMatchesTheOriginalQuestion()
        {
            Assert.That(this.ResultsBody.RootElement.GetProperty("sql").GetString(), Does.Contain("customer").IgnoreCase);
        }

        [Test]
        public void ThenTheResultsContainData()
        {
            Assert.That(this.ResultsBody.RootElement.GetProperty("rows").GetArrayLength(), Is.GreaterThan(0));
        }
    }
}
