using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Hantei;

public class Ha6FileTests
{
    [Fact]
    public void Read_NotHantei_Throws()
    {
        // Arrange
        byte[] bytes = new byte[64];

        // Act
        Exception? exception = Record.Exception(() => Ha6File.Read(bytes));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }

    [Fact]
    public void Read_Pattern_KeepsIdNameAndFrames()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(7, "Stand", pattern => pattern.Frame(_ => { }).Frame(_ => { })).Build();

        // Act
        IReadOnlyDictionary<int, Ha6Pattern> patterns = Ha6File.Read(bytes);

        // Assert
        Assert.Equal("Stand", patterns[7].Name);
        Assert.Equal(2, patterns[7].Frames.Count);
    }

    [Theory]
    [InlineData("AFD5", 5)]
    [InlineData("AFD9", 9)]
    public void Read_DurationInTheTag_ReadsIt(string tag, int expected)
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame.Anim(anim => anim.Tag(tag)))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Equal(expected, frame.Duration);
    }

    [Fact]
    public void Read_LongDuration_ReadsItsValue()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame.Anim(anim => anim.Tag("AFDL", 40)))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Equal(40, frame.Duration);
    }

    [Fact]
    public void Read_NoDuration_LastsOneFrame()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(_ => { })).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Equal(1, frame.Duration);
    }

    [Fact]
    public void Read_Layers_ReadsSpriteAndOffsetOfEachLayer()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame.Anim(anim => anim
            .Tag("AFGX", 0, 0, 12).Tag("AFOF", -304, -369)
            .Tag("AFGX", 1, 1, 3)
            .Tag("AFGX", 2, 0, -1)))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Equal([new Ha6Layer(0, false, 12, -304, -369), new Ha6Layer(1, true, 3, 0, 0), new Ha6Layer(2, false, -1, 0, 0)], frame.Layers);
    }

    [Fact]
    public void Read_Boxes_SplitsNormalAndAttackBoxes()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame
            .Tag("HRNM", 1, -60, -320, 60, 0)
            .Tag("HRAT", 0, 10, -200, 90, -100))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Equal(new Ha6Box(-60, -320, 60, 0), frame.Boxes[1]);
        Assert.Equal(new Ha6Box(10, -200, 90, -100), frame.AttackBoxes[0]);
    }

    [Fact]
    public void Read_BoxWithNoArea_IsLeftOut()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame.Tag("HRNM", 1, 5, -320, 5, 0))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Empty(frame.Boxes);
    }

    [Fact]
    public void Read_BoxAlias_CopiesTheBoxEmittedAtThatOrdinal()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern
            .Frame(frame => frame.Tag("HRNM", 1, -60, -320, 60, 0).Tag("HRAT", 0, 10, -200, 90, -100))
            .Frame(frame => frame.Tag("HRNS", 2, 0).Tag("HRAS", 3, 1))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[1];

        // Assert
        Assert.Equal(new Ha6Box(-60, -320, 60, 0), frame.Boxes[2]);
        Assert.Equal(new Ha6Box(10, -200, 90, -100), frame.AttackBoxes[3]);
    }

    [Fact]
    public void Read_StateCopy_TakesTheStateBlockAtThatOrdinal()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern
            .Frame(frame => frame.State(state => state.Tag("ASYS", 3).Tag("ASS1")))
            .Frame(frame => frame.Tag("ASSM", 0))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[1];

        // Assert
        Assert.Equal([3], frame.State["ASYS"]);
        Assert.True(frame.State.ContainsKey("ASS1"));
    }

    [Fact]
    public void Read_AttackBlock_KeepsItsValues()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame.Attack(attack => attack
            .Tag("ATGD", 7).Tag("ATV2", 3, 2, 0, 1, 0, 2, 0, 3, 0, 4, 0, 5, 0, 6)))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Equal([7], frame.Attack["ATGD"]);
        Assert.Equal(14, frame.Attack["ATV2"].Length);
    }

    [Fact]
    public void Read_SingleValueVectorChunk_KeepsTheChunksAfterIt()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame.Attack(attack => attack
            .Tag("ATVD", 20).Tag("ATGD", 7)))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Assert.Equal([20], frame.Attack["ATVD"]);
        Assert.Equal([7], frame.Attack["ATGD"]);
    }

    [Fact]
    public void Read_Effect_ReadsTypeNumberAndParameters()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame
            .Tag("EFST", 0).Tag("EFTP", 8).Tag("EFNO", 2).Tag("EFPR", 2, 11, -12).Tag("EFED"))).Build();

        // Act
        Ha6Frame frame = Ha6File.Read(bytes)[0].Frames[0];

        // Assert
        Ha6Effect effect = Assert.Single(frame.Effects);
        Assert.Equal(8, effect.Type);
        Assert.Equal(2, effect.Number);
        Assert.Equal([11, -12], effect.Parameters);
    }

    [Fact]
    public void Read_UnknownChunk_Throws()
    {
        // Arrange
        byte[] bytes = new Ha6Bytes().Pattern(0, "A", pattern => pattern.Frame(frame => frame.Anim(anim => anim.Tag("ZZZZ", 1)))).Build();

        // Act
        Exception? exception = Record.Exception(() => Ha6File.Read(bytes));

        // Assert
        Assert.IsType<InvalidHanteiDataException>(exception);
    }

    [Fact]
    public void Merge_LaterFileFillsAPattern_LaterWins()
    {
        // Arrange
        Dictionary<int, Ha6Pattern> first = new() { [0] = Pattern(0, "Old", 1) };
        Dictionary<int, Ha6Pattern> second = new() { [0] = Pattern(0, "New", 2) };

        // Act
        IReadOnlyDictionary<int, Ha6Pattern> merged = Ha6File.Merge([first, second], new Dictionary<int, Ha6Pattern>());

        // Assert
        Assert.Equal("New", merged[0].Name);
    }

    [Fact]
    public void Merge_EmptyDeclaration_KeepsTheFilledPattern()
    {
        // Arrange
        Dictionary<int, Ha6Pattern> first = new() { [0] = Pattern(0, "Filled", 1) };
        Dictionary<int, Ha6Pattern> second = new() { [0] = Pattern(0, "Empty", 0) };

        // Act
        IReadOnlyDictionary<int, Ha6Pattern> merged = Ha6File.Merge([first, second], new Dictionary<int, Ha6Pattern>());

        // Assert
        Assert.Equal("Filled", merged[0].Name);
    }

    [Fact]
    public void Merge_SharedTemplate_NeverReplacesACharacterPattern()
    {
        // Arrange
        Dictionary<int, Ha6Pattern> character = new() { [0] = Pattern(0, "Character", 1) };
        Dictionary<int, Ha6Pattern> shared = new() { [0] = Pattern(0, "Template", 1), [1] = Pattern(1, "Common", 1) };

        // Act
        IReadOnlyDictionary<int, Ha6Pattern> merged = Ha6File.Merge([character], shared);

        // Assert
        Assert.Equal("Character", merged[0].Name);
        Assert.Equal("Common", merged[1].Name);
    }

    private static Ha6Pattern Pattern(int id, string name, int frames) =>
        new(id, name, Enumerable.Range(0, frames).Select(_ => Ha6Frame.Empty).ToArray());
}
