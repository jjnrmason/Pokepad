using NUnit.Framework;

namespace Pokepad.Infra.Tests.VectorStoreStack.WhenWorkingWithTheVectorStoreConstruct;

public partial class WhenWorkingWithTheVectorStoreConstruct
{
    public class AndCheckingTheRdsInstance : VectorStoreConstructTestBase
    {
        private IDictionary<string, object> GetRdsProps()
        {
            var instances = this.Template.FindResources("AWS::RDS::DBInstance");
            return (IDictionary<string, object>)instances.Values.Single()["Properties"];
        }

        [Test]
        public void ThenItCreatesOneRdsInstance()
        {
            Assert.That(this.Template.FindResources("AWS::RDS::DBInstance"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheInstanceIdentifierIsPokepadVectorStore()
        {
            Assert.That(this.GetRdsProps()["DBInstanceIdentifier"]?.ToString(), Is.EqualTo("pokepad-vector-store"));
        }

        [Test]
        public void ThenTheDatabaseEngineIsPostgres()
        {
            var engine = this.GetRdsProps()["Engine"]?.ToString();
            Assert.That(engine, Is.EqualTo("postgres"));
        }

        [Test]
        public void ThenTheDatabaseNameIsPokepad()
        {
            Assert.That(this.GetRdsProps()["DBName"]?.ToString(), Is.EqualTo("pokepad"));
        }

        [Test]
        public void ThenStorageIsEncrypted()
        {
            Assert.That(this.GetRdsProps()["StorageEncrypted"], Is.True);
        }

        [Test]
        public void ThenMultiAzIsDisabledByDefault()
        {
            var props = this.GetRdsProps();
            var multiAz = props.TryGetValue("MultiAZ", out var val) && val is bool b && b;
            Assert.That(multiAz, Is.False);
        }

        [Test]
        public void ThenBackupRetentionIsSevenDays()
        {
            Assert.That(Convert.ToInt32(this.GetRdsProps()["BackupRetentionPeriod"]), Is.EqualTo(7));
        }

        [Test]
        public void ThenTheInstanceIsNotPubliclyAccessible()
        {
            Assert.That(this.GetRdsProps()["PubliclyAccessible"], Is.False);
        }

        [Test]
        public void ThenAConnectionStringParameterIsCreated()
        {
            var parameters = this.Template.FindResources("AWS::SSM::Parameter");
            var hasConnString = parameters.Values.Any(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                return props["Name"]?.ToString() == "/pokepad/vector-db-connection-string";
            });
            Assert.That(hasConnString, Is.True);
        }

        [Test]
        public void ThenItExposesTheDbSecretAsAPublicProperty()
        {
            Assert.That(this.VectorStoreConstruct.DbSecret, Is.Not.Null);
        }
    }
}
