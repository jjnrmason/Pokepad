using Anthropic;
using Anthropic.Models.Messages;

namespace Pokepad.Lambda.Services;

public sealed class ClaudeService(AnthropicClient client, ILogger<ClaudeService> logger)
{
    private const string Model = "claude-sonnet-4-6";

    public async Task<string> GenerateSqlAsync(string question, string schema)
    {
        logger.LogInformation("Generating SQL for question: {Question}", question);
        var systemText = $"""
            You are a SQL expert working with an AWS Athena database.
            Only write SELECT queries. Do not use semicolons.

            {schema}
            """;

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = Model,
            MaxTokens = 1024,
            System = new List<TextBlockParam>
            {
                new() { Text = systemText, CacheControl = new CacheControlEphemeral() }
            },
            Messages =
            [
                new MessageParam { Role = Role.User, Content = $"""
                    Write a single valid Athena SQL SELECT statement to answer this question:
                    {question}

                    Return ONLY the SQL statement — no explanation, no markdown, no semicolon.
                    """ }
            ]
        });

        if (response.Content.Count > 0 && response.Content[0].TryPickText(out var textBlock))
        {
            var sql = textBlock!.Text.Trim();
            logger.LogInformation("Generated SQL ({InputTokens} in / {OutputTokens} out): {Sql}",
                response.Usage.InputTokens, response.Usage.OutputTokens, sql);
            return sql;
        }

        throw new InvalidOperationException("No text content in Claude response.");
    }
}
