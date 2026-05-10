using NUnit.Framework;

namespace Pokepad.Gold.Api.Tests.GlueSchemaService.WhenWorkingWithTheGlueSchemaService;

public partial class WhenWorkingWithTheGlueSchemaService
{
    public class AndFetchingTheSchema : GlueSchemaServiceTestBase
    {
        [Test]
        public async Task ThenItIncludesTheDatabaseName()
        {
            var result = await this.GlueSchemaService.GetSchemaAsync();

            Assert.That(result, Does.Contain("Database: ecommerce_gold"));
        }

        [Test]
        public async Task ThenItIncludesTheTableName()
        {
            var result = await this.GlueSchemaService.GetSchemaAsync();

            Assert.That(result, Does.Contain("Table: products"));
        }

        [Test]
        public async Task ThenItIncludesTheColumnNameAndType()
        {
            var result = await this.GlueSchemaService.GetSchemaAsync();

            Assert.That(result, Does.Contain("product_id (string)"));
        }

        [Test]
        public async Task ThenItIncludesAColumnComment()
        {
            var result = await this.GlueSchemaService.GetSchemaAsync();

            Assert.That(result, Does.Contain(": Unique identifier"));
        }
    }
}
