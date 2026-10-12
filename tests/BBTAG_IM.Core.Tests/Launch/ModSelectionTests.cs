using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Launch;

public class ModSelectionTests
{
    private const string LauncherDirectory = @"D:\BBTAG_IM";
    private const string ModsFolder = @"D:\BBTAG_IM\mods";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Resolve_FoldersGivenOnTheCommandLine_UsesThemInOrder()
    {
        // Arrange
        ModAt(@"D:\Mods\Second");
        ModAt(@"D:\Mods\First");
        LaunchOptions options = new(null, [@"D:\Mods\Second", @"D:\Mods\First"], false);

        // Act
        IReadOnlyList<ModFolder> mods = ModSelection.Resolve(options, LauncherDirectory, _fileSystemMock);

        // Assert
        Assert.Equal(["Second", "First"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void Resolve_CommandLineFolderThatDoesNotExist_ThrowsInvalidModFolder()
    {
        // Arrange
        LaunchOptions options = new(null, [@"D:\Mods\Missing"], false);

        // Act
        Exception? exception = Record.Exception(() => ModSelection.Resolve(options, LauncherDirectory, _fileSystemMock));

        // Assert
        Assert.IsType<InvalidModFolderException>(exception);
    }

    [Fact]
    public void Resolve_NoFoldersOnTheCommandLine_LoadsTheModsFolderNextToTheLauncher()
    {
        // Arrange
        string overhaul = Path.Combine(ModsFolder, "Overhaul");
        _fileSystemMock.DirectoryExists(ModsFolder).Returns(true);
        _fileSystemMock.EnumerateDirectories(ModsFolder).Returns([overhaul]);
        ModAt(overhaul);

        // Act
        IReadOnlyList<ModFolder> mods = ModSelection.Resolve(new LaunchOptions(null, [], false), LauncherDirectory, _fileSystemMock);

        // Assert
        Assert.Equal(["Overhaul"], mods.Select(mod => mod.Name));
    }

    [Fact]
    public void Resolve_NothingToLoad_ThrowsInvalidModFolderPointingAtTheModsFolder()
    {
        // Act
        Exception? exception = Record.Exception(() => ModSelection.Resolve(new LaunchOptions(null, [], false), LauncherDirectory, _fileSystemMock));

        // Assert
        Assert.Equal(
            $"No mods to load. Put mod folders inside {ModsFolder}, or drag a mod folder onto the launcher.",
            Assert.IsType<InvalidModFolderException>(exception).Message);
    }

    private void ModAt(string path)
    {
        _fileSystemMock.DirectoryExists(path).Returns(true);
        _fileSystemMock.DirectoryExists(Path.Combine(path, "data2")).Returns(true);
    }
}
