using NSubstitute;
using NUnit.Framework;

namespace Pokepad.DataGeneration.Tests.S3Uploader.WhenWorkingWithTheS3Uploader;

public partial class WhenWorkingWithTheS3Uploader
{
    public class AndUploadingWhenParquetFilesExist : S3UploaderTestBase
    {
        [Test]
        public async Task ThenItUploadsToTheCorrectS3Key()
        {
            this.FileSystem.GetDirectories("./output").Returns(["./output/customers"]);
            this.FileSystem.FileExists(Path.Combine("./output/customers", "customers.parquet")).Returns(true);

            await this.S3Uploader.UploadAsync("my-bucket", "./output");

            await this.TransferUtility.Received(1).UploadAsync(
                Path.Combine("./output/customers", "customers.parquet"),
                "my-bucket",
                "gold/customers/customers.parquet");
        }

        [Test]
        public async Task ThenItUploadsFromTheCorrectLocalPath()
        {
            this.FileSystem.GetDirectories("./output").Returns(["./output/products"]);
            this.FileSystem.FileExists(Path.Combine("./output/products", "products.parquet")).Returns(true);

            await this.S3Uploader.UploadAsync("data-bucket", "./output");

            await this.TransferUtility.Received(1).UploadAsync(
                Path.Combine("./output/products", "products.parquet"),
                Arg.Any<string>(),
                Arg.Any<string>());
        }

        [Test]
        public async Task ThenItUploadsAllDirectoriesWithParquetFiles()
        {
            this.FileSystem.GetDirectories("./output").Returns(["./output/customers", "./output/products"]);
            this.FileSystem.FileExists(Path.Combine("./output/customers", "customers.parquet")).Returns(true);
            this.FileSystem.FileExists(Path.Combine("./output/products", "products.parquet")).Returns(true);

            await this.S3Uploader.UploadAsync("my-bucket", "./output");

            await this.TransferUtility.Received(2).UploadAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>());
        }
    }
}
