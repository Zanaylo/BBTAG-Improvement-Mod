using BBTAG_IM.Core.Hantei;

namespace BBTAG_IM.Core.Porting.Moves;

public sealed record MoveScripts(string Registrations, string States, string Objects, IReadOnlyList<string> ReplacedStates, IReadOnlyList<string> Stats)
{
    private const string Separator = "\n\n";

    public static MoveScripts Build(string commands, string moves, string vectors, IReadOnlyDictionary<int, Ha6Pattern> patterns, string spritePrefix, NativeCommandTable? table = null, string cpu = "", CharacterStats? stats = null)
    {
        var writer = new MoveStateWriter(HitVectorTable.Parse(vectors), spritePrefix, patterns, stats?.DamageScale ?? 1);
        var definitions = MoveCatalog.Build(commands, moves, patterns, table, cpu).DistinctBy(move => move.State).ToArray();
        var objects = definitions.SelectMany(move => move.Objects).DistinctBy(moved => moved.Code);

        return new MoveScripts(
            string.Join(Separator, definitions.Where(move => move.IsRegistered).Select(writer.WriteRegistration)),
            string.Join(Separator, definitions.Where(move => move.HasBody).Select(writer.Write)),
            string.Join(Separator, objects.Select(moved => writer.WriteObject(moved, patterns.Values.First(pattern => pattern.Code == moved.Code)))),
            definitions.Where(move => move.ReplacesCommonState).Select(move => move.State).ToArray(),
            stats?.Lines ?? []);
    }
}
