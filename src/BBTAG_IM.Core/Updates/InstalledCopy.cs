using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Updates;

public enum Edition
{
    Player,
    Dev,
}

public sealed record InstalledCopy(Edition Edition, Version Version)
{
    public const string ModLoaderFileName = "BBTAG_Mod_Loader.exe";
    public const string PlayerLibraryFileName = "dinput8.dll";

    public static InstalledCopy? Detect(string folder, IFileSystem fileSystem) =>
        Read(folder, ModLoaderFileName, Edition.Dev, fileSystem) ?? Read(folder, PlayerLibraryFileName, Edition.Player, fileSystem);

    private static InstalledCopy? Read(string folder, string fileName, Edition edition, IFileSystem fileSystem)
    {
        var path = Path.Combine(folder, fileName);

        if (!fileSystem.FileExists(path) || fileSystem.FileVersion(path) is not { } version)
            return null;

        return new InstalledCopy(edition, version);
    }
}
