using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class CollisionFunctionsTests
{
    private const uint CodeAddress = 0x401000;
    private const int CodeSize = 0x400;
    private const uint WorldBoxOffset = 0x40;
    private const uint PositionXOffset = 0x100;
    private const uint PositionYOffset = 0x200;
    private const uint LoadBoxesOffset = 0x300;

    private const string WorldBoxCode = "55 8B EC 83 EC 24 53 8B D9 56 57 89 5D F0 8D 8B 30 02 00 00 E8 A7 C3 27 00 8B 55 0C 8D 4B 3C FF 75 08";
    private const string PositionXCode = "56 8B F1 8B 8E 04 02 00 00 85 C9 75 2C 8B 8E 08 02 00 00 85 C9 75 22 8B 8E 24 02 00 00 85 C9 75 18 8B 86 68 02 00 00 25 00 20 00 20 3D 00 20 00 20 75 13 8B 8E 9C 04 00 00 E8 C2 FF FF FF 03 86 78 02 00 00";
    private const string PositionYCode = "56 8B F1 8B 8E 04 02 00 00 85 C9 75 39 8B 8E 0C 02 00 00 85 C9 75 2F 8B 8E 24 02 00 00 85 C9 74 0D E8";
    private const string LoadBoxesCode = "55 8B EC 8B 55 08 83 EC 08 56 8B F1 85 D2 75 67 89 96 BC 00 00 00 8D 46 10 89 96 9C 00 00 00 B9 11 00 00 00";

    [Fact]
    public void Locate_GameCode_FindsTheBoxAndPositionFunctions()
    {
        // Arrange
        CodeImage image = CreateImage(WorldBoxCode, PositionXCode, PositionYCode);

        // Act
        CollisionFunctions functions = CollisionFunctions.Locate(image);

        // Assert
        Assert.Equal(
            new CollisionFunctions(CodeAddress + WorldBoxOffset, CodeAddress + PositionXOffset, CodeAddress + PositionYOffset, CodeAddress + LoadBoxesOffset),
            functions);
    }

    [Fact]
    public void Locate_CodeWithoutTheBoxLoader_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateImage(WorldBoxCode, PositionXCode, PositionYCode, "90");

        // Act
        Exception? exception = Record.Exception(() => CollisionFunctions.Locate(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Collision file loader was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Locate_CodeWithoutTheBoxFunction_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateImage("90", PositionXCode, PositionYCode);

        // Act
        Exception? exception = Record.Exception(() => CollisionFunctions.Locate(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Collision box function was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Locate_CodeWithoutThePositionFunction_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateImage(WorldBoxCode, "90", PositionYCode);

        // Act
        Exception? exception = Record.Exception(() => CollisionFunctions.Locate(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Object position function was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    [Fact]
    public void Locate_CodeWithoutTheHeightFunction_ThrowsGameLayoutException()
    {
        // Arrange
        CodeImage image = CreateImage(WorldBoxCode, PositionXCode, "90");

        // Act
        Exception? exception = Record.Exception(() => CollisionFunctions.Locate(image));

        // Assert
        GameLayoutException layoutException = Assert.IsType<GameLayoutException>(exception);
        Assert.Equal("Object height function was not found in the game code. This game version is not supported.", layoutException.Message);
    }

    private static CodeImage CreateImage(string worldBox, string positionX, string positionY, string loadBoxes = LoadBoxesCode) =>
        new([new CodeBlock(CodeAddress, new GameCode(CodeSize)
            .Bytes(WorldBoxOffset, worldBox)
            .Bytes(PositionXOffset, positionX)
            .Bytes(PositionYOffset, positionY)
            .Bytes(LoadBoxesOffset, loadBoxes)
            .ToArray())]);
}
