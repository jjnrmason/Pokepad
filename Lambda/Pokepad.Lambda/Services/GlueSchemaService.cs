using Amazon.Glue;
using Amazon.Glue.Model;

namespace Pokepad.Lambda.Services;

public sealed class GlueSchemaService(IAmazonGlue glue)
{
    private const string DatabaseName = "ecommerce_gold";
    private string? _cachedSchema;

    public async Task<string> GetSchemaAsync()
    {
        if (_cachedSchema is not null) return _cachedSchema;

        var response = await glue.GetTablesAsync(new GetTablesRequest { DatabaseName = DatabaseName });

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Database: {DatabaseName}");
        sb.AppendLine();

        foreach (var table in response.TableList)
        {
            sb.AppendLine($"Table: {table.Name}");
            foreach (var col in table.StorageDescriptor.Columns)
            {
                var comment = string.IsNullOrEmpty(col.Comment) ? string.Empty : $": {col.Comment}";
                sb.AppendLine($"  - {col.Name} ({col.Type}){comment}");
            }
            sb.AppendLine();
        }

        _cachedSchema = sb.ToString();
        return _cachedSchema;
    }
}
