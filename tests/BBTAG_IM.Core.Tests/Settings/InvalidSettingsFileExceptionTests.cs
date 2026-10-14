using BBTAG_IM.Core.Settings;

namespace BBTAG_IM.Core.Tests.Settings;

public class InvalidSettingsFileExceptionTests
{
    private const string SettingsPath = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\ImprovementMod.json";

    [Fact]
    public void Message_LineKnown_NamesTheFileAndTheLineCountedFromOne()
    {
        // Arrange
        InvalidSettingsFileException exception = new(SettingsPath, 4);

        // Act
        string message = exception.Message;

        // Assert
        Assert.Equal("ImprovementMod.json has a mistake on line 5, so it was not read. Fix it or delete it. It is not saved over until then.", message);
    }

    [Fact]
    public void Message_LineUnknown_NamesOnlyTheFile()
    {
        // Arrange
        InvalidSettingsFileException exception = new(SettingsPath, null);

        // Act
        string message = exception.Message;

        // Assert
        Assert.Equal("ImprovementMod.json has a mistake, so it was not read. Fix it or delete it. It is not saved over until then.", message);
    }
}
