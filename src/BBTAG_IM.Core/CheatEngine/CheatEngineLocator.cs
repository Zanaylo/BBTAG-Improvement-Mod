using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.CheatEngine;

public sealed class CheatEngineLocator(IFileSystem fileSystem)
{
    public const string ExecutableName = "cheatengine-x86_64.exe";

    private const string InstallFolderPrefix = "Cheat Engine";

    public string? Locate(string? configuredPath, IEnumerable<string> programFilesDirectories)
    {
        var configured = FromConfiguredPath(configuredPath);

        if (configured is not null)
            return configured;

        return programFilesDirectories
            .Where(fileSystem.DirectoryExists)
            .SelectMany(fileSystem.EnumerateDirectories)
            .Where(IsInstallFolder)
            .Select(folder => Path.Combine(folder, ExecutableName))
            .Where(fileSystem.FileExists)
            .OrderByDescending(fileSystem.FileVersion)
            .FirstOrDefault();
    }

    private string? FromConfiguredPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        if (fileSystem.FileExists(configuredPath))
            return configuredPath;

        var inFolder = Path.Combine(configuredPath, ExecutableName);
        return fileSystem.FileExists(inFolder) ? inFolder : null;
    }

    private static bool IsInstallFolder(string folder) =>
        Path.GetFileName(folder).StartsWith(InstallFolderPrefix, StringComparison.OrdinalIgnoreCase);
}
