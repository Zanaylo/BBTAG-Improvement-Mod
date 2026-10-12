using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Steam;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Launch;

public class GameLocatorTests
{
    private const string LauncherDirectory = @"D:\Hobby\BBTAG_IM";
    private const string SteamPath = @"C:\Steam";
    private const string Library = @"D:\Library";
    private const string GameInLibrary = @"D:\Library\steamapps\common\BBTAG";

    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ISteamInstallation _steam = Substitute.For<ISteamInstallation>();
    private readonly GameLocator _locator;

    public GameLocatorTests()
    {
        _locator = new GameLocator(_fileSystem, _steam);
    }

    private void InstallInSecondLibrary()
    {
        _steam.SteamPath.Returns(SteamPath);
        var libraryFolders = Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf");
        _fileSystem.FileExists(libraryFolders).Returns(true);
        _fileSystem.ReadAllText(libraryFolders).Returns("\"path\" \"C:\\\\Steam\"\n\"path\" \"D:\\\\Library\"");
        var manifest = Path.Combine(Library, "steamapps", "appmanifest_702890.acf");
        _fileSystem.FileExists(manifest).Returns(true);
        _fileSystem.ReadAllText(manifest).Returns("\"installdir\" \"BBTAG\"");
        _fileSystem.FileExists(Path.Combine(GameInLibrary, "BBTAG.exe")).Returns(true);
    }

    [Fact]
    public void Locate_UsesRequestedDirectory()
    {
        _fileSystem.FileExists(@"E:\Games\BBTAG\BBTAG.exe").Returns(true);

        Assert.Equal(@"E:\Games\BBTAG", _locator.Locate(@"E:\Games\BBTAG\", LauncherDirectory));
    }

    [Fact]
    public void Locate_RejectsRequestedDirectoryWithoutGame()
    {
        InstallInSecondLibrary();

        Assert.Throws<GameNotFoundException>(() => _locator.Locate(@"E:\Games\Empty", LauncherDirectory));
    }

    [Fact]
    public void Locate_UsesLauncherDirectoryWhenGameIsThere()
    {
        InstallInSecondLibrary();
        _fileSystem.FileExists(Path.Combine(LauncherDirectory, "BBTAG.exe")).Returns(true);

        Assert.Equal(LauncherDirectory, _locator.Locate(null, LauncherDirectory));
    }

    [Fact]
    public void Locate_FindsGameInSteamLibraries()
    {
        InstallInSecondLibrary();

        Assert.Equal(GameInLibrary, _locator.Locate(null, LauncherDirectory));
    }

    [Fact]
    public void Locate_SearchesSteamFolderWhenLibraryListIsMissing()
    {
        _steam.SteamPath.Returns(SteamPath);
        var manifest = Path.Combine(SteamPath, "steamapps", "appmanifest_702890.acf");
        _fileSystem.FileExists(manifest).Returns(true);
        _fileSystem.ReadAllText(manifest).Returns("\"installdir\" \"BBTAG\"");
        _fileSystem.FileExists(Path.Combine(SteamPath, "steamapps", "common", "BBTAG", "BBTAG.exe")).Returns(true);

        Assert.Equal(Path.Combine(SteamPath, "steamapps", "common", "BBTAG"), _locator.Locate(null, LauncherDirectory));
    }

    [Fact]
    public void Locate_FailsWithoutSteam()
    {
        _steam.SteamPath.Returns((string?)null);

        Assert.Throws<GameNotFoundException>(() => _locator.Locate(null, LauncherDirectory));
    }
}
