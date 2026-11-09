using System.Globalization;
using System.Text.Json;
using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Scripting;

public sealed record CommandDatabaseFile(string Name, string Path);

public sealed class CommandDatabaseChoice(IFileSystem fileSystem, string folder)
{
    public const string ChoiceFile = "command_db_choice.txt";
    public const string LibraryFolder = "command_dbs";

    private const string JsonExtension = ".json";

    public string DefaultPath => Path.Combine(folder, ScriptDatabaseFolder.CommandsFile);

    public string CurrentPath => Chosen() is { } chosen && fileSystem.FileExists(chosen) ? chosen : DefaultPath;

    public IReadOnlyList<CommandDatabaseFile> Available()
    {
        var library = Path.Combine(folder, LibraryFolder);
        var files = new List<CommandDatabaseFile>();

        if (fileSystem.FileExists(DefaultPath))
            files.Add(new CommandDatabaseFile(ScriptDatabaseFolder.CommandsFile, DefaultPath));

        if (fileSystem.DirectoryExists(library))
        {
            files.AddRange(fileSystem.EnumerateFiles(library)
                .Where(IsJson)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(ToFile));
        }

        var current = CurrentPath;

        if (fileSystem.FileExists(current) && !files.Any(file => IsSamePath(file.Path, current)))
            files.Add(ToFile(current));

        return files;
    }

    public void Choose(string path)
    {
        var choice = Path.Combine(folder, ChoiceFile);

        if (IsSamePath(path, DefaultPath))
        {
            if (fileSystem.FileExists(choice))
                fileSystem.DeleteFile(choice);

            return;
        }

        fileSystem.WriteAllText(choice, IsInside(path) ? Path.GetRelativePath(folder, path) : Path.GetFullPath(path));
    }

    public string Add(string source)
    {
        if (IsInside(source) && IsSamePath(Path.GetDirectoryName(source)!, Path.Combine(folder, LibraryFolder)))
            return source;

        var bytes = fileSystem.ReadAllBytes(source);
        RequireCommandList(bytes, source);
        var target = FreePath(Path.GetFileNameWithoutExtension(source));
        fileSystem.WriteAllBytes(target, bytes);
        return target;
    }

    private string? Chosen()
    {
        var choice = Path.Combine(folder, ChoiceFile);

        if (!fileSystem.FileExists(choice))
            return null;

        var saved = fileSystem.ReadAllText(choice).Trim();
        return saved.Length == 0 ? null : Path.GetFullPath(saved, folder);
    }

    private string FreePath(string name)
    {
        var library = Path.Combine(folder, LibraryFolder);
        var path = Path.Combine(library, name + JsonExtension);

        for (var copy = 2; fileSystem.FileExists(path); copy++)
            path = Path.Combine(library, string.Create(CultureInfo.InvariantCulture, $"{name} ({copy}){JsonExtension}"));

        return path;
    }

    private static void RequireCommandList(byte[] bytes, string source)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });

            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return;
        }
        catch (JsonException exception)
        {
            throw new InvalidScriptDatabaseException($"{Path.GetFileName(source)} is not valid JSON: {exception.Message}");
        }

        throw new InvalidScriptDatabaseException($"{Path.GetFileName(source)} is not a command list. It must map command numbers to their names and formats.");
    }

    private bool IsInside(string path) =>
        Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    private static CommandDatabaseFile ToFile(string path) => new(Path.GetFileNameWithoutExtension(path), path);

    private static bool IsJson(string path) => Path.GetExtension(path).Equals(JsonExtension, StringComparison.OrdinalIgnoreCase);

    private static bool IsSamePath(string path, string other) =>
        string.Equals(Path.GetFullPath(path), Path.GetFullPath(other), StringComparison.OrdinalIgnoreCase);
}
