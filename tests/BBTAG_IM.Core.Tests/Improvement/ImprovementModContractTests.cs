using BBTAG_IM.Core.Improvement;

namespace BBTAG_IM.Core.Tests.Improvement;

public class ImprovementModContractTests
{
    private const string LoaderFolder = @"C:\Steam\BBTAG\BBTAG_IM-0.1.0";
    private const string ImprovementMod = LoaderFolder + @"\mods\BBTAG-Improvement-Mod";
    private const string GameFolder = @"C:\Steam\BBTAG";

    [Fact]
    public void GameFolder_FolderAnnouncedByTheLoader_ReturnsIt()
    {
        // Act
        string folder = ImprovementModContract.GameFolder(GameFolder, ImprovementMod);

        // Assert
        Assert.Equal(GameFolder, folder);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GameFolder_NothingAnnounced_ReturnsTheFolderHoldingTheMods(string? announced)
    {
        // Act
        string folder = ImprovementModContract.GameFolder(announced, ImprovementMod);

        // Assert
        Assert.Equal(LoaderFolder, folder);
    }
}
