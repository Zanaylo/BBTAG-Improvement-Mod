using System.Text.Json;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Mods.Updates;

public sealed class ModSourceStore(IFileSystem files)
{
    public const string FileName = ".mod-source.json";

    public ModSource? Load(string modFolder)
    {
        var path = PathOf(modFolder);

        if (!files.FileExists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize(files.ReadAllText(path), SettingsJson.Default.ModSource);
        }
        catch (JsonException exception)
        {
            throw new ModUpdateException($"{path} is damaged. Delete it and link the mod again.", exception);
        }
    }

    public void Save(string modFolder, ModSource source) =>
        files.WriteAllText(PathOf(modFolder), JsonSerializer.Serialize(source, SettingsJson.Default.ModSource));

    public void Link(string modFolder, GitHubRepository repository)
    {
        var current = Load(modFolder);

        if (current is not null && GitHubRepository.TryParse(current.Repository) is { } linked && linked.IsSame(repository))
            return;

        Save(modFolder, new ModSource { Repository = repository.Url });
    }

    public void Unlink(string modFolder)
    {
        if (files.FileExists(PathOf(modFolder)))
            files.DeleteFile(PathOf(modFolder));
    }

    public string AddFolder(string modsDirectory, GitHubRepository repository)
    {
        var folder = Path.Combine(modsDirectory, repository.Name);

        if (files.DirectoryExists(folder) && !IsLinkedTo(folder, repository))
            throw new ModUpdateException($"A folder named {repository.Name} is already in the mods folder. Select that mod and save the link there instead.");

        Link(folder, repository);
        return folder;
    }

    public IReadOnlyList<string> LinkedFolders(string modsDirectory)
    {
        if (!files.DirectoryExists(modsDirectory))
            return [];

        return files.EnumerateDirectories(modsDirectory)
            .Where(folder => files.FileExists(PathOf(folder)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private bool IsLinkedTo(string folder, GitHubRepository repository) =>
        Load(folder) is { } source && GitHubRepository.TryParse(source.Repository) is { } linked && linked.IsSame(repository);

    private static string PathOf(string modFolder) => Path.Combine(modFolder, FileName);
}
