using System.Numerics;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Hitboxes.Editing;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class BoxDragTests
{
    private const int Precision = 3;
    private const float PixelsPerUnit = 9.6f;

    private static readonly Viewport FullHd = new(0, 0, 1920, 1080);
    private static readonly CameraView HundredUnitsAcross = new(Matrix4x4.Identity, Matrix4x4.CreateScale(1 / 36.8f, 1 / 36.8f, 1));
    private static readonly BoxAxes GrowingRight = new(PixelsPerUnit, PixelsPerUnit);
    private static readonly BoxAxes GrowingLeft = new(-PixelsPerUnit, PixelsPerUnit);
    private static readonly JonbinBox Box = new(0, -40, -300, 80, 200);
    private static readonly ScreenRect Rectangle = new(100, 200, 180, 400);

    [Fact]
    public void Axes_CharacterFacingRight_GrowsTowardsItsBackAndDownOnScreen()
    {
        // Act
        BoxAxes axes = BoxAxes.From(HundredUnitsAcross, FullHd, new GamePoint(0, 0), facingLeft: false);

        // Assert
        Assert.Equal(-PixelsPerUnit, axes.PixelsPerUnitX, Precision);
        Assert.Equal(5.4f, axes.PixelsPerUnitY, Precision);
    }

    [Fact]
    public void Axes_CharacterFacingLeft_GrowsRightOnScreen()
    {
        // Act
        BoxAxes axes = BoxAxes.From(HundredUnitsAcross, FullHd, new GamePoint(0, 0), facingLeft: true);

        // Assert
        Assert.Equal(PixelsPerUnit, axes.PixelsPerUnitX, Precision);
    }

    [Fact]
    public void HandleAt_MouseInsideTheBox_MovesIt()
    {
        // Act
        BoxHandle handle = BoxHandles.At(Rectangle, new Vector2(140, 300));

        // Assert
        Assert.Equal(BoxHandle.Move, handle);
    }

    [Fact]
    public void HandleAt_MouseOnTheRightEdge_ResizesThatEdge()
    {
        // Act
        BoxHandle handle = BoxHandles.At(Rectangle, new Vector2(182, 300));

        // Assert
        Assert.Equal(BoxHandle.Right, handle);
    }

    [Fact]
    public void HandleAt_MouseOnTheTopLeftCorner_ResizesBothEdges()
    {
        // Act
        BoxHandle handle = BoxHandles.At(Rectangle, new Vector2(98, 203));

        // Assert
        Assert.Equal(BoxHandle.TopLeft, handle);
    }

    [Fact]
    public void HandleAt_MouseAwayFromTheBox_IsNone()
    {
        // Act
        BoxHandle handle = BoxHandles.At(Rectangle, new Vector2(300, 300));

        // Assert
        Assert.Equal(BoxHandle.None, handle);
    }

    [Fact]
    public void Rectangle_RotatedCorners_SpansAllOfThem()
    {
        // Arrange
        Quad quad = new(new Vector2(180, 400), new Vector2(100, 400), new Vector2(100, 200), new Vector2(180, 200));

        // Act
        ScreenRect rectangle = ScreenRect.Around(quad);

        // Assert
        Assert.Equal(Rectangle, rectangle);
    }

    [Fact]
    public void Drag_MoveWhereBoxesGrowRight_MovesTheBoxByWholeUnits()
    {
        // Act
        JonbinBox moved = BoxDrag.Apply(Box, BoxHandle.Move, new Vector2(97, -48), GrowingRight);

        // Assert
        Assert.Equal(Box with { X = -30, Y = -305 }, moved);
    }

    [Fact]
    public void Drag_MoveWhereBoxesGrowLeft_MovesTheBoxTheOtherWay()
    {
        // Act
        JonbinBox moved = BoxDrag.Apply(Box, BoxHandle.Move, new Vector2(96, 0), GrowingLeft);

        // Assert
        Assert.Equal(Box with { X = -50 }, moved);
    }

    [Fact]
    public void Drag_RightEdgeWhereBoxesGrowRight_WidensTheBox()
    {
        // Act
        JonbinBox resized = BoxDrag.Apply(Box, BoxHandle.Right, new Vector2(96, 0), GrowingRight);

        // Assert
        Assert.Equal(Box with { Width = 90 }, resized);
    }

    [Fact]
    public void Drag_LeftEdgeWhereBoxesGrowLeft_MovesTheFarEndOfTheBox()
    {
        // Act
        JonbinBox resized = BoxDrag.Apply(Box, BoxHandle.Left, new Vector2(-96, 0), GrowingLeft);

        // Assert
        Assert.Equal(Box with { Width = 90 }, resized);
    }

    [Fact]
    public void Drag_TopEdgeUpwards_MakesTheBoxTaller()
    {
        // Act
        JonbinBox resized = BoxDrag.Apply(Box, BoxHandle.Top, new Vector2(0, -96), GrowingRight);

        // Assert
        Assert.Equal(Box with { Y = -310, Height = 210 }, resized);
    }

    [Fact]
    public void Drag_BottomRightCorner_ResizesBothEdges()
    {
        // Act
        JonbinBox resized = BoxDrag.Apply(Box, BoxHandle.BottomRight, new Vector2(48, 96), GrowingRight);

        // Assert
        Assert.Equal(Box with { Width = 85, Height = 210 }, resized);
    }

    [Fact]
    public void Drag_LeftEdgeOfABoxWithNegativeWidth_WidensItAndKeepsTheWidthNegative()
    {
        // Arrange
        JonbinBox mirrored = new(0, 40, -300, -80, 200);

        // Act
        JonbinBox resized = BoxDrag.Apply(mirrored, BoxHandle.Left, new Vector2(-96, 0), GrowingLeft);

        // Assert
        Assert.Equal(mirrored with { X = 50, Width = -90 }, resized);
    }

    [Fact]
    public void Drag_RightEdgeOfABoxWithNegativeWidth_MovesTheEdgeUnderTheMouse()
    {
        // Arrange
        JonbinBox mirrored = new(0, 40, -300, -80, 200);

        // Act
        JonbinBox resized = BoxDrag.Apply(mirrored, BoxHandle.Right, new Vector2(-96, 0), GrowingLeft);

        // Assert
        Assert.Equal(mirrored with { Width = -70 }, resized);
    }

    [Fact]
    public void Drag_BottomEdgeOfABoxWithNegativeHeight_MakesItTaller()
    {
        // Arrange
        JonbinBox upsideDown = new(0, -40, -100, 80, -200);

        // Act
        JonbinBox resized = BoxDrag.Apply(upsideDown, BoxHandle.Bottom, new Vector2(0, 96), GrowingRight);

        // Assert
        Assert.Equal(upsideDown with { Y = -90, Height = -210 }, resized);
    }

    [Fact]
    public void Drag_EdgePastTheOtherSide_KeepsTheSmallestBox()
    {
        // Act
        JonbinBox resized = BoxDrag.Apply(Box, BoxHandle.Right, new Vector2(-2000, 0), GrowingRight);

        // Assert
        Assert.Equal(Box with { Width = BoxDrag.SmallestSide }, resized);
    }
}
