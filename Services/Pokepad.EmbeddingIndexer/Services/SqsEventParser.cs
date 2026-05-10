using System.Text.Json;

namespace Pokepad.EmbeddingIndexer.Services;

public static class SqsEventParser
{
    public record S3Event(string Bucket, string Key);

    public static S3Event Parse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var bucket = root.GetProperty("bucket").GetProperty("name").GetString()!;
        var key = root.GetProperty("object").GetProperty("key").GetString()!;
        return new S3Event(bucket, key);
    }
}
