using Amazon.S3.Transfer;

namespace Pokepad.DataGeneration.Generators;

public class S3Uploader
{
    private readonly ITransferUtility _transferUtility;
    private readonly IFileSystem _fileSystem;

    public S3Uploader(ITransferUtility transferUtility, IFileSystem fileSystem)
    {
        _transferUtility = transferUtility;
        _fileSystem = fileSystem;
    }

    public async Task UploadAsync(string bucketName, string outputDirectory)
    {
        foreach (var directory in _fileSystem.GetDirectories(outputDirectory))
        {
            var entityName = Path.GetFileName(directory);
            var csvPath = Path.Combine(directory, $"{entityName}.parquet");

            if (!_fileSystem.FileExists(csvPath))
            {
                continue;
            }

            var s3Key = $"gold/{entityName}/{entityName}.parquet";
            await _transferUtility.UploadAsync(csvPath, bucketName, s3Key);
            Console.WriteLine($"  Uploaded: s3://{bucketName}/{s3Key}");
        }
    }
}
