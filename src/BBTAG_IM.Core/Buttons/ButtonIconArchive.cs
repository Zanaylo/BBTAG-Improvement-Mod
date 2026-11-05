using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Buttons;

public sealed class ButtonIconArchive(IFileSystem fileSystem, IReadOnlyList<string> dataFolders, string cacheFolder)
{
    public const string FileName = "gr_johchu_cmn.pac";

    private const string Folder = "gr";

    private readonly Lock _writing = new();
    private bool _written;

    public string? Substitute(string? requestedPath)
    {
        if (!IsTheIconArchive(requestedPath) || Source() is not { } source)
            return null;

        var cached = Path.Combine(cacheFolder, FileName);

        lock (_writing)
        {
            if (_written && fileSystem.FileExists(cached))
                return cached;

            fileSystem.WriteAllBytes(cached, ButtonIcons.Patch(fileSystem.ReadAllBytes(source), ButtonIcons.ECell()));
            _written = true;
            return cached;
        }
    }

    private static bool IsTheIconArchive(string? requestedPath) =>
        requestedPath is not null
        && FileName.Equals(Path.GetFileName(requestedPath), StringComparison.OrdinalIgnoreCase)
        && Folder.Equals(Path.GetFileName(Path.GetDirectoryName(requestedPath)), StringComparison.OrdinalIgnoreCase);

    private string? Source() =>
        dataFolders.Select(folder => Path.Combine(folder, Folder, FileName)).FirstOrDefault(fileSystem.FileExists);
}
