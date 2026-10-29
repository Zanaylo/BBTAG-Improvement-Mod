namespace BBTAG_IM.Core.Harness;

public interface IHarnessTarget
{
    int ProcessId { get; }

    bool PlayKeys(KeyScript script);

    string Grab(string path);

    string State();

    string BackToCharacterSelect();

    string Quit();
}
