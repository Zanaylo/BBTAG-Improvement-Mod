namespace BBTAG_IM.Core.Scripting.Compiling;

public sealed record ScriptLine(int Offset, int Line);

public sealed class ScriptLineMap(IReadOnlyList<ScriptLine> lines)
{
    public IReadOnlyList<ScriptLine> Lines { get; } = lines;

    public int? LineAt(int offset)
    {
        var low = 0;
        var high = Lines.Count - 1;
        int? found = null;

        while (low <= high)
        {
            var middle = (low + high) / 2;

            if (Lines[middle].Offset <= offset)
            {
                found = Lines[middle].Line;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return found;
    }
}
