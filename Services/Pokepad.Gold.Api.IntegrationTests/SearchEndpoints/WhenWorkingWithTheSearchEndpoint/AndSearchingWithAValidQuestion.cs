using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.Gold.Api.IntegrationTests.SearchEndpoints.WhenWorkingWithTheSearchEndpoint;

public partial class WhenWorkingWithTheSearchEndpoint
{
    public class AndSearchingWithAValidQuestion : SearchEndpointTestBase
    {
        private HttpResponseMessage Response { get; set; } = null!;
        private JsonDocument Body { get; set; } = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.Response = await this.PrimaryUserClient.PostAsync("v1/search", JsonBody(new { question = ValidQuestion }));
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
        public void ThenTheGeneratedSqlIsReturned()
        {
            Assert.That(this.Body.RootElement.GetProperty("sql").GetString(), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ThenTheColumnsAreReturned()
        {
            Assert.That(this.Body.RootElement.GetProperty("columns").GetArrayLength(), Is.GreaterThan(0));
        }

        [Test]
        public void ThenTheRowsAreReturned()
        {
            Assert.That(this.Body.RootElement.GetProperty("rows").GetArrayLength(), Is.GreaterThan(0));
        }
    }
}
