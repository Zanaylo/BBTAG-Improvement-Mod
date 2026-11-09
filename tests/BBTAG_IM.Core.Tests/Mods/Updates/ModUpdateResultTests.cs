using BBTAG_IM.Core.Mods.Updates;

namespace BBTAG_IM.Core.Tests.Mods.Updates;

public class ModUpdateResultTests
{
    [Fact]
    public void Message_UpToDate_NamesTheVersion()
    {
        // Arrange
        ModUpdateResult result = new("OverHaul", ModUpdateOutcome.UpToDate, "f965cea", 0, 0);

        // Act
        string message = result.Message;

        // Assert
        Assert.Equal("OverHaul is up to date (f965cea).", message);
    }

    [Fact]
    public void Message_Updated_CountsTheFiles()
    {
        // Arrange
        ModUpdateResult result = new("OverHaul", ModUpdateOutcome.Updated, "f965cea", 41, 3);

        // Act
        string message = result.Message;

        // Assert
        Assert.Equal("OverHaul was updated to f965cea: 41 files downloaded, 3 removed.", message);
    }

    [Fact]
    public void Message_UpdatedWithOneFile_UsesTheSingular()
    {
        // Arrange
        ModUpdateResult result = new("OverHaul", ModUpdateOutcome.Updated, "v2", 1, 1);

        // Act
        string message = result.Message;

        // Assert
        Assert.Equal("OverHaul was updated to v2: 1 file downloaded, 1 removed.", message);
    }
}
