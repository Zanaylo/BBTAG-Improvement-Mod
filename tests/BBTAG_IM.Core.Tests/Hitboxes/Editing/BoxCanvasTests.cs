using System.Numerics;
using BBTAG_IM.Core.Hitboxes.Editing;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class BoxCanvasTests
{
    private const int Precision = 3;

    private static readonly Vector2 CanvasSize = new(200, 200);

    [Fact]
    public void Fit_OneBox_FillsMostOfTheCanvasWithItAndTheOrigin()
    {
        // Arrange
        JonbinBox box = new(0, 0, -100, 100, 100);

        // Act
        BoxCanvas canvas = BoxCanvas.Fit([box], Vector2.Zero, CanvasSize);

        // Assert
        Assert.Equal(1.8f, canvas.Scale, Precision);
        Assert.Equal(new Vector2(190, 190), canvas.Origin);
    }

    [Fact]
    public void Fit_NoBoxes_PutsTheOriginInTheMiddle()
    {
        // Act
        BoxCanvas canvas = BoxCanvas.Fit([], new Vector2(10, 20), CanvasSize);

        // Assert
        Assert.Equal(new Vector2(110, 120), canvas.Origin);
    }

    [Fact]
    public void ToCanvas_Box_DrawsTheCharacterFacingRightSoXGrowsLeft()
    {
        // Arrange
        BoxCanvas canvas = new(2, new Vector2(190, 190));

        // Act
        ScreenRect rectangle = canvas.ToCanvas(new JonbinBox(0, 10, -100, 50, 40));

        // Assert
        Assert.Equal(new ScreenRect(70, -10, 170, 70), rectangle);
    }

    [Fact]
    public void ToCanvas_BoxWithNegativeWidthAndHeight_ReturnsTheRectangleItCovers()
    {
        // Arrange
        BoxCanvas canvas = new(2, new Vector2(190, 190));

        // Act
        ScreenRect rectangle = canvas.ToCanvas(new JonbinBox(0, 60, -60, -50, -40));

        // Assert
        Assert.Equal(new ScreenRect(70, -10, 170, 70), rectangle);
    }

    [Fact]
    public void Fit_BoxWithNegativeWidth_FitsLikeTheSameBoxWithAPositiveWidth()
    {
        // Arrange
        JonbinBox negative = new(0, 60, -349, -118, 349);
        JonbinBox positive = new(0, -58, -349, 118, 349);

        // Act
        BoxCanvas fitted = BoxCanvas.Fit([negative], Vector2.Zero, CanvasSize);

        // Assert
        Assert.Equal(BoxCanvas.Fit([positive], Vector2.Zero, CanvasSize), fitted);
    }

    [Fact]
    public void Axes_Canvas_GrowsLeftAndDownByItsScale()
    {
        // Arrange
        BoxCanvas canvas = new(2, Vector2.Zero);

        // Act
        BoxAxes axes = canvas.Axes;

        // Assert
        Assert.Equal(new BoxAxes(-2, 2), axes);
    }
}
