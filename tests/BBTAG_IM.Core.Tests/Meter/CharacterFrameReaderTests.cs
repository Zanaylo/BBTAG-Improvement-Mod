using System.Text;
using BBTAG_IM.Core.Meter;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Meter;

public class CharacterFrameReaderTests
{
    private const uint Character = 0x40189024;
    private const int CanAttack = 0x20000000;
    private const int DefaultAttributes = 0x001BBFF9;
    private const int Invincible = 0x2;

    private readonly FakeGameMemory _memory = new();

    public CharacterFrameReaderTests()
    {
        _memory.Map(Character, CharacterFrameReader.ObjectLength)
            .Int32At(Character + 0x4, 1568)
            .Int32At(Character + 0x2EC, -260000)
            .Int32At(Character + 0x0A80, DefaultAttributes);
        State("CmnActStand");
    }

    [Fact]
    public void Read_StandingCharacter_ReturnsAFreeIdleFrame()
    {
        // Arrange
        _memory.Int32At(Character + 0x2908, unchecked((int)0xB011716E))
            .Int32At(Character + 0x168, 44)
            .Int32At(Character + 0xABC, 17000);

        // Act
        CharacterFrame? frame = CharacterFrameReader.Read(_memory, Character);

        // Assert
        Assert.Equal(new CharacterFrame(1568, "CmnActStand", true, 44, 0, 0, 0, 0, Invulnerability.None, false, Contact.None, false, 17000), frame);
    }

    [Fact]
    public void Read_CharacterInHitstunAndHitstop_ReadsBothCounters()
    {
        // Arrange
        State("CmnActHitStandLv4");
        _memory.Int32At(Character + 0x1D0, 10).Int32At(Character + 0x5814, 17);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal((10, 17, 0, false), (frame.Hitstop, frame.Hitstun, frame.Blockstun, frame.CanAct));
    }

    [Fact]
    public void Read_CharacterBlocking_ReadsBlockstun()
    {
        // Arrange
        _memory.Int32At(Character + 0x2918, 16);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal(16, frame.Blockstun);
    }

    [Fact]
    public void Read_AttackWithHitboxesThatHit_ReadsHitboxesAndContact()
    {
        // Arrange
        _memory.Int32At(Character + 0x98, 2).Int32At(Character + 0x268, 0x40000061);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal((2, Contact.Hit), (frame.Hitboxes, frame.Contact));
    }

    [Fact]
    public void Read_AttackThatWasBlocked_ReadsBlockedContact()
    {
        // Arrange
        _memory.Int32At(Character + 0x268, 0x40000021);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal(Contact.Blocked, frame.Contact);
    }

    [Fact]
    public void Read_InvincibleFlag_ReturnsEveryAttributeTheFlagsCover()
    {
        // Arrange
        _memory.Int32At(Character + 0xA80, DefaultAttributes | Invincible);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal(Invulnerability.Full, frame.Invulnerability);
    }

    [Fact]
    public void Read_InvincibleForSomeFrames_CountsAsInvulnerable()
    {
        // Arrange
        _memory.Int32At(Character + 0xA7C, 7);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal(Invulnerability.Full, frame.Invulnerability);
    }

    [Fact]
    public void Read_InvincibleOnlyToThrows_ReturnsThrow()
    {
        // Arrange
        _memory.Int32At(Character + 0xA80, Invincible | 0x40);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal(Invulnerability.Throw, frame.Invulnerability);
    }

    [Fact]
    public void Read_ArmorAndAirborne_ReadsBoth()
    {
        // Arrange
        _memory.Int32At(Character + 0xA84, 1).Int32At(Character + 0x2F0, 120000);

        // Act
        CharacterFrame frame = CharacterFrameReader.Read(_memory, Character)!;

        // Assert
        Assert.Equal((true, true), (frame.Armor, frame.Airborne));
    }

    [Fact]
    public void Read_ObjectOutsideReadableMemory_ReturnsNull()
    {
        // Act
        CharacterFrame? frame = CharacterFrameReader.Read(_memory, 0x10000000);

        // Assert
        Assert.Null(frame);
    }

    private void State(string name)
    {
        var bytes = new byte[32];
        Encoding.ASCII.GetBytes(name).CopyTo(bytes, 0);
        _memory.BytesAt(Character + 0x25DC, bytes);
    }
}
