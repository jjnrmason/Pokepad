using NUnit.Framework;
using Pokepad.EmbeddingIndexer.Services;

namespace Pokepad.EmbeddingIndexer.Tests.SqsEventParser.WhenWorkingWithTheSqsEventParser;

public partial class WhenWorkingWithTheSqsEventParser
{
    public class AndParsingAValidMessage : SqsEventParserTestBase
    {
        private const string Body = """
            {
              "bucket": { "name": "my-data-lake" },
              "object": { "key": "gold/products/data.parquet" }
            }
            """;

        [Test]
        public void ThenItReturnsTheCorrectBucket()
        {
            var result = Services.SqsEventParser.Parse(Body);

            Assert.That(result.Bucket, Is.EqualTo("my-data-lake"));
        }

        [Test]
        public void ThenItReturnsTheCorrectKey()
        {
            var result = Services.SqsEventParser.Parse(Body);

            Assert.That(result.Key, Is.EqualTo("gold/products/data.parquet"));
        }
    }
}
