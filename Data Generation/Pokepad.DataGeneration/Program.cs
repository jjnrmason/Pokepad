using Amazon.S3;
using Amazon.S3.Transfer;
using System.CommandLine;
using Parquet.Serialization;
using Pokepad.DataGeneration.Generators;

Option<string?> bucketOption = new("--gold-bucket-name")
{
    Description = "Name of the s3 gold bucket.",
    Required = false,
    DefaultValueFactory = result => result.Tokens.Count == 0 ? Environment.GetEnvironmentVariable("GOLD_BUCKET_NAME") : result.Tokens.Single().Value
};

Option<int> customersOption = new("--customers")
{
    Description = "Number of customers to generate.",
    Required = false,
    DefaultValueFactory = result => result.Tokens.Count == 0 ? 500 : int.Parse(result.Tokens.Single().Value)
};

Option<int> productsOption = new("--products")
{
    Description = "Number of products to generate.",
    Required = false,
    DefaultValueFactory = result => result.Tokens.Count == 0 ? 2000 : int.Parse(result.Tokens.Single().Value)
};

Option<int> ordersOption = new("--orders")
{
    Description = "Number of orders to generate.",
    Required = false,
    DefaultValueFactory = result => result.Tokens.Count == 0 ? 2000 : int.Parse(result.Tokens.Single().Value)
};

RootCommand rootCommand = new("Generate dummy Pokepad data in gold layer.");
rootCommand.Options.Add(bucketOption);
rootCommand.Options.Add(customersOption);
rootCommand.Options.Add(productsOption);
rootCommand.Options.Add(ordersOption);

var parseResult = rootCommand.Parse(args);
if (parseResult.Errors.Count == 0 &&
    parseResult.GetValue(bucketOption) is var parsedBucketName &&
    parseResult.GetValue(customersOption) is var parsedCustomerCount &&
    parseResult.GetValue(productsOption) is var parsedProductCount &&
    parseResult.GetValue(ordersOption) is var parsedOrderCount)
{
    await GenerateData(parsedBucketName, parsedCustomerCount, parsedProductCount, parsedOrderCount);
    return 0;
}

foreach (var parseError in parseResult.Errors)
{
    Console.Error.WriteLine(parseError.Message);
}
return 1;

async Task GenerateData(string? bucketName, int customerCount, int productCount, int orderCount)
{
    const string outputDirectory = "./output";

    Console.WriteLine($"Generating {customerCount} customers, {productCount} products, {orderCount} orders...");

    var generator = new ECommerceDataGenerator(seed: 42);
    var (customers, products, orders, orderItems) = generator.Generate(customerCount, productCount, orderCount);

    Console.WriteLine($"Generated {orderItems.Count} order items across {orders.Count} orders.");
    Console.WriteLine();

    await WriteParquet(customers, Path.Combine(outputDirectory, "customers", "customers.parquet"));
    await WriteParquet(products, Path.Combine(outputDirectory, "products", "products.parquet"));
    await WriteParquet(orders, Path.Combine(outputDirectory, "orders", "orders.parquet"));
    await WriteParquet(orderItems, Path.Combine(outputDirectory, "order_items", "order_items.parquet"));

    Console.WriteLine();
    Console.WriteLine("Parquet files written to ./output/");

    if (!string.IsNullOrWhiteSpace(bucketName))
    {
        Console.WriteLine();
        Console.WriteLine($"Uploading to S3 bucket: {bucketName}...");
        using var s3Client = new AmazonS3Client();
        using var transferUtility = new TransferUtility(s3Client);
        var uploader = new S3Uploader(transferUtility, new FileSystem());
        await uploader.UploadAsync(bucketName, outputDirectory);
        Console.WriteLine("Upload complete.");
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("Tip: pass --gold-bucket-name=<bucket-name> or set GOLD_BUCKET_NAME to upload to S3.");
    }

    return;

    static async Task WriteParquet<T>(IEnumerable<T> records, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await using var stream = File.Create(filePath);
        await ParquetSerializer.SerializeAsync(records, stream);
        Console.WriteLine($" Written: {filePath}");
    }
}
