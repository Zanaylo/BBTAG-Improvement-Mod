using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Settings;

public sealed class JsonFileStore<T>(IFileSystem fileSystem, string path, JsonTypeInfo<T> typeInfo) where T : class, new()
{
    public string Path => path;

    public T Load()
    {
        try
        {
            return Read();
        }
        catch (JsonException)
        {
            return new T();
        }
    }

    public T? TryLoad()
    {
        try
        {
            return Read();
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return null;
        }
    }

    private T Read()
    {
        if (!fileSystem.FileExists(path))
            return new T();

        return JsonSerializer.Deserialize(fileSystem.ReadAllText(path), typeInfo) ?? new T();
    }

    public void Save(T settings) => fileSystem.WriteAllText(path, JsonSerializer.Serialize(settings, typeInfo));
}
