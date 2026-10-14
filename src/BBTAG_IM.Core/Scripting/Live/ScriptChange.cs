using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed record ScriptWrite(int Offset, byte[] Bytes);

public abstract record ScriptChange
{
    public static ScriptChange Between(ScriptBinary current, ScriptBinary next)
    {
        var currentFunctions = ScriptFunctions.Of(current);
        var nextFunctions = ScriptFunctions.Of(next);

        if (current.Bytes.Length == next.Bytes.Length && currentFunctions.SequenceEqual(nextFunctions))
            return new InPlaceChange(Writes(current.Bytes, next.Bytes));

        var nextByKey = nextFunctions.ToDictionary(function => function.Key);
        var offsets = currentFunctions
            .Where(function => nextByKey.ContainsKey(function.Key))
            .ToDictionary(function => function.Offset, function => nextByKey[function.Key].Offset);
        var currentKeys = currentFunctions.Select(function => function.Key).ToHashSet();
        var added = nextFunctions.Where(function => !currentKeys.Contains(function.Key)).Select(function => function.Key.Name).ToList();

        return new MovedChange(offsets, added);
    }

    private static List<ScriptWrite> Writes(byte[] current, byte[] next) =>
        ByteRuns.Changed(current, next).Select(run => new ScriptWrite(run.Start, next[run.Start..run.End])).ToList();
}

public sealed record InPlaceChange(IReadOnlyList<ScriptWrite> Writes) : ScriptChange;

public sealed record MovedChange(IReadOnlyDictionary<int, int> Offsets, IReadOnlyList<string> AddedFunctions) : ScriptChange;
