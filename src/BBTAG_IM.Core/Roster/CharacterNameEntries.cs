namespace BBTAG_IM.Core.Roster;

public static class CharacterNameEntries
{
    public static IReadOnlyList<KeyValuePair<string, string>> For(CharacterManifest manifest)
    {
        var code = manifest.Code.ToUpperInvariant();

        return
        [
            new($"Chara_Name{code}", manifest.Names.Full),
            new($"Chara_Name{code}_Short", manifest.Names.Short),
            new($"CharaSelect_Name{code}", manifest.Names.Select),
            new($"TRIChara_Name{code}_Short", $"[{manifest.Names.Short}]"),
        ];
    }
}
