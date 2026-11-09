using BBTAG_IM.Core.Overlay;

namespace BBTAG_IM.Core.Tests.Overlay;

public class SoftwareCursorTests
{
    [Fact]
    public void IsNeeded_GameHidTheCursorOverItsWindow_IsTrue()
    {
        // Act
        bool needed = SoftwareCursor.IsNeeded(systemCursorShowing: false, overGameWindow: true);

        // Assert
        Assert.True(needed);
    }

    [Fact]
    public void IsNeeded_WindowsShowsTheCursor_IsFalse()
    {
        // Act
        bool needed = SoftwareCursor.IsNeeded(systemCursorShowing: true, overGameWindow: true);

        // Assert
        Assert.False(needed);
    }

    [Fact]
    public void IsNeeded_CursorOutsideTheGameWindow_IsFalse()
    {
        // Act
        bool needed = SoftwareCursor.IsNeeded(systemCursorShowing: false, overGameWindow: false);

        // Assert
        Assert.False(needed);
    }
}
