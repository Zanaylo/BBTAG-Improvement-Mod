using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Palettes;

public class PaletteCatalogTests
{
    private const string ModFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string AegisFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes\Aegis";
    private const string RagnaFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes\Ragna";
    private const int Aegis = 24;
    private const int Ragna = 0;

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly List<Action> _backgroundWork = [];

    public PaletteCatalogTests()
    {
        FolderHolds(AegisFolder, "Minato", Aegis);
        FolderHolds(RagnaFolder, "Red", Ragna);
    }

    public PaletteCatalog CreatePaletteCatalog() => new(new PaletteLibrary(_fileSystemMock, ModFolder), _backgroundWork.Add);

    [Fact]
    public void Load_FolderReadBefore_ReadsTheDiskOnlyOnce()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();
        catalog.Load("Aegis");

        // Act
        CharacterPalettes palettes = catalog.Load("aegis");

        // Assert
        Assert.Equal("Minato", palettes.Palettes.Single().Name);
        _fileSystemMock.Received(1).EnumerateFilesRecursively(AegisFolder);
    }

    [Fact]
    public void Loaded_FolderNeverRead_ReturnsNull()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();

        // Act
        CharacterPalettes? palettes = catalog.Loaded("Aegis");

        // Assert
        Assert.Null(palettes);
    }

    [Fact]
    public void LoadInBackground_FolderNotReadYet_ReturnsNullAndReadsItInTheBackground()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();

        // Act
        CharacterPalettes? palettes = catalog.LoadInBackground("Aegis");

        // Assert
        Assert.Null(palettes);
        _fileSystemMock.DidNotReceive().EnumerateFilesRecursively(AegisFolder);
        RunBackgroundWork();
        Assert.Equal("Minato", catalog.Loaded("Aegis")?.Palettes.Single().Name);
    }

    [Fact]
    public void LoadInBackground_FolderAlreadyRead_ReturnsItWithoutQueueingWork()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();
        CharacterPalettes read = catalog.Load("Aegis");

        // Act
        CharacterPalettes? palettes = catalog.LoadInBackground("Aegis");

        // Assert
        Assert.Same(read, palettes);
        Assert.Empty(_backgroundWork);
    }

    [Fact]
    public void Reload_FolderRead_KeepsThePreviousPalettesUntilTheNewReadEnds()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();
        CharacterPalettes previous = catalog.Load("Aegis");
        FolderHolds(AegisFolder, "Shadow", Aegis);

        // Act
        catalog.Reload("Aegis");

        // Assert
        Assert.Same(previous, catalog.Loaded("Aegis"));
        RunBackgroundWork();
        Assert.Equal("Shadow", catalog.Loaded("Aegis")?.Palettes.Single().Name);
    }

    [Fact]
    public void Reload_TwiceBeforeTheWorkRuns_ReadsTheFolderOnce()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();

        // Act
        catalog.Reload("Aegis");
        catalog.Reload("Aegis");

        // Assert
        RunBackgroundWork();
        _fileSystemMock.Received(1).EnumerateFilesRecursively(AegisFolder);
    }

    [Fact]
    public void Reload_WhileTheFolderIsBeingRead_ReadsItAgainAfterwards()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();
        bool reloadedDuringRead = false;
        _fileSystemMock.When(fileSystem => fileSystem.EnumerateFilesRecursively(AegisFolder)).Do(_ =>
        {
            if (reloadedDuringRead)
                return;

            reloadedDuringRead = true;
            catalog.Reload("Aegis");
        });

        // Act
        catalog.Reload("Aegis");

        // Assert
        RunBackgroundWork();
        _fileSystemMock.Received(2).EnumerateFilesRecursively(AegisFolder);
    }

    [Fact]
    public void ReloadAll_TwoFoldersRead_ReadsEachAgain()
    {
        // Arrange
        PaletteCatalog catalog = CreatePaletteCatalog();
        catalog.Load("Aegis");
        catalog.Load("Ragna");

        // Act
        catalog.ReloadAll();

        // Assert
        RunBackgroundWork();
        _fileSystemMock.Received(2).EnumerateFilesRecursively(AegisFolder);
        _fileSystemMock.Received(2).EnumerateFilesRecursively(RagnaFolder);
    }

    private void RunBackgroundWork()
    {
        while (_backgroundWork.Count > 0)
        {
            Action work = _backgroundWork[0];
            _backgroundWork.RemoveAt(0);
            work();
        }
    }

    private void FolderHolds(string folder, string paletteName, int character)
    {
        string path = $@"{folder}\{paletteName}.impl";
        _fileSystemMock.DirectoryExists(folder).Returns(true);
        _fileSystemMock.EnumerateFilesRecursively(folder).Returns([path]);
        _fileSystemMock.ReadAllBytes(path).Returns(ImplFile.Create(character));
    }
}
