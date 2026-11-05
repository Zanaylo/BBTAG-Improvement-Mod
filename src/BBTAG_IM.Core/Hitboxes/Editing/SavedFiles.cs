namespace BBTAG_IM.Core.Hitboxes.Editing;

public static class SavedFiles
{
    public static string? CommonFolder(IReadOnlyCollection<string> files)
    {
        var folders = files.Select(Path.GetDirectoryName).OfType<string>().ToArray();
        return folders.Length == 0 ? null : folders.Aggregate(FolderHolding);
    }

    private static string FolderHolding(string folder, string other)
    {
        var common = folder;

        while (!IsInside(other, common) && Path.GetDirectoryName(common) is { } parent)
            common = parent;

        return common;
    }

    private static bool IsInside(string path, string folder) =>
        path.Equals(folder, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
