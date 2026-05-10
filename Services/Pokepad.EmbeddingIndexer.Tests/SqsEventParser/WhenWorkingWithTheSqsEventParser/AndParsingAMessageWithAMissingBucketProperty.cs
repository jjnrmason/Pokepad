using NUnit.Framework;

namespace Pokepad.EmbeddingIndexer.Tests.SqsEventParser.WhenWorkingWithTheSqsEventParser;

public partial class WhenWorkingWithTheSqsEventParser
{
    public class AndParsingAMessageWithAMissingBucketProperty : SqsEventParserTestBase
    {
        private const string Body = """
            {
              "object": { "key": "gold/products/data.parquet" }
            }
            """;

        [Test]
        public void ThenItThrowsAKeyNotFoundException()
        {
            Assert.That(() => Services.SqsEventParser.Parse(Body), Throws.TypeOf<KeyNotFoundException>());
        }
    }
}
