using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryResultsEndpoint;

public partial class WhenWorkingWithTheQueryResultsEndpoint
{
    public class AndFetchingResultsBeforeTheQueryHasFinished : QueryResultsEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns409WithTheCurrentStatus()
        {
            var executionId = await this.StartQueryAsync(this.PrimaryUserClient, ValidQuestion);

            var response = await this.PrimaryUserClient.GetAsync($"v1/query/{executionId}/results");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Assert.Inconclusive("The query completed before the results were requested, so the conflict path could not be exercised.");
            }

            using var body = await ReadJsonAsync(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(body.RootElement.GetProperty("status").GetString(), Is.AnyOf("QUEUED", "RUNNING"));
        }
    }
}
