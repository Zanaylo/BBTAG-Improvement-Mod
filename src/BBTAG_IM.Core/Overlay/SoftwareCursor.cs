namespace BBTAG_IM.Core.Overlay;

public static class SoftwareCursor
{
    public static bool IsNeeded(bool systemCursorShowing, bool overGameWindow) => !systemCursorShowing && overGameWindow;
}
