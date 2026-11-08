using BBTAG_IM.Core.Hantei.Games;

namespace BBTAG_IM.Core.Tests.Hantei.Games;

public class MbtlCipherTests
{
    [Fact]
    public void Decode_FirstTwoBytes_LoseTheirFixedMask()
    {
        // Arrange
        byte[] coded = [0xA5 ^ 0x41, 0x18 ^ 0x42];

        // Act
        byte[] plain = MbtlCipher.Decode(coded, new byte[1024]);

        // Assert
        Assert.Equal([0x41, 0x42], plain);
    }

    [Fact]
    public void Decode_LaterBytes_TakeTheKeyFromTheFileHead()
    {
        // Arrange
        byte[] key = new byte[1024];
        key[0x381] = 0x0F;
        key[0x382] = 0xF0;

        // Act
        byte[] plain = MbtlCipher.Decode([0xA5, 0x18, 0x11, 0x22], key);

        // Assert
        Assert.Equal([0, 0, 0x11 ^ 0xF0, 0x22 ^ 0x0F], plain);
    }

    [Fact]
    public void Decode_ShortFile_ReturnsItUntouched()
    {
        // Arrange
        byte[] coded = [9];

        // Act
        byte[] plain = MbtlCipher.Decode(coded, new byte[1024]);

        // Assert
        Assert.Equal(coded, plain);
    }
}
