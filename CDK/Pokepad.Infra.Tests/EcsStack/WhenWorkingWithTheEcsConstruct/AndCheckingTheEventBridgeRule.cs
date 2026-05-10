using NUnit.Framework;

namespace Pokepad.Infra.Tests.EcsStack.WhenWorkingWithTheEcsConstruct;

public partial class WhenWorkingWithTheEcsConstruct
{
    public class AndCheckingTheEventBridgeRule : EcsConstructTestBase
    {
        private IDictionary<string, object> GetGoldPutRuleProps()
        {
            var rules = this.Template.FindResources("AWS::Events::Rule");
            var rule = rules.Values.First(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props.TryGetValue("Name", out var n) && n?.ToString() == "pokepad-gold-object-created";
            });
            return (IDictionary<string, object>)rule["Properties"];
        }

        [Test]
        public void ThenTheEventBridgeRuleIsCreated()
        {
            var rules = this.Template.FindResources("AWS::Events::Rule");
            var hasRule = rules.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props.TryGetValue("Name", out var n) && n?.ToString() == "pokepad-gold-object-created";
            });
            Assert.That(hasRule, Is.True);
        }

        [Test]
        public void ThenTheRuleListensForS3ObjectCreatedEvents()
        {
            var props = this.GetGoldPutRuleProps();
            var pattern = (IDictionary<string, object>)props["EventPattern"];
            var source = (IList<object>)pattern["source"];
            Assert.That(source.Any(s => s?.ToString() == "aws.s3"), Is.True);
        }

        [Test]
        public void ThenTheRuleTargetIsTheSqsQueue()
        {
            var props = this.GetGoldPutRuleProps();
            var targets = (IList<object>)props["Targets"];
            Assert.That(targets, Has.Count.EqualTo(1));
            var target = (IDictionary<string, object>)targets[0];
            Assert.That(target.ContainsKey("Arn"), Is.True);
        }
    }
}
