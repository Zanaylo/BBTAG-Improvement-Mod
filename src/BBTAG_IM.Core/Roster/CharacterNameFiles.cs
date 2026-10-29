using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Roster;

public sealed class CharacterNameFiles(
    IFileSystem fileSystem,
    IReadOnlyList<string> dataFolders,
    IReadOnlyList<KeyValuePair<string, string>> entries,
    string cacheFolder)
{
    private const string LocalizeFolder = "localize";
    private const string ListSuffix = "_idlist.txt";
    private const string SharedList = "cmn_idlist.txt";

    private readonly HashSet<string> _written = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _writing = new();

    public string? Substitute(string? requestedPath)
    {
        if (entries.Count == 0 || requestedPath is null || !IsLanguageList(requestedPath))
            return null;

        var fileName = Path.GetFileName(requestedPath);

        if (Source(fileName) is not { } source)
            return null;

        var cached = Path.Combine(cacheFolder, fileName);

        lock (_writing)
        {
            if (_written.Contains(fileName) && fileSystem.FileExists(cached))
                return cached;

            fileSystem.WriteAllBytes(cached, IdList.Insert(fileSystem.ReadAllBytes(source), entries));
            _written.Add(fileName);
            return cached;
        }
    }

    private static bool IsLanguageList(string requestedPath)
    {
        var fileName = Path.GetFileName(requestedPath);
        var folder = Path.GetFileName(Path.GetDirectoryName(requestedPath));

        return LocalizeFolder.Equals(folder, StringComparison.OrdinalIgnoreCase)
            && fileName.EndsWith(ListSuffix, StringComparison.OrdinalIgnoreCase)
            && !SharedList.Equals(fileName, StringComparison.OrdinalIgnoreCase);
    }

    private string? Source(string fileName) =>
        dataFolders.Select(folder => Path.Combine(folder, LocalizeFolder, fileName)).FirstOrDefault(fileSystem.FileExists);
}
