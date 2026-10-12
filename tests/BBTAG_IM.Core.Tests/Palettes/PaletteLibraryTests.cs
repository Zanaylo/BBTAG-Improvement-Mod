using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BBTAG_IM.Core.Tests.Palettes;

public class PaletteLibraryTests
{
    private const string ModFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string PalettesFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes";
    private const string AegisFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes\Aegis";
    private const string ModdedFolder = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\palettes\mko";
    private const int Kohaku = 120;
    private const int Aegis = 24;
    private const int Ragna = 0;

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public PaletteLibrary CreatePaletteLibrary() => new(_fileSystemMock, ModFolder);

    [Fact]
    public void Load_CharacterWithoutFolder_ReturnsNoPalettesAndNoProblems()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.DirectoryExists(AegisFolder).Returns(false);

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Equal((0, 0), (palettes.Palettes.Count, palettes.Problems.Count));
    }

    [Fact]
    public void Load_PalettesInSubfolders_ReturnsThemSortedByName()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds(
            (AegisFolder + @"\Pack\Zeta.impl", ImplFile.Create(Aegis)),
            (AegisFolder + @"\Minato.impl", ImplFile.Create(Aegis)),
            (AegisFolder + @"\alpha.impl", ImplFile.Create(Aegis)));

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Equal(["alpha", "Minato", "Zeta"], palettes.Palettes.Select(palette => palette.Name));
    }

    [Fact]
    public void Load_FilesThatAreNotPalettes_IgnoresThemQuietly()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds((AegisFolder + @"\Pack.zip", [1, 2, 3]), (AegisFolder + @"\Minato.impl", ImplFile.Create(Aegis)));

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Equal((1, 0), (palettes.Palettes.Count, palettes.Problems.Count));
    }

    [Fact]
    public void Load_PaletteMadeForAnotherCharacter_SkipsItAndSaysWhy()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds((AegisFolder + @"\Red.impl", ImplFile.Create(Ragna)));

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Empty(palettes.Palettes);
        Assert.Equal(["Red.impl is a palette of Ragna, so the Aegis folder skips it."], palettes.Problems);
    }

    [Fact]
    public void Load_BrokenPalette_ReportsItAndKeepsTheOthers()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds((AegisFolder + @"\Broken.impl", [1, 2, 3]), (AegisFolder + @"\Minato.impl", ImplFile.Create(Aegis)));

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Equal(["Minato"], palettes.Palettes.Select(palette => palette.Name));
        Assert.Equal(["Broken.impl is too short to be a palette."], palettes.Problems);
    }

    [Fact]
    public void Load_UnreadablePalette_ReportsTheReadError()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds((AegisFolder + @"\Locked.impl", ImplFile.Create(Aegis)));
        _fileSystemMock.ReadAllBytes(AegisFolder + @"\Locked.impl").Throws(new IOException("The file is in use."));

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Equal(["Locked.impl could not be read: The file is in use."], palettes.Problems);
    }

    [Fact]
    public void Load_FolderThatCannotBeListed_ReportsItInsteadOfFailing()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.DirectoryExists(AegisFolder).Returns(true);
        _fileSystemMock.EnumerateFilesRecursively(AegisFolder).Throws(new UnauthorizedAccessException("Access is denied."));

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Equal((0, "The Aegis palettes could not be read: Access is denied."), (palettes.Palettes.Count, palettes.Problems.Single()));
    }

    [Fact]
    public void Load_TwoPalettesWithTheSameName_KeepsTheFirstAndReportsTheOther()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds((AegisFolder + @"\A\Minato.impl", ImplFile.Create(Aegis)), (AegisFolder + @"\B\Minato.impl", ImplFile.Create(Aegis)));

        // Act
        CharacterPalettes palettes = library.Load("Aegis");

        // Assert
        Assert.Single(palettes.Palettes);
        Assert.Equal(["Aegis has two palettes named Minato. Only the first one is used."], palettes.Problems);
    }

    [Fact]
    public void CharactersWithPalettes_FoldersOfCharactersAndOthers_ReturnsTheGameCharactersInGameOrderThenModdedOnes()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.DirectoryExists(PalettesFolder).Returns(true);
        _fileSystemMock.EnumerateDirectories(PalettesFolder).Returns([PalettesFolder + @"\aegis", PalettesFolder + @"\Packs", PalettesFolder + @"\mko", PalettesFolder + @"\Ragna", PalettesFolder + @"\Character 140", PalettesFolder + @"\baz2"]);

        // Act
        IReadOnlyList<string> characters = library.CharactersWithPalettes();

        // Assert
        Assert.Equal(["Ragna", "Aegis", "baz2", "Character 140", "mko"], characters);
    }

    [Fact]
    public void CharactersWithPalettes_NoPalettesFolder_ReturnsNone()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.DirectoryExists(PalettesFolder).Returns(false);

        // Act
        IReadOnlyList<string> characters = library.CharactersWithPalettes();

        // Assert
        Assert.Empty(characters);
    }

    [Fact]
    public void Load_ModdedCharacterFolder_KeepsPalettesWhateverIndexTheyHold()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.DirectoryExists(ModdedFolder).Returns(true);
        _fileSystemMock.EnumerateFilesRecursively(ModdedFolder).Returns([ModdedFolder + @"\Red.impl"]);
        _fileSystemMock.ReadAllBytes(ModdedFolder + @"\Red.impl").Returns(ImplFile.Create(Kohaku));

        // Act
        CharacterPalettes palettes = library.Load("mko");

        // Assert
        Assert.Equal((1, 0), (palettes.Palettes.Count, palettes.Problems.Count));
    }

    [Fact]
    public void Save_NewPalette_WritesItIntoTheCharacterFolder()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        ImplPalette palette = ImplPalette.Create("Minato", Aegis, "", "", new PaletteColors());

        // Act
        string path = library.Save("Aegis", palette);

        // Assert
        Assert.Equal(AegisFolder + @"\Minato.impl", path);
        _fileSystemMock.Received(1).WriteAllBytes(path, Arg.Is<byte[]>(bytes => bytes.SequenceEqual(palette.ToBytes())));
    }

    [Fact]
    public void Save_PaletteWithTheNameOfOneInASubfolder_OverwritesThatFile()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds((AegisFolder + @"\Pack\minato.impl", ImplFile.Create(Aegis)));
        ImplPalette palette = ImplPalette.Create("Minato", Aegis, "", "", new PaletteColors());

        // Act
        string path = library.Save("Aegis", palette);

        // Assert
        Assert.Equal(AegisFolder + @"\Pack\minato.impl", path);
    }

    [Theory]
    [InlineData("Minato", true)]
    [InlineData("Shadow Aegis 2", true)]
    [InlineData("", false)]
    [InlineData(" Minato", false)]
    [InlineData(".Minato", false)]
    [InlineData("Min/ato", false)]
    [InlineData("Min:ato", false)]
    [InlineData("Game colors", false)]
    [InlineData("Random", false)]
    [InlineData("random_exclude_default", false)]
    public void IsUsableName_Name_AcceptsOnlyNamesThatMakeAFile(string name, bool expected)
    {
        // Act
        bool usable = PaletteLibrary.IsUsableName(name);

        // Assert
        Assert.Equal(expected, usable);
    }

    [Fact]
    public void Open_PaletteFile_NamesItAfterTheFile()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.ReadAllBytes(@"C:\Downloads\Minato.impl").Returns(ImplFile.Create(Aegis));

        // Act
        ImplPalette palette = library.Open(@"C:\Downloads\Minato.impl");

        // Assert
        Assert.Equal(("Minato", Aegis), (palette.Name, palette.CharacterIndex));
    }

    [Fact]
    public void Open_FileThatIsNotAPalette_ThrowsInvalidPaletteException()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.ReadAllBytes(@"C:\Downloads\Minato.impl").Returns([1, 2, 3]);

        // Act
        Exception? exception = Record.Exception(() => library.Open(@"C:\Downloads\Minato.impl"));

        // Assert
        Assert.IsType<InvalidPaletteException>(exception);
    }

    [Fact]
    public void Import_FreeName_WritesThePaletteIntoTheCharacterFolder()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Aegis, fill: 0x42));

        // Act
        string name = library.Import(palette, "Aegis");

        // Assert
        Assert.Equal("Minato", name);
        _fileSystemMock.Received(1).WriteAllBytes(AegisFolder + @"\Minato.impl", Arg.Is<byte[]>(bytes => bytes.SequenceEqual(ImplPalette.Create("Minato", Aegis, "", "", palette.Colors()).ToBytes())));
    }

    [Fact]
    public void Import_NameAlreadyTaken_AddsTheFirstFreeNumber()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds((AegisFolder + @"\Minato.impl", ImplFile.Create(Aegis)), (AegisFolder + @"\Pack\minato_2.impl", ImplFile.Create(Aegis)));
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Aegis));

        // Act
        string name = library.Import(palette, "Aegis");

        // Assert
        Assert.Equal("Minato_3", name);
        _fileSystemMock.Received(1).WriteAllBytes(AegisFolder + @"\Minato_3.impl", Arg.Any<byte[]>());
    }

    [Fact]
    public void Import_PaletteOfAnotherCharacter_StampsTheChosenCharacterInTheFile()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Ragna));

        // Act
        library.Import(palette, "Aegis");

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(Arg.Any<string>(), Arg.Is<byte[]>(bytes => ImplPalette.Parse("Minato", bytes).CharacterIndex == Aegis));
    }

    [Fact]
    public void Import_IntoModdedCharacterFolder_KeepsTheCharacterOfTheFile()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        ImplPalette palette = ImplPalette.Parse("Red", ImplFile.Create(Kohaku));

        // Act
        library.Import(palette, "mko");

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(ModdedFolder + @"\Red.impl", Arg.Is<byte[]>(bytes => ImplPalette.Parse("Red", bytes).CharacterIndex == Kohaku));
    }

    [Fact]
    public void Export_Palette_WritesItsFileToThePath()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Aegis, "Painter"));

        // Act
        library.Export(palette, @"C:\Shared\Minato.impl");

        // Assert
        _fileSystemMock.Received(1).WriteAllBytes(@"C:\Shared\Minato.impl", Arg.Is<byte[]>(bytes => bytes.SequenceEqual(palette.ToBytes())));
    }

    [Fact]
    public void Delete_PaletteInSeveralFolders_DeletesEveryFileWithThatNameAndCountsThem()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        FolderHolds(
            (AegisFolder + @"\Minato.impl", ImplFile.Create(Aegis)),
            (AegisFolder + @"\Pack\minato.impl", ImplFile.Create(Aegis)),
            (AegisFolder + @"\Gold.impl", ImplFile.Create(Aegis)),
            (AegisFolder + @"\Minato.png", [1, 2, 3]));

        // Act
        int deleted = library.Delete("Aegis", "Minato");

        // Assert
        Assert.Equal(2, deleted);
        _fileSystemMock.Received(1).DeleteFile(AegisFolder + @"\Minato.impl");
        _fileSystemMock.Received(1).DeleteFile(AegisFolder + @"\Pack\minato.impl");
        _fileSystemMock.Received(2).DeleteFile(Arg.Any<string>());
    }

    [Fact]
    public void Delete_CharacterWithoutFolder_DeletesNothing()
    {
        // Arrange
        PaletteLibrary library = CreatePaletteLibrary();
        _fileSystemMock.DirectoryExists(AegisFolder).Returns(false);

        // Act
        int deleted = library.Delete("Aegis", "Minato");

        // Assert
        Assert.Equal(0, deleted);
        _fileSystemMock.DidNotReceive().DeleteFile(Arg.Any<string>());
    }

    private void FolderHolds(params (string Path, byte[] Bytes)[] files)
    {
        _fileSystemMock.DirectoryExists(AegisFolder).Returns(true);
        _fileSystemMock.EnumerateFilesRecursively(AegisFolder).Returns(files.Select(file => file.Path).ToArray());

        foreach ((string path, byte[] bytes) in files)
            _fileSystemMock.ReadAllBytes(path).Returns(bytes);
    }
}
