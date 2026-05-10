using Amazon.DynamoDBv2.Model;
using NSubstitute;
using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.QueryTrackingService.WhenWorkingWithTheQueryTrackingService;

public partial class WhenWorkingWithTheQueryTrackingService
{
    public class AndGettingTheOwnerOfAnExistingQuery : QueryTrackingServiceTestBase
    {
        private string? _result;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.DynamoDb.GetItemAsync(
                    Arg.Any<GetItemRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new GetItemResponse
                {
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["userId"] = new AttributeValue { S = "user-456" }
                    }
                });

            _result = await this.QueryTrackingService.GetOwnerAsync("exec-123");
        }

        [Test]
        public void ThenItReturnsTheOwnerUserId()
        {
            Assert.That(_result, Is.EqualTo("user-456"));
        }
    }
}
