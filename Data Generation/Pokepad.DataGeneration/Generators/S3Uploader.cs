using Amazon.S3;
using Amazon.S3.Transfer;

namespace Pokepad.DataGeneration.Generators;

public static class S3Uploader
{
    public static async Task UploadAsync(string bucketName, string outputDirectory)
    {
        using var s3Client = new AmazonS3Client();
        using var transferUtility = new TransferUtility(s3Client);

        foreach (var directory in Directory.GetDirectories(outputDirectory))
        {
            var entityName = Path.GetFileName(directory);
            var csvPath = Path.Combine(directory, $"{entityName}.parquet");

            if (!File.Exists(csvPath))
            {
                continue;
            }

            var s3Key = $"gold/{entityName}/{entityName}.parquet";
            await transferUtility.UploadAsync(csvPath, bucketName, s3Key);
            Console.WriteLine($"  Uploaded: s3://{bucketName}/{s3Key}");
        }
    }
}
