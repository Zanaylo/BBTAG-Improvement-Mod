namespace BBTAG_IM.Core.Scripting;

public interface INormalInputs
{
    IEnumerable<NamedValue> Named { get; }

    string? NameOf(int value);

    int? ValueOf(string name);
}
