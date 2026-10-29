namespace BBTAG_IM.Core.Game;

public static class FatalErrorPolicy
{
    private const string ModuleName = "BBTAG.exe";

    private static readonly Dictionary<uint, string> FatalCodes = new()
    {
        [0xC0000005] = "access violation",
        [0xC000001D] = "illegal instruction",
        [0xC0000094] = "integer division by zero",
        [0xC0000096] = "privileged instruction",
        [0xC000008C] = "array bounds exceeded",
    };

    public static bool CanRecover(uint code, uint address, uint moduleStart, uint moduleEnd, bool armedOnThisThread) =>
        armedOnThisThread && FatalCodes.ContainsKey(code) && address >= moduleStart && address < moduleEnd;

    public static string Describe(uint code, uint address, uint moduleStart) =>
        $"{FatalCodes.GetValueOrDefault(code, "error")} (0x{code:X8}) at {ModuleName}+0x{address - moduleStart:X}";
}
