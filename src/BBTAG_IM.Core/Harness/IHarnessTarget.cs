namespace BBTAG_IM.Core.Harness;

public interface IHarnessTarget
{
    int ProcessId { get; }

    bool PlayKeys(KeyScript script);

    string Grab(string path);

    string State();

    string ToggleMenu();

    string MenuKey(int key, bool down);

    string MenuClick(int x, int y);

    string BackToCharacterSelect();

    string Quit();
}
