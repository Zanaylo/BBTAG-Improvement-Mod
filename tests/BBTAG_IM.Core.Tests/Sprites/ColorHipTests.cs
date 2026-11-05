using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Tests.Sprites;

public class ColorHipTests
{
    private static readonly byte[] Red = [0x00, 0x00, 0xFF, 0xFF];
    private static readonly byte[] Clear = [0x00, 0x00, 0x00, 0x00];

    [Fact]
    public void Decode_Runs_RepeatsEachColor()
    {
        // Arrange
        byte[] hip = Hip(2, 2, [.. Clear, 3, .. Red, 1]);

        // Act
        ColorImage image = ColorHip.Decode(hip);

        // Assert
        Assert.Equal((2, 2), (image.Width, image.Height));
        Assert.Equal([.. Clear, .. Clear, .. Clear, .. Red], image.Bgra);
    }

    [Fact]
    public void Decode_NotAHip_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] hip = new byte[0x40];

        // Act
        Exception? exception = Record.Exception(() => ColorHip.Decode(hip));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    [Fact]
    public void Decode_RunsEndBeforeThePicture_ThrowsInvalidSpriteDataException()
    {
        // Arrange
        byte[] hip = Hip(2, 2, [.. Red, 2]);

        // Act
        Exception? exception = Record.Exception(() => ColorHip.Decode(hip));

        // Assert
        Assert.IsType<InvalidSpriteDataException>(exception);
    }

    [Fact]
    public void Encode_Image_GroupsEqualColorsIntoRuns()
    {
        // Arrange
        byte[] template = Hip(2, 2, [.. Clear, 4]);
        ColorImage image = new(2, 2, [.. Red, .. Red, .. Red, .. Clear]);

        // Act
        byte[] hip = ColorHip.Encode(template, image);

        // Assert
        Assert.Equal([.. Red, 3, .. Clear, 1], hip[0x20..]);
    }

    [Fact]
    public void Encode_Image_WritesItsLengthInTheHeader()
    {
        // Arrange
        byte[] template = Hip(2, 2, [.. Clear, 4]);
        ColorImage image = new(2, 2, [.. Red, .. Clear, .. Red, .. Clear]);

        // Act
        byte[] hip = ColorHip.Encode(template, image);

        // Assert
        Assert.Equal(hip.Length, BitConverter.ToInt32(hip, 8));
    }

    [Fact]
    public void Encode_LongRun_SplitsItAt255Pixels()
    {
        // Arrange
        byte[] template = Hip(300, 1, [.. Clear, 255, .. Clear, 45]);
        ColorImage image = new(300, 1, new byte[300 * 4]);

        // Act
        byte[] hip = ColorHip.Encode(template, image);

        // Assert
        Assert.Equal([.. Clear, 255, .. Clear, 45], hip[0x20..]);
    }

    public static byte[] Hip(int width, int height, byte[] runs)
    {
        byte[] hip = new byte[0x20 + runs.Length];
        "HIP\0"u8.CopyTo(hip);
        BitConverter.GetBytes(0x125).CopyTo(hip, 4);
        BitConverter.GetBytes(hip.Length).CopyTo(hip, 8);
        BitConverter.GetBytes(width).CopyTo(hip, 0x10);
        BitConverter.GetBytes(height).CopyTo(hip, 0x14);
        BitConverter.GetBytes(0x110).CopyTo(hip, 0x18);
        runs.CopyTo(hip, 0x20);
        return hip;
    }
}
