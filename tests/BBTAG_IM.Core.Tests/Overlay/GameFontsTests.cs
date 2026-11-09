using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.Core.Sprites;
using BBTAG_IM.Core.Tests.Sprites;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Overlay;

public class GameFontsTests
{
    private const string GameData = @"D:\BBTAG\data";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Interface_GridOfTheGameFont_HoldsEveryPrintableCharacterTheMeterWrites()
    {
        // Act
        string characters = GameFonts.Interface.Grid.Characters;

        // Assert
        Assert.All("0123456789+-/ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz()%:.", character => Assert.Contains(character, characters));
        Assert.Equal('~', characters[92]);
    }

    [Fact]
    public void Load_ArchiveWithTheFontPicture_MeasuresTheSheet()
    {
        // Arrange
        Archive(SpriteFiles.Fpac(("fhd.pac", SpriteFiles.Fpac((GameFonts.Interface.FileName, OpaqueFontPicture())))));

        // Act
        GlyphSheet? sheet = GameFonts.Load(new GameArchiveFile(_fileSystemMock, [GameData]), GameFonts.Interface);

        // Assert
        Assert.NotNull(sheet);
        Assert.NotNull(sheet.Glyph('A'));
    }

    [Fact]
    public void Load_FontMissing_ReturnsNull()
    {
        // Act
        GlyphSheet? sheet = GameFonts.Load(new GameArchiveFile(_fileSystemMock, [GameData]), GameFonts.Interface);

        // Assert
        Assert.Null(sheet);
    }

    [Fact]
    public void Load_DamagedPicture_ReturnsNull()
    {
        // Arrange
        Archive(SpriteFiles.Fpac((GameFonts.Interface.FileName, [1, 2, 3])));

        // Act
        GlyphSheet? sheet = GameFonts.Load(new GameArchiveFile(_fileSystemMock, [GameData]), GameFonts.Interface);

        // Assert
        Assert.Null(sheet);
    }

    private static byte[] OpaqueFontPicture()
    {
        const int width = 150;
        const int height = 75;
        var runs = new List<byte>();

        for (var pixel = 0; pixel < width * height; pixel += byte.MaxValue)
            runs.AddRange([0, 0, 0, 0xFF, (byte)Math.Min(byte.MaxValue, width * height - pixel)]);

        return ColorHipTests.Hip(width, height, [.. runs]);
    }

    private void Archive(byte[] bytes)
    {
        var path = Path.Combine(GameData, GameFonts.Interface.Archive);
        _fileSystemMock.FileExists(path).Returns(true);
        _fileSystemMock.ReadAllBytes(path).Returns(bytes);
    }
}
