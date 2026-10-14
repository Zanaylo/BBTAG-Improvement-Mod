using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting.Compiling;

namespace BBTAG_IM.Core.Scripting.Live;

public enum ScriptApplyKind
{
    InPlace,
    Moved,
}

public sealed record ScriptApplyResult(ScriptApplyKind Kind, int ChangedBytes, IReadOnlyList<string> AddedFunctions);

public sealed class LiveScript(IGameMemory memory, IScriptBuffers buffers, uint recordAddress, ScriptBinary loaded, ScriptDatabase database)
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

        Current = next;
        lines = compiled.Lines;
        return result;
    }

    public int? LineOf(uint instruction) => OffsetOf(instruction) is { } offset ? LineAt(offset) : null;

    public int? OffsetOf(uint instruction) =>
        instruction >= file && instruction - file < (uint)Current.Bytes.Length ? (int)(instruction - file) : null;

    public int? LineAt(int offset) => lines?.LineAt(offset);

    private ScriptApplyResult WriteInPlace(InPlaceChange change)
    {
        foreach (var write in change.Writes)
            memory.Write(file + (uint)write.Offset, write.Bytes);

        return new ScriptApplyResult(ScriptApplyKind.InPlace, change.Writes.Sum(write => write.Bytes.Length), []);
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
        return new ScriptApplyResult(ScriptApplyKind.Moved, next.Bytes.Length, change.AddedFunctions);
    }
}
