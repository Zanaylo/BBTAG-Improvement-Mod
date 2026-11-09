namespace BBTAG_IM.Core.Overlay;

public interface IHudFont
{
    float CellHeight { get; }

    float Width(string text, float scale);
}
