using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Mods.Archives;

public static class UnpackedModRoot
{
    public static string In(string folder, IFileSystem files)
    {
        var root = folder;

        while (!ModFolder.Inspect(root, files).HasGameFiles)
        {
            var folders = files.EnumerateDirectories(root).ToArray();

            if (folders.Length != 1)
                return folder;

            root = folders[0];
        }

        return root;
    }
}
