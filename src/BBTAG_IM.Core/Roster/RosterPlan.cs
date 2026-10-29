namespace BBTAG_IM.Core.Roster;

public sealed record RosterPlan(IReadOnlyList<RosterAddition> Additions, IReadOnlyList<string> Problems)
{
    public const int FirstRefusedIndex = 0x3F;

    public static RosterPlan Create(IReadOnlyList<CharacterManifest> manifests, IReadOnlyList<string> rosterCodes)
    {
        List<RosterAddition> additions = [];
        List<string> problems = [];
        var freeIndexes = FirstRefusedIndex - rosterCodes.Count;

        foreach (var manifest in manifests)
        {
            if (Refusal(manifest, rosterCodes, additions, freeIndexes) is { } refusal)
            {
                problems.Add($"{manifest.Code} was left out: {refusal}");
                continue;
            }

            additions.Add(new RosterAddition(rosterCodes.Count + additions.Count, IndexOf(rosterCodes, manifest.Template), manifest));
        }

        return new RosterPlan(additions, problems);
    }

    private static string? Refusal(CharacterManifest manifest, IReadOnlyList<string> rosterCodes, List<RosterAddition> additions, int freeIndexes)
    {
        if (rosterCodes.Contains(manifest.Code))
            return "the roster already has that code.";

        if (additions.Any(addition => addition.Manifest.Code == manifest.Code))
            return "a mod higher in the list already adds that code.";

        if (IndexOf(rosterCodes, manifest.Template) < 0)
            return $"its template {manifest.Template} is not in the roster.";

        return additions.Count >= freeIndexes ? $"the roster has room for {freeIndexes} new characters." : null;
    }

    private static int IndexOf(IReadOnlyList<string> codes, string code)
    {
        for (var index = 0; index < codes.Count; index++)
        {
            if (codes[index] == code)
                return index;
        }

        return -1;
    }
}
