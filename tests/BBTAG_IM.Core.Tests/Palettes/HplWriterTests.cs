using System.Text;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Tests.Palettes;

public class HplWriterTests
{
    [Fact]
    public void Write_Palette_HasTheNativeHeaderAndLength()
    {
        // Arrange
        byte[] palette = new byte[1024];

        // Act
        byte[] hpl = HplWriter.Write(palette);

        // Assert
        Assert.Equal(1056, hpl.Length);
        Assert.Equal("HPAL", Encoding.ASCII.GetString(hpl, 0, 4));
        Assert.Equal(1056, BitConverter.ToInt32(hpl, 8));
        Assert.Equal(256, BitConverter.ToInt32(hpl, 12));
    }

    [Fact]
    public void Write_Colors_AreStoredBlueFirst()
    {
        // Arrange
        byte[] palette = new byte[1024];
        palette[4] = 0x11;
        palette[5] = 0x22;
        palette[6] = 0x33;
        palette[7] = 0xFF;

        // Act
        byte[] hpl = HplWriter.Write(palette);

        // Assert
        Assert.Equal([0x33, 0x22, 0x11, 0xFF], hpl.AsSpan(36, 4).ToArray());
    }
}
