using System.Net;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.QueryEndpoints.WhenWorkingWithTheQueryStartEndpoint;

public partial class WhenWorkingWithTheQueryStartEndpoint
{
    public class AndStartingAQueryWithoutAuthentication : QueryStartEndpointTestBase
    {
        [Test]
        public async Task ThenItReturns401()
        {
            var response = await this.AnonymousClient.PostAsync("v1/query/start", JsonBody(new { question = ValidQuestion }));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
