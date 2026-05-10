using NUnit.Framework;

namespace Pokepad.Infra.Tests.GlueCatalogStack.WhenWorkingWithTheGlueCatalogConstruct;

public partial class WhenWorkingWithTheGlueCatalogConstruct
{
    public class AndCheckingTheTables : GlueCatalogConstructTestBase
    {
        [Test]
        public void ThenTheCustomersTableExists()
        {
            Assert.That(() => this.GetTableByName("customers"), Throws.Nothing);
        }

        [Test]
        public void ThenTheProductsTableExists()
        {
            Assert.That(() => this.GetTableByName("products"), Throws.Nothing);
        }

        [Test]
        public void ThenTheOrdersTableExists()
        {
            Assert.That(() => this.GetTableByName("orders"), Throws.Nothing);
        }

        [Test]
        public void ThenTheOrderItemsTableExists()
        {
            Assert.That(() => this.GetTableByName("order_items"), Throws.Nothing);
        }

        [Test]
        public void ThenAllTablesAreExternalTables()
        {
            var tables = this.Template.FindResources("AWS::Glue::Table");
            var allExternal = tables.Values.All(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                var input = (IDictionary<string, object>)props["TableInput"];
                return input["TableType"]?.ToString() == "EXTERNAL_TABLE";
            });
            Assert.That(allExternal, Is.True);
        }

        [Test]
        public void ThenAllTablesUseParquetClassification()
        {
            var tables = this.Template.FindResources("AWS::Glue::Table");
            var allParquet = tables.Values.All(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                var input = (IDictionary<string, object>)props["TableInput"];
                var parameters = (IDictionary<string, object>)input["Parameters"];
                return parameters["classification"]?.ToString() == "parquet";
            });
            Assert.That(allParquet, Is.True);
        }

        [Test]
        public void ThenAllTablesUseParquetSerDe()
        {
            var tables = this.Template.FindResources("AWS::Glue::Table");
            var allParquetSerDe = tables.Values.All(r =>
            {
                var props = (IDictionary<string, object>)r["Properties"];
                var input = (IDictionary<string, object>)props["TableInput"];
                var sd = (IDictionary<string, object>)input["StorageDescriptor"];
                var serdeInfo = (IDictionary<string, object>)sd["SerdeInfo"];
                return serdeInfo["SerializationLibrary"]?.ToString()?.Contains("parquet") == true;
            });
            Assert.That(allParquetSerDe, Is.True);
        }

        [Test]
        public void ThenTheCustomersTableHasNineColumns()
        {
            var props = this.GetTableByName("customers");
            var input = (IDictionary<string, object>)props["TableInput"];
            var sd = (IDictionary<string, object>)input["StorageDescriptor"];
            var columns = (IList<object>)sd["Columns"];
            Assert.That(columns, Has.Count.EqualTo(9));
        }

        [Test]
        public void ThenTheProductsTableHasSixColumns()
        {
            var props = this.GetTableByName("products");
            var input = (IDictionary<string, object>)props["TableInput"];
            var sd = (IDictionary<string, object>)input["StorageDescriptor"];
            var columns = (IList<object>)sd["Columns"];
            Assert.That(columns, Has.Count.EqualTo(6));
        }

        [Test]
        public void ThenTheOrdersTableHasSixColumns()
        {
            var props = this.GetTableByName("orders");
            var input = (IDictionary<string, object>)props["TableInput"];
            var sd = (IDictionary<string, object>)input["StorageDescriptor"];
            var columns = (IList<object>)sd["Columns"];
            Assert.That(columns, Has.Count.EqualTo(6));
        }

        [Test]
        public void ThenTheOrderItemsTableHasSixColumns()
        {
            var props = this.GetTableByName("order_items");
            var input = (IDictionary<string, object>)props["TableInput"];
            var sd = (IDictionary<string, object>)input["StorageDescriptor"];
            var columns = (IList<object>)sd["Columns"];
            Assert.That(columns, Has.Count.EqualTo(6));
        }
    }
}
