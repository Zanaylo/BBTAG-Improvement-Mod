using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Sprites;

public static class GameDataFolders
{
    public static IReadOnlyList<string> Of(string modFolder, IFileSystem fileSystem)
    {
        var modData = EnabledMods.Of(modFolder, fileSystem).SelectMany(mod => mod.DataRoots).Select(root => root.Path);

        return [.. modData, Path.Combine(EnabledMods.GameFolderOf(modFolder), ModFolder.GameDataFolder)];
    }
}
