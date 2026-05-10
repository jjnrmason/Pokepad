using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Npgsql;
using OpenAI.Embeddings;
using Parquet.Serialization;
using Pokepad.EmbeddingIndexer.Models;
using Pokepad.EmbeddingIndexer.Services;

var sqsQueueUrl = GetEnv("SQS_QUEUE_URL");
var aiKeyParam = GetEnv("AI_API_KEY_PARAM");
var connectionStringParam = GetEnv("VECTOR_DB_CONNECTION_STRING_PARAM");
var dbSecretArn = GetEnv("VECTOR_DB_SECRET_ARN");

using var ssm = new AmazonSimpleSystemsManagementClient();
using var sm = new AmazonSecretsManagerClient();
using var sqs = new AmazonSQSClient();
using var s3 = new AmazonS3Client();

var apiKey = (await ssm.GetParameterAsync(new GetParameterRequest
{
    Name = aiKeyParam,
    WithDecryption = true
})).Parameter.Value;

var connectionStringBase = (await ssm.GetParameterAsync(new GetParameterRequest
{
    Name = connectionStringParam
})).Parameter.Value;

var secretJson = (await sm.GetSecretValueAsync(new GetSecretValueRequest
{
    SecretId = dbSecretArn
})).SecretString;

using var dbSecretDoc = JsonDocument.Parse(secretJson);
var password = dbSecretDoc.RootElement.GetProperty("password").GetString()!;
var connectionString = $"{connectionStringBase};Password={password}";

var embeddingClient = new EmbeddingClient("text-embedding-3-small", apiKey);

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

IProductEmbeddingRepository repository = new ProductEmbeddingRepository(conn);

// Honour SIGTERM from ECS scale-in
using var cts = new CancellationTokenSource();
AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Console.WriteLine("SIGTERM received — finishing current message then exiting.");
    cts.Cancel();
};

Console.WriteLine("Polling SQS for work...");

while (!cts.Token.IsCancellationRequested)
{
    ReceiveMessageResponse response;
    try
    {
        response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = sqsQueueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 20,       // long poll
            VisibilityTimeout = 300     // must be >= max processing time per file
        }, cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }

    if (response.Messages.Count == 0)
        continue;

    var message = response.Messages[0];

    try
    {
        await ProcessMessageAsync(message.Body);
        await sqs.DeleteMessageAsync(sqsQueueUrl, message.ReceiptHandle, cts.Token);
        Console.WriteLine("Message processed and deleted.");
    }
    catch (Exception ex)
    {
        // Leave the message in the queue — SQS will re-deliver after VisibilityTimeout.
        // After maxReceiveCount attempts it moves to the DLQ.
        Console.WriteLine($"Failed to process message: {ex.Message}");
    }
}

Console.WriteLine("Worker exiting.");

async Task ProcessMessageAsync(string body)
{
    var s3Event = SqsEventParser.Parse(body);
    Console.WriteLine($"Processing s3://{s3Event.Bucket}/{s3Event.Key}");

    using var getResponse = await s3.GetObjectAsync(new GetObjectRequest
    {
        BucketName = s3Event.Bucket,
        Key = s3Event.Key
    });

    using var ms = new MemoryStream();
    await getResponse.ResponseStream.CopyToAsync(ms);
    ms.Position = 0;

    var products = await ParquetSerializer.DeserializeAsync<ProductRecord>(ms);
    var productList = products.ToList();

    Console.WriteLine($"Loaded {productList.Count} products from {s3Event.Key}");

    const int batchSize = 25;

    for (var i = 0; i < productList.Count; i += batchSize)
    {
        var batch = productList.GetRange(i, Math.Min(batchSize, productList.Count - i));
        var texts = batch.Select(ProductTextFormatter.Format).ToList();

        var result = await embeddingClient.GenerateEmbeddingsAsync(texts);

        for (var j = 0; j < batch.Count; j++)
        {
            var floats = result.Value[j].ToFloats().ToArray();
            await repository.UpsertAsync(batch[j], floats);
        }

        Console.WriteLine($"Upserted batch {i / batchSize + 1}: products {i + 1}–{i + batch.Count}");
    }
}

static string GetEnv(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Environment variable '{name}' is not set.");
