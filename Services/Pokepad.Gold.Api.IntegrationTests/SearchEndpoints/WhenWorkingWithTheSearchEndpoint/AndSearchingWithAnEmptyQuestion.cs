using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.SearchEndpoints.WhenWorkingWithTheSearchEndpoint;

public partial class WhenWorkingWithTheSearchEndpoint
{
    public class AndSearchingWithAnEmptyQuestion : SearchEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns400()
        {
            var response = await this.PrimaryUserClient.PostAsync("v1/search", JsonBody(new { question = "" }));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
    }
}
