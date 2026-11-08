using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public static class SubroutineCallCode
{
    private const string Name = "Script subroutine call";

    private static readonly BytePattern CallSubroutine = BytePattern.Parse(
        "55 8B EC 6A FF 68 ?? ?? ?? ?? 64 A1 00 00 00 00 50 83 EC 34 A1 ?? ?? ?? ?? 33 C5 89 45 F0 53 56 57 50 8D 45 F4 64 A3 00 00 00 00 8B D9 8B 45 08 33 C9 8B 93 00 14 00 00");

    public static uint Locate(CodeImage image) => image.FindSingle(CallSubroutine, Name);
}
