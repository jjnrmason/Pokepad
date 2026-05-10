using Amazon.DynamoDBv2.Model;
using NSubstitute;
using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.QueryTrackingService.WhenWorkingWithTheQueryTrackingService;

public partial class WhenWorkingWithTheQueryTrackingService
{
    public class AndGettingTheOwnerOfAnUnknownQuery : QueryTrackingServiceTestBase
    {
        private string? _result;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.DynamoDb.GetItemAsync(
                    Arg.Any<GetItemRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new GetItemResponse());

            _result = await this.QueryTrackingService.GetOwnerAsync("exec-unknown");
        }

        [Test]
        public void ThenItReturnsNull()
        {
            Assert.That(_result, Is.Null);
        }
    }
}
