using NSubstitute;
using NUnit.Framework;

namespace Pokepad.DataGeneration.Tests.S3Uploader.WhenWorkingWithTheS3Uploader;

public partial class WhenWorkingWithTheS3Uploader
{
    public class AndUploadingWhenParquetFilesDoNotExist : S3UploaderTestBase
    {
        [Test]
        public async Task ThenItDoesNotUploadAnything()
        {
            this.FileSystem.GetDirectories("./output").Returns(["./output/customers"]);
            this.FileSystem.FileExists(Path.Combine("./output/customers", "customers.parquet")).Returns(false);

            await this.S3Uploader.UploadAsync("my-bucket", "./output");

            await this.TransferUtility.DidNotReceive().UploadAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>());
        }

        [Test]
        public async Task ThenItSkipsTheMissingFileButUploadsOthers()
        {
            this.FileSystem.GetDirectories("./output").Returns(["./output/customers", "./output/products"]);
            this.FileSystem.FileExists(Path.Combine("./output/customers", "customers.parquet")).Returns(false);
            this.FileSystem.FileExists(Path.Combine("./output/products", "products.parquet")).Returns(true);

            await this.S3Uploader.UploadAsync("my-bucket", "./output");

            await this.TransferUtility.Received(1).UploadAsync(
                Path.Combine("./output/products", "products.parquet"),
                "my-bucket",
                "gold/products/products.parquet");
        }
    }
}
