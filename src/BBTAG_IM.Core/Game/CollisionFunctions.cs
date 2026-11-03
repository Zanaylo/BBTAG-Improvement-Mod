using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public sealed record CollisionFunctions(uint WorldBox, uint PositionX, uint PositionY, uint LoadBoxes)
{
    private static readonly BytePattern WorldBoxSite =
        BytePattern.Parse("55 8B EC 83 EC 24 53 8B D9 56 57 89 5D F0 8D 8B 30 02 00 00 E8 ?? ?? ?? ?? 8B 55 0C 8D 4B 3C FF 75 08");

    private static readonly BytePattern PositionXSite =
        BytePattern.Parse("56 8B F1 8B 8E 04 02 00 00 85 C9 75 ?? 8B 8E 08 02 00 00 85 C9 75 ?? 8B 8E 24 02 00 00 85 C9 75 ?? 8B 86 68 02 00 00 25 00 20 00 20 3D 00 20 00 20 75 ?? 8B 8E 9C 04 00 00 E8 ?? ?? ?? ?? 03 86 78 02 00 00");

    private static readonly BytePattern PositionYSite =
        BytePattern.Parse("56 8B F1 8B 8E 04 02 00 00 85 C9 75 ?? 8B 8E 0C 02 00 00 85 C9 75 ?? 8B 8E 24 02 00 00 85 C9 74 ?? E8");

    private static readonly BytePattern LoadBoxesSite =
        BytePattern.Parse("55 8B EC 8B 55 08 83 EC 08 56 8B F1 85 D2 75 ?? 89 96 BC 00 00 00 8D 46 10 89 96 9C 00 00 00 B9 11 00 00 00");

    public static CollisionFunctions Locate(CodeImage image) =>
        new(
            image.FindSingle(WorldBoxSite, "Collision box function"),
            image.FindSingle(PositionXSite, "Object position function"),
            image.FindSingle(PositionYSite, "Object height function"),
            image.FindSingle(LoadBoxesSite, "Collision file loader"));
}
