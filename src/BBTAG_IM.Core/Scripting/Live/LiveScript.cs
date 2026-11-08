using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Compiling;

namespace BBTAG_IM.Core.Scripting.Live;

public enum ScriptApplyKind
{
    InPlace,
    Moved,
}

public sealed record ScriptApplyResult(
    ScriptApplyKind Kind,
    int ChangedBytes,
    IReadOnlyList<string> AddedFunctions,
    IReadOnlyList<string> WaitingFunctions,
    MoveRefreshResult Moves = MoveRefreshResult.Unchanged);

public sealed class LiveScript(
    IGameMemory memory,
    IScriptBuffers buffers,
    IGameHeap heap,
    uint recordAddress,
    ScriptBinary loaded,
    ScriptDatabase database,
    IMoveRefresh moves)
{
    private readonly ScriptRecord record = new(memory, recordAddress);
    private readonly List<uint> movedFiles = [];
    private uint file = memory.ReadUInt32(recordAddress);
    private ScriptLineMap? lines;

    public uint RecordAddress => recordAddress;

    public ScriptBinary Current { get; private set; } = loaded;

    public bool IsLoaded => record.File == file;

    public IReadOnlyList<uint> MovedFiles => movedFiles;

    public ScriptApplyResult Apply(CompiledScript compiled)
    {
        if (!IsLoaded)
            throw new ScriptReloadedException();

        var next = ScriptBinary.Read(compiled.Bytes, database);
        var result = ScriptChange.Between(Current, next) switch
        {
            InPlaceChange inPlace => WriteInPlace(inPlace),
            MovedChange moved => Move(next, moved),
            _ => throw new InvalidOperationException("Unknown script change."),
        };

        var before = Current;
        Current = next;
        lines = compiled.Lines;
        return result with { Moves = moves.Refresh(recordAddress, before, next) };
    }

    public int? LineOf(uint instruction) => OffsetOf(instruction) is { } offset ? LineAt(offset) : null;

    public int? OffsetOf(uint instruction) =>
        instruction >= file && instruction - file < (uint)Current.Bytes.Length ? (int)(instruction - file) : null;

    public int? LineAt(int offset) => lines?.LineAt(offset);

    private ScriptApplyResult WriteInPlace(InPlaceChange change)
    {
        foreach (var write in change.Writes)
            memory.Write(file + (uint)write.Offset, write.Bytes);

        return new ScriptApplyResult(ScriptApplyKind.InPlace, change.Writes.Sum(write => write.Bytes.Length), [], []);
    }

    private ScriptApplyResult Move(ScriptBinary next, MovedChange change)
    {
        var moved = buffers.Allocate(next.Bytes);
        movedFiles.Add(moved);
        var nodes = record.Maps.SelectMany(record.Nodes).ToList();

        record.Point(moved, next.States.Count);

        foreach (var node in nodes)
        {
            if (change.Offsets.TryGetValue(node.Offset, out var offset))
                record.SetOffset(node, offset);
        }

        file = moved;
        var names = FollowNames(next, nodes.FirstOrDefault()?.Address);
        return new ScriptApplyResult(ScriptApplyKind.Moved, next.Bytes.Length, names.Added, names.Waiting);
    }

    private (List<string> Added, List<string> Waiting) FollowNames(ScriptBinary next, uint? template)
    {
        var added = new List<string>();
        var waiting = new List<string>();

        foreach (var (map, start) in new[] { (record.StateMap, ScriptCommands.StartState), (record.SubroutineMap, ScriptCommands.StartSubroutine) })
        {
            var scriptMap = record.Map(map);
            var before = FirstOffsets(Current, start);
            var after = FirstOffsets(next, start);
            var known = scriptMap.Nodes();

            foreach (var gone in known.Where(node => before.ContainsKey(node.Name) && !after.ContainsKey(node.Name)))
                scriptMap.Remove(gone);

            foreach (var (name, offset) in after.Where(pair => !before.ContainsKey(pair.Key) && known.All(node => node.Name != pair.Key)))
            {
                if (template is not { } copy)
                {
                    waiting.Add(name);
                    continue;
                }

                scriptMap.Add(name, offset, copy, heap);
                added.Add(name);
            }
        }

        return (added, waiting);
    }

    private static Dictionary<string, int> FirstOffsets(ScriptBinary script, int start) =>
        ScriptFunctions.Of(script)
            .Where(function => function.Key.Start == start)
            .GroupBy(function => function.Key.Name)
            .ToDictionary(group => group.Key, group => group.First().Offset);
}
