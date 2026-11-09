using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Mods.Updates;

public sealed record ModFilePlan(IReadOnlyList<GitTreeFile> Downloads, IReadOnlyList<string> Deletions)
{
    public static ModFilePlan Create(
        IReadOnlyDictionary<string, string> previous,
        IReadOnlyList<GitTreeFile> remote,
        Func<string, bool> exists,
        Func<string, string> localSha)
    {
        var remotePaths = remote.Select(file => file.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var downloads = remote.Where(file => NeedsDownload(file, previous, exists, localSha)).ToArray();
        var deletions = previous.Keys.Where(path => !remotePaths.Contains(path) && exists(path)).ToArray();

        return new ModFilePlan(downloads, deletions);
    }

    private static bool NeedsDownload(GitTreeFile file, IReadOnlyDictionary<string, string> previous, Func<string, bool> exists, Func<string, string> localSha)
    {
        if (!exists(file.Path))
            return true;

        if (previous.TryGetValue(file.Path, out var sent))
            return sent != file.Sha;

        return localSha(file.Path) != file.Sha;
    }
}
