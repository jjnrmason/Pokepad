using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryResultsEndpoint;

public partial class WhenWorkingWithTheQueryResultsEndpoint
{
    public class AndFetchingResultsForAnUnknownExecution : QueryResultsEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns404()
        {
            var unknownExecutionId = Guid.NewGuid().ToString();

            var response = await this.PrimaryUserClient.GetAsync($"v1/query/{unknownExecutionId}/results");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}
