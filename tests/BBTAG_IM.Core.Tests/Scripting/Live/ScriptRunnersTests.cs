using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class ScriptRunnersTests
{
    private const uint Battle = 0x10000000;
    private const uint EntityList = 0x11000000;
    private const uint First = 0x12000000;
    private const uint Second = 0x12100000;
    private const uint Record = 0x01000000;
    private const uint OtherRecord = 0x01000018;
    private const uint CurrentFile = 0x06000000;
    private const int CurrentLength = 0x1000;

    private readonly FakeGameMemory _memoryMock = new();

    [Fact]
    public void InstructionsOf_ObjectsRunningTwoScripts_ReturnsOnlyThoseOfTheRecord()
    {
        // Arrange
        _memoryMock.Map(Battle + 0xA4210, 4).UInt32At(Battle + 0xA4210, EntityList)
            .Map(EntityList, GameView.EntityCount * 4).UInt32At(EntityList, First).UInt32At(EntityList + 4, Second)
            .Map(First, 0x2000).UInt32At(First + 0x1400, Record).UInt32At(First + 0x1408, 0x02000100)
            .Map(Second, 0x2000).UInt32At(Second + 0x1400, OtherRecord).UInt32At(Second + 0x1408, 0x03000100);
        ScriptRunners runners = CreateScriptRunners();

        // Act
        IReadOnlyList<uint> instructions = runners.InstructionsOf(Record);

        // Assert
        Assert.Equal([0x02000100u], instructions);
    }

    [Fact]
    public void InstructionsOf_NoMatchRunning_ReturnsNothing()
    {
        // Arrange
        _memoryMock.Map(Battle + 0xA4210, 4);
        ScriptRunners runners = CreateScriptRunners();

        // Act
        IReadOnlyList<uint> instructions = runners.InstructionsOf(Record);

        // Assert
        Assert.Empty(instructions);
    }

    [Fact]
    public void StaleOf_ObjectInAStateOutsideTheCurrentFile_ReturnsItWithItsStateName()
    {
        // Arrange
        MapTwoObjects(firstStateStart: 0x05000040, secondStateStart: 0x02000040);
        ScriptRunners runners = CreateScriptRunners();

        // Act
        IReadOnlyList<StaleRunner> stale = runners.StaleOf(Record, CurrentFile, CurrentLength);

        // Assert
        Assert.Equal([new StaleRunner(First, "CmnActStand")], stale);
    }

    [Fact]
    public void StaleOf_EveryObjectInTheCurrentFile_ReturnsNothing()
    {
        // Arrange
        MapTwoObjects(firstStateStart: CurrentFile + 0x40, secondStateStart: 0x02000040);
        ScriptRunners runners = CreateScriptRunners();

        // Act
        IReadOnlyList<StaleRunner> stale = runners.StaleOf(Record, CurrentFile, CurrentLength);

        // Assert
        Assert.Empty(stale);
    }

    [Fact]
    public void StaleOf_StateStartRightAfterTheCurrentFile_CountsAsStale()
    {
        // Arrange
        MapTwoObjects(firstStateStart: CurrentFile + CurrentLength, secondStateStart: 0x02000040);
        ScriptRunners runners = CreateScriptRunners();

        // Act
        IReadOnlyList<StaleRunner> stale = runners.StaleOf(Record, CurrentFile, CurrentLength);

        // Assert
        Assert.Equal(First, Assert.Single(stale).Entity);
    }

    private void MapTwoObjects(uint firstStateStart, uint secondStateStart)
    {
        byte[] name = new byte[32];
        "CmnActStand"u8.CopyTo(name);
        _memoryMock.Map(Battle + 0xA4210, 4).UInt32At(Battle + 0xA4210, EntityList)
            .Map(EntityList, GameView.EntityCount * 4).UInt32At(EntityList, First).UInt32At(EntityList + 4, Second)
            .Map(First, 0x2800).UInt32At(First + 0x1400, Record).UInt32At(First + 0x140C, firstStateStart).BytesAt(First + 0x25DC, name)
            .Map(Second, 0x2800).UInt32At(Second + 0x1400, OtherRecord).UInt32At(Second + 0x140C, secondStateStart).BytesAt(Second + 0x25DC, name);
    }

    public ScriptRunners CreateScriptRunners() => new(new GameView(_memoryMock, new GameAddresses(0, Battle, 0, 0)));
}
