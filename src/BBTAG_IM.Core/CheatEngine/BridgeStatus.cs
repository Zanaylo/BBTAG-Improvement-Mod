namespace BBTAG_IM.Core.CheatEngine;

public sealed record BridgeStatus(int Pending, int Running, int Waiting)
{
    public bool IsWorking => Pending > 0 || Running > Waiting;
}
