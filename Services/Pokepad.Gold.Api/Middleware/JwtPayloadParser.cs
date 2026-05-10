using System.Security.Claims;
using System.Text.Json;

namespace Pokepad.Gold.Api.Middleware;

public static class JwtPayloadParser
{
    public static IReadOnlyList<Claim> ParseClaims(string encodedPayload)
    {
        var padded = encodedPayload.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');

        using var doc = JsonDocument.Parse(Convert.FromBase64String(padded));
        return doc.RootElement.EnumerateObject()
            .Select(p => new Claim(p.Name, p.Value.ToString()))
            .ToList();
    }
}
