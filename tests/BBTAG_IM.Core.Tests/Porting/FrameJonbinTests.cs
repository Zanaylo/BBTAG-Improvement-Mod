using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Porting;

namespace BBTAG_IM.Core.Tests.Porting;

public class FrameJonbinTests
{
    private static readonly SheetImage Sheet = new("hy000_000", 864, 640);

    [Fact]
    public void Build_Layer_PlacesTheCanvasMirroredAroundTheOrigin()
    {
        // Arrange
        Ha6Frame frame = new() { Layers = [new Ha6Layer(0, false, 0, -304, -369)] };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        Assert.Equal(["hy000_000.bmp"], jonbin.Images);
        SpriteChunk chunk = Assert.Single(jonbin.Chunks);
        Assert.Equal((0f, 0f, 864f, 640f), (chunk.SourceX, chunk.SourceY, chunk.SourceWidth, chunk.SourceHeight));
        Assert.Equal((432f, -593f, -864f, 640f), (chunk.X, chunk.Y, chunk.Width, chunk.Height));
    }

    [Fact]
    public void Build_LayerWithNoSprite_DrawsNothing()
    {
        // Arrange
        Ha6Frame frame = new() { Layers = [new Ha6Layer(0, false, -1, 0, 0)] };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        Assert.Empty(jonbin.Chunks);
    }

    [Fact]
    public void Build_PatLayer_LeavesItToTheNativeRenderer()
    {
        // Arrange
        Ha6Frame frame = new() { Layers = [new Ha6Layer(0, true, 5, 0, 0)] };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        Assert.Empty(jonbin.Chunks);
    }

    [Fact]
    public void Build_TwoLayersOnOneImage_ListsTheImageOnce()
    {
        // Arrange
        Ha6Frame frame = new() { Layers = [new Ha6Layer(0, false, 1, 0, 0), new Ha6Layer(1, false, 1, 4, 4)] };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        Assert.Single(jonbin.Images);
        Assert.Equal([0, 0], jonbin.Chunks.Select(chunk => chunk.Layer));
    }

    [Fact]
    public void Build_HurtBoxes_AreMirroredIntoTypeZero()
    {
        // Arrange
        Ha6Frame frame = new() { Boxes = new Dictionary<int, Ha6Box> { [1] = new Ha6Box(-62, -147, 110, 0) } };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        JonbinBox box = Assert.Single(jonbin.Boxes);
        Assert.Equal(0, box.Type);
        Assert.Equal((-110f, 62f, -147f, 0f), (box.LowX, box.HighX, box.LowY, box.HighY));
    }

    [Fact]
    public void Build_PushBox_IsNotAHurtBox()
    {
        // Arrange
        Ha6Frame frame = new() { Boxes = new Dictionary<int, Ha6Box> { [0] = new Ha6Box(-50, -300, 50, 0) } };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        Assert.Empty(jonbin.Boxes);
    }

    [Fact]
    public void Build_BoxOutsideTheHurtRange_IsLeftOut()
    {
        // Arrange
        Ha6Frame frame = new() { Boxes = new Dictionary<int, Ha6Box> { [12] = new Ha6Box(-50, -300, 50, 0) } };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        Assert.Empty(jonbin.Boxes);
    }

    [Fact]
    public void Build_AttackBoxes_AreMirroredIntoTypeOne()
    {
        // Arrange
        Ha6Frame frame = new() { AttackBoxes = new Dictionary<int, Ha6Box> { [0] = new Ha6Box(10, -200, 90, -100) } };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => Sheet));

        // Assert
        JonbinBox box = Assert.Single(jonbin.Boxes);
        Assert.Equal(1, box.Type);
        Assert.Equal((-90f, -10f, -200f, -100f), (box.LowX, box.HighX, box.LowY, box.HighY));
    }

    [Fact]
    public void Build_ImageTheSheetDoesNotHave_IsSkipped()
    {
        // Arrange
        Ha6Frame frame = new() { Layers = [new Ha6Layer(0, false, 3, 0, 0)] };

        // Act
        Jonbin jonbin = Jonbin.Read(FrameJonbin.Build(frame, _ => null));

        // Assert
        Assert.Empty(jonbin.Chunks);
    }
}
