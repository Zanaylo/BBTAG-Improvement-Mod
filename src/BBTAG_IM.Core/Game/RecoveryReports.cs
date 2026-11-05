namespace BBTAG_IM.Core.Game;

public sealed class RecoveryReports
{
    private readonly HashSet<uint> _reported = [];

    private long _match = -1;

    public bool IsFirst(long matchNumber, uint address)
    {
        if (matchNumber != _match)
        {
            _match = matchNumber;
            _reported.Clear();
        }

        return _reported.Add(address);
    }
}
