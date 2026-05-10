using NUnit.Framework;

namespace Pokepad.Infra.Tests.EcsStack.WhenWorkingWithTheEcsConstruct;

public partial class WhenWorkingWithTheEcsConstruct
{
    public class AndCheckingTheClusterAndService : EcsConstructTestBase
    {
        [Test]
        public void ThenOneEcsClusterIsCreated()
        {
            Assert.That(this.Template.FindResources("AWS::ECS::Cluster"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheClusterNameIsPokepad()
        {
            var clusters = this.Template.FindResources("AWS::ECS::Cluster");
            var props = (IDictionary<string, object>)clusters.Values.Single()["Properties"];
            Assert.That(props["ClusterName"]?.ToString(), Is.EqualTo("pokepad"));
        }

        [Test]
        public void ThenOneFargateTaskDefinitionIsCreated()
        {
            Assert.That(this.Template.FindResources("AWS::ECS::TaskDefinition"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheTaskFamilyIsPokepadEmbeddingIndexer()
        {
            var taskDefs = this.Template.FindResources("AWS::ECS::TaskDefinition");
            var props = (IDictionary<string, object>)taskDefs.Values.Single()["Properties"];
            Assert.That(props["Family"]?.ToString(), Is.EqualTo("pokepad-embedding-indexer"));
        }

        [Test]
        public void ThenTheTaskCpuIs256()
        {
            var taskDefs = this.Template.FindResources("AWS::ECS::TaskDefinition");
            var props = (IDictionary<string, object>)taskDefs.Values.Single()["Properties"];
            Assert.That(props["Cpu"]?.ToString(), Is.EqualTo("256"));
        }

        [Test]
        public void ThenTheTaskMemoryIs512()
        {
            var taskDefs = this.Template.FindResources("AWS::ECS::TaskDefinition");
            var props = (IDictionary<string, object>)taskDefs.Values.Single()["Properties"];
            Assert.That(props["Memory"]?.ToString(), Is.EqualTo("512"));
        }

        [Test]
        public void ThenOneFargateServiceIsCreated()
        {
            Assert.That(this.Template.FindResources("AWS::ECS::Service"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheServiceNameIsPokepadEmbeddingIndexer()
        {
            var services = this.Template.FindResources("AWS::ECS::Service");
            var props = (IDictionary<string, object>)services.Values.Single()["Properties"];
            Assert.That(props["ServiceName"]?.ToString(), Is.EqualTo("pokepad-embedding-indexer"));
        }

        [Test]
        public void ThenTheServiceStartsWithZeroDesiredTasks()
        {
            var services = this.Template.FindResources("AWS::ECS::Service");
            var props = (IDictionary<string, object>)services.Values.Single()["Properties"];
            Assert.That(Convert.ToInt32(props["DesiredCount"]), Is.EqualTo(0));
        }

        [Test]
        public void ThenTheServiceDoesNotAssignPublicIps()
        {
            var services = this.Template.FindResources("AWS::ECS::Service");
            var props = (IDictionary<string, object>)services.Values.Single()["Properties"];
            var networkConfig = (IDictionary<string, object>)props["NetworkConfiguration"];
            var awsvpcConfig = (IDictionary<string, object>)networkConfig["AwsvpcConfiguration"];
            Assert.That(awsvpcConfig["AssignPublicIp"]?.ToString(), Is.EqualTo("DISABLED"));
        }
    }
}
