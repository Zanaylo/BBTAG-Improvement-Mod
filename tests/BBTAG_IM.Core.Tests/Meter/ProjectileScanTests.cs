using BBTAG_IM.Core.Meter;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Meter;

public class ProjectileScanTests
{
    private const uint List = 0x30000000;
    private const uint Player1 = 0x40000000;
    private const uint Player2 = 0x40100000;
    private const uint Player1Partner = 0x40200000;
    private const uint Player2Partner = 0x40300000;
    private const uint Fireball = 0x41000000;
    private const uint Effect = 0x41100000;
    private const int ActiveStatus = 1;

    private readonly FakeGameMemory _memory = new();
    private readonly uint?[] _characters = [Player1, Player2, Player1Partner, Player2Partner];

    public ProjectileScanTests()
    {
        _memory.Map(List, ProjectileScan.ListLength);
        Entity(0, Player1, owner: 0, hitboxes: 0);
        Entity(1, Player2, owner: 0, hitboxes: 0);
        Entity(2, Player1Partner, owner: 0, hitboxes: 0);
        Entity(3, Player2Partner, owner: 0, hitboxes: 0);
    }

    [Fact]
    public void Owners_ActiveProjectileOfThePartner_MarksThePartnerSlot()
    {
        // Arrange
        Entity(10, Fireball, owner: Player1Partner, hitboxes: 1);

        // Act
        IReadOnlyList<bool> owners = ProjectileScan.Owners(_memory, List, _characters);

        // Assert
        Assert.Equal([false, false, true, false], owners);
    }

    [Fact]
    public void Owners_ObjectWithoutHitboxes_MarksNothing()
    {
        // Arrange
        Entity(10, Effect, owner: Player2, hitboxes: 0);

        // Act
        IReadOnlyList<bool> owners = ProjectileScan.Owners(_memory, List, _characters);

        // Assert
        Assert.Equal([false, false, false, false], owners);
    }

    [Fact]
    public void Owners_InactiveObject_MarksNothing()
    {
        // Arrange
        Entity(10, Fireball, owner: Player2, hitboxes: 2, status: 0);

        // Act
        IReadOnlyList<bool> owners = ProjectileScan.Owners(_memory, List, _characters);

        // Assert
        Assert.Equal([false, false, false, false], owners);
    }

    [Fact]
    public void Owners_CharacterWithItsOwnHitboxes_IsNotAProjectile()
    {
        // Arrange
        _memory.Int32At(Player1 + 0x98, 2);

        // Act
        IReadOnlyList<bool> owners = ProjectileScan.Owners(_memory, List, _characters);

        // Assert
        Assert.Equal([false, false, false, false], owners);
    }

    [Fact]
    public void Owners_UnreadableList_MarksNothing()
    {
        // Act
        IReadOnlyList<bool> owners = ProjectileScan.Owners(_memory, 0x50000000, _characters);

        // Assert
        Assert.Equal([false, false, false, false], owners);
    }

    private void Entity(int slot, uint address, uint owner, int hitboxes, int status = ActiveStatus)
    {
        _memory.UInt32At(List + (uint)slot * sizeof(uint), address);

        if (!_memory.CanRead(address, ProjectileScan.EntityLength))
            _memory.Map(address, ProjectileScan.EntityLength);

        _memory.Int32At(address + 0x10, status).Int32At(address + 0x98, hitboxes).UInt32At(address + 0x1F4, owner);
    }
}
