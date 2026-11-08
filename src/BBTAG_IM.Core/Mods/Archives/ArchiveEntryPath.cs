namespace BBTAG_IM.Core.Mods.Archives;

public static class ArchiveEntryPath
{
    private const char ArchiveSeparator = '/';

    public static string Inside(string folder, string key)
    {
        var relative = key.Replace(ArchiveSeparator, Path.DirectorySeparatorChar);
        var root = Path.TrimEndingDirectorySeparator(folder) + Path.DirectorySeparatorChar;

        if (Path.IsPathRooted(relative))
            throw Escaping(key);

        var path = Path.GetFullPath(relative, root);

        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw Escaping(key);

        return path;
    }

    private static ModArchiveException Escaping(string key) => new($"{key} points outside the mod folder.");
}
