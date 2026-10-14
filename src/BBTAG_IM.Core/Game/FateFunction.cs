using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public static class FateFunction
{
    private const string Name = "Character fate";
    private const uint FateCallOffset = 15;

    private static readonly BytePattern Site = BytePattern.Parse("55 8B EC 56 8B 75 08 56 E8 ?? ?? ?? ?? 8B C8 E8 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 5E 5D C2 04 00");

    public static uint Locate(CodeImage image) => image.CallTarget(image.FindSingle(Site, Name) + FateCallOffset, Name);
}
