using Amazon.Glue;
using Amazon.Glue.Model;

namespace Pokepad.Gold.Api.Services;

public sealed class GlueSchemaService(IAmazonGlue glue, ILogger<GlueSchemaService> logger)
{
    private const string DatabaseName = "ecommerce_gold";
    private string? _cachedSchema;

    public async Task<string> GetSchemaAsync()
    {
        if (_cachedSchema is not null)
        {
            logger.LogDebug("Returning cached schema for database {Database}", DatabaseName);
            return _cachedSchema;
        }

        logger.LogInformation("Fetching schema from Glue for database {Database}", DatabaseName);

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

        var tableNames = response.TableList.Select(t => t.Name).ToArray();
        logger.LogInformation("Schema loaded: {TableCount} tables ({Tables})", tableNames.Length, string.Join(", ", tableNames));

        return _cachedSchema;
    }
}
