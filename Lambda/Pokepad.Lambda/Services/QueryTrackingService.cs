using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Pokepad.Lambda.Services;

public sealed class QueryTrackingService(IAmazonDynamoDB dynamo, IConfiguration config)
{
    private readonly string _tableName = config["DYNAMODB_TABLE_NAME"]
        ?? throw new InvalidOperationException("DYNAMODB_TABLE_NAME is required");

    public async Task TrackAsync(string executionId, string userId)
    {
        var ttl = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();

        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["executionId"] = new() { S = executionId },
                ["userId"] = new() { S = userId },
                ["ttl"] = new() { N = ttl.ToString() }
            }
        });
    }

    public async Task<string?> GetOwnerAsync(string executionId)
    {
        var response = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["executionId"] = new() { S = executionId }
            }
        });

        if (!response.IsItemSet) return null;
        return response.Item.TryGetValue("userId", out var attr) ? attr.S : null;
    }
}
