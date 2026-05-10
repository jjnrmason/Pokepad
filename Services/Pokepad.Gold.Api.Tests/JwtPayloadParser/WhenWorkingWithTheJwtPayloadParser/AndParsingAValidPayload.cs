using System.Text;
using NUnit.Framework;
using Pokepad.Gold.Api.Middleware;

namespace Pokepad.Gold.Api.Tests.JwtPayloadParser.WhenWorkingWithTheJwtPayloadParser;

public partial class WhenWorkingWithTheJwtPayloadParser
{
    public class AndParsingAValidPayload : JwtPayloadParserTestBase
    {
        private static string BuildPayload()
        {
            var json = """{"sub":"user-123","email":"test@example.com"}""";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        [Test]
        public void ThenItReturnsTheSubClaim()
        {
            var result = Middleware.JwtPayloadParser.ParseClaims(BuildPayload());

            Assert.That(result.Single(c => c.Type == "sub").Value, Is.EqualTo("user-123"));
        }

        [Test]
        public void ThenItReturnsTheEmailClaim()
        {
            var result = Middleware.JwtPayloadParser.ParseClaims(BuildPayload());

            Assert.That(result.Single(c => c.Type == "email").Value, Is.EqualTo("test@example.com"));
        }
    }
}
