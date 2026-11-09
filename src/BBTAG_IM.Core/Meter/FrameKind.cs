namespace BBTAG_IM.Core.Meter;

public enum FrameKind : byte
{
    Empty,
    Idle,
    Movement,
    Startup,
    Active,
    Recovery,
    Hitstun,
    Blockstun,
    Tech,
    Leaving,
}
