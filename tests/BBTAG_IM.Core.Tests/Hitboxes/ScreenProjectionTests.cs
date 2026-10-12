using System.Numerics;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class ScreenProjectionTests
{
    private static readonly Viewport FullHd = new(0, 0, 1920, 1080);
    private static readonly CameraView Identity = new(Matrix4x4.Identity, Matrix4x4.Identity);

    [Fact]
    public void ToScreen_WorldOriginWithoutCamera_LandsInTheMiddleOfTheScreen()
    {
        // Act
        Vector2 screen = ScreenProjection.ToScreen(Vector2.Zero, Identity, FullHd);

        // Assert
        Assert.Equal(new Vector2(960, 540), screen);
    }

    [Fact]
    public void ToScreen_TopRightOfTheView_LandsInTheTopRightCornerBecauseScreenYGrowsDownwards()
    {
        // Act
        Vector2 screen = ScreenProjection.ToScreen(new Vector2(1, 1), Identity, FullHd);

        // Assert
        Assert.Equal(new Vector2(1920, 0), screen);
    }

    [Fact]
    public void ToScreen_ProjectionWithDepth_DividesByW()
    {
        // Arrange
        Matrix4x4 projection = Matrix4x4.Identity with { M44 = 2 };
        CameraView camera = new(Matrix4x4.Identity, projection);

        // Act
        Vector2 screen = ScreenProjection.ToScreen(new Vector2(1, 1), camera, FullHd);

        // Assert
        Assert.Equal(new Vector2(1440, 270), screen);
    }

    [Fact]
    public void ToScreen_ViewTranslatesTheWorld_AppliesTheViewBeforeTheProjection()
    {
        // Arrange
        CameraView camera = new(Matrix4x4.CreateTranslation(-1, 0, 0), Matrix4x4.CreateScale(0.5f));

        // Act
        Vector2 screen = ScreenProjection.ToScreen(new Vector2(1, 0), camera, FullHd);

        // Assert
        Assert.Equal(new Vector2(960, 540), screen);
    }

    [Fact]
    public void ToScreen_ViewportNotAtTheCorner_OffsetsByIt()
    {
        // Arrange
        Viewport viewport = new(100, 50, 200, 100);

        // Act
        Vector2 screen = ScreenProjection.ToScreen(Vector2.Zero, Identity, viewport);

        // Assert
        Assert.Equal(new Vector2(200, 100), screen);
    }

    [Fact]
    public void ToScreen_Quad_ProjectsEveryCorner()
    {
        // Arrange
        Quad world = new(new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1));

        // Act
        Quad screen = ScreenProjection.ToScreen(world, Identity, FullHd);

        // Assert
        Assert.Equal(new Quad(new Vector2(0, 0), new Vector2(1920, 0), new Vector2(1920, 1080), new Vector2(0, 1080)), screen);
    }
}
