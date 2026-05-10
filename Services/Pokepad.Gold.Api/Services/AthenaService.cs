using Amazon.Athena;
using Amazon.Athena.Model;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Services;

public sealed class AthenaService(IAmazonAthena athena, IConfiguration config, ILogger<AthenaService> logger)
{
    private const string WorkGroup = "pokepad";
    private readonly string _outputLocation = config["ATHENA_OUTPUT_LOCATION"]
        ?? throw new InvalidOperationException("ATHENA_OUTPUT_LOCATION is required");

    public async Task<QueryResult> ExecuteAsync(string sql)
    {
        var executionId = await StartAsync(sql);
        await WaitForCompletionAsync(executionId);
        return await FetchResultsAsync(executionId);
    }

    public async Task<string> StartAsync(string sql)
    {
        logger.LogInformation("Submitting SQL: {Sql}", sql);

        var response = await athena.StartQueryExecutionAsync(new StartQueryExecutionRequest
        {
            QueryString = sql,
            WorkGroup = WorkGroup,
            QueryExecutionContext = new QueryExecutionContext { Database = "ecommerce_gold" },
            ResultConfiguration = new ResultConfiguration { OutputLocation = _outputLocation }
        });

        logger.LogInformation("Query started: {ExecutionId}", response.QueryExecutionId);
        return response.QueryExecutionId;
    }

    public async Task<QueryExecution> GetExecutionAsync(string executionId)
    {
        var response = await athena.GetQueryExecutionAsync(new GetQueryExecutionRequest
        {
            QueryExecutionId = executionId
        });
        return response.QueryExecution;
    }

    public async Task<QueryResult> FetchResultsAsync(string executionId)
    {
        return await GetResultsAsync(executionId);
    }

    private async Task WaitForCompletionAsync(string executionId)
    {
        while (true)
        {
            var execution = await GetExecutionAsync(executionId);
            var state = execution.Status.State;

            if (state == QueryExecutionState.SUCCEEDED)
            {
                var stats = execution.Statistics;
                logger.LogInformation(
                    "Query {ExecutionId} succeeded: {DataScannedMB:F2} MB scanned, {DurationMs} ms engine time",
                    executionId,
                    stats.DataScannedInBytes / 1_048_576.0,
                    stats.EngineExecutionTimeInMillis);
                return;
            }

            if (state == QueryExecutionState.FAILED || state == QueryExecutionState.CANCELLED)
            {
                logger.LogError(
                    "Query {ExecutionId} {State}: {Reason}",
                    executionId, state, execution.Status.StateChangeReason);
                throw new InvalidOperationException(
                    $"Query {state}: {execution.Status.StateChangeReason}");
            }

            await Task.Delay(500);
        }
    }

    private async Task<QueryResult> GetResultsAsync(string executionId)
    {
        var response = await athena.GetQueryResultsAsync(new GetQueryResultsRequest
        {
            QueryExecutionId = executionId
        });

        var columns = response.ResultSet.ResultSetMetadata.ColumnInfo
            .Select(c => c.Name)
            .ToList();

        var rows = response.ResultSet.Rows
            .Skip(1) // first row is the column header
            .Select(r => r.Data.Select(d => (string?)d.VarCharValue).ToList())
            .ToList();

        return new QueryResult(columns, rows);
    }
}
