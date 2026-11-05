using BBTAG_IM.Core.Display;

namespace BBTAG_IM.Core.Tests.Display;

public class SavedResolutionMapTests
{
    private static readonly Resolution[] Overhaul =
    [
        new(640, 360), new(854, 480), new(960, 540), new(1024, 576),
        new(1280, 720), new(1366, 768), new(1600, 900), new(1920, 1080),
        new(2048, 1152), new(2560, 1440), new(3200, 1800), new(3840, 2160),
    ];

    public SavedResolutionMap CreateMap() => new(WindowResolutionCode.Resolutions, Overhaul);

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 6)]
    [InlineData(2, 7)]
    [InlineData(5, 2)]
    [InlineData(11, 11)]
    public void ToRunning_SavedPosition_ReturnsThePositionOfTheSameSize(int saved, int expected)
    {
        // Arrange
        SavedResolutionMap map = CreateMap();

        // Act
        int running = map.ToRunning(saved);

        // Assert
        Assert.Equal(expected, running);
    }

    [Theory]
    [InlineData(4, 0)]
    [InlineData(6, 1)]
    [InlineData(7, 2)]
    [InlineData(2, 5)]
    [InlineData(11, 11)]
    public void ToSaved_RunningPosition_ReturnsThePositionOfTheSameSize(int running, int expected)
    {
        // Arrange
        SavedResolutionMap map = CreateMap();

        // Act
        int saved = map.ToSaved(running);

        // Assert
        Assert.Equal(expected, saved);
    }

    [Fact]
    public void ToRunning_ThenToSaved_EveryPosition_ReturnsTheSamePosition()
    {
        // Arrange
        SavedResolutionMap map = CreateMap();
        int[] positions = [.. Enumerable.Range(0, WindowResolutionCode.ExtendedCount)];

        // Act
        int[] saved = [.. positions.Select(position => map.ToSaved(map.ToRunning(position)))];

        // Assert
        Assert.Equal(positions, saved);
    }

    [Fact]
    public void ToRunning_SizeMissingFromTheRunningList_KeepsThePosition()
    {
        // Arrange
        SavedResolutionMap map = new(WindowResolutionCode.Resolutions, [new(640, 360), new(800, 600), new(1024, 768)]);

        // Act
        int running = map.ToRunning(2);

        // Assert
        Assert.Equal(2, running);
    }

    [Fact]
    public void ToSaved_SizeMissingFromTheNativeList_KeepsThePosition()
    {
        // Arrange
        SavedResolutionMap map = new(WindowResolutionCode.Resolutions, [new(800, 600)]);

        // Act
        int saved = map.ToSaved(0);

        // Assert
        Assert.Equal(0, saved);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(-1)]
    public void ToRunning_PositionOutsideTheList_KeepsThePosition(int saved)
    {
        // Arrange
        SavedResolutionMap map = CreateMap();

        // Act
        int running = map.ToRunning(saved);

        // Assert
        Assert.Equal(saved, running);
    }

    [Fact]
    public void ToSavedText_RunningPositionAsText_ReturnsTheSavedPositionAsText()
    {
        // Arrange
        SavedResolutionMap map = CreateMap();

        // Act
        string saved = map.ToSavedText("7");

        // Assert
        Assert.Equal("2", saved);
    }

    [Fact]
    public void ToSavedText_TextThatIsNotANumber_KeepsTheText()
    {
        // Arrange
        SavedResolutionMap map = CreateMap();

        // Act
        string saved = map.ToSavedText("wide");

        // Assert
        Assert.Equal("wide", saved);
    }

    [Theory]
    [InlineData("iDisplayResolution", true)]
    [InlineData("IDISPLAYRESOLUTION", true)]
    [InlineData("iDisplayMode", false)]
    [InlineData(null, false)]
    public void IsResolutionSetting_SettingName_TellsWhetherItIsTheWindowResolution(string? name, bool expected)
    {
        // Act
        bool isResolution = SavedResolutionMap.IsResolutionSetting(name);

        // Assert
        Assert.Equal(expected, isResolution);
    }
}
