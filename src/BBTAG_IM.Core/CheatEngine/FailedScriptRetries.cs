namespace BBTAG_IM.Core.CheatEngine;

public sealed class FailedScriptRetries
{
    private readonly HashSet<int> _retried = [];
    private bool _ended;

    public IReadOnlyList<int> SelectForRetry(IEnumerable<int> failedIds)
    {
        if (_ended)
            return [];

        return failedIds.Where(_retried.Add).ToArray();
    }

    public void EndRetries() => _ended = true;
}
