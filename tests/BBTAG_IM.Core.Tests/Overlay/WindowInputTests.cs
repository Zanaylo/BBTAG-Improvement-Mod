using System.Numerics;
using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Overlay;

public class WindowInputTests
{
    private const uint KeyDown = 0x100;
    private const uint SystemKeyDown = 0x104;
    private const uint KeyUp = 0x101;
    private const uint SystemKeyUp = 0x105;
    private const uint Character = 0x102;
    private const uint MouseMove = 0x200;
    private const uint LeftButtonDown = 0x201;
    private const uint LeftButtonUp = 0x202;
    private const uint LeftButtonDoubleClick = 0x203;
    private const uint RightButtonDown = 0x204;
    private const uint RightButtonDoubleClick = 0x206;
    private const uint MiddleButtonUp = 0x208;
    private const uint MiddleButtonDoubleClick = 0x209;
    private const uint MouseWheel = 0x20A;
    private const uint Paint = 0x000F;
    private const ulong F1 = 0x70;
    private const long RepeatFlag = 1L << 30;

    [Fact]
    public void Parse_KeyPressed_ReturnsTheKey()
    {
        // Act
        WindowInput? input = WindowInput.Parse(KeyDown, F1, 0);

        // Assert
        Assert.Equal(new KeyPressed(0x70), input);
    }

    [Fact]
    public void Parse_SystemKeyPressed_ReturnsTheKey()
    {
        // Act
        WindowInput? input = WindowInput.Parse(SystemKeyDown, F1, 0);

        // Assert
        Assert.Equal(new KeyPressed(0x70), input);
    }

    [Theory]
    [InlineData(KeyUp)]
    [InlineData(SystemKeyUp)]
    public void Parse_KeyReleased_ReturnsTheKey(uint message)
    {
        // Act
        WindowInput? input = WindowInput.Parse(message, F1, 0);

        // Assert
        Assert.Equal(new KeyReleased(0x70), input);
    }

    [Fact]
    public void Parse_CharacterTyped_ReturnsTheCharacter()
    {
        // Act
        WindowInput? input = WindowInput.Parse(Character, 'a', RepeatFlag);

        // Assert
        Assert.Equal(new CharacterTyped('a'), input);
    }

    [Fact]
    public void Parse_KeyHeldDownAndRepeating_ReturnsNothing()
    {
        // Act
        WindowInput? input = WindowInput.Parse(KeyDown, F1, RepeatFlag);

        // Assert
        Assert.Null(input);
    }

    [Fact]
    public void Parse_MouseMoveWithNegativeX_ReadsBothSignedCoordinates()
    {
        // Arrange
        long position = (300L << 16) | 0xFFF6;

        // Act
        WindowInput? input = WindowInput.Parse(MouseMove, 0, position);

        // Assert
        Assert.Equal(new MouseMoved(-10, 300), input);
    }

    [Theory]
    [InlineData(LeftButtonDown, 0, true)]
    [InlineData(LeftButtonUp, 0, false)]
    [InlineData(RightButtonDown, 1, true)]
    [InlineData(MiddleButtonUp, 2, false)]
    [InlineData(LeftButtonDoubleClick, 0, true)]
    [InlineData(RightButtonDoubleClick, 1, true)]
    [InlineData(MiddleButtonDoubleClick, 2, true)]
    public void Parse_MouseButton_ReturnsWhichButtonAndItsState(uint message, int button, bool down)
    {
        // Act
        WindowInput? input = WindowInput.Parse(message, 0, 0);

        // Assert
        Assert.Equal(new MouseButton(button, down), input);
    }

    [Fact]
    public void Parse_WheelTurnedBackOneNotch_ReturnsMinusOne()
    {
        // Arrange
        ulong wheel = 0xFF88UL << 16;

        // Act
        WindowInput? input = WindowInput.Parse(MouseWheel, wheel, 0);

        // Assert
        Assert.Equal(new MouseWheelTurned(-1), input);
    }

    [Fact]
    public void Parse_OtherMessage_ReturnsNothing()
    {
        // Act
        WindowInput? input = WindowInput.Parse(Paint, 0, 0);

        // Assert
        Assert.Null(input);
    }

    [Fact]
    public void ToDisplay_WindowSmallerThanTheBackBuffer_ScalesThePointer()
    {
        // Act
        Vector2 point = WindowInput.ToDisplay(new MouseMoved(480, 270), new Vector2(960, 540), new Vector2(1920, 1080));

        // Assert
        Assert.Equal(new Vector2(960, 540), point);
    }

    [Fact]
    public void ToDisplay_WindowSizeUnknown_KeepsThePointer()
    {
        // Act
        Vector2 point = WindowInput.ToDisplay(new MouseMoved(480, 270), Vector2.Zero, new Vector2(1920, 1080));

        // Assert
        Assert.Equal(new Vector2(480, 270), point);
    }
}
