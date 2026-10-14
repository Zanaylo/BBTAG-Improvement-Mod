namespace BBTAG_IM.Core.Display;

public readonly record struct Resolution(int Width, int Height)
{
    public override string ToString() => $"{Width}x{Height}";
}
