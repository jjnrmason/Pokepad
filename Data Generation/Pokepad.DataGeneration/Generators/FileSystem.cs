namespace Pokepad.DataGeneration.Generators;

public class FileSystem : IFileSystem
{
    public string[] GetDirectories(string path) => Directory.GetDirectories(path);

    public bool FileExists(string path) => File.Exists(path);
}
