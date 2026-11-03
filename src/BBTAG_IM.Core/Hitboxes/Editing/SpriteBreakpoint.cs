namespace BBTAG_IM.Core.Hitboxes.Editing;

public sealed class SpriteBreakpoint
{
    private bool _wasShowing;

    public bool Reached(bool showing)
    {
        var entered = showing && !_wasShowing;
        _wasShowing = showing;
        return entered;
    }
}
