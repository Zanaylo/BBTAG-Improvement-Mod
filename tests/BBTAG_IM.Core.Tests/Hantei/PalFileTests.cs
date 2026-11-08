using BBTAG_IM.Core.Hantei;

namespace BBTAG_IM.Core.Tests.Hantei;

public class PalFileTests
{
    [Fact]
    public void Read_NoPalettes_Throws()
    {
        // Arrange
        byte[] bytes = new byte[8];

        // Act
        Exception? exception = Record.Exception(() => PalFile.Read(bytes));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }

    [Fact]
    public void Read_Palettes_SplitsEveryPaletteOfTheCount()
    {
        // Arrange
        byte[] bytes = new byte[16 + 2 * 1024];
        BitConverter.TryWriteBytes(bytes.AsSpan(12), 2);
        bytes[16 + 1024] = 0xAB;

        // Act
        IReadOnlyList<byte[]> palettes = PalFile.Read(bytes);

        // Assert
        Assert.Equal(2, palettes.Count);
        Assert.Equal(1024, palettes[1].Length);
        Assert.Equal(0xAB, palettes[1][0]);
    }

    [Fact]
    public void Read_FileShorterThanItsCount_Throws()
    {
        // Arrange
        byte[] bytes = new byte[16 + 1024];
        BitConverter.TryWriteBytes(bytes.AsSpan(12), 2);

        // Act
        Exception? exception = Record.Exception(() => PalFile.Read(bytes));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }
}
