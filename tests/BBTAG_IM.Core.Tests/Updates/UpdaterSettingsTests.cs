using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public class UpdaterSettingsTests
{
    [Fact]
    public void Skips_VersionTheUserSkipped_IsTrue()
    {
        // Arrange
        UpdaterSettings settings = new();
        settings.Skip(new Version(0, 3, 0));

        // Act
        bool skipped = settings.Skips(new Version(0, 3, 0));

        // Assert
        Assert.True(skipped);
    }

    [Fact]
    public void Skips_NewerVersionThanTheSkippedOne_IsFalse()
    {
        // Arrange
        UpdaterSettings settings = new();
        settings.Skip(new Version(0, 3, 0));

        // Act
        bool skipped = settings.Skips(new Version(0, 3, 1));

        // Assert
        Assert.False(skipped);
    }

    [Fact]
    public void Skips_NothingSkipped_IsFalse()
    {
        // Act
        bool skipped = new UpdaterSettings().Skips(new Version(0, 3, 0));

        // Assert
        Assert.False(skipped);
    }
}
