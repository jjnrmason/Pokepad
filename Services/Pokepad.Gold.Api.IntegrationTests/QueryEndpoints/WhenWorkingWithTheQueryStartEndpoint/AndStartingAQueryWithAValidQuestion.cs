using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryStartEndpoint;

public partial class WhenWorkingWithTheQueryStartEndpoint
{
    public class AndStartingAQueryWithAValidQuestion : QueryStartEndpointTestBase
    {
        private HttpResponseMessage Response { get; set; } = null!;
        private JsonDocument Body { get; set; } = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.Response = await this.PrimaryUserClient.PostAsync("v1/query/start", JsonBody(new { question = ValidQuestion }));
            this.Body = await ReadJsonAsync(this.Response);
        }

        [OneTimeTearDown]
        public void TearDownScenario()
        {
            this.Body.Dispose();
            this.Response.Dispose();
        }

        [Test]
        public void ThenItReturns202()
        {
            Assert.That(this.Response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        }

        [Test]
        public void ThenAnExecutionIdIsReturned()
        {
            Assert.That(this.Body.RootElement.GetProperty("executionId").GetString(), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ThenTheLocationHeaderPointsToTheStatusEndpoint()
        {
            var executionId = this.Body.RootElement.GetProperty("executionId").GetString();

            Assert.That(this.Response.Headers.Location?.ToString(), Does.EndWith($"/v1/query/{executionId}/status"));
        }
    }
}
