using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Mods.Updates;

public sealed class ModUpdater(IGitHubApi gitHub, IFileSystem files, IArchiveUnpacker unpacker)
{
    private const int ShortCommitLength = 7;
    private const string DownloadSuffix = ".download";
    private const string ArchiveFolder = "archive";
    private const string UnpackedFolder = "files";
    private const char RepositorySeparator = '/';
    private const long Megabyte = 1024 * 1024;

    private readonly ModSourceStore _store = new(files);

    public async Task<ModUpdateResult> UpdateAsync(string modFolder, IProgress<string> progress, CancellationToken cancellation)
    {
        var modName = Path.GetFileName(Path.TrimEndingDirectorySeparator(modFolder));
        var source = _store.Load(modFolder) ?? throw new ModUpdateException($"{modName} has no GitHub link.");
        var repository = GitHubRepository.TryParse(source.Repository) ?? throw new ModUpdateException($"{modName} has a link that is not a GitHub repository: {source.Repository}");
        progress.Report($"Checking {repository.Url} for a new version of {modName}.");

        var release = await gitHub.LatestReleaseAsync(repository, cancellation);
        var archive = release?.Assets.FirstOrDefault(asset => ModArchiveName.IsArchive(asset.Name));
        var target = new UpdateTarget(modFolder, modName, source, repository, progress, cancellation);

        return archive is null ? await FromNewestCommitAsync(target) : await FromReleaseAsync(target, release!, archive);
    }

    private async Task<ModUpdateResult> FromNewestCommitAsync(UpdateTarget target)
    {
        var commit = await gitHub.HeadCommitAsync(target.Repository, target.Cancellation);
        var version = commit[..Math.Min(ShortCommitLength, commit.Length)];

        if (target.Source.Version == commit)
            return new ModUpdateResult(target.ModName, ModUpdateOutcome.UpToDate, version, 0, 0);

        var tree = RepositoryLayout.ModFiles(await gitHub.TreeAsync(target.Repository, commit, target.Cancellation));
        target.Progress.Report($"{target.ModName}: comparing {tree.Count} files with {version}.");
        var plan = ModFilePlan.Create(target.Source.Files, tree, path => files.FileExists(LocalPath(target, path)), path => LocalSha(target, path));

        try
        {
            await DownloadAllAsync(target, commit, plan.Downloads);
            Delete(target, plan.Deletions);
            ForgetMissing(target.Source, tree);
            target.Source.Version = commit;
        }
        finally
        {
            _store.Save(target.Folder, target.Source);
        }

        return new ModUpdateResult(target.ModName, ModUpdateOutcome.Updated, version, plan.Downloads.Count, plan.Deletions.Count);
    }

    private async Task DownloadAllAsync(UpdateTarget target, string commit, IReadOnlyList<GitTreeFile> downloads)
    {
        for (var index = 0; index < downloads.Count; index++)
        {
            var file = downloads[index];
            target.Progress.Report($"{target.ModName}: downloading {index + 1} of {downloads.Count}, {file.Path}.");
            await DownloadAsync(target, target.Repository.RawFile(commit, file.Path), LocalPath(target, file.Path), file.Sha);
            target.Source.Files[file.Path] = file.Sha;
        }
    }

    private async Task DownloadAsync(UpdateTarget target, Uri address, string path, string expectedSha)
    {
        var download = path + DownloadSuffix;

        try
        {
            await using (var stream = files.CreateFile(download))
            {
                await gitHub.DownloadAsync(address, stream, new Progress<long>(), target.Cancellation);

                if (GitBlob.Sha(stream) != expectedSha)
                    throw new ModUpdateException($"{Path.GetFileName(path)} arrived different from the repository. Try again.");
            }

            files.MoveFile(download, path);
        }
        finally
        {
            if (files.FileExists(download))
                files.DeleteFile(download);
        }
    }

    private async Task<ModUpdateResult> FromReleaseAsync(UpdateTarget target, GitHubRelease release, GitHubAsset archive)
    {
        var version = $"{release.Tag} {archive.Id} {archive.UpdatedAt:O}";

        if (target.Source.Version == version)
            return new ModUpdateResult(target.ModName, ModUpdateOutcome.UpToDate, release.Tag, 0, 0);

        var work = Path.Combine(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(target.Folder))!, ModArchiveInstaller.UnpackingPrefix + target.ModName);
        RemoveFolder(work);

        try
        {
            var archivePath = Path.Combine(work, ArchiveFolder, archive.Name);
            await DownloadArchiveAsync(target, archive, archivePath);
            var unpacked = Path.Combine(work, UnpackedFolder);
            target.Progress.Report($"{target.ModName}: unpacking {archive.Name}.");
            unpacker.Unpack(archivePath, unpacked);
            return Install(target, release.Tag, version, UnpackedModRoot.In(unpacked, files));
        }
        catch (ModArchiveException exception)
        {
            throw new ModUpdateException($"{archive.Name} could not be unpacked: {exception.Message}", exception);
        }
        finally
        {
            RemoveFolder(work);
        }
    }

    private async Task DownloadArchiveAsync(UpdateTarget target, GitHubAsset archive, string path)
    {
        var total = Math.Max(archive.Size, 1) / (double)Megabyte;
        var shown = -1L;
        var progress = new Progress<long>(bytes =>
        {
            var megabytes = bytes / Megabyte;

            if (megabytes == shown)
                return;

            shown = megabytes;
            target.Progress.Report($"{target.ModName}: downloading {archive.Name}, {megabytes} of {total:0} MB.");
        });

        await using var stream = files.CreateFile(path);
        await gitHub.DownloadAsync(archive.DownloadUrl, stream, progress, target.Cancellation);
    }

    private ModUpdateResult Install(UpdateTarget target, string tag, string version, string root)
    {
        var unpacked = files.EnumerateFilesRecursively(root).ToArray();
        var paths = unpacked.Select(file => Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, RepositorySeparator)).ToArray();
        var kept = paths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var deletions = target.Source.Files.Keys.Where(path => !kept.Contains(path) && files.FileExists(LocalPath(target, path))).ToArray();

        for (var index = 0; index < unpacked.Length; index++)
            files.MoveFile(unpacked[index], LocalPath(target, paths[index]));

        Delete(target, deletions);
        target.Source.Files.Clear();

        foreach (var path in paths)
            target.Source.Files[path] = "";

        target.Source.Version = version;
        _store.Save(target.Folder, target.Source);
        return new ModUpdateResult(target.ModName, ModUpdateOutcome.Updated, tag, paths.Length, deletions.Length);
    }

    private void Delete(UpdateTarget target, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            files.DeleteFile(LocalPath(target, path));
            target.Source.Files.Remove(path);
        }
    }

    private static void ForgetMissing(ModSource source, IReadOnlyList<GitTreeFile> tree)
    {
        var current = tree.Select(file => file.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var path in source.Files.Keys.Where(path => !current.Contains(path)).ToArray())
            source.Files.Remove(path);
    }

    private string LocalSha(UpdateTarget target, string path)
    {
        using var stream = files.OpenRead(LocalPath(target, path));
        return GitBlob.Sha(stream);
    }

    private void RemoveFolder(string folder)
    {
        if (files.DirectoryExists(folder))
            files.DeleteDirectory(folder);
    }

    private static string LocalPath(UpdateTarget target, string path) =>
        ArchiveEntryPath.Inside(target.Folder, path);

    private sealed record UpdateTarget(
        string Folder,
        string ModName,
        ModSource Source,
        GitHubRepository Repository,
        IProgress<string> Progress,
        CancellationToken Cancellation);
}
