using NUnit.Framework;
using Pokepad.Gold.Api.Middleware;

namespace Pokepad.Gold.Api.Tests.JwtPayloadParser.WhenWorkingWithTheJwtPayloadParser;

public partial class WhenWorkingWithTheJwtPayloadParser
{
    public class AndParsingAPayloadWithInvalidBase64 : JwtPayloadParserTestBase
    {
        [Test]
        public void ThenItThrowsAFormatException()
        {
            Assert.That(
                () => Middleware.JwtPayloadParser.ParseClaims("!!!invalid!!!"),
                Throws.TypeOf<FormatException>());
        }
    }
}
