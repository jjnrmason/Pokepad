using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryResultsEndpoint;

public partial class WhenWorkingWithTheQueryResultsEndpoint
{
    public class AndFetchingResultsForAnotherUsersExecution : QueryResultsEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns403()
        {
            var executionId = await this.StartQueryAsync(this.PrimaryUserClient, ValidQuestion);

            var response = await this.SecondaryUserClient.GetAsync($"v1/query/{executionId}/results");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }
}
