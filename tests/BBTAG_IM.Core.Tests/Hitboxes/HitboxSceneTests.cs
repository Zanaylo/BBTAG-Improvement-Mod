using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Hitboxes;
using BBTAG_IM.Core.Settings;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Hitboxes;

public class HitboxSceneTests
{
    private const uint GameObject = 0x00E59100;
    private const uint BattleObject = 0x014A77A0;
    private const uint CharacterList = 0x0157D3F8;
    private const uint CameraList = 0x00C0A788;
    private const uint EntityList = 0x07000000;
    private const uint FirstEntity = 0x09000000;
    private const uint EntityStride = 0x1000;
    private const uint FirstBoxList = 0x0A000000;
    private const int ProjectileSlot = 10;

    private readonly FakeGameMemory _memoryMock = new();
    private static readonly GameRect Hurtbox = new(-10000, 10000, 0, 300000);
    private static readonly GameRect Hitbox = new(20000, 90000, 100000, 150000);

    private readonly IGameCollision _collisionMock = Substitute.For<IGameCollision>();

    public HitboxSceneTests()
    {
        _memoryMock.Map(BattleObject, 0xA5000).UInt32At(BattleObject + 0xA4210, EntityList);
        _memoryMock.Map(EntityList, GameView.EntityCount * 4);
    }

    public GameView CreateGameView() => new(_memoryMock, new GameAddresses(GameObject, BattleObject, CharacterList, CameraList));

    [Fact]
    public void Collect_BattleNotRunning_ReturnsNoShapes()
    {
        // Arrange
        _memoryMock.UInt32At(BattleObject + 0xA4210, 0);
        GameView game = CreateGameView();

        // Act
        IReadOnlyList<HitboxShape> shapes = HitboxScene.Collect(game, _collisionMock).Boxes;

        // Assert
        Assert.Empty(shapes);
    }

