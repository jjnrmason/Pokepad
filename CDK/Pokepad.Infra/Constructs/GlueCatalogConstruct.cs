using Amazon.CDK;
using Amazon.CDK.AWS.Glue;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Pokepad.Infra.Constructs;

public sealed class GlueCatalogConstruct : Construct
{
    public string DatabaseName { get; } = "ecommerce_gold";

    public GlueCatalogConstruct(Construct scope, string id, Bucket goldBucket) : base(scope, id)
    {
        var account = Stack.Of(this).Account;

        var database = new CfnDatabase(this, "ecommerce-gold-database", new CfnDatabaseProps
        {
            CatalogId = account,
            DatabaseInput = new CfnDatabase.DatabaseInputProperty
            {
                Name = "ecommerce_gold",
                Description = "Gold layer e-commerce data catalog. Tables contain cleaned, structured data suitable for analytical queries."
            }
        });

        CreateTable(database, goldBucket, "customers", CustomersColumns(), account);
        CreateTable(database, goldBucket, "products", ProductsColumns(), account);
        CreateTable(database, goldBucket, "orders", OrdersColumns(), account);
        CreateTable(database, goldBucket, "order_items", OrderItemsColumns(), account);
    }

    private void CreateTable(CfnDatabase database, Bucket goldBucket, string tableName, CfnTable.ColumnProperty[] columns, string account)
    {
        new CfnTable(this, $"{tableName}-table", new CfnTableProps
        {
            CatalogId = account,
            DatabaseName = database.Ref,
            TableInput = new CfnTable.TableInputProperty
            {
                Name = tableName,
                TableType = "EXTERNAL_TABLE",
                Parameters = new Dictionary<string, object>
                {
                    { "classification", "parquet" },
                    { "EXTERNAL", "TRUE" }
                },
                StorageDescriptor = new CfnTable.StorageDescriptorProperty
                {
                    Location = $"s3://{goldBucket.BucketName}/gold/{tableName}/",
                    InputFormat = "org.apache.hadoop.hive.ql.io.parquet.MapredParquetInputFormat",
                    OutputFormat = "org.apache.hadoop.hive.ql.io.parquet.MapredParquetOutputFormat",
                    SerdeInfo = new CfnTable.SerdeInfoProperty
                    {
                        SerializationLibrary = "org.apache.hadoop.hive.ql.io.parquet.serde.ParquetHiveSerDe",
                        Parameters = new Dictionary<string, object>
                        {
                            { "serialization.format", "1" }
                        }
                    },
                    Columns = columns
                }
            }
        });
    }

    private static CfnTable.ColumnProperty[] CustomersColumns() =>
    [
        Col("CustomerId",  "string",    "Unique customer identifier (UUID)"),
        Col("FirstName",   "string",    "Customer first name"),
        Col("LastName",    "string",    "Customer last name"),
        Col("Email",       "string",    "Customer email address — unique per customer"),
        Col("Phone",       "string",    "Customer contact phone number"),
        Col("Address",     "string",    "Customer street address"),
        Col("City",        "string",    "Customer city"),
        Col("Country",     "string",    "Customer country"),
        Col("CreatedAt",   "timestamp", "Timestamp when the customer account was created"),
    ];

    private static CfnTable.ColumnProperty[] ProductsColumns() =>
    [
        Col("ProductId",     "string", "Unique product identifier (UUID)"),
        Col("Name",          "string", "Product display name"),
        Col("Category",      "string", "Product category: Electronics, Clothing, Home & Garden, Sports, Books, Toys, Beauty, Automotive"),
        Col("Description",   "string", "Product description"),
        Col("Price",         "double", "Product unit price in USD"),
        Col("StockQuantity", "int",    "Available stock quantity"),
    ];

    private static CfnTable.ColumnProperty[] OrdersColumns() =>
    [
        Col("OrderId",         "string",    "Unique order identifier (UUID)"),
        Col("CustomerId",      "string",    "Foreign key referencing customers.CustomerId"),
        Col("OrderDate",       "timestamp", "Timestamp when the order was placed"),
        Col("Status",          "string",    "Order status: Pending, Processing, Shipped, Delivered, Cancelled"),
        Col("TotalAmount",     "double",    "Total order value in USD"),
        Col("ShippingAddress", "string",    "Full shipping address for this order"),
    ];

    private static CfnTable.ColumnProperty[] OrderItemsColumns() =>
    [
        Col("OrderItemId", "string", "Unique order item identifier (UUID)"),
        Col("OrderId",     "string", "Foreign key referencing orders.OrderId"),
        Col("ProductId",   "string", "Foreign key referencing products.ProductId"),
        Col("Quantity",    "int",    "Number of units ordered"),
        Col("UnitPrice",   "double", "Unit price of the product at time of order in USD"),
        Col("Subtotal",    "double", "Line total: Quantity * UnitPrice in USD"),
    ];

    private static CfnTable.ColumnProperty Col(string name, string type, string comment) =>
        new() { Name = name, Type = type, Comment = comment };
}
