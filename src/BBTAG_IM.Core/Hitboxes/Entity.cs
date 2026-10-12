using System.Buffers.Binary;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Hitboxes;

public enum BoxKind
{
    Hurtbox = 0,
    Hitbox = 1,
}

public sealed record CollisionBox(BoxKind Kind, float X, float Y, float Width, float Height);

public sealed record Entity(
    int Status,
    uint Owner,
    bool FacingLeft,
    int PositionX,
    int PositionY,
    int PivotX,
    int OffsetX,
    int OffsetY,
    int Rotation,
    int ScaleX,
    int ScaleY,
    IReadOnlyList<CollisionBox> Boxes)
{
    public const int ActiveStatus = 1;
    public const int MaximumBoxes = 64;

    private const int Length = 0x310;
    private const int StatusField = 0x10;
    private const int BoxListField = 0x4C;
    private const int HurtboxCountField = 0x94;
    private const int HitboxCountField = 0x98;
    private const int OwnerField = 0x1F4;
    private const int FacingLeftField = 0x274;
    private const int PivotXField = 0x284;
    private const int RotationField = 0x28C;
    private const int ScaleXField = 0x294;
    private const int ScaleYField = 0x298;
    private const int PositionXField = 0x2EC;
    private const int PositionYField = 0x2F0;
    private const int OffsetXField = 0x304;
    private const int OffsetYField = 0x30C;

    private const int BoxLength = 20;
    private const int BoxXField = 4;
    private const int BoxYField = 8;
    private const int BoxWidthField = 12;
    private const int BoxHeightField = 16;

    public bool IsActive => Status == ActiveStatus && Boxes.Count > 0;

    public static Entity? Read(IGameMemory memory, uint address)
    {
        if (!memory.CanRead(address, Length))
            return null;

        Span<byte> fields = stackalloc byte[Length];
        memory.Read(address, fields);

        return new Entity(
            Int32(fields, StatusField),
            (uint)Int32(fields, OwnerField),
            Int32(fields, FacingLeftField) != 0,
            Int32(fields, PositionXField),
            Int32(fields, PositionYField),
            Int32(fields, PivotXField),
            Int32(fields, OffsetXField),
            Int32(fields, OffsetYField),
            Int32(fields, RotationField),
            Int32(fields, ScaleXField),
            Int32(fields, ScaleYField),
            ReadBoxes(memory, fields));
    }

    private static IReadOnlyList<CollisionBox> ReadBoxes(IGameMemory memory, ReadOnlySpan<byte> fields)
    {
        var list = (uint)Int32(fields, BoxListField);
        var count = (long)(uint)Int32(fields, HurtboxCountField) + (uint)Int32(fields, HitboxCountField);

        if (list == 0 || count is 0 or > MaximumBoxes || !memory.CanRead(list, (int)count * BoxLength))
            return [];

        var entries = memory.ReadBytes(list, (int)count * BoxLength);
        List<CollisionBox> boxes = [];

        for (var index = 0; index < count; index++)
        {
            var entry = entries.AsSpan(index * BoxLength, BoxLength);
            var kind = (BoxKind)Int32(entry, 0);

            if (!Enum.IsDefined(kind))
                continue;

            boxes.Add(new CollisionBox(kind, Single(entry, BoxXField), Single(entry, BoxYField), Single(entry, BoxWidthField), Single(entry, BoxHeightField)));
        }

        return boxes;
    }

    private static int Int32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);

    private static float Single(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
}
