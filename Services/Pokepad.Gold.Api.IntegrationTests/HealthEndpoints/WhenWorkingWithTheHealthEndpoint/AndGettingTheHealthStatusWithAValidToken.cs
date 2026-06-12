using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.HealthEndpoints.WhenWorkingWithTheHealthEndpoint;

public partial class WhenWorkingWithTheHealthEndpoint
{
    public class AndGettingTheHealthStatusWithAValidToken : HealthEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns200()
        {
            var response = await this.PrimaryUserClient.GetAsync("v1/health");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
