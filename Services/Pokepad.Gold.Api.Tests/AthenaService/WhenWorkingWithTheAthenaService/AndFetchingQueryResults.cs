using Amazon.Athena.Model;
using NSubstitute;
using NUnit.Framework;
using Pokepad.Gold.Api.Models;

namespace Pokepad.Gold.Api.Tests.AthenaService.WhenWorkingWithTheAthenaService;

public partial class WhenWorkingWithTheAthenaService
{
    public class AndFetchingQueryResults : AthenaServiceTestBase
    {
        private QueryResult _result = null!;

        [OneTimeSetUp]
        public async Task SetUpScenario()
        {
            this.Athena.GetQueryResultsAsync(
                    Arg.Any<GetQueryResultsRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new GetQueryResultsResponse
                {
                    ResultSet = new ResultSet
                    {
                        ResultSetMetadata = new ResultSetMetadata
                        {
                            ColumnInfo =
                            [
                                new ColumnInfo { Name = "product_id" },
                                new ColumnInfo { Name = "name" }
                            ]
                        },
                        Rows =
                        [
                            new Row { Data = [new Datum { VarCharValue = "product_id" }, new Datum { VarCharValue = "name" }] },
                            new Row { Data = [new Datum { VarCharValue = "abc-123" }, new Datum { VarCharValue = "Charizard" }] }
                        ]
                    }
                });

            _result = await this.AthenaService.FetchResultsAsync("exec-abc-123");
        }

        [Test]
        public void ThenItReturnsTheCorrectColumnNames()
        {
            Assert.That(_result.Columns, Is.EqualTo(new[] { "product_id", "name" }));
        }

        [Test]
        public void ThenItSkipsTheHeaderRow()
        {
            Assert.That(_result.Rows.Count, Is.EqualTo(1));
        }

        [Test]
        public void ThenItReturnsTheCorrectRowValues()
        {
            Assert.That(_result.Rows[0], Is.EqualTo(new[] { "abc-123", "Charizard" }));
        }
    }
}
