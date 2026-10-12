using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Game;

public sealed class GameView(IGameMemory memory, GameAddresses addresses)
{
    public const int CharacterSlots = 4;
    public const int EntityCount = 0x194;

    private const uint ModeOffset = 0x114;
    private const uint StateOffset = 0x118;
    private const uint FirstPaletteIndexOffset = 0x27B0;
    private const uint PaletteIndexStride = 0x28;
    private const uint FirstPaletteTableOffset = 0xB44;
    private const uint PaletteTableStride = 0x20BDC;
    private const uint EntityListOffset = 0xA4210;
    private const uint CameraCountOffset = 8;
    private const uint CameraViewOffset = 0x4C;
    private const uint CameraProjectionOffset = 0x9C;
    private const uint CharacterIndexOffset = 0x34;
    private const uint PointerSize = sizeof(uint);

    public IGameMemory Memory => memory;

    public GameMode Mode => (GameMode)memory.ReadInt32(addresses.Game + ModeOffset);

    public GameState State => (GameState)memory.ReadInt32(addresses.Game + StateOffset);

    public bool InTrainingMatch => Mode == GameMode.Training && State == GameState.Match;

    public uint? EntityList => memory.ReadPointer(addresses.Battle + EntityListOffset);

    public uint PaletteIndexAddress(int slot) => addresses.Game + FirstPaletteIndexOffset + (uint)slot * PaletteIndexStride;

    public uint PaletteTableAddress(int slot) => addresses.Battle + FirstPaletteTableOffset + (uint)slot * PaletteTableStride;

    public uint? Character(int slot) => memory.ReadPointer(addresses.Characters + (uint)slot * PointerSize);

    public int? CharacterIndex(int slot) =>
        Character(slot) is { } character && memory.CanRead(character + CharacterIndexOffset, sizeof(int))
            ? memory.ReadInt32(character + CharacterIndexOffset)
            : null;

    public CameraView? Camera()
    {
        if (memory.ReadUInt32(addresses.Cameras + CameraCountOffset) == 0)
            return null;

        var camera = memory.ReadPointer(addresses.Cameras) is { } cameras ? memory.ReadPointer(cameras) : null;

        if (camera is not { } address)
            return null;

        return new CameraView(memory.ReadMatrix(address + CameraViewOffset), memory.ReadMatrix(address + CameraProjectionOffset));
    }
}
