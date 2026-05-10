using NUnit.Framework;

namespace Pokepad.Infra.Tests.DynamoStack.WhenWorkingWithTheDynamoConstruct;

public partial class WhenWorkingWithTheDynamoConstruct
{
    public class AndCheckingTheTable : DynamoConstructTestBase
    {
        [Test]
        public void ThenItExposesTheTableAsAPublicProperty()
        {
            Assert.That(this.DynamoConstruct.Table, Is.Not.Null);
        }

        [Test]
        public void ThenItCreatesOneTable()
        {
            Assert.That(this.Template.FindResources("AWS::DynamoDB::Table"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheTableNameIsPokepadQueryExecutions()
        {
            var props = this.GetTableProps();
            Assert.That(props["TableName"]?.ToString(), Is.EqualTo("pokepad-query-executions"));
        }

        [Test]
        public void ThenThePartitionKeyIsExecutionId()
        {
            var props = this.GetTableProps();
            var keySchema = (IList<object>)props["KeySchema"];
            var hashKey = keySchema.Cast<IDictionary<string, object>>()
                .FirstOrDefault(k => k["KeyType"]?.ToString() == "HASH");
            Assert.That(hashKey, Is.Not.Null);
            Assert.That(hashKey!["AttributeName"]?.ToString(), Is.EqualTo("executionId"));
        }

        [Test]
        public void ThenTheTtlAttributeIsTtl()
        {
            var props = this.GetTableProps();
            var ttl = (IDictionary<string, object>)props["TimeToLiveSpecification"];
            Assert.That(ttl["AttributeName"]?.ToString(), Is.EqualTo("ttl"));
            Assert.That(ttl["Enabled"], Is.True);
        }

        [Test]
        public void ThenBillingModeIsPayPerRequest()
        {
            var props = this.GetTableProps();
            Assert.That(props["BillingMode"]?.ToString(), Is.EqualTo("PAY_PER_REQUEST"));
        }

        [Test]
        public void ThenTheCfnOutputIsCreated()
        {
            var outputs = this.Template.FindOutputs("*");
            Assert.That(outputs.Keys.Any(k => k.Contains("TableName")), Is.True);
        }
    }
}
