namespace BBTAG_IM.Launcher.Ui;

internal interface ILauncherView
{
    void Report(string message);

    void ShowStatus(string message);

    void ShowActiveScripts(IReadOnlySet<int> activeScripts);
}
