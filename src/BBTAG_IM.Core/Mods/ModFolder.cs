using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods;

public sealed record ModDataRoot(string Path, string MountedAs);

public sealed record ModFolder(string Name, string RootPath, IReadOnlyList<ModDataRoot> DataRoots, string? ExecutablePath)
{
    public const string GameDataFolder = "data";
    public const string OverhaulDataFolder = "data2";
    public const string GameExecutableName = "BBTAG.exe";

    private static readonly string[] DataFolders = [GameDataFolder, OverhaulDataFolder];

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

        if (dataRoots.Length == 0 && executablePath is null)
            throw new InvalidModFolderException(
                $"{root} has no '{GameDataFolder}' or '{OverhaulDataFolder}' folder and no {GameExecutableName}, so there is nothing to load.");

        return new ModFolder(Path.GetFileName(root), root, dataRoots, executablePath);
    }
}
