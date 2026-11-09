using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Mods.Updates;

public static class RepositoryLayout
{
    private const char Separator = '/';
    private const string GitNamePrefix = ".git";

    private static readonly string[] DataFolders = [ModFolder.GameDataFolder, ModFolder.OverhaulDataFolder];

    public static IReadOnlyList<GitTreeFile> ModFiles(IReadOnlyList<GitTreeFile> tree)
    {
        var files = tree.Where(file => !IsGitFile(file.Path)).ToArray();
        var prefix = "";

        while (!HoldsMod(files, prefix))
        {
            var folders = files.Where(file => IsInside(file, prefix) && Relative(file, prefix).Contains(Separator))
                .Select(file => Relative(file, prefix).Split(Separator)[0])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (folders.Length != 1)
                throw new ModUpdateException($"The repository has no '{ModFolder.GameDataFolder}' folder, {ModFolder.GameExecutableName}, {ModFolder.TableExtension} table or {ModFolder.PluginExtension} to load.");

            prefix += folders[0] + Separator;
        }

        return [.. files.Where(file => IsInside(file, prefix)).Select(file => file with { Path = Relative(file, prefix) })];
    }

    private static bool HoldsMod(IEnumerable<GitTreeFile> files, string prefix) =>
        files.Where(file => IsInside(file, prefix)).Select(file => Relative(file, prefix)).Any(IsModFile);

    private static bool IsModFile(string path)
    {
        var parts = path.Split(Separator);

        if (parts.Length > 1)
            return DataFolders.Contains(parts[0], StringComparer.OrdinalIgnoreCase);

        return path.Equals(ModFolder.GameExecutableName, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(ModFolder.TableExtension, StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(ModFolder.PluginExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGitFile(string path) =>
        path.Split(Separator).Any(part => part.StartsWith(GitNamePrefix, StringComparison.OrdinalIgnoreCase));

    private static bool IsInside(GitTreeFile file, string prefix) => file.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static string Relative(GitTreeFile file, string prefix) => file.Path[prefix.Length..];
}
