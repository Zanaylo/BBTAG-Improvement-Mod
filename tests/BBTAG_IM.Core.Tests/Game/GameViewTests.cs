using System.Numerics;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Game;

public class GameViewTests
{
    private const uint GameObject = 0x00E59100;
    private const uint BattleObject = 0x014A77A0;
    private const uint CharacterList = 0x0157D3F8;
    private const uint CameraList = 0x00C0A788;
    private const uint CameraArray = 0x05000000;
    private const uint MainCamera = 0x05100000;

    private readonly FakeGameMemory _memoryMock = new();

    public GameView CreateGameView() => new(_memoryMock, new GameAddresses(GameObject, BattleObject, CharacterList, CameraList));

    [Fact]
    public void Mode_TrainingWrittenInTheGameObject_ReturnsTraining()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(GameObject, 0x3000).Int32At(GameObject + 0x114, 6);

        // Act
        GameMode mode = view.Mode;

        // Assert
        Assert.Equal(GameMode.Training, mode);
    }

    [Fact]
    public void State_MatchWrittenInTheGameObject_ReturnsMatch()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(GameObject, 0x3000).Int32At(GameObject + 0x118, 11);

        // Act
        GameState state = view.State;

        // Assert
        Assert.Equal(GameState.Match, state);
    }

    [Theory]
    [InlineData(6, 11, true)]
    [InlineData(6, 6, false)]
    [InlineData(1, 11, false)]
    public void InTrainingMatch_ModeAndState_IsTrueOnlyDuringATrainingMatch(int mode, int state, bool expected)
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(GameObject, 0x3000).Int32At(GameObject + 0x114, mode).Int32At(GameObject + 0x118, state);

        // Act
        bool inTrainingMatch = view.InTrainingMatch;

        // Assert
        Assert.Equal(expected, inTrainingMatch);
    }

    [Theory]
    [InlineData(0, 0x00E5B8B0u)]
    [InlineData(3, 0x00E5B928u)]
    public void PaletteIndexAddress_Slot_PointsIntoThePlayerChoicesOfTheGameObject(int slot, uint expected)
    {
        // Arrange
        GameView view = CreateGameView();

        // Act
        uint address = view.PaletteIndexAddress(slot);

        // Assert
        Assert.Equal(expected, address);
    }

    [Theory]
    [InlineData(0, 0x014A82E4u)]
    [InlineData(2, 0x014E9A9Cu)]
    public void PaletteTableAddress_Slot_PointsIntoTheCharacterGraphicsOfTheBattleObject(int slot, uint expected)
    {
        // Arrange
        GameView view = CreateGameView();

        // Act
        uint address = view.PaletteTableAddress(slot);

        // Assert
        Assert.Equal(expected, address);
    }

    [Fact]
    public void Character_SlotFilled_ReturnsTheCharacterObject()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(CharacterList, 16).UInt32At(CharacterList + 8, 0x09000000);

        // Act
        uint? character = view.Character(2);

        // Assert
        Assert.Equal(0x09000000u, character);
    }

    [Fact]
    public void CharacterIndex_SlotFilled_ReadsTheIndexFromTheCharacterObject()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(CharacterList, 16).UInt32At(CharacterList + 4, 0x09000000);
        _memoryMock.Map(0x09000000, 0x100).Int32At(0x09000034, 24);

        // Act
        int? index = view.CharacterIndex(1);

        // Assert
        Assert.Equal(24, index);
    }

    [Fact]
    public void CharacterIndex_SlotEmpty_ReturnsNull()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(CharacterList, 16);

        // Act
        int? index = view.CharacterIndex(1);

        // Assert
        Assert.Null(index);
    }

    [Fact]
    public void EntityList_BattleNotStarted_ReturnsNull()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(BattleObject, 0xA5000);

        // Act
        uint? entities = view.EntityList;

        // Assert
        Assert.Null(entities);
    }

    [Fact]
    public void EntityList_BattleRunning_ReturnsTheListAddress()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(BattleObject, 0xA5000).UInt32At(BattleObject + 0xA4210, 0x07000000);

        // Act
        uint? entities = view.EntityList;

        // Assert
        Assert.Equal(0x07000000u, entities);
    }

    [Fact]
    public void Camera_NoCameraYet_ReturnsNull()
    {
        // Arrange
        GameView view = CreateGameView();
        _memoryMock.Map(CameraList, 12).UInt32At(CameraList, CameraArray);

        // Act
        CameraView? camera = view.Camera();

        // Assert
        Assert.Null(camera);
    }

    [Fact]
    public void Camera_MainCameraPresent_ReadsItsViewAndProjectionMatrices()
    {
        // Arrange
        GameView view = CreateGameView();
        Matrix4x4 viewMatrix = Matrix4x4.CreateTranslation(1, 2, 3);
        Matrix4x4 projection = Matrix4x4.CreateScale(4);
        _memoryMock.Map(CameraList, 12).UInt32At(CameraList, CameraArray).UInt32At(CameraList + 8, 1);
        _memoryMock.Map(CameraArray, 4).UInt32At(CameraArray, MainCamera);
        _memoryMock.Map(MainCamera, 0x100).BytesAt(MainCamera + 0x4C, MatrixBytes(viewMatrix)).BytesAt(MainCamera + 0x9C, MatrixBytes(projection));

        // Act
        CameraView? camera = view.Camera();

        // Assert
        Assert.Equal(new CameraView(viewMatrix, projection), camera);
    }

    private static byte[] MatrixBytes(Matrix4x4 matrix)
    {
        float[] values =
        [
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44,
        ];

        return values.SelectMany(BitConverter.GetBytes).ToArray();
    }
}
