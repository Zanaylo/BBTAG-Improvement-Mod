using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods.Archives;

public static class UnpackedModRoot
{
    public static string In(string folder, IFileSystem files)
    {
        var root = folder;

        while (ModFolder.TryInspect(root, files) is null)
        {
            var folders = files.EnumerateDirectories(root).ToArray();

            if (folders.Length != 1)
                throw new ModArchiveException($"it has no '{ModFolder.GameDataFolder}' folder, {ModFolder.TableExtension} table or {ModFolder.PluginExtension} to load.");

            root = folders[0];
        }

        return root;
    }
}
