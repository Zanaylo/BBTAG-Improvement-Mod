namespace BBTAG_IM.Core.Scripting.Live;

public enum MoveRefreshResult
{
    Unchanged,
    Refreshed,
    NotRefreshed,
}

public interface IMoveRefresh
{
    MoveRefreshResult Refresh(uint recordAddress, ScriptBinary before, ScriptBinary after);
}
