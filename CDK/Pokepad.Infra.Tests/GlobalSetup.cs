using NUnit.Framework;

[SetUpFixture]
public class GlobalSetup
{
    private string? _previousDirectory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _previousDirectory = System.Environment.CurrentDirectory;
        var cdkRoot = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
        System.Environment.CurrentDirectory = cdkRoot;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_previousDirectory is not null)
            System.Environment.CurrentDirectory = _previousDirectory;
    }
}
