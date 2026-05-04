using System.CommandLine;
using CsvHelper;
using CsvHelper.Configuration;
using Pokepad.DataGeneration.Generators;
using System.Globalization;

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

RootCommand rootCommand = new("Generate dummy PokepadData in gold layer.");
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

foreach (var parseError in parseResult.Errors) {
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

    WriteCsv(customers, Path.Combine(outputDirectory, "customers", "customers.csv"));
    WriteCsv(products, Path.Combine(outputDirectory, "products", "products.csv"));
    WriteCsv(orders, Path.Combine(outputDirectory, "orders", "orders.csv"));
    WriteCsv(orderItems, Path.Combine(outputDirectory, "order_items", "order_items.csv"));

    Console.WriteLine();
    Console.WriteLine("CSV files written to ./output/");

    if (!string.IsNullOrWhiteSpace(bucketName))
    {
        Console.WriteLine();
        Console.WriteLine($"Uploading to S3 bucket: {bucketName}...");
        await S3Uploader.UploadAsync(bucketName, outputDirectory);
        Console.WriteLine("Upload complete.");
    }
    else
    {
        Console.WriteLine();
        Console.WriteLine("Tip: pass --gold-bucket-name=<bucket-name> or set GOLD_BUCKET_NAME to upload to S3.");
    }

    return;

    static void WriteCsv<T>(IEnumerable<T> records, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
        csv.WriteRecords(records);
        Console.WriteLine($" Written: {filePath}");
    }
}
