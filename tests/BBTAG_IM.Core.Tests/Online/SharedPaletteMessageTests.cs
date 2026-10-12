using BBTAG_IM.Core.Online;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Tests.Online;

public class SharedPaletteMessageTests
{
    private const string Folder = "mko";
    private const int Color = 4;
    private const int EffectFile = 2;
    private const int Entry = 9;

    [Fact]
    public void Decode_EncodedPalette_GivesTheSameCharacterAndColor()
    {
        // Arrange
        SharedPalette palette = CreateSharedPalette();

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(SharedPaletteMessage.Encode(palette));

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(Folder, decoded.Folder);
        Assert.Equal(Color, decoded.Color);
    }

    [Fact]
    public void Decode_EncodedPalette_GivesTheSameColors()
    {
        // Arrange
        SharedPalette palette = CreateSharedPalette();

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(SharedPaletteMessage.Encode(palette));

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal(new PaletteColor(10, 20, 30, 255), decoded.Palette.Colors().Get(EffectFile, Entry));
    }

    [Fact]
    public void Decode_EncodedPalette_GivesTheSameNameAndCreator()
    {
        // Arrange
        SharedPalette palette = CreateSharedPalette();

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(SharedPaletteMessage.Encode(palette));

        // Assert
        Assert.NotNull(decoded);
        Assert.Equal("Night", decoded.Palette.Name);
        Assert.Equal("Someone", decoded.Palette.Creator);
    }

    [Fact]
    public void Decode_OtherData_ReturnsNull()
    {
        // Arrange
        byte[] bytes = new byte[SharedPaletteMessage.Encode(CreateSharedPalette()).Length];

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(bytes);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_CutShort_ReturnsNull()
    {
        // Arrange
        byte[] bytes = SharedPaletteMessage.Encode(CreateSharedPalette());

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(bytes.AsSpan(0, bytes.Length - 1));

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_HeaderOnly_ReturnsNull()
    {
        // Arrange
        byte[] bytes = SharedPaletteMessage.Encode(CreateSharedPalette());

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(bytes.AsSpan(0, SharedPaletteMessage.HeaderLength));

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Decode_NewerVersion_ReturnsNull()
    {
        // Arrange
        byte[] bytes = SharedPaletteMessage.Encode(CreateSharedPalette());
        bytes[SharedPaletteMessage.VersionOffset]++;

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(bytes);

        // Assert
        Assert.Null(decoded);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("a\b")]
    [InlineData("")]
    public void Decode_FolderThatIsNotACharacterCode_ReturnsNull(string folder)
    {
        // Arrange
        byte[] bytes = SharedPaletteMessage.Encode(CreateSharedPalette() with { Folder = folder });

        // Act
        SharedPalette? decoded = SharedPaletteMessage.Decode(bytes);

        // Assert
        Assert.Null(decoded);
    }

    [Fact]
    public void Encode_FolderLongerThanTheLimit_Throws()
    {
        // Arrange
        SharedPalette palette = CreateSharedPalette() with { Folder = new string('a', SharedPaletteMessage.LongestFolder + 1) };

        // Act
        Exception? exception = Record.Exception(() => SharedPaletteMessage.Encode(palette));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    private static SharedPalette CreateSharedPalette()
    {
        PaletteColors colors = new();
        colors.Set(EffectFile, Entry, new PaletteColor(10, 20, 30, 255));
        return new SharedPalette(Folder, Color, ImplPalette.Create("Night", 0, "Someone", "", colors));
    }
}
