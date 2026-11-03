using System.Text;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class EntitySpriteTests
{
    private const uint EntityAddress = 0x3CD20024;
    private const uint ArchiveAddress = 0x4D2F6908;
    private const uint JonbinAddress = 0x4D307638;
    private const int EntityLength = 0x2600;

    private readonly FakeGameMemory _memory = new();

    [Fact]
    public void Read_CharacterShowingASprite_ReturnsItsNameArchiveAndBoxes()
    {
        // Arrange
        Entity("rg000_04", ArchiveAddress, JonbinAddress);

        // Act
        EntitySprite? sprite = EntitySprite.Read(_memory, EntityAddress);

        // Assert
        Assert.Equal(new EntitySprite(EntityAddress, ArchiveAddress, "rg000_04", JonbinAddress), sprite);
    }

    [Fact]
    public void Read_ObjectWithoutACollisionArchive_ReturnsNull()
    {
        // Arrange
        Entity("rg000_04", 0, JonbinAddress);

        // Act
        EntitySprite? sprite = EntitySprite.Read(_memory, EntityAddress);

        // Assert
        Assert.Null(sprite);
    }

    [Fact]
    public void Read_ObjectWithoutASprite_ReturnsNull()
    {
        // Arrange
        Entity(string.Empty, ArchiveAddress, JonbinAddress);

        // Act
        EntitySprite? sprite = EntitySprite.Read(_memory, EntityAddress);

        // Assert
        Assert.Null(sprite);
    }

    [Fact]
    public void Read_UnreadableObject_ReturnsNull()
    {
        // Arrange
        _memory.Map(EntityAddress, 0x100);

        // Act
        EntitySprite? sprite = EntitySprite.Read(_memory, EntityAddress);

        // Assert
        Assert.Null(sprite);
    }

    [Fact]
    public void FileName_Sprite_IsTheCollisionFileOfThatSprite()
    {
        // Arrange
        EntitySprite sprite = new(EntityAddress, ArchiveAddress, "rg000_04", JonbinAddress);

        // Act
        string fileName = sprite.FileName;

        // Assert
        Assert.Equal("rg000_04.jonbin", fileName);
    }

    [Fact]
    public void StateOf_CharacterInAMove_ReturnsTheStateName()
    {
        // Arrange
        Entity("rg211_11", ArchiveAddress, JonbinAddress);
        _memory.BytesAt(EntityAddress + 0x25DC, Encoding.ASCII.GetBytes("NmlAtk5A\0"));

        // Act
        string? state = EntitySprite.StateOf(_memory, EntityAddress);

        // Assert
        Assert.Equal("NmlAtk5A", state);
    }

    [Fact]
    public void StateOf_UnreadableObject_ReturnsNull()
    {
        // Arrange
        _memory.Map(EntityAddress, 0x100);

        // Act
        string? state = EntitySprite.StateOf(_memory, EntityAddress);

        // Assert
        Assert.Null(state);
    }

    private void Entity(string sprite, uint archive, uint jonbin)
    {
        _memory.Map(EntityAddress, EntityLength)
            .UInt32At(EntityAddress + 0x40, jonbin)
            .UInt32At(EntityAddress + 0x154, archive)
            .BytesAt(EntityAddress + 0x1420, Encoding.ASCII.GetBytes(sprite + "\0"));
    }
}
