namespace BBTAG_IM.Core.Mods.Archives;

public static class ModArchiveName
{
    private static readonly string[] Extensions = [".tar.gz", ".tgz", ".tar", ".zip", ".rar", ".7z"];

    public static bool IsArchive(string path) => ExtensionOf(path) is not null;

    public static string FolderOf(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName[..^(ExtensionOf(path)?.Length ?? 0)];
    }

    private static string? ExtensionOf(string path) =>
        Extensions.FirstOrDefault(extension => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
}
