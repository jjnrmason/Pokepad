using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryStatusEndpoint;

public partial class WhenWorkingWithTheQueryStatusEndpoint
{
    public class AndCheckingTheStatusOfAnUnknownExecution : QueryStatusEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns404()
        {
            var unknownExecutionId = Guid.NewGuid().ToString();

            var response = await this.PrimaryUserClient.GetAsync($"v1/query/{unknownExecutionId}/status");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }
}
