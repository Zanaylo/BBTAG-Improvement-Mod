namespace BBTAG_IM.Core.Scripting.Importing;

internal sealed class NameClaims(IEnumerable<string> usedNames)
{
    private readonly HashSet<string> used = new(usedNames, StringComparer.OrdinalIgnoreCase);
    private readonly List<string> skipped = [];

    public IReadOnlyList<string> Skipped => skipped;

    public bool TryClaim(string name, string owner)
    {
        if (!FunctionNames.IsIdentifier(name))
        {
            skipped.Add($"{owner} {name}: scripts cannot use this name");
            return false;
        }

        if (!used.Add(name))
        {
            skipped.Add($"{owner} {name}: another one already has this name");
            return false;
        }

        return true;
    }
}
