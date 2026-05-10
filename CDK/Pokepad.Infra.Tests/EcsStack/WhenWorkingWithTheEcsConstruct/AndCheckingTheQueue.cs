using NUnit.Framework;

namespace Pokepad.Infra.Tests.EcsStack.WhenWorkingWithTheEcsConstruct;

public partial class WhenWorkingWithTheEcsConstruct
{
    public class AndCheckingTheQueue : EcsConstructTestBase
    {
        private IDictionary<string, object>? FindQueueByName(string name) =>
            this.Template.FindResources("AWS::SQS::Queue").Values
                .FirstOrDefault(r =>
                {
                    var props = (IDictionary<string, object>)r["Properties"];
                    return props.TryGetValue("QueueName", out var n) && n?.ToString() == name;
                });

        [Test]
        public void ThenTwoQueuesAreCreated()
        {
            Assert.That(this.Template.FindResources("AWS::SQS::Queue"), Has.Count.EqualTo(2));
        }

        [Test]
        public void ThenTheMainQueueExists()
        {
            Assert.That(this.FindQueueByName("pokepad-embedding-indexer-queue"), Is.Not.Null);
        }

        [Test]
        public void ThenTheDlqExists()
        {
            Assert.That(this.FindQueueByName("pokepad-embedding-indexer-dlq"), Is.Not.Null);
        }

        [Test]
        public void ThenTheMainQueueVisibilityTimeoutIsThreeHundredSeconds()
        {
            var queue = this.FindQueueByName("pokepad-embedding-indexer-queue")!;
            var props = (IDictionary<string, object>)queue["Properties"];
            Assert.That(Convert.ToInt32(props["VisibilityTimeout"]), Is.EqualTo(300));
        }

        [Test]
        public void ThenTheDlqRetentionIsFourteenDays()
        {
            var dlq = this.FindQueueByName("pokepad-embedding-indexer-dlq")!;
            var props = (IDictionary<string, object>)dlq["Properties"];
            Assert.That(Convert.ToInt32(props["MessageRetentionPeriod"]), Is.EqualTo(14 * 24 * 60 * 60));
        }

        [Test]
        public void ThenTheMainQueueHasADeadLetterQueueConfigured()
        {
            var queue = this.FindQueueByName("pokepad-embedding-indexer-queue")!;
            var props = (IDictionary<string, object>)queue["Properties"];
            Assert.That(props.ContainsKey("RedrivePolicy"), Is.True);
        }

        [Test]
        public void ThenTheMaxReceiveCountIsThree()
        {
            var queue = this.FindQueueByName("pokepad-embedding-indexer-queue")!;
            var props = (IDictionary<string, object>)queue["Properties"];
            var redrive = (IDictionary<string, object>)props["RedrivePolicy"];
            Assert.That(Convert.ToInt32(redrive["maxReceiveCount"]), Is.EqualTo(3));
        }
    }
}
