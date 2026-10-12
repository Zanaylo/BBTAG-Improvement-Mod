namespace BBTAG_IM.ImprovementMod.Menu;

internal sealed class UnavailableSection(string title, string reason) : IMenuSection
{
    public string Title => title;

    public void Reopened()
    {
    }

    public void Draw() => MenuText.Wrapped(reason);
}
