using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Tests.QueryTrackingService.WhenWorkingWithTheQueryTrackingService;

public class QueryTrackingServiceTestBase
{
    protected IAmazonDynamoDB DynamoDb { get; private set; } = null!;
    protected Services.QueryTrackingService QueryTrackingService { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.DynamoDb = Substitute.For<IAmazonDynamoDB>();

        var config = Substitute.For<IConfiguration>();
        config["DYNAMODB_TABLE_NAME"].Returns("test-table");

        this.QueryTrackingService = new Services.QueryTrackingService(this.DynamoDb, config);
    }

    [SetUp]
    public virtual void SetUp()
    {
        this.DynamoDb.ClearReceivedCalls();
    }
}
