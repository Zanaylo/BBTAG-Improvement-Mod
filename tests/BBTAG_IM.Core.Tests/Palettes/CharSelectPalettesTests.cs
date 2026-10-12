using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Palettes;

public class CharSelectPalettesTests
{
    private const string ModFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string RagnaPalettes = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes\Ragna";
    private const string ModData = @"D:\BBTAG\mods\BBTAG-OverHaul\data";
    private const string GameData = @"D:\BBTAG\data";
    private const string CacheFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\cache";
    private const string Requested = @"data/etc/char_select_pal/char_select_brg_pal.pac";
    private const string GameArchive = @"D:\BBTAG\data\etc\char_select_pal\char_select_brg_pal.pac";
    private const string ModArchive = @"D:\BBTAG\mods\BBTAG-OverHaul\data\etc\char_select_pal\char_select_brg_pal.pac";
    private const string Patched = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\cache\char_select_brg_pal.pac";
    private const int Ragna = 0;

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly ImprovementSettings _settings = new();

    public CharSelectPalettesTests()
    {
        _fileSystemMock.DirectoryExists(RagnaPalettes).Returns(true);
        _fileSystemMock.EnumerateFilesRecursively(RagnaPalettes).Returns([RagnaPalettes + @"\Minato.impl"]);
        _fileSystemMock.ReadAllBytes(RagnaPalettes + @"\Minato.impl").Returns(ImplFile.Create(Ragna, fill: 0x77));
        _fileSystemMock.FileExists(GameArchive).Returns(true);
        _fileSystemMock.ReadAllBytes(GameArchive).Returns(Archive(0x11));
    }

    public CharSelectPalettes CreateCharSelectPalettes() =>
        new(_fileSystemMock, [ModData, GameData], new PaletteCatalog(new PaletteLibrary(_fileSystemMock, ModFolder), work => work()), () => _settings, CacheFolder);

    [Fact]
    public void Substitute_CharacterWithAnAssignedPalette_WritesAPatchedCopyAndReturnsItsPath()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();
        _settings.ChoosePalette("Ragna", 2, "Minato");

        // Act
        string? path = palettes.Substitute(Requested);

        // Assert
        Assert.Equal(Patched, path);
        _fileSystemMock.Received(1).WriteAllBytes(Patched, Arg.Is<byte[]>(bytes => SecondColor(bytes).All(value => value == 0x77)));
    }

    [Fact]
    public void Substitute_SameAssignmentsAsLastTime_ReturnsTheCopyWithoutPatchingAgain()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();
        _settings.ChoosePalette("Ragna", 2, "Minato");
        palettes.Substitute(Requested);
        _fileSystemMock.FileExists(Patched).Returns(true);

        // Act
        string? path = palettes.Substitute(Requested);

        // Assert
        Assert.Equal(Patched, path);
        _fileSystemMock.Received(1).ReadAllBytes(GameArchive);
        _fileSystemMock.Received(1).WriteAllBytes(Patched, Arg.Any<byte[]>());
    }

    [Fact]
    public void Substitute_AssignmentChangedSinceLastTime_PatchesANewCopy()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();
        _settings.ChoosePalette("Ragna", 2, "Minato");
        palettes.Substitute(Requested);
        _fileSystemMock.FileExists(Patched).Returns(true);
        _settings.AssignPalette("Ragna", "Minato", 3);

        // Act
        string? path = palettes.Substitute(Requested);

        // Assert
        Assert.Equal(Patched, path);
        _fileSystemMock.Received(2).WriteAllBytes(Patched, Arg.Any<byte[]>());
    }

    [Fact]
    public void Substitute_ModWithItsOwnArchive_PatchesTheModArchive()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();
        _settings.ChoosePalette("Ragna", 1, "Minato");
        _fileSystemMock.FileExists(ModArchive).Returns(true);
        _fileSystemMock.ReadAllBytes(ModArchive).Returns(Archive(0x33));

        // Act
        palettes.Substitute(Requested);

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(Patched, Arg.Is<byte[]>(bytes => SecondColor(bytes).All(value => value == 0x33)));
    }

    [Fact]
    public void Substitute_CharacterWithoutAssignments_ReturnsNullWithoutWriting()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();

        // Act
        string? path = palettes.Substitute(Requested);

        // Assert
        Assert.Null(path);
        _fileSystemMock.DidNotReceive().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void Substitute_OnlyRandomOrMissingPalettes_ReturnsNullWithoutWriting()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();
        _settings.ChoosePalette("Ragna", 1, RandomPalette.Any);
        _settings.ChoosePalette("Ragna", 2, "Deleted");

        // Act
        string? path = palettes.Substitute(Requested);

        // Assert
        Assert.Null(path);
        _fileSystemMock.DidNotReceive().WriteAllBytes(Arg.Any<string>(), Arg.Any<byte[]>());
    }

    [Fact]
    public void Substitute_AnyOtherFile_ReturnsNullWithoutReadingPalettes()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();
        _settings.ChoosePalette("Ragna", 2, "Minato");

        // Act
        string? path = palettes.Substitute(@"data\char\char_brg_pal.pac");

        // Assert
        Assert.Null(path);
        _fileSystemMock.DidNotReceive().ReadAllBytes(Arg.Any<string>());
    }

    [Fact]
    public void Substitute_ArchiveThatCannotBeRead_ReturnsNull()
    {
        // Arrange
        CharSelectPalettes palettes = CreateCharSelectPalettes();
        _settings.ChoosePalette("Ragna", 2, "Minato");
        _fileSystemMock.ReadAllBytes(GameArchive).Returns(new byte[0x40]);

        // Act
        string? path = palettes.Substitute(Requested);

        // Assert
        Assert.Null(path);
    }

    private static byte[] Archive(byte fill) =>
        SpriteFiles.Fpac(("brg00_00.hpl", SpriteFiles.Hpl(fill)), ("brg01_00.hpl", SpriteFiles.Hpl(fill)));

    private static byte[] SecondColor(byte[] archive)
    {
        FpacEntry entry = FpacArchive.Entries(archive)[1];
        return archive.AsSpan((int)entry.Offset + SpriteFiles.HplHeaderLength, SpriteFiles.HplColorsLength).ToArray();
    }
}
