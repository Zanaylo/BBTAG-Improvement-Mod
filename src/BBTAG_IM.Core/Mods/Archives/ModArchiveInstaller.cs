using System.Globalization;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Core.Mods.Archives;

public sealed class ModArchiveInstaller(IFileSystem files, IArchiveUnpacker unpacker, string modsDirectory)
{
    public const string UnpackingPrefix = ".unpacking-";

    private const string StampFile = ".mod-archive";

    public IReadOnlyList<string> Pending()
    {
        if (!files.DirectoryExists(modsDirectory))
            return [];

        return files.EnumerateFiles(modsDirectory).Where(ModArchiveName.IsArchive).Where(archive => !IsUnpacked(archive)).ToArray();
    }

    public void InstallPending(Action<string> report)
    {
        foreach (var archive in Pending())
        {
            var fileName = Path.GetFileName(archive);
            report($"Unpacking {fileName}.");

            try
            {
                Install(archive);
                report($"{fileName} was unpacked.");
            }
            catch (ModArchiveException exception)
            {
                report(exception.Message);
            }
        }
    }

    public LaunchOptions WithArchivesInstalled(LaunchOptions options) =>
        options with { ModFolders = options.ModFolders.Select(InstalledFolder).ToArray() };

    public void Install(string archive)
    {
        var fileName = Path.GetFileName(archive);

        try
        {
            Unpack(archive);
        }
        catch (Exception exception) when (exception is ModArchiveException or IOException or UnauthorizedAccessException)
        {
            throw new ModArchiveException($"{fileName} could not be unpacked: {exception.Message}", exception);
        }
    }

    private string InstalledFolder(string path)
    {
        if (!ModArchiveName.IsArchive(path) || !files.FileExists(path))
            return path;

        if (!IsUnpacked(path))
            Install(path);

        return TargetOf(path);
    }

    private void Unpack(string archive)
    {
        var target = TargetOf(archive);

        if (files.DirectoryExists(target) && !files.FileExists(StampOf(target)))
            throw new ModArchiveException($"a folder named {Path.GetFileName(target)} is already there. Move it away to use the archive.");

        var work = Path.Combine(modsDirectory, UnpackingPrefix + Path.GetFileName(target));
        Remove(work);

        try
        {
            unpacker.Unpack(archive, work);
            MoveFiles(UnpackedModRoot.In(work, files), target);
            files.WriteAllText(StampOf(target), StampFor(archive));
        }
        finally
        {
            Remove(work);
        }
    }

    private void MoveFiles(string from, string to)
    {
        foreach (var file in files.EnumerateFilesRecursively(from).ToArray())
            files.MoveFile(file, Path.Combine(to, Path.GetRelativePath(from, file)));
    }

    private void Remove(string folder)
    {
        if (files.DirectoryExists(folder))
            files.DeleteDirectory(folder);
    }

    private bool IsUnpacked(string archive)
    {
        var stamp = StampOf(TargetOf(archive));
        return files.FileExists(stamp) && files.ReadAllText(stamp) == StampFor(archive);
    }

    private string TargetOf(string archive) => Path.Combine(modsDirectory, ModArchiveName.FolderOf(archive));

    private string StampFor(string archive) => files.LastWriteTimeUtc(archive).Ticks.ToString(CultureInfo.InvariantCulture);

    private static string StampOf(string target) => Path.Combine(target, StampFile);
}
