namespace Pokepad.Lambda;

public sealed class SqlValidator
{
    private static readonly string[] BlockedKeywords =
    [
        "DROP", "DELETE", "INSERT", "UPDATE", "CREATE", "ALTER",
        "TRUNCATE", "EXEC", "EXECUTE", "MERGE", "--", "/*"
    ];

    public void Validate(string sql)
    {
        var trimmed = sql.TrimStart();

        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only SELECT statements are permitted.");
        }

        var upper = trimmed.ToUpperInvariant();
        foreach (var keyword in BlockedKeywords)
        {
            if (upper.Contains(keyword))
            {
                throw new InvalidOperationException($"SQL contains blocked keyword: {keyword}");
            }
        }
    }
}
