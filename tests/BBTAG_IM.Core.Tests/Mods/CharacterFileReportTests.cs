using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Tests.Mods;

public class CharacterFileReportTests
{
    [Fact]
    public void Message_OneMissingFile_NamesIt()
    {
        // Arrange
        CharacterFileReport report = new("mko", [@"data\eff3d\eff3d_mko.pac"], 48);

        // Act
        string message = report.Message;

        // Assert
        Assert.Equal(
            @"mko lacks 1 file that every original character has. A match with it may crash. Missing: data\eff3d\eff3d_mko.pac.",
            message);
    }

    [Fact]
    public void Message_TwoMissingFiles_NamesBoth()
    {
        // Arrange
        CharacterFileReport report = new("mko", [@"data\eff3d\eff3d_mko.pac", @"data\sound\se\semko.pac"], 48);

        // Act
        string message = report.Message;

        // Assert
        Assert.Equal(
            @"mko lacks 2 files that every original character has. A match with it may crash. Missing: data\eff3d\eff3d_mko.pac, data\sound\se\semko.pac.",
            message);
    }

    [Fact]
    public void Message_ManyMissingFiles_NamesTheFirstEightAndCountsTheRest()
    {
        // Arrange
        string[] missing = Enumerable.Range(1, 10).Select(number => $"f{number}").ToArray();
        CharacterFileReport report = new("mko", missing, 48);

        // Act
        string message = report.Message;

        // Assert
        Assert.Equal(
            "mko lacks 10 files that every original character has. A match with it may crash. Missing: f1, f2, f3, f4, f5, f6, f7, f8 and 2 more.",
            message);
    }

    [Fact]
    public void HasNoFiles_EveryFileMissing_ReturnsTrue()
    {
        // Arrange
        CharacterFileReport report = new("dak", ["a", "b"], 2);

        // Act
        bool hasNoFiles = report.HasNoFiles;

        // Assert
        Assert.True(hasNoFiles);
    }

    [Fact]
    public void HasNoFiles_SomeFilesPresent_ReturnsFalse()
    {
        // Arrange
        CharacterFileReport report = new("mko", ["a"], 2);

        // Act
        bool hasNoFiles = report.HasNoFiles;

        // Assert
        Assert.False(hasNoFiles);
    }
}
