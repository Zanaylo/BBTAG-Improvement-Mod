namespace BBTAG_IM.Core.Selection;

public static class ChoiceSearch
{
    public static IReadOnlyList<int> Matching(IReadOnlyList<string> labels, string typed)
    {
        var text = typed.Trim();

        return
        [
            .. Enumerable.Range(0, labels.Count)
                .Where(index => labels[index].Contains(text, StringComparison.OrdinalIgnoreCase))
                .OrderBy(index => !labels[index].StartsWith(text, StringComparison.OrdinalIgnoreCase)),
        ];
    }
}
