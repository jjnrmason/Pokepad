namespace Pokepad.DataGeneration.Generators;

public interface IFileSystem
{
    string[] GetDirectories(string path);
    bool FileExists(string path);
}
