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
    private const string GameInSteamFolder = @"C:\Steam\steamapps\common\BBTAG";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly ISteamInstallation _steamMock = Substitute.For<ISteamInstallation>();

    public GameLocator CreateGameLocator() => new(
        _fileSystemMock,
        _steamMock
    );

    [Fact]
    public void Locate_RequestedDirectoryHoldsTheGame_ReturnsItWithoutTrailingSeparator()
    {
        // Arrange
        GameLocator locator = CreateGameLocator();
        _fileSystemMock.FileExists(@"E:\Games\BBTAG\BBTAG.exe").Returns(true);

        // Act
        string directory = locator.Locate(@"E:\Games\BBTAG\", LauncherDirectory);

        // Assert
        Assert.Equal(@"E:\Games\BBTAG", directory);
    }

    [Fact]
    public void Locate_RequestedDirectoryWithoutTheGame_ThrowsGameNotFoundEvenWhenSteamHasIt()
    {
        // Arrange
        GameLocator locator = CreateGameLocator();
        InstallInSecondLibrary();

        // Act
        Exception? exception = Record.Exception(() => locator.Locate(@"E:\Games\Empty", LauncherDirectory));

        // Assert
        Assert.Equal(@"BBTAG.exe was not found in E:\Games\Empty.", Assert.IsType<GameNotFoundException>(exception).Message);
    }

    [Fact]
    public void Locate_GameNextToTheLauncher_PrefersTheLauncherDirectory()
    {
        // Arrange
        GameLocator locator = CreateGameLocator();
        InstallInSecondLibrary();
        _fileSystemMock.FileExists(Path.Combine(LauncherDirectory, "BBTAG.exe")).Returns(true);

        // Act
        string directory = locator.Locate(null, LauncherDirectory);

        // Assert
        Assert.Equal(LauncherDirectory, directory);
    }

    [Fact]
    public void Locate_GameInASecondSteamLibrary_ReturnsItsFolder()
    {
        // Arrange
        GameLocator locator = CreateGameLocator();
        InstallInSecondLibrary();

        // Act
        string directory = locator.Locate(null, LauncherDirectory);

        // Assert
        Assert.Equal(GameInLibrary, directory);
    }

    [Fact]
    public void Locate_SteamWithoutLibraryList_SearchesTheSteamFolder()
    {
        // Arrange
        GameLocator locator = CreateGameLocator();
        _steamMock.SteamPath.Returns(SteamPath);
        Manifest(SteamPath);
        _fileSystemMock.FileExists(Path.Combine(GameInSteamFolder, "BBTAG.exe")).Returns(true);

        // Act
        string directory = locator.Locate(null, LauncherDirectory);

        // Assert
        Assert.Equal(GameInSteamFolder, directory);
    }

    [Fact]
    public void Locate_SteamNotInstalled_ThrowsGameNotFound()
    {
        // Arrange
        GameLocator locator = CreateGameLocator();
        _steamMock.SteamPath.Returns((string?)null);

        // Act
        Exception? exception = Record.Exception(() => locator.Locate(null, LauncherDirectory));

        // Assert
        Assert.IsType<GameNotFoundException>(exception);
    }

    private void InstallInSecondLibrary()
    {
        _steamMock.SteamPath.Returns(SteamPath);
        string libraryFolders = Path.Combine(SteamPath, "steamapps", "libraryfolders.vdf");
        _fileSystemMock.FileExists(libraryFolders).Returns(true);
        _fileSystemMock.ReadAllText(libraryFolders).Returns("\"path\" \"C:\\\\Steam\"\n\"path\" \"D:\\\\Library\"");
        Manifest(Library);
        _fileSystemMock.FileExists(Path.Combine(GameInLibrary, "BBTAG.exe")).Returns(true);
    }

    private void Manifest(string library)
    {
        string manifest = Path.Combine(library, "steamapps", "appmanifest_702890.acf");
        _fileSystemMock.FileExists(manifest).Returns(true);
        _fileSystemMock.ReadAllText(manifest).Returns("\"installdir\" \"BBTAG\"");
    }
}
