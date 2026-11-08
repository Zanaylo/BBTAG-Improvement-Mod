using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed class MoveRefresh(GameView game, IScriptBuffers buffers, IGameHeap heap, ISubroutineCall call) : IMoveRefresh
{
    public const string TemporarySubroutine = "IMMoveRegisters";

    private sealed record Rebuild(uint Player, MoveList List, int Start);

    public MoveRefreshResult Refresh(uint recordAddress, ScriptBinary before, ScriptBinary after)
    {
        var old = MoveRegistrations.Of(before);
        var next = MoveRegistrations.Of(after);

        if (old.SameAs(next))
            return MoveRefreshResult.Unchanged;

        var record = new ScriptRecord(game.Memory, recordAddress);
        var rebuilds = RebuildsOf(recordAddress, old, next);

        if (rebuilds is null || TemplateOf(record) is not { } template)
            return MoveRefreshResult.NotRefreshed;

        var block = buffers.Allocate(next.AsSubroutine(TemporarySubroutine));
        var subroutines = record.Map(record.SubroutineMap);
        var node = subroutines.Add(TemporarySubroutine, unchecked((int)(block - record.Bytecode)), template, heap);

        try
        {
            foreach (var rebuild in rebuilds)
                Register(rebuild, old.Names.Count);
        }
        finally
        {
            subroutines.Remove(new ScriptMapNode(node, TemporarySubroutine, 0));
        }

        return MoveRefreshResult.Refreshed;
    }

    private List<Rebuild>? RebuildsOf(uint recordAddress, MoveRegistrations old, MoveRegistrations next)
    {
        var rebuilds = new List<Rebuild>();

        foreach (var player in PlayersOf(recordAddress))
        {
            var list = new MoveList(game.Memory, player);

            if (list.StartOf(old.Names) is not { } start || list.Count - old.Names.Count + next.Names.Count > MoveList.MostMoves)
                return null;

            rebuilds.Add(new Rebuild(player, list, start));
        }

        return rebuilds.Count == 0 ? null : rebuilds;
    }

    private IEnumerable<uint> PlayersOf(uint recordAddress) =>
        Enumerable.Range(0, GameView.CharacterSlots)
            .Select(game.Character)
            .OfType<uint>()
            .Distinct()
            .Where(player => game.Memory.CanRead(player + ScriptRecord.ObjectRecordOffset, sizeof(uint)) && game.Memory.ReadUInt32(player + ScriptRecord.ObjectRecordOffset) == recordAddress);

    private void Register(Rebuild rebuild, int oldCount)
    {
        var tailStart = rebuild.Start + oldCount;
        var tail = rebuild.List.Read(tailStart, rebuild.List.Count - tailStart);

        rebuild.List.Truncate(rebuild.Start);
        call.Call(rebuild.Player, TemporarySubroutine);
        rebuild.List.Append(tail);
        rebuild.List.IndexNames();
    }

    private static uint? TemplateOf(ScriptRecord record) =>
        record.Maps.SelectMany(record.Nodes).Select(node => (uint?)node.Address).FirstOrDefault();
}
