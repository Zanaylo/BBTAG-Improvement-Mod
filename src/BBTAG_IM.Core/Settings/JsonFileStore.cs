using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Settings;

public sealed class JsonFileStore<T>(IFileSystem fileSystem, string path, JsonTypeInfo<T> typeInfo) where T : class, new()
{
    public string Path => path;

    public T Load() => Read();

    public T? TryLoad()
    {
        try
        {
            return Read();
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Save(T settings)
    {
        Read();
        fileSystem.WriteAllText(path, JsonSerializer.Serialize(settings, typeInfo));
    }

    private T Read()
    {
        if (!fileSystem.FileExists(path))
            return new T();

        try
        {
            return JsonSerializer.Deserialize(fileSystem.ReadAllText(path), typeInfo) ?? new T();
        }
        catch (JsonException exception)
        {
            throw new InvalidSettingsFileException(path, exception.LineNumber);
        }
    }
}
