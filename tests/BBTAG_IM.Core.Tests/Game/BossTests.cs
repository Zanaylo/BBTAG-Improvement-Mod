using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Game;

public class BossTests
{
    private const string OverhaulData = @"D:\BBTAG\mods\BBTAG-OverHaul\data";
    private const string GameData = @"D:\BBTAG\data";
    private const uint RosterGetter = 0x00401100;
    private const uint RosterTable = 0x00C37768;
    private const uint RosterRecordLength = 0xB8;
    private const uint RosterLastIndexOffset = 0x2E00;
    private const uint StageGetter = 0x00401200;
    private const uint Stages = 0x00E5DAD0;
    private const uint StageRecordLength = 0xC4;
    private const int Take = 53;
    private const int TakeStage = 119;

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly FakeGameMemory _memoryMock = new();

    [Fact]
    public void IsInstalled_BossWithSpritesInAModDataFolder_IsTrue()
    {
        // Arrange
        _fileSystemMock.FileExists(OverhaulData + @"\char2x\char_bta_vri.pac").Returns(true);

        // Act
        bool installed = Bosses.TakeMikazuchi.IsInstalled(_fileSystemMock, [OverhaulData, GameData]);

        // Assert
        Assert.True(installed);
    }

    [Fact]
    public void IsInstalled_NoDataFolderHasTheBossSprites_IsFalse()
    {
        // Act
        bool installed = Bosses.TakeMikazuchi.IsInstalled(_fileSystemMock, [OverhaulData, GameData]);

        // Assert
        Assert.False(installed);
    }

    [Fact]
    public void IsInstalled_BossTheGameShips_IsTrueWithoutAnyFile()
    {
        // Act
        bool installed = Bosses.SystemXX.IsInstalled(_fileSystemMock, [GameData]);

        // Assert
        Assert.True(installed);
    }

    [Fact]
    public void FindIn_BossAndItsStageInTheTables_ReturnsTheFightWithTheirIndexes()
    {
        // Arrange
        RosterWith(Take, "bta");
        StagesWith(TakeStage, "takemikazuchi");

        // Act
        BossFight fight = Bosses.TakeMikazuchi.FindIn(CreateRoster(), CreateStageTable());

        // Assert
        Assert.Equal(new BossFight(Bosses.TakeMikazuchi, Take, TakeStage), fight);
    }

    [Fact]
    public void FindIn_BossMissingFromTheRoster_ThrowsBossNotReadyException()
    {
        // Arrange
        RosterWith(Take, "bcr");
        StagesWith(TakeStage, "takemikazuchi");

        // Act
        Exception? exception = Record.Exception(() => Bosses.TakeMikazuchi.FindIn(CreateRoster(), CreateStageTable()));

        // Assert
        BossNotReadyException notReady = Assert.IsType<BossNotReadyException>(exception);
        Assert.Equal("Take-Mikazuchi is not in the character list yet. Wait until the table scripts are on.", notReady.Message);
    }

    [Fact]
    public void FindIn_BossStageMissingFromTheStageList_ThrowsBossNotReadyException()
    {
        // Arrange
        RosterWith(Take, "bta");
        StagesWith(TakeStage, "p4corridor_2");

        // Act
        Exception? exception = Record.Exception(() => Bosses.TakeMikazuchi.FindIn(CreateRoster(), CreateStageTable()));

        // Assert
        BossNotReadyException notReady = Assert.IsType<BossNotReadyException>(exception);
        Assert.Equal("The takemikazuchi stage of Take-Mikazuchi is not in the stage list yet. Wait until the table scripts are on.", notReady.Message);
    }

    private CharacterRoster CreateRoster() => new(_memoryMock, RosterGetter);

    private StageTable CreateStageTable() => new(_memoryMock, StageGetter);

    private void RosterWith(int index, string code)
    {
        _memoryMock.Map(RosterGetter, 8).BytesAt(RosterGetter, [0xB8, 0, 0, 0, 0, GameCode.Return]).UInt32At(RosterGetter + 1, RosterTable);
        _memoryMock.Map(RosterTable, (int)RosterLastIndexOffset + sizeof(int)).Int32At(RosterTable + RosterLastIndexOffset, index);
        _memoryMock.BytesAt(RosterTable + (uint)index * RosterRecordLength, Encoding.Latin1.GetBytes(code));
    }

    private void StagesWith(int index, string name)
    {
        _memoryMock.Map(StageGetter, 8).BytesAt(StageGetter, [0xB8, 0, 0, 0, 0, GameCode.Return]).UInt32At(StageGetter + 1, Stages);
        _memoryMock.Map(Stages, (index + 1) * (int)StageRecordLength);
        _memoryMock.BytesAt(Stages + (uint)index * StageRecordLength, Encoding.Latin1.GetBytes(name));
    }
}
