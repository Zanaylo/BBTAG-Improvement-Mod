using System.Buffers.Binary;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Palettes;

public class ImplPaletteTests
{
    private const int Aegis = 24;

    [Fact]
    public void Parse_ValidFile_TakesTheNameFromTheFileAndTheRestFromTheHeader()
    {
        // Arrange
        byte[] bytes = ImplFile.Create(Aegis, "Painter", "Blue and gold");

        // Act
        ImplPalette palette = ImplPalette.Parse("Minato", bytes);

        // Assert
        Assert.Equal(("Minato", Aegis, "Painter", "Blue and gold"), (palette.Name, palette.CharacterIndex, palette.Creator, palette.Description));
    }

    [Fact]
    public void Parse_ValidFile_KeepsTheEightPaletteFiles()
    {
        // Arrange
        byte[] bytes = ImplFile.Create(Aegis, fill: 0x42);

        // Act
        ImplPalette palette = ImplPalette.Parse("Minato", bytes);

        // Assert
        Assert.Equal(Enumerable.Repeat((byte)0x42, ImplFile.FileLength), palette.File(7).ToArray());
    }

    [Fact]
    public void Parse_FileTooShort_ThrowsInvalidPaletteException()
    {
        // Arrange
        byte[] bytes = ImplFile.Create(Aegis)[..100];

        // Act
        Exception? exception = Record.Exception(() => ImplPalette.Parse("Minato", bytes));

        // Assert
        InvalidPaletteException paletteException = Assert.IsType<InvalidPaletteException>(exception);
        Assert.Equal("Minato.impl is too short to be a palette.", paletteException.Message);
    }

    [Fact]
    public void Parse_OtherSignature_ThrowsInvalidPaletteException()
    {
        // Arrange
        byte[] bytes = ImplFile.Create(Aegis);
        bytes[0] = 0x58;

        // Act
        Exception? exception = Record.Exception(() => ImplPalette.Parse("Minato", bytes));

        // Assert
        InvalidPaletteException paletteException = Assert.IsType<InvalidPaletteException>(exception);
        Assert.Equal("Minato.impl is not a palette file.", paletteException.Message);
    }

    [Fact]
    public void Parse_OtherDataLength_ThrowsInvalidPaletteException()
    {
        // Arrange
        byte[] bytes = ImplFile.Create(Aegis);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12), 100);

        // Act
        Exception? exception = Record.Exception(() => ImplPalette.Parse("Minato", bytes));

        // Assert
        InvalidPaletteException paletteException = Assert.IsType<InvalidPaletteException>(exception);
        Assert.Equal("Minato.impl holds palette data of an unknown size.", paletteException.Message);
    }

    [Fact]
    public void IsFileEmpty_FileOfZeros_ReturnsTrue()
    {
        // Arrange
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Aegis).WithEmptyFile(3));

        // Act
        bool isEmpty = palette.IsFileEmpty(3);

        // Assert
        Assert.True(isEmpty);
    }

    [Fact]
    public void IsFileEmpty_FileWithColors_ReturnsFalse()
    {
        // Arrange
        ImplPalette palette = ImplPalette.Parse("Minato", ImplFile.Create(Aegis).WithEmptyFile(3));

        // Act
        bool isEmpty = palette.IsFileEmpty(2);

        // Assert
        Assert.False(isEmpty);
    }

    [Fact]
    public void CharacterColors_BlueGreenRedAlphaBytes_ReturnsArgbValues()
    {
        // Arrange
        byte[] bytes = ImplFile.Create(Aegis);
        bytes[ImplFile.FirstFileOffset + 4] = 0x5F;
        bytes[ImplFile.FirstFileOffset + 5] = 0x9B;
        bytes[ImplFile.FirstFileOffset + 6] = 0x69;
        bytes[ImplFile.FirstFileOffset + 7] = 0xFF;
        ImplPalette palette = ImplPalette.Parse("Minato", bytes);

        // Act
        IReadOnlyList<uint> colors = palette.CharacterColors();

        // Assert
        Assert.Equal((256, 0xFF699B5Fu), (colors.Count, colors[1]));
    }

    [Fact]
    public void ToBytes_CreatedPalette_ParsesBackToTheSamePalette()
    {
        // Arrange
        PaletteColors colors = new();
        colors.Set(4, 9, new PaletteColor(1, 2, 3, 4));
        ImplPalette palette = ImplPalette.Create("Minato", Aegis, "Painter", "Blue and gold", colors);

        // Act
        ImplPalette parsed = ImplPalette.Parse("Minato", palette.ToBytes());

        // Assert
        Assert.Equal(("Minato", Aegis, "Painter", "Blue and gold"), (parsed.Name, parsed.CharacterIndex, parsed.Creator, parsed.Description));
        Assert.True(parsed.Colors().SameAs(colors));
    }

    [Fact]
    public void ToBytes_CreatedPalette_WritesTheHeaderOfTheImprovementModFormat()
    {
        // Arrange
        ImplPalette palette = ImplPalette.Create("Minato", Aegis, "", "", new PaletteColors());

        // Act
        byte[] bytes = palette.ToBytes();

        // Assert
        Assert.Equal("IMPL\0\0\0\0"u8.ToArray(), bytes[..8]);
        Assert.Equal((ImplFile.HeaderLength, ImplFile.DataLength), (BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)), BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12))));
        Assert.Equal(ImplFile.HeaderLength + ImplFile.DataLength, bytes.Length);
    }

    [Fact]
    public void ToBytes_DescriptionLongerThanItsField_CutsItAndKeepsTheTerminator()
    {
        // Arrange
        ImplPalette palette = ImplPalette.Create("Minato", Aegis, "", new string('d', 100), new PaletteColors());

        // Act
        ImplPalette parsed = ImplPalette.Parse("Minato", palette.ToBytes());

        // Assert
        Assert.Equal(new string('d', 63), parsed.Description);
    }

    [Fact]
    public void ColorAt_StoredBlueGreenRedAlpha_ReturnsTheColorInRedGreenBlueOrder()
    {
        // Arrange
        byte[] bytes = ImplFile.Create(Aegis);
        int entry = 13;
        new byte[] { 0x30, 0x20, 0x10, 0xFF }.CopyTo(bytes, ImplFile.FirstFileOffset + ImplFile.FileLength * 2 + entry * 4);
        ImplPalette palette = ImplPalette.Parse("Minato", bytes);

        // Act
        PaletteColor color = palette.ColorAt(2, entry);

        // Assert
        Assert.Equal(new PaletteColor(0x10, 0x20, 0x30, 0xFF), color);
    }
}
