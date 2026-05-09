using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Pokepad.Infra;

public sealed class DynamoConstruct : Construct
{
    public Table Table { get; }

    public DynamoConstruct(Construct scope, string id) : base(scope, id)
    {
        Table = new Table(this, "query-executions", new TableProps
        {
            TableName = "pokepad-query-executions",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute { Name = "executionId", Type = AttributeType.STRING },
            TimeToLiveAttribute = "ttl",
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY
        });
        
        _ = new CfnOutput(this, "TableName", new CfnOutputProps
        {
            Value = Table.TableName,
            Description = "Name of the dynamoDB table used for async queries"
        });
    }
}
