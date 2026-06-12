using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.SearchEndpoints.WhenWorkingWithTheSearchEndpoint;

public partial class WhenWorkingWithTheSearchEndpoint
{
    public class AndSearchingWithoutAuthentication : SearchEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns401()
        {
            var response = await this.AnonymousClient.PostAsync("v1/search", JsonBody(new { question = ValidQuestion }));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
