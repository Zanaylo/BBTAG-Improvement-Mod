using System.IO.Compression;
using System.Text;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Mods.Updates;
using BBTAG_IM.Core.Tests.Support;
using BBTAG_IM.Core.Updates;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods.Updates;

public sealed class ModUpdaterTests : IDisposable
{
    private const string Commit = "f965cea7ae5b5529533c73d7cdf8e4b02e29818c";
    private const string OldCommit = "856849d000000000000000000000000000000000";

    private static readonly GitHubRepository Repository = new("owner", "mod");

    private readonly string _mods = Path.Combine(Path.GetTempPath(), "bbtag-updates-" + Guid.NewGuid().ToString("N"));
    private readonly string _folder;
    private readonly IGitHubApi _gitHubMock = Substitute.For<IGitHubApi>();
    private readonly Dictionary<Uri, byte[]> _downloads = [];
    private readonly List<string> _messages = [];

    public ModUpdaterTests()
    {
        _folder = Path.Combine(_mods, "OverHaul");
        Directory.CreateDirectory(_folder);
        _gitHubMock.LatestReleaseAsync(Repository, Arg.Any<CancellationToken>()).Returns((GitHubRelease?)null);
        _gitHubMock.HeadCommitAsync(Repository, Arg.Any<CancellationToken>()).Returns(Commit);
        _gitHubMock.DownloadAsync(Arg.Any<Uri>(), Arg.Any<Stream>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Stream>().WriteAsync(_downloads[call.Arg<Uri>()]).AsTask());
    }

    public void Dispose() => Directory.Delete(_mods, recursive: true);

    public ModUpdater CreateModUpdater() => new(_gitHubMock, new PhysicalFileSystem(), new ArchiveUnpacker());

    private ModSourceStore Store { get; } = new(new PhysicalFileSystem());

    private Task<ModUpdateResult> UpdateAsync() =>
        CreateModUpdater().UpdateAsync(_folder, new SynchronousProgress<string>(_messages.Add), CancellationToken.None);

    private void Link(string? version = null, params (string Path, string Sha)[] files)
    {
        ModSource source = new() { Repository = Repository.Url, Version = version };

        foreach (var (path, sha) in files)
            source.Files[path] = sha;

        Store.Save(_folder, source);
    }

    private void Local(string path, string text)
    {
        string full = Path.Combine(_folder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private string ReadLocal(string path) => File.ReadAllText(Path.Combine(_folder, path));

    private bool HasLocal(string path) => File.Exists(Path.Combine(_folder, path));

    private GitTreeFile Remote(string path, string text, string? sha = null)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        _downloads[Repository.RawFile(Commit, path)] = bytes;
        return new GitTreeFile(path, sha ?? GitBlob.Sha(new MemoryStream(bytes)), bytes.Length);
    }

    private void Tree(params GitTreeFile[] files) =>
        _gitHubMock.TreeAsync(Repository, Commit, Arg.Any<CancellationToken>()).Returns(files);

    private static string ShaOf(string text) => GitBlob.Sha(new MemoryStream(Encoding.UTF8.GetBytes(text)));

    private void Release(string tag, params GitHubAsset[] assets) =>
        _gitHubMock.LatestReleaseAsync(Repository, Arg.Any<CancellationToken>())
            .Returns(new GitHubRelease(tag, new Uri("https://github.com/owner/mod/releases/tag/" + tag), assets));

    private GitHubAsset ZipAsset(long id, params (string Path, string Text)[] entries)
    {
        using MemoryStream zip = new();

        using (ZipArchive archive = new(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, text) in entries)
            {
                using StreamWriter writer = new(archive.CreateEntry(path).Open());
                writer.Write(text);
            }
        }

        Uri address = new($"https://github.com/owner/mod/releases/download/v1/Mod-{id}.zip");
        _downloads[address] = zip.ToArray();
        return new GitHubAsset(id, $"Mod-{id}.zip", zip.Length, address, DateTimeOffset.Parse("2026-11-09T08:20:54Z"));
    }

    [Fact]
    public async Task UpdateAsync_FolderWithoutALink_Throws()
    {
        // Act
        Exception? exception = await Record.ExceptionAsync(UpdateAsync);

        // Assert
        Assert.IsType<ModUpdateException>(exception);
    }

