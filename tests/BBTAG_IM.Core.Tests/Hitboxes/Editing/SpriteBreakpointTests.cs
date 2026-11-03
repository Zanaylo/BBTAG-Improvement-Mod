using BBTAG_IM.Core.Hitboxes.Editing;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class SpriteBreakpointTests
{
    [Fact]
    public void Reached_FrameStartsShowing_IsTrue()
    {
        // Arrange
        SpriteBreakpoint breakpoint = new();
        breakpoint.Reached(showing: false);

        // Act
        bool reached = breakpoint.Reached(showing: true);

        // Assert
        Assert.True(reached);
    }

    [Fact]
    public void Reached_FrameStillShowing_IsFalseAfterTheFirstFrame()
    {
        // Arrange
        SpriteBreakpoint breakpoint = new();
        breakpoint.Reached(showing: false);
        breakpoint.Reached(showing: true);

        // Act
        bool reached = breakpoint.Reached(showing: true);

        // Assert
        Assert.False(reached);
    }

    [Fact]
    public void Reached_FrameNotShowing_IsFalse()
    {
        // Arrange
        SpriteBreakpoint breakpoint = new();

        // Act
        bool reached = breakpoint.Reached(showing: false);

        // Assert
        Assert.False(reached);
    }
}
