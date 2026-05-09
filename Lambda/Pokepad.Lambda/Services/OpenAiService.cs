using OpenAI;
using OpenAI.Chat;

namespace Pokepad.Lambda.Services;

public sealed class OpenAiService(OpenAIClient client, ILogger<OpenAiService> logger)
{
    private const string Model = "gpt-4o";

    public async Task<string> GenerateSqlAsync(string question, string schema)
    {
        logger.LogInformation("Generating SQL for question: {Question}", question);

        var systemText = $"""
            You are a SQL expert working with an AWS Athena database.
            Only write SELECT queries. Do not use semicolons.

            {schema}
            """;

        var chatClient = client.GetChatClient(Model);

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
        logger.LogInformation("Generated SQL ({InputTokens} in / {OutputTokens} out): {Sql}",
            response.Value.Usage.InputTokenCount, response.Value.Usage.OutputTokenCount, sql);

        return sql;
    }
}
