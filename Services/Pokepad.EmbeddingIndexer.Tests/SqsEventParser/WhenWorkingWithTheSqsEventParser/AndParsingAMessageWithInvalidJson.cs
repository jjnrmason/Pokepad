using System.Text.Json;
using NUnit.Framework;

namespace Pokepad.EmbeddingIndexer.Tests.SqsEventParser.WhenWorkingWithTheSqsEventParser;

public partial class WhenWorkingWithTheSqsEventParser
{
    public class AndParsingAMessageWithInvalidJson : SqsEventParserTestBase
    {
        [Test]
        public void ThenItThrowsAJsonException()
        {
            Assert.That(() => Services.SqsEventParser.Parse("not valid json"), Throws.InstanceOf<JsonException>());
        }
    }
}
