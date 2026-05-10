using NUnit.Framework;

namespace Pokepad.EmbeddingIndexer.Tests.SqsEventParser.WhenWorkingWithTheSqsEventParser;

public partial class WhenWorkingWithTheSqsEventParser
{
    public class AndParsingAMessageWithAMissingObjectProperty : SqsEventParserTestBase
    {
        private const string Body = """
            {
              "bucket": { "name": "my-data-lake" }
            }
            """;

        [Test]
        public void ThenItThrowsAKeyNotFoundException()
        {
            Assert.That(() => Services.SqsEventParser.Parse(Body), Throws.TypeOf<KeyNotFoundException>());
        }
    }
}
