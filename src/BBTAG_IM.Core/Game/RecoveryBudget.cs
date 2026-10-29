namespace BBTAG_IM.Core.Game;

public sealed class RecoveryBudget(int perMatch)
{
    private long _match = -1;
    private int _spent;

    public bool TrySpend(long matchNumber)
    {
        if (matchNumber != _match)
        {
            _match = matchNumber;
            _spent = 0;
        }

        return ++_spent <= perMatch;
    }
}
