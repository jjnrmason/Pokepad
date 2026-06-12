using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryStatusEndpoint;

public partial class WhenWorkingWithTheQueryStatusEndpoint
{
    public class AndCheckingTheStatusOfAnotherUsersExecution : QueryStatusEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns403()
        {
            var executionId = await this.StartQueryAsync(this.PrimaryUserClient, ValidQuestion);

            var response = await this.SecondaryUserClient.GetAsync($"v1/query/{executionId}/status");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }
}
