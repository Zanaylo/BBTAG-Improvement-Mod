using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModFileRedirectorTests
{
    private const string GameDirectory = @"D:\Steam\steamapps\common\BBTAG";
    private const string GameFile = GameDirectory + @"\data\char\char_adh_col.pac";
    private const string ModDirectory = GameDirectory + @"\mods\Overhaul";
    private const string ModFile = ModDirectory + @"\data\char\char_adh_col.pac";
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileWriteData = 0x00000002;
    private const uint FileAppendData = 0x00000004;
    private const uint CreateAlways = 2;
    private const uint OpenAlways = 4;

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public ModFileRedirectorTests()
    {
        _fileSystemMock.EnumerateFilesRecursively(ModDirectory + @"\data").Returns([ModFile]);
    }

    public ModFileRedirector CreateModFileRedirector(string executableDirectory = GameDirectory, DevOverlay? dev = null)
    {
        ModFolder mod = new("Overhaul", ModDirectory, [new ModDataRoot(ModDirectory + @"\data", "data")], null, null, []);
        return new ModFileRedirector(
            ModFileIndex.Build([mod], _fileSystemMock),
            GameDirectory,
            executableDirectory,
            dev
        );
    }

    [Fact]
    public void RedirectOpen_FileInTheDevFolder_WinsOverTheMods()
    {
        // Arrange
        string devFile = GameDirectory + @"\mods\DEV\data\char\char_adh_col.pac";
        _fileSystemMock.FileExists(devFile).Returns(true);
        ModFileRedirector redirector = CreateModFileRedirector(dev: new DevOverlay(_fileSystemMock, GameDirectory + @"\mods\DEV", _ => null));

        // Act
        string? redirected = redirector.RedirectOpen(GameFile, GenericRead, ModFileRedirector.OpenExisting);

        // Assert
        Assert.Equal(devFile, redirected);
    }

    [Theory]
    [InlineData(GameFile)]
    [InlineData("data/char/char_adh_col.pac")]
    public void RedirectOpen_ReadOfAFileAModProvides_ReturnsTheModFile(string path)
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();

        // Act
        string? redirected = redirector.RedirectOpen(path, GenericRead, ModFileRedirector.OpenExisting);

        // Assert
        Assert.Equal(ModFile, redirected);
    }

    [Theory]
    [InlineData(GenericRead | GenericWrite, ModFileRedirector.OpenExisting)]
    [InlineData(FileWriteData, ModFileRedirector.OpenExisting)]
    [InlineData(FileAppendData, ModFileRedirector.OpenExisting)]
    [InlineData(GenericRead, CreateAlways)]
    [InlineData(GenericRead, OpenAlways)]
    public void RedirectOpen_WriteOrCreateOfAFileAModProvides_LeavesTheGameFile(uint access, uint disposition)
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();

        // Act
        string? redirected = redirector.RedirectOpen(GameFile, access, disposition);

        // Assert
        Assert.Null(redirected);
    }

    [Theory]
    [InlineData(GameDirectory + @"\data\char\char_adh_pal.pac")]
    [InlineData(null)]
    public void RedirectOpen_FileNoModProvides_ReturnsNull(string? path)
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();

        // Act
        string? redirected = redirector.RedirectOpen(path, GenericRead, ModFileRedirector.OpenExisting);

        // Assert
        Assert.Null(redirected);
    }

    [Theory]
    [InlineData(@"data\common\fhd_ads.pac", GameDirectory + @"\data\common\fhd_ads.pac")]
    [InlineData("data/common/fhd_ads.pac", GameDirectory + @"\data\common\fhd_ads.pac")]
    [InlineData("BBTAG.ini", GameDirectory + @"\BBTAG.ini")]
    public void AnchorToGame_RelativePath_PointsIntoTheGameFolderWhateverTheWorkingDirectoryIs(string path, string expected)
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();

        // Act
        string? anchored = redirector.AnchorToGame(path);

        // Assert
        Assert.Equal(expected, anchored);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(GameFile)]
    [InlineData(@"\.\pipe\bbtag-im-harness")]
    public void AnchorToGame_EmptyOrRootedPath_ReturnsNull(string? path)
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();

        // Act
        string? anchored = redirector.AnchorToGame(path);

        // Assert
        Assert.Null(anchored);
    }

    [Fact]
    public void RedirectLookup_AttributesOfAFileAModProvides_ReturnsTheModFile()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();

        // Act
        string? redirected = redirector.RedirectLookup(GameFile);

        // Assert
        Assert.Equal(ModFile, redirected);
    }

    [Fact]
    public void RedirectLookup_WildcardPattern_ReturnsNull()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();

        // Act
        string? redirected = redirector.RedirectLookup(GameDirectory + @"\data\char\*.pac");

        // Assert
        Assert.Null(redirected);
    }

    [Fact]
    public void Redirections_TwoRedirectsAndOneMiss_CountsTwo()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector();
        redirector.RedirectOpen(GameFile, GenericRead, ModFileRedirector.OpenExisting);
        redirector.RedirectLookup(GameFile);
        redirector.RedirectLookup(@"D:\Elsewhere\file.pac");

        // Act
        long redirections = redirector.Redirections;

        // Assert
        Assert.Equal(2, redirections);
    }

    [Fact]
    public void RedirectOpen_GameFileAskedBesideTheModExecutable_GoesToTheGameFolder()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);

        // Act
        string? redirected = redirector.RedirectOpen(ModDirectory + @"\data\char\char_adh_pal.pac", GenericRead, ModFileRedirector.OpenExisting);

        // Assert
        Assert.Equal(GameDirectory + @"\data\char\char_adh_pal.pac", redirected);
    }

    [Fact]
    public void RedirectOpen_ModFileAskedBesideTheModExecutable_ReturnsTheModFile()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);

        // Act
        string? redirected = redirector.RedirectOpen(ModDirectory + "/data/char/char_adh_col.pac", GenericRead, ModFileRedirector.OpenExisting);

        // Assert
        Assert.Equal(ModFile, redirected);
    }

    [Fact]
    public void RedirectOpen_WriteBesideTheModExecutable_GoesToTheGameFolder()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);

        // Act
        string? redirected = redirector.RedirectOpen(ModDirectory + @"\Save\setting.dat", GenericWrite, CreateAlways);

        // Assert
        Assert.Equal(GameDirectory + @"\Save\setting.dat", redirected);
    }

    [Fact]
    public void RedirectOpen_TheModExecutableItself_ReturnsNull()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);

        // Act
        string? redirected = redirector.RedirectOpen(ModDirectory + @"\BBTAG.exe", GenericRead, ModFileRedirector.OpenExisting);

        // Assert
        Assert.Null(redirected);
    }

    [Fact]
    public void RedirectOpen_RelativePathWhileRunningFromTheMod_ReturnsTheModFile()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);

        // Act
        string? redirected = redirector.RedirectOpen(@"data\char\char_adh_col.pac", GenericRead, ModFileRedirector.OpenExisting);

        // Assert
        Assert.Equal(ModFile, redirected);
    }

    [Fact]
    public void RedirectLookup_GameFileAskedBesideTheModExecutable_GoesToTheGameFolder()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);

        // Act
        string? redirected = redirector.RedirectLookup(ModDirectory + @"\data\char\char_adh_pal.pac");

        // Assert
        Assert.Equal(GameDirectory + @"\data\char\char_adh_pal.pac", redirected);
    }

    [Theory]
    [InlineData(@"data\char\char_xyz_img.pac", @"data\char\char_xyz_img.pac")]
    [InlineData(GameDirectory + @"\data\bg\bg_01.pac", @"data\bg\bg_01.pac")]
    [InlineData(ModDirectory + @"\data\char\char_xyz_img.pac", @"data\char\char_xyz_img.pac")]
    [InlineData(@"C:\Windows\System32\d3d9.dll", null)]
    [InlineData(GameDirectory + @"\BBTAG.exe", null)]
    [InlineData(null, null)]
    public void GameFileKey_RunningFromTheMod_NamesFilesInsideTheGameOrBesideTheModExecutable(string? path, string? expected)
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);

        // Act
        string? key = redirector.GameFileKey(path);

        // Assert
        Assert.Equal(expected, key);
    }

    [Fact]
    public void Redirections_RunningFromTheMod_CountsOnlyPathsThatWereRedirected()
    {
        // Arrange
        ModFileRedirector redirector = CreateModFileRedirector(ModDirectory);
        redirector.RedirectLookup(ModDirectory + @"\data\char\char_adh_pal.pac");
        redirector.RedirectLookup(ModDirectory + @"\BBTAG.exe");

        // Act
        long redirections = redirector.Redirections;

        // Assert
        Assert.Equal(1, redirections);
    }

    [Theory]
    [InlineData(0x80000000u, 3u, true)]
    [InlineData(0xC0000000u, 3u, false)]
    [InlineData(0x80000000u, 2u, false)]
    public void ReadsExistingFile_AccessAndDisposition_IsTrueOnlyForReadingAFileThatExists(uint access, uint disposition, bool expected)
    {
        // Act
        bool reads = ModFileRedirector.ReadsExistingFile(access, disposition);

        // Assert
        Assert.Equal(expected, reads);
    }
}
