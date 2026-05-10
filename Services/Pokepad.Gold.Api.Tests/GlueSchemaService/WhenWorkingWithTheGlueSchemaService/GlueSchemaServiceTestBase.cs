using Amazon.Glue;
using Amazon.Glue.Model;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Services;

namespace Pokepad.Gold.Api.Tests.GlueSchemaService.WhenWorkingWithTheGlueSchemaService;

public class GlueSchemaServiceTestBase
{
    protected IAmazonGlue Glue { get; private set; } = null!;
    protected Services.GlueSchemaService GlueSchemaService { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.Glue = Substitute.For<IAmazonGlue>();

        this.Glue.GetTablesAsync(
                Arg.Any<GetTablesRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new GetTablesResponse
            {
                TableList =
                [
                    new Table
                    {
                        Name = "products",
                        StorageDescriptor = new StorageDescriptor
                        {
                            Columns =
                            [
                                new Column { Name = "product_id", Type = "string", Comment = "Unique identifier" },
                                new Column { Name = "name", Type = "string", Comment = "" }
                            ]
                        }
                    }
                ]
            });

        this.GlueSchemaService = new Services.GlueSchemaService(this.Glue, Substitute.For<ILogger<Services.GlueSchemaService>>());
    }

    [SetUp]
    public virtual void SetUp()
    {
        this.Glue.ClearReceivedCalls();
    }
}
