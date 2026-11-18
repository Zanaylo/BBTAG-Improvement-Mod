using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Mods.Assets;

namespace BBTAG_IM.Core.Sprites;

public static class GameDataFolders
{
    public static IReadOnlyList<string> Of(string modFolder, string gameFolder, IFileSystem fileSystem)
    {
        var modData = EnabledMods.Of(modFolder, fileSystem).SelectMany(ReadableFoldersOf);
        var gameAssets = Path.Combine(gameFolder, EncryptedAssets.FolderName);

        return [.. modData, .. GameOnly(gameFolder), .. fileSystem.DirectoryExists(gameAssets) ? [gameAssets] : Array.Empty<string>()];
    }

    public static IReadOnlyList<string> GameOnly(string gameFolder) => [Path.Combine(gameFolder, ModFolder.GameDataFolder)];

    public static IEnumerable<string> EncryptedAmong(IEnumerable<string> dataFolders) =>
        dataFolders.Where(folder => Path.GetFileName(folder).Equals(EncryptedAssets.FolderName, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> ReadableFoldersOf(ModFolder mod) =>
        mod.DataRoots.Select(root => root.Path).Concat(mod.EncryptedAssetsPath is null ? [] : [mod.EncryptedAssetsPath]);
}
