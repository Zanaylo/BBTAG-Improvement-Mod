using System.IO.Compression;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Mods.Archives;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Mods.Archives;

public sealed class ModArchiveInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bbtag-archives-" + Guid.NewGuid().ToString("N"));
    private readonly string _mods;

    public ModArchiveInstallerTests()
    {
        _mods = Path.Combine(_root, "mods");
        Directory.CreateDirectory(_mods);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    public ModArchiveInstaller CreateModArchiveInstaller() => new(new PhysicalFileSystem(), new ArchiveUnpacker(), _mods);

    [Theory]
    [InlineData(nameof(ArchiveFixtures.Rar), "Foo.rar")]
    [InlineData(nameof(ArchiveFixtures.SolidRar), "Foo.rar")]
    [InlineData(nameof(ArchiveFixtures.SevenZip), "Foo.7z")]
    [InlineData(nameof(ArchiveFixtures.Tar), "Foo.tar")]
    [InlineData(nameof(ArchiveFixtures.TarGz), "Foo.tar.gz")]
    public void Install_RealArchive_UnpacksItsFilesIntoTheModFolder(string fixture, string fileName)
    {
        // Arrange
        string archive = Fixture(fixture, fileName);

        // Act
        CreateModArchiveInstaller().Install(archive);

        // Assert
        Assert.Equal("hello", File.ReadAllText(Path.Combine(_mods, "Foo", "data", "note.txt")));
        Assert.Equal("world", File.ReadAllText(Path.Combine(_mods, "Foo", "readme.txt")));
    }

    [Fact]
    public void Install_Zip_UnpacksItsFiles()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));

        // Act
        CreateModArchiveInstaller().Install(archive);

        // Assert
        Assert.Equal("hello", File.ReadAllText(Path.Combine(_mods, "Bar", "data", "note.txt")));
    }

    [Fact]
    public void Install_ModInsideSeveralFolders_FindsTheModRoot()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("read me.txt", "x"), ("Bar-main/mods/Bar/data/note.txt", "hello"));

        // Act
        CreateModArchiveInstaller().Install(archive);

        // Assert
        Assert.True(File.Exists(Path.Combine(_mods, "Bar", "data", "note.txt")));
    }

    [Fact]
    public void Install_Mod_ShowsUpInDiscovery()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));

        // Act
        CreateModArchiveInstaller().Install(archive);

        // Assert
        Assert.Equal(["Bar"], ModDiscovery.InModsDirectory(_mods, new PhysicalFileSystem()).Select(mod => mod.Name));
    }

    [Fact]
    public void Install_Mod_LeavesNoWorkFolderBehind()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));

        // Act
        CreateModArchiveInstaller().Install(archive);

        // Assert
        Assert.Equal(["Bar"], Directory.GetDirectories(_mods).Select(Path.GetFileName));
    }

    [Fact]
    public void Install_ArchiveWasUpdated_ReplacesItsFilesAndKeepsTheUsersFiles()
    {
        // Arrange
        ModArchiveInstaller installer = CreateModArchiveInstaller();
        string archive = Zip("Bar.zip", ("data/note.txt", "old"));
        installer.Install(archive);
        File.WriteAllText(Path.Combine(_mods, "Bar", "settings.json"), "mine");
        Zip("Bar.zip", ("data/note.txt", "new"));
        File.SetLastWriteTimeUtc(archive, DateTime.UtcNow.AddMinutes(5));

        // Act
        installer.Install(archive);

        // Assert
        Assert.Equal("new", File.ReadAllText(Path.Combine(_mods, "Bar", "data", "note.txt")));
        Assert.Equal("mine", File.ReadAllText(Path.Combine(_mods, "Bar", "settings.json")));
    }

    [Fact]
    public void Install_FolderOfTheSameNameNotFromAnArchive_LeavesItAlone()
    {
        // Arrange
        string note = Path.Combine(_mods, "Bar", "data", "note.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(note)!);
        File.WriteAllText(note, "mine");
        string archive = Zip("Bar.zip", ("data/note.txt", "theirs"));

        // Act
        Exception? exception = Record.Exception(() => CreateModArchiveInstaller().Install(archive));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
        Assert.Equal("mine", File.ReadAllText(note));
    }

    [Fact]
    public void Install_ArchiveWithoutAMod_ThrowsAndLeavesNothing()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("photos/a.txt", "x"), ("music/b.txt", "y"));

        // Act
        Exception? exception = Record.Exception(() => CreateModArchiveInstaller().Install(archive));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
        Assert.Empty(Directory.GetDirectories(_mods));
    }

    [Fact]
    public void Install_EntryEscapingTheFolder_ThrowsAndWritesNothingOutside()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("data/note.txt", "x"), ("../../evil.txt", "y"));

        // Act
        Exception? exception = Record.Exception(() => CreateModArchiveInstaller().Install(archive));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
        Assert.False(File.Exists(Path.Combine(_root, "evil.txt")));
        Assert.Empty(Directory.GetDirectories(_mods));
    }

    [Fact]
    public void Install_PasswordProtectedArchive_ThrowsModArchive()
    {
        // Arrange
        string archive = Fixture(nameof(ArchiveFixtures.PasswordRar), "Foo.rar");

        // Act
        Exception? exception = Record.Exception(() => CreateModArchiveInstaller().Install(archive));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
        Assert.Empty(Directory.GetDirectories(_mods));
    }

    [Fact]
    public void Install_DamagedArchive_ThrowsModArchive()
    {
        // Arrange
        string archive = Path.Combine(_mods, "Bar.zip");
        File.WriteAllBytes(archive, [1, 2, 3, 4, 5]);

        // Act
        Exception? exception = Record.Exception(() => CreateModArchiveInstaller().Install(archive));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
        Assert.Empty(Directory.GetDirectories(_mods));
    }

    [Fact]
    public void Install_LeftoverWorkFolder_IsReplaced()
    {
        // Arrange
        string leftover = Path.Combine(_mods, ModArchiveInstaller.UnpackingPrefix + "Bar", "data");
        Directory.CreateDirectory(leftover);
        File.WriteAllText(Path.Combine(leftover, "stale.txt"), "x");
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));

        // Act
        CreateModArchiveInstaller().Install(archive);

        // Assert
        Assert.False(File.Exists(Path.Combine(_mods, "Bar", "data", "stale.txt")));
    }

    [Fact]
    public void Pending_ArchiveNotUnpackedYet_ListsIt()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));

        // Act
        IReadOnlyList<string> pending = CreateModArchiveInstaller().Pending();

        // Assert
        Assert.Equal([archive], pending);
    }

    [Fact]
    public void Pending_ArchiveAlreadyUnpacked_ListsNothing()
    {
        // Arrange
        ModArchiveInstaller installer = CreateModArchiveInstaller();
        installer.Install(Zip("Bar.zip", ("data/note.txt", "hello")));

        // Act
        IReadOnlyList<string> pending = installer.Pending();

        // Assert
        Assert.Empty(pending);
    }

    [Fact]
    public void Pending_ArchiveChangedAfterUnpacking_ListsIt()
    {
        // Arrange
        ModArchiveInstaller installer = CreateModArchiveInstaller();
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));
        installer.Install(archive);
        File.SetLastWriteTimeUtc(archive, DateTime.UtcNow.AddMinutes(5));

        // Act
        IReadOnlyList<string> pending = installer.Pending();

        // Assert
        Assert.Equal([archive], pending);
    }

    [Fact]
    public void Pending_ModsFolderMissing_ListsNothing()
    {
        // Arrange
        ModArchiveInstaller installer = new(new PhysicalFileSystem(), new ArchiveUnpacker(), Path.Combine(_root, "nowhere"));

        // Act
        IReadOnlyList<string> pending = installer.Pending();

        // Assert
        Assert.Empty(pending);
    }

    [Fact]
    public void InstallPending_OneGoodOneDamaged_ReportsBothAndInstallsTheGoodOne()
    {
        // Arrange
        Zip("Good.zip", ("data/note.txt", "hello"));
        File.WriteAllBytes(Path.Combine(_mods, "Bad.zip"), [1, 2, 3]);
        List<string> messages = [];

        // Act
        CreateModArchiveInstaller().InstallPending(messages.Add);

        // Assert
        Assert.True(File.Exists(Path.Combine(_mods, "Good", "data", "note.txt")));
        Assert.Contains(messages, message => message.StartsWith("Bad.zip could not be unpacked", StringComparison.Ordinal));
        Assert.Contains("Good.zip was unpacked.", messages);
    }

    [Fact]
    public void WithArchivesInstalled_ArchiveGivenToTheLoader_PointsAtItsModFolder()
    {
        // Arrange
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));
        LaunchOptions options = new(null, [archive], false);

        // Act
        LaunchOptions installed = CreateModArchiveInstaller().WithArchivesInstalled(options);

        // Assert
        Assert.Equal([Path.Combine(_mods, "Bar")], installed.ModFolders);
        Assert.True(File.Exists(Path.Combine(_mods, "Bar", "data", "note.txt")));
    }

    [Fact]
    public void WithArchivesInstalled_FolderGivenToTheLoader_KeepsIt()
    {
        // Arrange
        LaunchOptions options = new(null, [@"D:\Mods\Folder.zip"], false);

        // Act
        LaunchOptions installed = CreateModArchiveInstaller().WithArchivesInstalled(options);

        // Assert
        Assert.Equal([@"D:\Mods\Folder.zip"], installed.ModFolders);
    }

    [Fact]
    public void WithArchivesInstalled_ArchiveAlreadyUnpacked_DoesNotUnpackAgain()
    {
        // Arrange
        ModArchiveInstaller installer = CreateModArchiveInstaller();
        string archive = Zip("Bar.zip", ("data/note.txt", "hello"));
        installer.Install(archive);
        string note = Path.Combine(_mods, "Bar", "data", "note.txt");
        File.WriteAllText(note, "edited");

        // Act
        installer.WithArchivesInstalled(new LaunchOptions(null, [archive], false));

        // Assert
        Assert.Equal("edited", File.ReadAllText(note));
    }

    private string Fixture(string name, string fileName)
    {
        var path = Path.Combine(_mods, fileName);
        var base64 = (string)typeof(ArchiveFixtures).GetField(name)!.GetRawConstantValue()!;
        File.WriteAllBytes(path, Convert.FromBase64String(base64));
        return path;
    }

    private string Zip(string fileName, params (string Name, string Text)[] entries)
    {
        var path = Path.Combine(_mods, fileName);
        File.Delete(path);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);

        foreach (var (name, text) in entries)
        {
            using var writer = new StreamWriter(zip.CreateEntry(name).Open());
            writer.Write(text);
        }

        return path;
    }
}
