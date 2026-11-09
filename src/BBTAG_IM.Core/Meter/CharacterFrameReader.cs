using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Meter;

public static class CharacterFrameReader
{
    public const int ObjectLength = HitstunField + sizeof(int);

    private const int FrameField = 0x4;
    private const int HitboxCountField = 0x98;
    private const int StateTimeField = 0x168;
    private const int HitstopField = 0x1D0;
    private const int ContactField = 0x268;
    private const int PositionYField = 0x2F0;
    private const int InvincibleFramesField = 0xA7C;
    private const int InvincibleField = 0xA80;
    private const int ArmorField = 0xA84;
    private const int HealthField = 0xABC;
    private const int StateField = 0x25DC;
    private const int StateLength = 32;
    private const int AllowedActionsField = 0x2908;
    private const int BlockstunField = 0x2918;
    private const int HitstunField = 0x5814;

    private const uint CanAttackBit = 0x20000000;
    private const uint ContactBit = 0x1;
    private const uint HitBit = 0x40;
    private const uint InvincibleBit = 0x2;

    private static readonly (uint Bit, Invulnerability Attribute)[] Attributes =
    [
        (0x8, Invulnerability.Head),
        (0x10, Invulnerability.Body),
        (0x20, Invulnerability.Leg),
        (0x80000, Invulnerability.Projectile),
        (0x40, Invulnerability.Throw),
    ];

    public static CharacterFrame? Read(IGameMemory memory, uint character)
    {
        if (!memory.CanRead(character, ObjectLength))
            return null;

        var fields = new byte[ObjectLength];
        memory.Read(character, fields);

        return new CharacterFrame(
            Int32(fields, FrameField),
            State(fields),
            (UInt32(fields, AllowedActionsField) & CanAttackBit) != 0,
            Int32(fields, StateTimeField),
            Int32(fields, HitstopField),
            Int32(fields, HitstunField),
            Int32(fields, BlockstunField),
            Int32(fields, HitboxCountField),
            InvulnerabilityOf(fields),
            Int32(fields, ArmorField) != 0,
            ContactOf(UInt32(fields, ContactField)),
            Int32(fields, PositionYField) > 0,
            Int32(fields, HealthField));
    }

    private static Invulnerability InvulnerabilityOf(ReadOnlySpan<byte> fields)
    {
        var flags = UInt32(fields, InvincibleField);
        var invincible = (flags & InvincibleBit) != 0 || Int32(fields, InvincibleFramesField) > 0;

        if (!invincible)
            return Invulnerability.None;

        return Attributes
            .Where(attribute => (flags & attribute.Bit) != 0)
            .Aggregate(Invulnerability.None, (covered, attribute) => covered | attribute.Attribute);
    }

    private static Contact ContactOf(uint flags)
    {
        if ((flags & ContactBit) == 0)
            return Contact.None;

        return (flags & HitBit) != 0 ? Contact.Hit : Contact.Blocked;
    }

    private static string State(ReadOnlySpan<byte> fields)
    {
        var name = fields.Slice(StateField, StateLength);
        var terminator = name.IndexOf((byte)0);
        return Encoding.Latin1.GetString(terminator < 0 ? name : name[..terminator]);
    }

    private static int Int32(ReadOnlySpan<byte> fields, int offset) => BinaryPrimitives.ReadInt32LittleEndian(fields[offset..]);

    private static uint UInt32(ReadOnlySpan<byte> fields, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(fields[offset..]);
}
