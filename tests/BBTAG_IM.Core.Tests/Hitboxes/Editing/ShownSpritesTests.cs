using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class ShownSpritesTests
{
    private const uint GameObject = 0x00E59100;
    private const uint BattleObject = 0x014A77A0;
    private const uint CharacterList = 0x0157D3F8;
    private const uint CameraList = 0x00C0A788;
    private const uint EntityList = 0x07000000;
    private const uint FirstEntity = 0x09000000;
    private const uint EntityStride = 0x4000;
    private const int EntityLength = 0x2600;
    private const uint Archive = 0x4D2F6908;
    private const int Active = 1;
    private const int Dying = 4;

    private readonly FakeGameMemory _memory = new();

    public ShownSpritesTests()
    {
        _memory.Map(BattleObject, 0xA5000).UInt32At(BattleObject + 0xA4210, EntityList);
        _memory.Map(EntityList, GameView.EntityCount * 4);
    }

    [Fact]
    public void Of_CharacterWithAProjectile_ListsBothSprites()
    {
        // Arrange
        uint character = EntityInSlot(0, "rg211_11", owner: 0, Active);
        EntityInSlot(10, "rgef402_00", owner: character, Active);

        // Act
        IReadOnlyList<EntitySprite> shown = ShownSprites.Of(CreateGameView(), character);

        // Assert
        Assert.Equal(["rg211_11", "rgef402_00"], shown.Select(sprite => sprite.Name));
    }

    [Fact]
    public void Of_ObjectsOfTheOtherPlayerOrDying_AreLeftOut()
    {
        // Arrange
        uint character = EntityInSlot(0, "rg211_11", owner: 0, Active);
        uint other = EntityInSlot(1, "rg000_00", owner: 0, Active);
        EntityInSlot(10, "rgef402_00", owner: other, Active);
        EntityInSlot(11, "rgef403_00", owner: character, Dying);

        // Act
        IReadOnlyList<EntitySprite> shown = ShownSprites.Of(CreateGameView(), character);

        // Assert
        Assert.Equal(["rg211_11"], shown.Select(sprite => sprite.Name));
    }

    [Fact]
    public void Of_NoBattle_ListsNothing()
    {
        // Arrange
        _memory.UInt32At(BattleObject + 0xA4210, 0);

        // Act
        IReadOnlyList<EntitySprite> shown = ShownSprites.Of(CreateGameView(), FirstEntity);

        // Assert
        Assert.Empty(shown);
    }

    private GameView CreateGameView() => new(_memory, new GameAddresses(GameObject, BattleObject, CharacterList, CameraList));

    private uint EntityInSlot(int slot, string sprite, uint owner, int status)
    {
        uint entity = FirstEntity + (uint)slot * EntityStride;
        _memory.Map(entity, EntityLength)
            .Int32At(entity + 0x10, status)
            .UInt32At(entity + 0x154, Archive)
            .UInt32At(entity + 0x1F4, owner)
            .BytesAt(entity + 0x1420, Encoding.ASCII.GetBytes(sprite + "\0"))
            .UInt32At(EntityList + (uint)slot * 4, entity);
        return entity;
    }
}
