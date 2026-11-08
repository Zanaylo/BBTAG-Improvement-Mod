namespace BBTAG_IM.Core.Training;

public interface IMatchPause
{
    bool IsPaused { get; }

    void PauseNow();

    void Resume();
}
