namespace BBTAG_IM.ImprovementMod.Menu;

internal interface IMenuSection
{
    string Title { get; }

    void Reopened();

    void Draw();
}
