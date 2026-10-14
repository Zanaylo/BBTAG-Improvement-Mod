using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class EntityTests
{
    private const uint Address = 0x09000000;
    private const uint BoxList = 0x0A000000;
    private const uint Owner = 0x09100000;

    private readonly FakeGameMemory _memoryMock = new();

    [Fact]
    public void Read_EntityObject_ReadsItsPlacement()
    {
        // Arrange
        EntityMemory.Write(_memoryMock, Address, new EntityFields(Owner: Owner, FacingLeft: false, PositionX: 150000, PositionY: 2000, PivotX: 10, OffsetX: 20, OffsetY: 30, Rotation: 90000, ScaleX: 500, ScaleY: 2000));

        // Act
        Entity? entity = Entity.Read(_memoryMock, Address);

        // Assert
        Assert.Equal(
            (Owner, false, 150000, 2000, 10, 20, 30, 90000, 500, 2000),
            (entity!.Owner, entity.FacingLeft, entity.PositionX, entity.PositionY, entity.PivotX, entity.OffsetX, entity.OffsetY, entity.Rotation, entity.ScaleX, entity.ScaleY));
    }

    [Fact]
    public void Read_EntityObject_ReadsItsCharacterIndex()
    {
        // Arrange
        EntityMemory.Write(_memoryMock, Address, new EntityFields(CharacterIndex: 26));

        // Act
        Entity? entity = Entity.Read(_memoryMock, Address);

        // Assert
        Assert.Equal(26, entity!.CharacterIndex);
    }

    [Fact]
    public void Read_HurtboxesAndHitboxes_ReadsEveryBoxInOrder()
    {
        // Arrange
        EntityMemory.Write(_memoryMock, Address, new EntityFields(), BoxList, hurtboxes: 1, hitboxes: 1);
        EntityMemory.WriteBoxes(_memoryMock, BoxList, (0, -50, 100, 120, 300), (1, 40, 80, 90, 20));

        // Act
        Entity? entity = Entity.Read(_memoryMock, Address);

        // Assert
        Assert.Equal([new CollisionBox(BoxKind.Hurtbox, -50, 100, 120, 300), new CollisionBox(BoxKind.Hitbox, 40, 80, 90, 20)], entity!.Boxes);
    }

    [Fact]
    public void Read_BoxOfAnUnknownKind_SkipsIt()
    {
        // Arrange
        EntityMemory.Write(_memoryMock, Address, new EntityFields(), BoxList, hurtboxes: 2);
        EntityMemory.WriteBoxes(_memoryMock, BoxList, (7, 0, 0, 1, 1), (0, 0, 0, 1, 1));

        // Act
        Entity? entity = Entity.Read(_memoryMock, Address);

        // Assert
        Assert.Equal([BoxKind.Hurtbox], entity!.Boxes.Select(box => box.Kind));
    }

    [Fact]
    public void Read_BoxCountsThatMakeNoSense_ReadsNoBoxes()
    {
        // Arrange
        EntityMemory.Write(_memoryMock, Address, new EntityFields(), BoxList, hurtboxes: -1, hitboxes: 3);
        EntityMemory.WriteBoxes(_memoryMock, BoxList, (0, 0, 0, 1, 1));

        // Act
        Entity? entity = Entity.Read(_memoryMock, Address);

        // Assert
        Assert.Empty(entity!.Boxes);
    }

    [Fact]
    public void Read_BoxListNotReadable_ReadsNoBoxes()
    {
        // Arrange
        EntityMemory.Write(_memoryMock, Address, new EntityFields(), BoxList, hurtboxes: 2);

        // Act
        Entity? entity = Entity.Read(_memoryMock, Address);

        // Assert
        Assert.Empty(entity!.Boxes);
    }

    [Fact]
    public void Read_AddressNotReadable_ReturnsNull()
    {
        // Act
        Entity? entity = Entity.Read(_memoryMock, Address);

        // Assert
        Assert.Null(entity);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, false)]
    public void IsActive_StatusAndBoxes_IsTrueOnlyForAnActiveEntityWithBoxes(int status, int boxes, bool expected)
    {
        // Arrange
        EntityMemory.Write(_memoryMock, Address, new EntityFields(Status: status), BoxList, hurtboxes: boxes);
        EntityMemory.WriteBoxes(_memoryMock, BoxList, (0, 0, 0, 1, 1));
        Entity entity = Entity.Read(_memoryMock, Address)!;

        // Act
        bool isActive = entity.IsActive;

        // Assert
        Assert.Equal(expected, isActive);
    }
}
