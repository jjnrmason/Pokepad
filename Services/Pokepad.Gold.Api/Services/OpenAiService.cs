using Pokepad.Gold.Api.Exceptions;
using Pokepad.Gold.Api.Models;
using System.Text.Json;

namespace Pokepad.Gold.Api.Services;

public sealed class OpenAiService(IChatService chatService, IModerationService moderationService, ILogger<OpenAiService> logger)
{
    public async Task<string> GenerateSqlAsync(string question, string schema)
    {
        logger.LogInformation("Generating SQL for question: {Question}", question);

        if (await moderationService.IsFlaggedAsync(question))
        {
            throw new InputValidationException("Input was flagged by content moderation.");
        }

        var systemText = $"""
            You are a SQL expert working with an AWS Athena database.
            Only write SELECT queries. Do not use semicolons.

            If the question is not about querying e-commerce data, respond with exactly: INVALID_QUERY

            Guidelines:
            - Use specific column names rather than SELECT *
            - Always include a WHERE clause or LIMIT when returning large datasets
            - Do not return entire tables without filtering or aggregation

            {schema}
            """;

        var userText = $"""
            Write a single valid Athena SQL SELECT statement to answer this question:
            {question}

            Return ONLY the SQL statement — no explanation, no markdown, no semicolon.
            """;

        var sql = await chatService.CompleteChatAsync(systemText, userText);

        if (sql.Equals("INVALID_QUERY", StringComparison.OrdinalIgnoreCase))
        {
            throw new InputValidationException("Question is not related to e-commerce data.");
        }

        logger.LogInformation("Generated SQL: {Sql}", sql);

        return sql;
    }

    public async Task<string> GenerateSemanticAnswerAsync(string question, IReadOnlyList<SemanticSearchResult> results)
    {
        var systemText = """
            You answer product search questions using only the semantic search results provided.
            Keep the answer concise and practical.
            If the results do not contain enough information, say so.
            """;

        var userText = $"""
            Question:
            {question}

            Semantic search results:
            {JsonSerializer.Serialize(results)}

            Return a plain-English answer only.
            """;

        var answer = await chatService.CompleteChatAsync(systemText, userText);

        logger.LogInformation("Generated semantic search answer");

        return answer;
    }
}
