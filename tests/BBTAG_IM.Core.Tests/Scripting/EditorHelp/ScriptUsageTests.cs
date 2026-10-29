using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.EditorHelp;

public class ScriptUsageTests
{
    private const int MoveRegister = 14001;
    private const int Upon = 15;

    [Fact]
    public void Add_Commands_CountsThemWithTheCharacterAndFunction()
    {
        // Arrange
        ScriptUsage usage = new();
        ScriptBinary script = Read(new ScriptBytes()
            .State("NmlAtk5A").Command(ScriptTestDatabase.Damage, 1200).Command(ScriptTestDatabase.Damage, 1300).Command(1)
            .Build());

        // Act
        usage.Add("brg", script);

        // Assert
        UsageEntry entry = usage.Find(new UsageKey(UsageKind.Command, ScriptTestDatabase.Damage))!;
        Assert.Equal(2, entry.Count);
        Assert.Equal(2, entry.Characters["brg"]);
        Assert.Equal(["brg NmlAtk5A"], entry.Samples);
    }

    [Fact]
    public void Add_NamedValues_AreCountedByKind()
    {
        // Arrange
        ScriptUsage usage = new();
        ScriptBinary script = Read(new ScriptBytes()
            .Subroutine("MatchInit")
            .Command(MoveRegister, "NmlAtk5A", 0x07).Command(14002)
            .Command(ScriptTestDatabase.CheckInput, 172)
            .Command(9)
            .State("NmlAtk5A").Command(Upon, 10).Command(16).Command(1)
            .Build());

        // Act
        usage.Add("brg", script);

        // Assert
        Assert.Equal(1, usage.Find(new UsageKey(UsageKind.NormalInput, 0x07))?.Count);
        Assert.Equal(["brg NmlAtk5A (registration)"], usage.Find(new UsageKey(UsageKind.NormalInput, 0x07))?.Samples);
        Assert.Equal(["brg MatchInit"], usage.Find(new UsageKey(UsageKind.MoveInput, 172))?.Samples);
        Assert.Equal(1, usage.Find(new UsageKey(UsageKind.MoveInput, 172))?.Count);
        Assert.Equal(["brg NmlAtk5A"], usage.Find(new UsageKey(UsageKind.Upon, 10))?.Samples);
    }

    [Fact]
    public void Add_ManyUses_KeepsALimitedNumberOfSamples()
    {
        // Arrange
        ScriptUsage usage = new();
        ScriptBytes bytes = new();

        for (int index = 0; index < ScriptUsage.SampleLimit + 5; index++)
            bytes.State("State" + index).Command(ScriptTestDatabase.Damage, 1).Command(1);

        // Act
        usage.Add("brg", Read(bytes.Build()));

        // Assert
        UsageEntry entry = usage.Find(new UsageKey(UsageKind.Command, ScriptTestDatabase.Damage))!;
        Assert.Equal(ScriptUsage.SampleLimit + 5, entry.Count);
        Assert.Equal(ScriptUsage.SampleLimit, entry.Samples.Count);
    }

    [Fact]
    public void Add_FullSamplesAndANewCharacter_MakesRoomForIt()
    {
        // Arrange
        ScriptUsage usage = new();
        ScriptBytes bytes = new();

        for (int index = 0; index < ScriptUsage.SampleLimit; index++)
            bytes.State("State" + index).Command(ScriptTestDatabase.Damage, 1).Command(1);

        usage.Add("brg", Read(bytes.Build()));

        // Act
        usage.Add("bha", Read(new ScriptBytes().State("ShotA").Command(ScriptTestDatabase.Damage, 1).Command(1).Build()));

        // Assert
        UsageEntry entry = usage.Find(new UsageKey(UsageKind.Command, ScriptTestDatabase.Damage))!;
        Assert.Equal(ScriptUsage.SampleLimit, entry.Samples.Count);
        Assert.Contains("bha ShotA", entry.Samples);
    }

    [Fact]
    public void Read_WrittenUsage_GivesTheSameUsageBack()
    {
        // Arrange
        ScriptUsage usage = new();
        usage.Add("brg", Read(new ScriptBytes().State("NmlAtk5A").Command(ScriptTestDatabase.Damage, 1).Command(1).Build()));

        // Act
        ScriptUsage read = ScriptUsage.Read(usage.Write());

        // Assert
        UsageEntry entry = read.Find(new UsageKey(UsageKind.Command, ScriptTestDatabase.Damage))!;
        Assert.Equal(1, entry.Count);
        Assert.Equal(["brg NmlAtk5A"], entry.Samples);
        Assert.Equal(1, entry.Characters["brg"]);
    }

    [Fact]
    public void Describe_UsedEntry_SaysHowOftenAndWhere()
    {
        // Arrange
        ScriptUsage usage = new();
        usage.Add("brg", Read(new ScriptBytes().State("NmlAtk5A").Command(ScriptTestDatabase.Damage, 1).Command(1).Build()));

        // Act
        string? text = usage.Describe(new UsageKey(UsageKind.Command, ScriptTestDatabase.Damage));

        // Assert
        Assert.Equal("In the game's scripts: 1 time, 1 character. For example brg NmlAtk5A.", text);
    }

    [Fact]
    public void Describe_SeveralCharacters_ShowsOneExampleOfEachFirst()
    {
        // Arrange
        ScriptUsage usage = new();
        usage.Add("brg", Read(new ScriptBytes()
            .State("A").Command(ScriptTestDatabase.Damage, 1).Command(1)
            .State("B").Command(ScriptTestDatabase.Damage, 1).Command(1)
            .Build()));
        usage.Add("bha", Read(new ScriptBytes().State("C").Command(ScriptTestDatabase.Damage, 1).Command(1).Build()));

        // Act
        string? text = usage.Describe(new UsageKey(UsageKind.Command, ScriptTestDatabase.Damage));

        // Assert
        Assert.Equal("In the game's scripts: 3 times, 2 characters. For example brg A, bha C, brg B.", text);
    }

    [Fact]
    public void Describe_UnusedEntry_ReturnsNull()
    {
        // Act
        string? text = new ScriptUsage().Describe(new UsageKey(UsageKind.Command, ScriptTestDatabase.Damage));

        // Assert
        Assert.Null(text);
    }

    private static ScriptBinary Read(byte[] bytes) => ScriptBinary.Read(bytes, ScriptTestDatabase.Create());
}
