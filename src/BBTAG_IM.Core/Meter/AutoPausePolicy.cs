using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Meter;

public static class AutoPausePolicy
{
    private const int FirstHit = 1;

    public static bool ShouldPause(MeterEvent happened, AutoPauseOptions options)
    {
        if (!Watches(happened.Attacker, options))
            return false;

        return happened.Kind switch
        {
            MeterEventKind.MoveStarted => options.OnMoveStart,
            MeterEventKind.HitLanded => (options.OnHit && happened.Count == FirstHit) || (options.OnCombo && options.ComboStops.Contains(happened.Count)),
            MeterEventKind.HitBlocked => options.OnBlock && options.BlockStops.Contains(happened.Count),
            _ => false,
        };
    }

    private static bool Watches(Side side, AutoPauseOptions options) => side == Side.Player1 ? options.Player1 : options.Player2;
}
