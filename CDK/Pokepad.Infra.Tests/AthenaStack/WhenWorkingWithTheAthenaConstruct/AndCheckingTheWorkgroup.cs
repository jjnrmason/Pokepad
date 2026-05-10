using NUnit.Framework;

namespace Pokepad.Infra.Tests.AthenaStack.WhenWorkingWithTheAthenaConstruct;

public partial class WhenWorkingWithTheAthenaConstruct
{
    public class AndCheckingTheWorkgroup : AthenaConstructTestBase
    {
        [Test]
        public void ThenItCreatesOneWorkgroup()
        {
            Assert.That(this.Template.FindResources("AWS::Athena::WorkGroup"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheWorkgroupNameIsPokepad()
        {
            var workgroups = this.Template.FindResources("AWS::Athena::WorkGroup");
            var props = (IDictionary<string, object>)workgroups.Values.Single()["Properties"];
            Assert.That(props["Name"]?.ToString(), Is.EqualTo("pokepad"));
        }

        [Test]
        public void ThenTheWorkgroupStateIsEnabled()
        {
            var workgroups = this.Template.FindResources("AWS::Athena::WorkGroup");
            var props = (IDictionary<string, object>)workgroups.Values.Single()["Properties"];
            Assert.That(props["State"]?.ToString(), Is.EqualTo("ENABLED"));
        }

        [Test]
        public void ThenItEnforcesWorkgroupConfiguration()
        {
            var workgroups = this.Template.FindResources("AWS::Athena::WorkGroup");
            var props = (IDictionary<string, object>)workgroups.Values.Single()["Properties"];
            var config = (IDictionary<string, object>)props["WorkGroupConfiguration"];
            Assert.That(config["EnforceWorkGroupConfiguration"], Is.True);
        }

        [Test]
        public void ThenItPublishesCloudWatchMetrics()
        {
            var workgroups = this.Template.FindResources("AWS::Athena::WorkGroup");
            var props = (IDictionary<string, object>)workgroups.Values.Single()["Properties"];
            var config = (IDictionary<string, object>)props["WorkGroupConfiguration"];
            Assert.That(config["PublishCloudWatchMetricsEnabled"], Is.True);
        }

        [Test]
        public void ThenItSetsAOnGigabyteScanLimit()
        {
            var workgroups = this.Template.FindResources("AWS::Athena::WorkGroup");
            var props = (IDictionary<string, object>)workgroups.Values.Single()["Properties"];
            var config = (IDictionary<string, object>)props["WorkGroupConfiguration"];
            Assert.That(Convert.ToInt64(config["BytesScannedCutoffPerQuery"]), Is.EqualTo(1_073_741_824));
        }

        [Test]
        public void ThenTheOutputLocationIsConfigured()
        {
            var workgroups = this.Template.FindResources("AWS::Athena::WorkGroup");
            var props = (IDictionary<string, object>)workgroups.Values.Single()["Properties"];
            var config = (IDictionary<string, object>)props["WorkGroupConfiguration"];
            var resultConfig = (IDictionary<string, object>)config["ResultConfiguration"];
            Assert.That(resultConfig.ContainsKey("OutputLocation"), Is.True);
            Assert.That(resultConfig["OutputLocation"], Is.Not.Null);
        }
    }
}
