using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public static class RecoveryPointCode
{
    public const int SavedEspOffset = 0;
    public const int SavedEbpOffset = 4;
    public const int SavedEbxOffset = 8;
    public const int SavedEsiOffset = 12;
    public const int SavedEdiOffset = 16;
    public const int ArmedOffset = 20;
    public const int ThreadOffset = 24;
    public const int HandlersOffset = 28;
    public const int FpuControlOffset = 32;
    public const int SseControlOffset = 36;
    public const int RecoveriesOffset = 40;
    public const int ResumeOffset = 64;
    public const int ArmOffset = 160;
    public const int ThunkOffset = 320;
    public const int BlockLength = 512;
    public const int JumpLength = 5;
    public const uint SmallestAddress = 0x10000;

    private const uint CallOperand = 1;
    private const byte JumpNear = 0xE9;
    private const byte CallNear = 0xE8;
    private const byte NoOperation = 0x90;

    public static uint Locate(CodeImage image, RecoveryPoint point) => image.FindSingle(point.Pattern, point.Name) + point.SiteOffset;

    public static uint CallTargetOf(CodeImage image, RecoveryPoint point, uint site) =>
        image.CallTarget(site + (uint)point.Prelude.Length, point.Name);

    public static byte[] Code(RecoveryPoint point, uint site, uint block, uint callTarget)
    {
        var back = site + (uint)point.ReplacedLength;
        byte[] restore =
        [
            0x8B, 0x25, .. Address(block + SavedEspOffset),
            0x8B, 0x2D, .. Address(block + SavedEbpOffset),
            0x8B, 0x1D, .. Address(block + SavedEbxOffset),
            0x8B, 0x35, .. Address(block + SavedEsiOffset),
            0x8B, 0x3D, .. Address(block + SavedEdiOffset),
            0x8B, 0x15, .. Address(block + HandlersOffset),
            0x64, 0x89, 0x15, 0x00, 0x00, 0x00, 0x00,
            0xDB, 0xE3,
            0xD9, 0x2D, .. Address(block + FpuControlOffset),
            0x0F, 0xAE, 0x15, .. Address(block + SseControlOffset),
            0xFC,
            .. SetArmed(block, 0),
            0xFF, 0x05, .. Address(block + RecoveriesOffset),
            0xB8, .. BitConverter.GetBytes(point.ResumeResult),
        ];
        byte[] resume = [.. restore, .. JumpTo(block + ResumeOffset + (uint)restore.Length, back)];
        byte[] beforeCall = [.. point.Prelude, .. Saves(block), .. SetArmed(block, 1)];
        var callAt = block + ArmOffset + (uint)beforeCall.Length;
        byte[] call = point.Call == RecoveryCall.ThroughEax ? [0xFF, 0x10] : [CallNear, .. Address(callTarget - (callAt + JumpLength))];
        byte[] arm = [.. beforeCall, .. call, .. SetArmed(block, 0)];
        var padding = Enumerable.Repeat(NoOperation, ArmOffset - ResumeOffset - resume.Length);

        return [.. resume, .. padding, .. arm, .. JumpTo(block + ArmOffset + (uint)arm.Length, back)];
    }

    public static byte[] Jump(RecoveryPoint point, uint site, uint block) =>
        [.. JumpTo(site, block + ArmOffset), .. Enumerable.Repeat(NoOperation, point.ReplacedLength - JumpLength)];

    public static byte[] MessageBoxThunk(uint managedHandler) =>
    [
        0xFF, 0x74, 0x24, 0x10,
        0xFF, 0x74, 0x24, 0x10,
        0xFF, 0x74, 0x24, 0x10,
        0xFF, 0x74, 0x24, 0x10,
        0xB8, .. Address(managedHandler),
        0xFF, 0xD0,
        0x3D, .. Address(SmallestAddress),
        0x73, 0x03,
        0xC2, 0x10, 0x00,
        0xFF, 0xE0,
    ];

    private static byte[] Saves(uint block) =>
    [
        0x89, 0x25, .. Address(block + SavedEspOffset),
        0x89, 0x2D, .. Address(block + SavedEbpOffset),
        0x89, 0x1D, .. Address(block + SavedEbxOffset),
        0x89, 0x35, .. Address(block + SavedEsiOffset),
        0x89, 0x3D, .. Address(block + SavedEdiOffset),
        0x64, 0x8B, 0x15, 0x24, 0x00, 0x00, 0x00,
        0x89, 0x15, .. Address(block + ThreadOffset),
        0x64, 0x8B, 0x15, 0x00, 0x00, 0x00, 0x00,
        0x89, 0x15, .. Address(block + HandlersOffset),
        0xD9, 0x3D, .. Address(block + FpuControlOffset),
        0x0F, 0xAE, 0x1D, .. Address(block + SseControlOffset),
    ];

    private static byte[] SetArmed(uint block, int value) => [0xC7, 0x05, .. Address(block + ArmedOffset), .. BitConverter.GetBytes(value)];

    private static byte[] JumpTo(uint from, uint target) => [JumpNear, .. Address(target - (from + JumpLength))];

    private static byte[] Address(uint value) => BitConverter.GetBytes(value);
}
