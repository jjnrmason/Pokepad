using Amazon.S3.Transfer;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using Pokepad.DataGeneration.Generators;

namespace Pokepad.DataGeneration.Tests.S3Uploader.WhenWorkingWithTheS3Uploader;

public class S3UploaderTestBase
{
    protected Generators.S3Uploader S3Uploader { get; private set; } = null!;
    protected ITransferUtility TransferUtility { get; private set; } = null!;
    protected IFileSystem FileSystem { get; private set; } = null!;

    [OneTimeSetUp]
    public virtual void OneTimeSetUp()
    {
        this.TransferUtility = Substitute.For<ITransferUtility>();
        this.FileSystem = Substitute.For<IFileSystem>();
        this.S3Uploader = new Generators.S3Uploader(this.TransferUtility, this.FileSystem);
    }

    [SetUp]
    public virtual void SetUp()
    {
        this.TransferUtility.ClearSubstitute();
        this.FileSystem.ClearSubstitute();
    }
}
