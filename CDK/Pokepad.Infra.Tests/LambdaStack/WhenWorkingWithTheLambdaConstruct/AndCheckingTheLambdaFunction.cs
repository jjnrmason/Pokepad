using NUnit.Framework;

namespace Pokepad.Infra.Tests.LambdaStack.WhenWorkingWithTheLambdaConstruct;

public partial class WhenWorkingWithTheLambdaConstruct
{
    public class AndCheckingTheLambdaFunction : LambdaConstructTestBase
    {
        private IDictionary<string, object> GetSearchFunctionProps() =>
            (IDictionary<string, object>)this.FindFunctionByName("pokepad-search")!["Properties"];

        [Test]
        public void ThenTheSearchFunctionIsCreated()
        {
            Assert.That(this.FindFunctionByName("pokepad-search"), Is.Not.Null);
        }

        [Test]
        public void ThenTheRuntimeIsProvidedAl2023()
        {
            Assert.That(this.GetSearchFunctionProps()["Runtime"]?.ToString(), Is.EqualTo("provided.al2023"));
        }

        [Test]
        public void ThenMemoryIsFiveHundredAndTwelveMb()
        {
            Assert.That(Convert.ToInt32(this.GetSearchFunctionProps()["MemorySize"]), Is.EqualTo(512));
        }

        [Test]
        public void ThenTimeoutIsThirtySeconds()
        {
            Assert.That(Convert.ToInt32(this.GetSearchFunctionProps()["Timeout"]), Is.EqualTo(30));
        }

        [Test]
        public void ThenTracingIsActive()
        {
            Assert.That(this.GetSearchFunctionProps()["TracingConfig"], Is.Not.Null);
            var tracingConfig = (IDictionary<string, object>)this.GetSearchFunctionProps()["TracingConfig"];
            Assert.That(tracingConfig["Mode"]?.ToString(), Is.EqualTo("Active"));
        }

        [Test]
        public void ThenItIsDeployedInTheVpc()
        {
            Assert.That(this.GetSearchFunctionProps().ContainsKey("VpcConfig"), Is.True);
        }

        [Test]
        public void ThenTheRoleHasGoldBucketReadAccess()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("s3:GetObject"), Is.True);
        }

        [Test]
        public void ThenTheRoleHasDynamoDbAccess()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("dynamodb:PutItem"), Is.True);
        }

        [Test]
        public void ThenTheRoleHasAthenaQueryAccess()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("athena:StartQueryExecution"), Is.True);
        }

        [Test]
        public void ThenTheRoleHasSsmParameterAccess()
        {
            Assert.That(this.AnyPolicyStatementContainsAction("ssm:GetParameter"), Is.True);
        }

        [Test]
        public void ThenTheErrorRateAlarmIsCreated()
        {
            var alarms = this.Template.FindResources("AWS::CloudWatch::Alarm");
            var hasAlarm = alarms.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["AlarmName"]?.ToString() == "pokepad-lambda-error-rate";
            });
            Assert.That(hasAlarm, Is.True);
        }

        [Test]
        public void ThenTheAlarmThresholdIsFivePercent()
        {
            var alarms = this.Template.FindResources("AWS::CloudWatch::Alarm");
            var alarm = alarms.Values.First(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["AlarmName"]?.ToString() == "pokepad-lambda-error-rate";
            });
            var alarmProps = (IDictionary<string, object>)alarm["Properties"];
            Assert.That(Convert.ToDouble(alarmProps["Threshold"]), Is.EqualTo(5));
        }
    }
}
