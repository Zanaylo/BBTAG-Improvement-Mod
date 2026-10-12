using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Steam;

namespace BBTAG_IM.Core.Launch;

public sealed class GameLocator(IFileSystem fileSystem, ISteamInstallation steam)
{
    public const string SteamAppId = "702890";

    private const string SteamAppsFolder = "steamapps";
    private const string CommonFolder = "common";
    private const string LibraryFoldersFile = "libraryfolders.vdf";

    public string Locate(string? requestedDirectory, string launcherDirectory)
    {
        if (requestedDirectory is not null)
            return ContainsGame(requestedDirectory)
                ? Path.TrimEndingDirectorySeparator(requestedDirectory)
                : throw new GameNotFoundException($"{ModFolder.GameExecutableName} was not found in {requestedDirectory}.");

        if (ContainsGame(launcherDirectory))
            return launcherDirectory;

        return FromSteam() ?? throw new GameNotFoundException(
            "BlazBlue Cross Tag Battle was not found in your Steam libraries. Pass its folder with --game \"<folder>\".");
    }

    private bool ContainsGame(string directory) => fileSystem.FileExists(Path.Combine(directory, ModFolder.GameExecutableName));

    private string? FromSteam()
    {
        var steamPath = steam.SteamPath;

        if (steamPath is null)
            return null;

        return LibrariesOf(steamPath).Select(GameInLibrary).FirstOrDefault(directory => directory is not null);
    }

    private IEnumerable<string> LibrariesOf(string steamPath)
    {
        var libraryFolders = Path.Combine(steamPath, SteamAppsFolder, LibraryFoldersFile);
        var listed = fileSystem.FileExists(libraryFolders)
            ? SteamLibraryFolders.ParsePaths(fileSystem.ReadAllText(libraryFolders))
            : [];

        return listed.Prepend(steamPath).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private string? GameInLibrary(string library)
    {
        var manifest = Path.Combine(library, SteamAppsFolder, $"appmanifest_{SteamAppId}.acf");

        if (!fileSystem.FileExists(manifest))
            return null;

        var installDirectory = SteamLibraryFolders.ParseInstallDirectory(fileSystem.ReadAllText(manifest));

        if (installDirectory is null)
            return null;

        var gameDirectory = Path.Combine(library, SteamAppsFolder, CommonFolder, installDirectory);
        return ContainsGame(gameDirectory) ? gameDirectory : null;
    }
}
