using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class CharacterMovesTests
{
    private const int EndState = 1;
    private const int Sprite = 2;
    private const int EndSubroutine = 9;
    private const int Upon = 15;
    private const int EndUpon = 16;
    private const int UnknownCommand = 31999;

    private readonly ScriptDatabase _database = ScriptTestDatabase.Create();

    [Fact]
    public void Read_StateShowingSprites_ListsItsFramesInOrder()
    {
        // Arrange
        byte[] script = new ScriptBytes().State("NmlAtk5A")
            .Command(Sprite, "rg211_00", 1).Command(ScriptTestDatabase.Damage, 10).Command(Sprite, "rg211_03", 3).Command(EndState).Build();

        // Act
        IReadOnlyList<CharacterMove> moves = CharacterMoves.Read(script, _database);

        // Assert
        CharacterMove move = Assert.Single(moves);
        Assert.Equal("NmlAtk5A", move.Name);
        Assert.Equal([new MoveFrame("rg211_00", 1), new MoveFrame("rg211_03", 3)], move.Frames);
    }

    [Fact]
    public void Read_EffectsScript_MarksItsMovesAsEffects()
    {
        // Arrange
        byte[] script = new ScriptBytes().State("RagnaShot").Command(Sprite, "rgef402_00", 2).Command(EndState).Build();

        // Act
        IReadOnlyList<CharacterMove> moves = CharacterMoves.Read(script, _database, effects: true);

        // Assert
        Assert.True(Assert.Single(moves).IsEffect);
    }

    [Fact]
    public void Read_StatesWithoutSprites_AreLeftOut()
    {
        // Arrange
        byte[] script = new ScriptBytes().State("Empty").Command(EndState)
            .State("Idle").Command(Sprite, "rg000_00", 6).Command(EndState).Build();

        // Act
        IReadOnlyList<CharacterMove> moves = CharacterMoves.Read(script, _database);

        // Assert
        Assert.Equal(["Idle"], moves.Select(move => move.Name));
    }

    [Fact]
    public void Read_SpritesOfSubroutinesAndUponBlocks_AreNotFramesOfTheMove()
    {
        // Arrange
        byte[] script = new ScriptBytes().Subroutine("Shared").Command(Sprite, "rg999_00", 1).Command(EndSubroutine)
            .State("Idle").Command(Upon, 0).Command(Sprite, "rg998_00", 1).Command(EndUpon)
            .Command(Sprite, "rg000_00", 6).Command(EndState).Build();

        // Act
        IReadOnlyList<CharacterMove> moves = CharacterMoves.Read(script, _database);

        // Assert
        CharacterMove move = Assert.Single(moves);
        Assert.Equal([new MoveFrame("rg000_00", 6)], move.Frames);
    }

    [Fact]
    public void Read_StateWithACommandTheDatabaseDoesNotKnow_LeavesOutOnlyThatState()
    {
        // Arrange
        byte[] script = new ScriptBytes().State("Broken").Command(Sprite, "rg100_00", 1).Command(UnknownCommand).Command(EndState)
            .State("Idle").Command(Sprite, "rg000_00", 6).Command(EndState).Build();

        // Act
        IReadOnlyList<CharacterMove> moves = CharacterMoves.Read(script, _database);

        // Assert
        Assert.Equal(["Idle"], moves.Select(move => move.Name));
    }

    [Fact]
    public void Read_StateWhoseLastCommandRunsIntoTheNextState_LeavesOutOnlyThatState()
    {
        // Arrange
        byte[] script = new ScriptBytes().State("Cut").Command(EndState).Command(Sprite, "rg100_00")
            .State("Idle").Command(Sprite, "rg000_00", 6).Command(EndState).Build();

        // Act
        IReadOnlyList<CharacterMove> moves = CharacterMoves.Read(script, _database);

        // Assert
        Assert.Equal(["Idle"], moves.Select(move => move.Name));
    }

    [Fact]
    public void Read_FileTooShortForItsStateList_ThrowsInvalidScriptException()
    {
        // Arrange
        byte[] script = [5, 0, 0, 0];

        // Act
        Exception? exception = Record.Exception(() => CharacterMoves.Read(script, _database));

        // Assert
        Assert.IsType<InvalidScriptException>(exception);
    }

    [Fact]
    public void FrameShowing_SpriteUsedTwice_PicksTheFirstAtOrAfterTheCurrentFrame()
    {
        // Arrange
        CharacterMove move = new("Idle", [new MoveFrame("rg000_00", 6), new MoveFrame("rg000_01", 6), new MoveFrame("rg000_00", 6)]);

        // Act
        int frame = move.FrameShowing("rg000_00", from: 1);

        // Assert
        Assert.Equal(2, frame);
    }

    [Fact]
    public void FrameShowing_SpriteOnlyBeforeTheCurrentFrame_WrapsAround()
    {
        // Arrange
        CharacterMove move = new("Idle", [new MoveFrame("rg000_00", 6), new MoveFrame("rg000_01", 6)]);

        // Act
        int frame = move.FrameShowing("rg000_00", from: 1);

        // Assert
        Assert.Equal(0, frame);
    }

    [Fact]
    public void FrameShowing_SpriteNotInTheMove_IsMinusOne()
    {
        // Arrange
        CharacterMove move = new("Idle", [new MoveFrame("rg000_00", 6)]);

        // Act
        int frame = move.FrameShowing("rg211_00", from: 0);

        // Assert
        Assert.Equal(-1, frame);
    }

    [Theory]
    [InlineData("null", false)]
    [InlineData("NULL", false)]
    [InlineData("rg211_00", true)]
    public void ShowsASprite_SpriteName_IsFalseOnlyForTheEmptySprite(string sprite, bool expected)
    {
        // Arrange
        MoveFrame frame = new(sprite, 3);

        // Act
        bool shows = frame.ShowsASprite;

        // Assert
        Assert.Equal(expected, shows);
    }
}
