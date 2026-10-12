using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Palettes;

public class PaletteTableTests
{
    private const uint TableField = 0x014A82E4;
    private const uint Area = 0x20000000;
    private const int Aegis = 24;
    private const int Color = 2;

    private readonly FakeGameMemory _memoryMock = new();

    public PaletteTable CreatePaletteTable() => new(_memoryMock, TableField);

    [Fact]
    public void IsLoaded_ChainToTheCharacterFileResolves_ReturnsTrue()
    {
        // Arrange
        PaletteMemory.Lay(_memoryMock, TableField, Area, Color);
        PaletteTable table = CreatePaletteTable();

        // Act
        bool isLoaded = table.IsLoaded(Color);

        // Assert
        Assert.True(isLoaded);
    }

    [Fact]
    public void IsLoaded_TableNotCreatedYet_ReturnsFalse()
    {
        // Arrange
        _memoryMock.Map(TableField, sizeof(uint));
        PaletteTable table = CreatePaletteTable();

        // Act
        bool isLoaded = table.IsLoaded(Color);

        // Assert
        Assert.False(isLoaded);
    }

    [Fact]
    public void IsLoaded_ColorNotLoaded_ReturnsFalse()
    {
        // Arrange
        PaletteMemory.Lay(_memoryMock, TableField, Area, Color);
        PaletteTable table = CreatePaletteTable();

        // Act
        bool isLoaded = table.IsLoaded(Color + 1);

        // Assert
        Assert.False(isLoaded);
    }

    [Fact]
    public void Write_Colors_WritesEachFileAndItsMirror()
    {
        // Arrange
        PaletteMemory.Lay(_memoryMock, TableField, Area, Color);
        PaletteTable table = CreatePaletteTable();
        PaletteColors colors = ImplPalette.Parse("Minato", ImplFile.Create(Aegis, fill: 0x42)).Colors();
        uint effect = PaletteMemory.FileAddress(Area, Color, 5);

        // Act
        table.Write(Color, colors);

        // Assert
        Assert.Equal(colors.File(5).ToArray(), _memoryMock.BytesFrom(effect, PaletteMemory.FileLength));
        Assert.Equal(colors.File(5).ToArray(), _memoryMock.BytesFrom(effect + PaletteMemory.MirrorOffset, PaletteMemory.FileLength));
    }

    [Fact]
    public void Write_CharacterWithoutAnEffectFile_WritesTheOtherFiles()
    {
        // Arrange
        PaletteMemory.Lay(_memoryMock, TableField, Area, Color);
        PaletteMemory.RemoveFile(_memoryMock, Area, Color, 7);
        PaletteTable table = CreatePaletteTable();
        PaletteColors colors = ImplPalette.Parse("Minato", ImplFile.Create(Aegis, fill: 0x42)).Colors();

        // Act
        int written = table.Write(Color, colors);

        // Assert
        Assert.Equal(7, written);
    }

    [Fact]
    public void Read_LoadedColor_ReturnsTheColorsOfEachFile()
    {
        // Arrange
        PaletteMemory.Lay(_memoryMock, TableField, Area, Color);
        _memoryMock.BytesAt(PaletteMemory.FileAddress(Area, Color, 3), [0x30, 0x20, 0x10, 0xFF]);
        PaletteTable table = CreatePaletteTable();

        // Act
        PaletteColors colors = table.Read(Color);

        // Assert
        Assert.Equal(new PaletteColor(0x10, 0x20, 0x30, 0xFF), colors.Get(3, 0));
    }

    [Fact]
    public void Read_CharacterWithoutAnEffectFile_LeavesThatFileEmpty()
    {
        // Arrange
        PaletteMemory.Lay(_memoryMock, TableField, Area, Color);
        _memoryMock.BytesAt(PaletteMemory.FileAddress(Area, Color, 7), [0x30, 0x20, 0x10, 0xFF]);
        PaletteMemory.RemoveFile(_memoryMock, Area, Color, 7);
        PaletteTable table = CreatePaletteTable();

        // Act
        PaletteColors colors = table.Read(Color);

        // Assert
        Assert.Equal(default, colors.Get(7, 0));
    }

    [Fact]
    public void HasFile_FileMissing_ReturnsFalse()
    {
        // Arrange
        PaletteMemory.Lay(_memoryMock, TableField, Area, Color);
        PaletteMemory.RemoveFile(_memoryMock, Area, Color, 7);
        PaletteTable table = CreatePaletteTable();

        // Act
        bool hasFile = table.HasFile(Color, 7);

        // Assert
        Assert.False(hasFile);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(19, 18)]
    [InlineData(98, 97)]
    public void PartnerOf_Color_ReturnsAColorThatIsAlwaysLoaded(int color, int expected)
    {
        // Arrange
        // Act
        int partner = PaletteTable.PartnerOf(color);

        // Assert
        Assert.Equal(expected, partner);
    }
}
