using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Scripting.Live;

public static class StateEntryCode
{
    private const string Name = "State change";

    private static readonly BytePattern EnterState = BytePattern.Parse(
        "55 8B EC 83 EC 28 A1 ?? ?? ?? ?? 33 C5 89 45 FC 8B 45 08 53 56 57 8B D9 89 45 D8 E8 ?? ?? ?? ?? 8B 80 ?? ?? ?? ?? 89 43 18");

    public static uint Locate(CodeImage image) => image.FindSingle(EnterState, Name);
}
