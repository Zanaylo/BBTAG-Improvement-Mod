using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Archives;

namespace BBTAG_IM.Core.Mods;

public static class ModOpener
{
    public static ModFolder? TryOpen(string path, IFileSystem files)
    {
        try
        {
            return Open(path, files);
        }
        catch (InvalidModFolderException)
        {
            return null;
        }
    }

    public static ModFolder Open(string path, IFileSystem files) =>
        ModArchiveName.IsArchive(path) && files.FileExists(path) ? OpenArchive(path, files) : ModFolder.Inspect(path, files);

    private static ModFolder OpenArchive(string archive, IFileSystem files)
    {
        var name = Path.GetFileName(archive);

        try
        {
            if (!files.DirectoryExists(archive))
                throw new InvalidModFolderException($"{name} cannot be read as a mod here.");

            var mod = ModFolder.Inspect(UnpackedModRoot.In(archive, files), files) with { Name = name, ArchivePath = archive };
            return new ArchiveModCache(files).WithFilesCopiedOut(mod, archive);
        }
        catch (Exception exception) when (exception is ModArchiveException or IOException or UnauthorizedAccessException)
        {
            throw new InvalidModFolderException($"{name} could not be read: {exception.Message}");
        }
    }
}
