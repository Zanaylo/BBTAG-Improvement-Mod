using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public enum RecoveryCall
{
    ThroughEax,
    Direct,
}

public sealed record RecoveryPoint(
    string Name,
    BytePattern Pattern,
    uint SiteOffset,
    byte[] Prelude,
    RecoveryCall Call,
    int ReplacedLength,
    int ResumeResult);

public static class RecoveryPoints
{
    public static readonly RecoveryPoint SceneStep = new(
        "Main loop scene step",
        BytePattern.Parse("8B 46 04 8D 4E 04 C7 45 FC 00 00 00 00 FF 10 85 C0 7F 05"),
        SiteOffset: 6,
        Prelude: [0xC7, 0x45, 0xFC, 0x00, 0x00, 0x00, 0x00],
        RecoveryCall.ThroughEax,
        ReplacedLength: 9,
        ResumeResult: 1);

    public static readonly RecoveryPoint ObjectScript = new(
        "Battle object script step",
        BytePattern.Parse("8B CB E8 ?? ?? ?? ?? F7 83 5C 01 00 00 00 00 40 00"),
        SiteOffset: 0,
        Prelude: [0x8B, 0xCB],
        RecoveryCall.Direct,
        ReplacedLength: 7,
        ResumeResult: 0);
}
