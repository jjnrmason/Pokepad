using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryStatusEndpoint;

public partial class WhenWorkingWithTheQueryStatusEndpoint
{
    public class AndCheckingTheStatusOfAKnownExecution : QueryStatusEndpointTestBase
    {
        private static readonly string[] ValidStatuses = ["QUEUED", "RUNNING", "SUCCEEDED", "FAILED", "CANCELLED"];

        private string ExecutionId { get; set; } = null!;
        private HttpResponseMessage Response { get; set; } = null!;
        private JsonDocument Body { get; set; } = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.ExecutionId = await this.StartQueryAsync(this.PrimaryUserClient, ValidQuestion);
            this.Response = await this.PrimaryUserClient.GetAsync($"v1/query/{this.ExecutionId}/status");
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
        public void ThenTheExecutionIdMatches()
        {
            Assert.That(this.Body.RootElement.GetProperty("executionId").GetString(), Is.EqualTo(this.ExecutionId));
        }

        [Test]
        public void ThenTheStatusIsAValidExecutionState()
        {
            Assert.That(this.Body.RootElement.GetProperty("status").GetString(), Is.AnyOf(ValidStatuses));
        }
    }
}
