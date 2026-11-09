using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class GameAddressesTests
{
    private const uint CodeAddress = 0x401000;
    private const int CodeSize = 0x400;
    private const uint GameObject = 0x00E59100;
    private const uint BattleObject = 0x014A77A0;
    private const uint CharacterList = 0x0157D3F8;
    private const uint CameraList = 0x00C0A788;

    public CodeImage CreateCodeImage(byte[] code) => new([new CodeBlock(CodeAddress, code)]);

    [Fact]
    public void Locate_CodeWithTheFourGetterCalls_ReturnsEachObjectAddress()
    {
        // Arrange
        CodeImage image = CreateCodeImage(CreateGameCode().ToArray());

        // Act
        GameAddresses addresses = GameAddresses.Locate(image);

        // Assert
        Assert.Equal(new GameAddresses(GameObject, BattleObject, CharacterList, CameraList), addresses);
    }

    [Fact]
    public void Locate_CameraCallMissing_ThrowsGameLayoutExceptionNamingTheCameraList()
    {
        // Arrange
        byte[] code = CreateGameCode().ToArray();
        Array.Fill(code, GameCode.Breakpoint, 0x0C0, 0x20);
        CodeImage image = CreateCodeImage(code);

        // Act
        Exception? exception = Record.Exception(() => GameAddresses.Locate(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Camera list was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Locate_OneGameStatePlaceChangedByAnotherPatch_StillReturnsTheGameObject()
    {
        // Arrange
        byte[] code = CreateGameCode()
            .Bytes(0x100, "?? ?? ?? ?? ?? C7 80 18 01 00 00 0B 00 00 00")
            .CallAt(0x100, 0x200)
            .Bytes(0x120, "?? ?? ?? ?? ?? C7 80 18 01 00 00 23 00 00 00")
            .CallAt(0x120, 0x200)
            .Bytes(0x005, "90 90 90 90 90 90 90 90 90 90")
            .ToArray();
        CodeImage image = CreateCodeImage(code);

        // Act
        GameAddresses addresses = GameAddresses.Locate(image);

        // Assert
        Assert.Equal(GameObject, addresses.Game);
    }

    private static GameCode CreateGameCode() =>
        new GameCode(CodeSize)
            .Bytes(0x000, "?? ?? ?? ?? ?? C7 80 18 01 00 00 17 00 00 00 E8")
            .CallAt(0x000, 0x200)
            .Bytes(0x040, "8D 45 A0 6A 0F 50 6A 05 E8 ?? ?? ?? ?? 83 C4 10 ?? ?? ?? ?? ??")
            .CallAt(0x050, 0x240)
            .Bytes(0x080, "56 68 00 05 00 00 6A 00 6A 00 8B F1 E8 ?? ?? ?? ?? 81 A6 50 01 00 00 FF F0 FF FF ?? ?? ?? ?? ??")
            .CallAt(0x09B, 0x280)
            .Bytes(0x0C0, "C7 45 98 00 00 00 00 ?? ?? ?? ?? ?? 8D 4D 98 51 8B C8 E8")
            .CallAt(0x0C7, 0x2C0)
            .SingletonGetterAt(0x200, GameObject)
            .SingletonGetterAt(0x240, BattleObject)
            .SingletonGetterAt(0x280, CharacterList)
            .SingletonGetterAt(0x2C0, CameraList);
}