    [Fact]
    public void Collect_CharacterWithBoxes_ReturnsAShapePerBox()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)), (0, 0, 0, 10, 10), (1, 0, 0, 5, 5));
        GameBoxes(Entity(0), [Hurtbox], [Hitbox]);
        GameView game = CreateGameView();

        // Act
        IReadOnlyList<HitboxShape> shapes = HitboxScene.Collect(game, _collisionMock).Boxes;

        // Assert
        Assert.Equal([BoxKind.Hurtbox, BoxKind.Hitbox], shapes.Select(shape => shape.Kind));
    }

    [Fact]
    public void Collect_ProjectileOfACharacter_ReturnsItsBoxes()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)));
        EntityInSlot(ProjectileSlot, new EntityFields(Owner: Entity(0)), (1, 0, 0, 5, 5));
        GameBoxes(Entity(ProjectileSlot), [], [Hitbox]);
        GameView game = CreateGameView();

        // Act
        IReadOnlyList<HitboxShape> shapes = HitboxScene.Collect(game, _collisionMock).Boxes;

        // Assert
        Assert.Equal([BoxKind.Hitbox], shapes.Select(shape => shape.Kind));
    }

    [Fact]
    public void Collect_InactiveProjectile_SkipsIt()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)));
        EntityInSlot(ProjectileSlot, new EntityFields(Status: 0, Owner: Entity(0)), (1, 0, 0, 5, 5));
        GameBoxes(Entity(ProjectileSlot), [], [Hitbox]);
        GameView game = CreateGameView();

        // Act
        IReadOnlyList<HitboxShape> shapes = HitboxScene.Collect(game, _collisionMock).Boxes;

        // Assert
        Assert.Empty(shapes);
    }

    [Fact]
    public void Collect_StageObjectOwnedByNoCharacter_SkipsIt()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)));
        EntityInSlot(ProjectileSlot, new EntityFields(Owner: 0x12345678), (1, 0, 0, 5, 5));
        GameBoxes(Entity(ProjectileSlot), [], [Hitbox]);
        GameView game = CreateGameView();

        // Act
        IReadOnlyList<HitboxShape> shapes = HitboxScene.Collect(game, _collisionMock).Boxes;

        // Assert
        Assert.Empty(shapes);
    }

    [Fact]
    public void Collect_EntityWithoutOwnerWhileACharacterSlotIsEmpty_SkipsIt()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)));
        EntityInSlot(ProjectileSlot, new EntityFields(Owner: 0), (1, 0, 0, 5, 5));
        GameBoxes(Entity(ProjectileSlot), [], [Hitbox]);
        GameView game = CreateGameView();

        // Act
        IReadOnlyList<HitboxShape> shapes = HitboxScene.Collect(game, _collisionMock).Boxes;

        // Assert
        Assert.Empty(shapes);
    }

    [Fact]
    public void Collect_TwoEntitiesWithBoxes_ReturnsTheOriginOfEach()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)), (0, 0, 0, 10, 10), (1, 0, 0, 5, 5));
        EntityInSlot(ProjectileSlot, new EntityFields(Owner: Entity(0)), (1, 0, 0, 5, 5));
        GameBoxes(Entity(0), [Hurtbox], [Hitbox]);
        GameBoxes(Entity(ProjectileSlot), [], [Hitbox]);
        GameView game = CreateGameView();

        // Act
        HitboxFrame frame = HitboxScene.Collect(game, _collisionMock);

        // Assert
        Assert.Equal(2, frame.Origins.Count);
    }

    [Fact]
    public void Collect_EntityWithoutBoxes_ReturnsNoOriginForIt()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)));
        GameView game = CreateGameView();

        // Act
        HitboxFrame frame = HitboxScene.Collect(game, _collisionMock);

        // Assert
        Assert.Empty(frame.Origins);
    }

    [Fact]
    public void Collect_CharacterWithoutActiveStatus_StillReturnsItsBoxes()
    {
        // Arrange
        EntityInSlot(1, new EntityFields(Status: 0, Owner: Entity(1)), (0, 0, 0, 10, 10));
        GameBoxes(Entity(1), [Hurtbox], []);
        GameView game = CreateGameView();

        // Act
        IReadOnlyList<HitboxShape> shapes = HitboxScene.Collect(game, _collisionMock).Boxes;

        // Assert
        Assert.Single(shapes);
    }

    [Fact]
    public void Collect_EntityWithoutBoxes_NeverAsksTheGameForThem()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)));
        GameView game = CreateGameView();

        // Act
        HitboxScene.Collect(game, _collisionMock);

        // Assert
        _collisionMock.DidNotReceiveWithAnyArgs().Boxes(default, default);
    }

    [Fact]
    public void Collect_BoxFromTheGame_PlacesItInWorldUnits()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)), (0, 0, 0, 10, 10));
        GameBoxes(Entity(0), [new GameRect(0, 100000, 0, 250000)], []);
        GameView game = CreateGameView();

        // Act
        Quad box = HitboxScene.Collect(game, _collisionMock).Boxes.Single().World;

        // Assert
        Assert.Equal(HitboxGeometry.World(new GameRect(0, 100000, 0, 250000)), box);
    }

    [Fact]
    public void Collect_PositionFromTheGame_PlacesTheOriginThere()
    {
        // Arrange
        EntityInSlot(0, new EntityFields(Owner: Entity(0)), (0, 0, 0, 10, 10));
        GameBoxes(Entity(0), [Hurtbox], []);
        _collisionMock.Position(Entity(0)).Returns(new GamePoint(100000, 0));
        GameView game = CreateGameView();

        // Act
        HitboxFrame frame = HitboxScene.Collect(game, _collisionMock);

        // Assert
        Assert.Equal(HitboxGeometry.World(new GamePoint(100000, 0)), frame.Origins.Single());
    }

    [Theory]
    [InlineData(true, 6, 11, true)]
    [InlineData(false, 6, 11, false)]
    [InlineData(true, 5, 11, false)]
    [InlineData(true, 6, 6, false)]
    public void IsVisible_SettingModeAndState_ShowsHitboxesOnlyDuringATrainingMatch(bool hitboxViewer, int mode, int state, bool expected)
    {
        // Arrange
        _memoryMock.Map(GameObject, 0x200).Int32At(GameObject + 0x114, mode).Int32At(GameObject + 0x118, state);
        GameView game = CreateGameView();
        ImprovementSettings settings = new() { HitboxViewer = hitboxViewer };

        // Act
        bool isVisible = HitboxScene.IsVisible(game, settings);

        // Assert
        Assert.Equal(expected, isVisible);
    }

    private void EntityInSlot(int slot, EntityFields fields, params (int Kind, float X, float Y, float Width, float Height)[] boxes)
    {
        uint boxList = FirstBoxList + (uint)slot * EntityStride;
        _memoryMock.UInt32At(EntityList + (uint)slot * 4, Entity(slot));
        EntityMemory.Write(_memoryMock, Entity(slot), fields, boxList, hurtboxes: boxes.Length);
        EntityMemory.WriteBoxes(_memoryMock, boxList, boxes);
    }

    private void GameBoxes(uint entity, IReadOnlyList<GameRect> hurtboxes, IReadOnlyList<GameRect> hitboxes)
    {
        _collisionMock.Boxes(entity, BoxKind.Hurtbox).Returns(hurtboxes);
        _collisionMock.Boxes(entity, BoxKind.Hitbox).Returns(hitboxes);
    }

    private static uint Entity(int slot) => FirstEntity + (uint)slot * EntityStride;
}
