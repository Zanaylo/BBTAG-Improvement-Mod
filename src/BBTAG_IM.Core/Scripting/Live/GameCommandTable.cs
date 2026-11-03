using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public sealed record GameCommandTable(uint Site)
{
    private const string Name = "Script command sizes";
    private const uint LastIndexOffset = 3;
    private const uint TableOffset = 19;

    private static readonly BytePattern SizeLookup = BytePattern.Parse("33 C9 BE ?? ?? ?? ?? 90 8D 04 0E 99 2B C2 D1 F8 8B 14 C5 ?? ?? ?? ??");

    public static GameCommandTable Locate(CodeImage image) => new(image.FindSingle(SizeLookup, Name));

    public CommandList Current(IGameMemory memory) =>
        new(memory.ReadUInt32(Site + TableOffset), memory.ReadInt32(Site + LastIndexOffset) + 1);
}
