namespace Pokepad.Gold.Api.Models;

public record QueryResult(List<string> Columns, List<List<string?>> Rows);
