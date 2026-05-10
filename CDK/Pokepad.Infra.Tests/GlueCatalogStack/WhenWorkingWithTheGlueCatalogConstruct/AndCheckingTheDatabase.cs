using NUnit.Framework;

namespace Pokepad.Infra.Tests.GlueCatalogStack.WhenWorkingWithTheGlueCatalogConstruct;

public partial class WhenWorkingWithTheGlueCatalogConstruct
{
    public class AndCheckingTheDatabase : GlueCatalogConstructTestBase
    {
        [Test]
        public void ThenTheDatabaseNamePropertyIsEcommerceGold()
        {
            Assert.That(this.GlueCatalogConstruct.DatabaseName, Is.EqualTo("ecommerce_gold"));
        }

        [Test]
        public void ThenItCreatesOneGlueDatabase()
        {
            Assert.That(this.Template.FindResources("AWS::Glue::Database"), Has.Count.EqualTo(1));
        }

        [Test]
        public void ThenTheDatabaseNameIsEcommerceGold()
        {
            var databases = this.Template.FindResources("AWS::Glue::Database");
            var props = (IDictionary<string, object>)databases.Values.Single()["Properties"];
            var dbInput = (IDictionary<string, object>)props["DatabaseInput"];
            Assert.That(dbInput["Name"]?.ToString(), Is.EqualTo("ecommerce_gold"));
        }

        [Test]
        public void ThenItCreatesFourGlueTables()
        {
            Assert.That(this.Template.FindResources("AWS::Glue::Table"), Has.Count.EqualTo(4));
        }
    }
}
