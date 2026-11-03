using BBTAG_IM.Core.Hitboxes.Editing;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class MoveFollowingTests
{
    private static readonly CharacterMove Idle = new("CmnActStand", [new MoveFrame("rg000_00", 6), new MoveFrame("rg000_01", 6)]);
    private static readonly CharacterMove Jab = new("NmlAtk5A", [new MoveFrame("rg211_00", 2), new MoveFrame("rg211_01", 3), new MoveFrame("rg211_00", 2)]);
    private static readonly CharacterMove Dog = new("408_dog", [new MoveFrame("vrrgef408_00", 2), new MoveFrame("vrrgef408_01", 2)], IsEffect: true);

    [Fact]
    public void Find_StateAndSpriteOfAMove_GivesTheMoveAndFrame()
    {
        // Act
        MovePosition? position = MoveFollowing.Find([Idle, Jab], [new ShownState("NmlAtk5A", "rg211_01")], current: null);

        // Assert
        Assert.Equal(new MovePosition(1, 1), position);
    }

    [Fact]
    public void Find_StateNotInTheList_GivesNothing()
    {
        // Act
        MovePosition? position = MoveFollowing.Find([Idle, Jab], [new ShownState("CmnActHurt", "rg211_01")], current: null);

        // Assert
        Assert.Null(position);
    }

    [Fact]
    public void Find_SpriteNotInTheMove_GivesNothing()
    {
        // Act
        MovePosition? position = MoveFollowing.Find([Idle, Jab], [new ShownState("NmlAtk5A", "rg000_00")], current: null);

        // Assert
        Assert.Null(position);
    }

    [Fact]
    public void Find_SpriteShownTwiceInTheMove_SearchesFromTheCurrentFrame()
    {
        // Act
        MovePosition? position = MoveFollowing.Find([Idle, Jab], [new ShownState("NmlAtk5A", "rg211_00")], current: new MovePosition(1, 1));

        // Assert
        Assert.Equal(new MovePosition(1, 2), position);
    }

    [Fact]
    public void Find_SeveralObjectsShowing_KeepsTheMoveAlreadyShown()
    {
        // Act
        MovePosition? position = MoveFollowing.Find(
            [Jab, Dog],
            [new ShownState("NmlAtk5A", "rg211_01"), new ShownState("408_dog", "vrrgef408_01")],
            current: new MovePosition(1, 0));

        // Assert
        Assert.Equal(new MovePosition(1, 1), position);
    }

    [Fact]
    public void Find_SeveralObjectsShowingOtherMoves_GivesTheFirstFound()
    {
        // Act
        MovePosition? position = MoveFollowing.Find(
            [Idle, Jab, Dog],
            [new ShownState("NmlAtk5A", "rg211_01"), new ShownState("408_dog", "vrrgef408_01")],
            current: new MovePosition(0, 0));

        // Assert
        Assert.Equal(new MovePosition(1, 1), position);
    }
}
