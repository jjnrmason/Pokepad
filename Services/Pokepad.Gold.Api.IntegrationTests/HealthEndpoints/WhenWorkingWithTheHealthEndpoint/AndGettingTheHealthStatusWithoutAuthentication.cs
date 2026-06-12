using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.HealthEndpoints.WhenWorkingWithTheHealthEndpoint;

public partial class WhenWorkingWithTheHealthEndpoint
{
    public class AndGettingTheHealthStatusWithoutAuthentication : HealthEndpointTestBase
    {
        private HttpResponseMessage Response { get; set; } = null!;
        private JsonDocument Body { get; set; } = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.Response = await this.AnonymousClient.GetAsync("v1/health");
            this.Body = await ReadJsonAsync(this.Response);
        }

        [OneTimeTearDown]
        public void TearDownScenario()
        {
            this.Body.Dispose();
            this.Response.Dispose();
        }

        [Test]
        public void ThenItReturns200()
        {
            Assert.That(this.Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void ThenTheStatusIsHealthy()
        {
            Assert.That(this.Body.RootElement.GetProperty("status").GetString(), Is.EqualTo("healthy"));
        }
    }
}
