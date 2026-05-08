namespace Pokepad.Lambda.Models;

public record SearchResponse(string Sql, List<string> Columns, List<List<string?>> Rows);
