namespace BBTAG_IM.Core.Tests.Support;

internal sealed record EntityFields(
    int Status = 1,
    uint Owner = 0,
    bool FacingLeft = true,
    int PositionX = 0,
    int PositionY = 0,
    int PivotX = 0,
    int OffsetX = 0,
    int OffsetY = 0,
    int Rotation = 0,
    int ScaleX = 1000,
    int ScaleY = 1000,
    int CharacterIndex = 0);

internal static class EntityMemory
{
    public const int Length = 0x310;

    private const int BoxLength = 20;

    public static void Write(FakeGameMemory memory, uint address, EntityFields fields, uint boxList = 0, int hurtboxes = 0, int hitboxes = 0)
    {
        memory.Map(address, Length)
            .Int32At(address + 0x10, fields.Status)
            .Int32At(address + 0x34, fields.CharacterIndex)
            .UInt32At(address + 0x4C, boxList)
            .Int32At(address + 0x94, hurtboxes)
            .Int32At(address + 0x98, hitboxes)
            .UInt32At(address + 0x1F4, fields.Owner)
            .Int32At(address + 0x274, fields.FacingLeft ? 1 : 0)
            .Int32At(address + 0x284, fields.PivotX)
            .Int32At(address + 0x28C, fields.Rotation)
            .Int32At(address + 0x294, fields.ScaleX)
            .Int32At(address + 0x298, fields.ScaleY)
            .Int32At(address + 0x2EC, fields.PositionX)
            .Int32At(address + 0x2F0, fields.PositionY)
            .Int32At(address + 0x304, fields.OffsetX)
            .Int32At(address + 0x30C, fields.OffsetY);
    }

    public static void WriteBoxes(FakeGameMemory memory, uint address, params (int Kind, float X, float Y, float Width, float Height)[] boxes)
    {
        memory.Map(address, Math.Max(boxes.Length, 1) * BoxLength);

        for (int index = 0; index < boxes.Length; index++)
        {
            uint entry = address + (uint)(index * BoxLength);
            memory.Int32At(entry, boxes[index].Kind)
                .SingleAt(entry + 4, boxes[index].X)
                .SingleAt(entry + 8, boxes[index].Y)
                .SingleAt(entry + 12, boxes[index].Width)
                .SingleAt(entry + 16, boxes[index].Height);
        }
    }
}
