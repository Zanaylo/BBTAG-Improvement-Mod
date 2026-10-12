using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods;

public sealed record ModDataRoot(string Path, string MountedAs);

public sealed record ModFolder(
    string Name,
    string RootPath,
    IReadOnlyList<ModDataRoot> DataRoots,
    string? ExecutablePath,
    string? TablePath,
    IReadOnlyList<string> PluginPaths)
{
    public const string GameDataFolder = "data";
    public const string OverhaulDataFolder = "data2";
    public const string GameExecutableName = "BBTAG.exe";
    public const string TableExtension = ".CT";
    public const string PluginExtension = ".dll";

    private static readonly string[] DataFolders = [GameDataFolder, OverhaulDataFolder];
    private static readonly string PreferredTableName = Path.GetFileNameWithoutExtension(GameExecutableName) + TableExtension;

    public static ModFolder Inspect(string path, IFileSystem fileSystem)
    {
        if (!fileSystem.DirectoryExists(path))
            throw new InvalidModFolderException($"Mod folder not found: {path}");

        var root = Path.TrimEndingDirectorySeparator(path);
        var dataRoots = DataFolders
            .Select(folder => Path.Combine(root, folder))
            .Where(fileSystem.DirectoryExists)
            .Select(dataPath => new ModDataRoot(dataPath, GameDataFolder))
            .ToArray();
        var executable = Path.Combine(root, GameExecutableName);
        var executablePath = fileSystem.FileExists(executable) ? executable : null;
        var files = fileSystem.EnumerateFiles(root).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var tablePath = FindTable(files);
        var pluginPaths = files.Where(file => HasExtension(file, PluginExtension)).ToArray();

        if (dataRoots.Length == 0 && executablePath is null && tablePath is null && pluginPaths.Length == 0)
            throw new InvalidModFolderException(
                $"{root} has no '{GameDataFolder}' or '{OverhaulDataFolder}' folder, no {GameExecutableName}, no {TableExtension} table and no {PluginExtension}, so there is nothing to load.");

        return new ModFolder(Path.GetFileName(root), root, dataRoots, executablePath, tablePath, pluginPaths);
    }

    private static string? FindTable(IReadOnlyList<string> files)
    {
        var tables = files.Where(file => HasExtension(file, TableExtension)).ToArray();
        return tables.FirstOrDefault(IsPreferredTable) ?? tables.FirstOrDefault();
    }

    private static bool HasExtension(string file, string extension) =>
        Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase);

    private static bool IsPreferredTable(string file) =>
        Path.GetFileName(file).Equals(PreferredTableName, StringComparison.OrdinalIgnoreCase);
}
