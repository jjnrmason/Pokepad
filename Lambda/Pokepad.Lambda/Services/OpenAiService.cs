using OpenAI;
using OpenAI.Chat;
using OpenAI.Moderations;
using Pokepad.Lambda.Exceptions;

namespace Pokepad.Lambda.Services;

public sealed class OpenAiService(OpenAIClient client, ILogger<OpenAiService> logger)
{
    private const string ChatModel = "gpt-4o";
    private const string ModerationModel = "omni-moderation-latest";

    public async Task<string> GenerateSqlAsync(string question, string schema)
    {
        logger.LogInformation("Generating SQL for question: {Question}", question);

        await ModerateAsync(question);

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

        var chatClient = client.GetChatClient(ChatModel);

        var response = await chatClient.CompleteChatAsync(
            [
                ChatMessage.CreateSystemMessage(systemText),
                ChatMessage.CreateUserMessage($"""
                    Write a single valid Athena SQL SELECT statement to answer this question:
                    {question}

                    Return ONLY the SQL statement — no explanation, no markdown, no semicolon.
                    """)
            ],
            new ChatCompletionOptions { MaxOutputTokenCount = 1024 }
        );

        var sql = response.Value.Content[0].Text.Trim();

        if (sql.Equals("INVALID_QUERY", StringComparison.OrdinalIgnoreCase))
            throw new InputValidationException("Question is not related to e-commerce data.");

        logger.LogInformation("Generated SQL ({InputTokens} in / {OutputTokens} out): {Sql}",
            response.Value.Usage.InputTokenCount, response.Value.Usage.OutputTokenCount, sql);

        return sql;
    }

    private async Task ModerateAsync(string input)
    {
        var moderationClient = client.GetModerationClient(ModerationModel);
        var result = await moderationClient.ClassifyTextAsync(input);

        if (result.Value.Flagged)
        {
            logger.LogWarning("Moderation flagged input: {Input}", input);
            throw new InputValidationException("Input was flagged by content moderation.");
        }
    }
}
