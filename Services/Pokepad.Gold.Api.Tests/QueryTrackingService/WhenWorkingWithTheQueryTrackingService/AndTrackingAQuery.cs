using Amazon.DynamoDBv2.Model;
using NSubstitute;
using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.QueryTrackingService.WhenWorkingWithTheQueryTrackingService;

public partial class WhenWorkingWithTheQueryTrackingService
{
    public class AndTrackingAQuery : QueryTrackingServiceTestBase
    {
        [Test]
        public async Task ThenItCallsPutItemOnDynamoDb()
        {
            await this.QueryTrackingService.TrackAsync("exec-123", "user-456");

            await this.DynamoDb.Received(1).PutItemAsync(
                Arg.Is<PutItemRequest>(r => r.TableName == "test-table"),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ThenItStoresTheExecutionId()
        {
            await this.QueryTrackingService.TrackAsync("exec-123", "user-456");

            await this.DynamoDb.Received(1).PutItemAsync(
                Arg.Is<PutItemRequest>(r => r.Item["executionId"].S == "exec-123"),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task ThenItStoresTheUserId()
        {
            await this.QueryTrackingService.TrackAsync("exec-123", "user-456");

            await this.DynamoDb.Received(1).PutItemAsync(
                Arg.Is<PutItemRequest>(r => r.Item["userId"].S == "user-456"),
                Arg.Any<CancellationToken>());
        }
    }
}
