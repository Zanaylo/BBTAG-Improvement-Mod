using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.IO;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Game;

public class BossesTests
{
    private const string OverhaulData = @"D:\BBTAG\mods\BBTAG-OverHaul\data";
    private const string GameData = @"D:\BBTAG\data";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Installed_OverhaulWithTakeSprites_ListsTakeMikazuchiThenSystemXX()
    {
        // Arrange
        _fileSystemMock.FileExists(OverhaulData + @"\char2x\char_bta_vri.pac").Returns(true);

        // Act
        IReadOnlyList<Boss> installed = Bosses.Installed(_fileSystemMock, [OverhaulData, GameData]);

        // Assert
        Assert.Equal([Bosses.TakeMikazuchi, Bosses.SystemXX], installed);
    }

    [Fact]
    public void Installed_GameWithoutTakeSprites_ListsOnlySystemXX()
    {
        // Act
        IReadOnlyList<Boss> installed = Bosses.Installed(_fileSystemMock, [GameData]);

        // Assert
        Assert.Equal([Bosses.SystemXX], installed);
    }

    [Theory]
    [InlineData("bta", "takemikazuchi")]
    [InlineData("bcr", "boss")]
    public void All_KnownBosses_FightOnTheirOwnStage(string code, string stage)
    {
        // Act
        Boss boss = Bosses.All.Single(candidate => candidate.Code == code);

        // Assert
        Assert.Equal(stage, boss.Stage);
    }
}