    [Fact]
    public async Task UpdateAsync_SameCommitAsLastTime_IsUpToDateWithoutReadingTheTree()
    {
        // Arrange
        Link(Commit);

        // Act
        ModUpdateResult result = await UpdateAsync();

        // Assert
        Assert.Equal(ModUpdateOutcome.UpToDate, result.Outcome);
        await _gitHubMock.DidNotReceive().TreeAsync(Arg.Any<GitHubRepository>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NewCommit_DownloadsChangedFilesAndDeletesRemovedOnes()
    {
        // Arrange
        Local("BBTAG.exe", "old exe");
        Local("BBTAG.CT", "table");
        Local("data/same.pac", "same");
        Link(OldCommit, ("BBTAG.exe", ShaOf("old exe")), ("BBTAG.CT", ShaOf("table")), ("data/same.pac", ShaOf("same")));
        Tree(Remote("BBTAG.exe", "baked exe"), Remote("data/same.pac", "same"), Remote("data/new.pac", "new"));

        // Act
        ModUpdateResult result = await UpdateAsync();

        // Assert
        Assert.Equal(new ModUpdateResult("OverHaul", ModUpdateOutcome.Updated, "f965cea", 2, 1), result);
        Assert.Equal("baked exe", ReadLocal("BBTAG.exe"));
        Assert.Equal("new", ReadLocal("data/new.pac"));
        Assert.False(HasLocal("BBTAG.CT"));
    }

    [Fact]
    public async Task UpdateAsync_NewCommit_RemembersTheCommitAndItsFiles()
    {
        // Arrange
        Link();
        GitTreeFile exe = Remote("BBTAG.exe", "exe");
        Tree(exe);

        // Act
        await UpdateAsync();

        // Assert
        ModSource source = Store.Load(_folder)!;
        Assert.Equal(Commit, source.Version);
        Assert.Equal(exe.Sha, source.Files["BBTAG.exe"]);
    }

    [Fact]
    public async Task UpdateAsync_FirstTimeWithTheFilesAlreadyThere_DownloadsOnlyTheDifferentOnes()
    {
        // Arrange
        Local("data/same.pac", "same");
        Local("BBTAG.exe", "old exe");
        Link();
        Tree(Remote("data/same.pac", "same"), Remote("BBTAG.exe", "baked exe"));

        // Act
        ModUpdateResult result = await UpdateAsync();

        // Assert
        Assert.Equal(1, result.Downloaded);
        await _gitHubMock.Received(1).DownloadAsync(Arg.Any<Uri>(), Arg.Any<Stream>(), Arg.Any<IProgress<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_DownloadDoesNotMatchTheRepository_ThrowsAndKeepsTheFile()
    {
        // Arrange
        Local("BBTAG.exe", "old exe");
        Link();
        Tree(Remote("BBTAG.exe", "baked exe", sha: ShaOf("something else")));

        // Act
        Exception? exception = await Record.ExceptionAsync(UpdateAsync);

        // Assert
        Assert.IsType<ModUpdateException>(exception);
        Assert.Equal("old exe", ReadLocal("BBTAG.exe"));
        Assert.Empty(Directory.GetFiles(_folder, "*.download", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task UpdateAsync_FailsHalfway_RemembersTheFilesThatArrivedButNotTheCommit()
    {
        // Arrange
        Link(OldCommit);
        GitTreeFile good = Remote("a.dll", "good");
        Tree(good, Remote("b.dll", "bad", sha: ShaOf("other")));

        // Act
        await Record.ExceptionAsync(UpdateAsync);

        // Assert
        ModSource source = Store.Load(_folder)!;
        Assert.Equal(OldCommit, source.Version);
        Assert.Equal(good.Sha, source.Files["a.dll"]);
    }

    [Fact]
    public async Task UpdateAsync_ReleaseWithAZip_UnpacksItIntoTheFolder()
    {
        // Arrange
        Link();
        Release("v1", ZipAsset(5, ("Mod/data/a.pac", "a"), ("Mod/Table.CT", "table")));

        // Act
        ModUpdateResult result = await UpdateAsync();

        // Assert
        Assert.Equal(new ModUpdateResult("OverHaul", ModUpdateOutcome.Updated, "v1", 2, 0), result);
        Assert.Equal("a", ReadLocal("data/a.pac"));
        Assert.Equal("table", ReadLocal("Table.CT"));
        await _gitHubMock.DidNotReceive().HeadCommitAsync(Arg.Any<GitHubRepository>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_NewerZip_DeletesFilesTheOldZipHadAndTheNewOneLacks()
    {
        // Arrange
        Release("v1", ZipAsset(5, ("data/a.pac", "a"), ("Table.CT", "table")));
        Link();
        await UpdateAsync();
        Release("v2", ZipAsset(6, ("data/a.pac", "a2")));

        // Act
        ModUpdateResult result = await UpdateAsync();

        // Assert
        Assert.Equal(1, result.Deleted);
        Assert.False(HasLocal("Table.CT"));
        Assert.Equal("a2", ReadLocal("data/a.pac"));
    }

    [Fact]
    public async Task UpdateAsync_SameZipAsLastTime_IsUpToDate()
    {
        // Arrange
        Release("v1", ZipAsset(5, ("data/a.pac", "a")));
        Link();
        await UpdateAsync();

        // Act
        ModUpdateResult result = await UpdateAsync();

        // Assert
        Assert.Equal(ModUpdateOutcome.UpToDate, result.Outcome);
    }

    [Fact]
    public async Task UpdateAsync_ReleaseWithoutAnArchive_FollowsTheNewestCommit()
    {
        // Arrange
        Link();
        Release("Release");
        Tree(Remote("BBTAG.exe", "exe"));

        // Act
        ModUpdateResult result = await UpdateAsync();

        // Assert
        Assert.Equal("f965cea", result.Version);
        Assert.Equal("exe", ReadLocal("BBTAG.exe"));
    }

    [Fact]
    public async Task UpdateAsync_Zip_LeavesNoWorkFolderBehind()
    {
        // Arrange
        Link();
        Release("v1", ZipAsset(5, ("data/a.pac", "a")));

        // Act
        await UpdateAsync();

        // Assert
        Assert.Equal([_folder], Directory.GetDirectories(_mods));
    }
}
