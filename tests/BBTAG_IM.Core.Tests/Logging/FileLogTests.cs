using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.Core.Tests.Logging;

public sealed class FileLogTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "BBTAG_IM.Tests", Guid.NewGuid().ToString("N"));
    private readonly FixedClock _clockMock = new(new DateTimeOffset(2026, 10, 11, 17, 30, 5, 120, TimeSpan.Zero));

    private string LogPath => Path.Combine(_directory, "logs", "BBTAG_IM.log");

    public FileLog CreateFileLog() => new(
        LogPath,
        _clockMock
    );

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Fact]
    public void InfoAndError_FolderDoesNotExist_CreatesItAndAppendsTimestampedLines()
    {
        // Arrange
        FileLog log = CreateFileLog();

        // Act
        log.Info("first");
        log.Error("second");

        // Assert
        Assert.Equal(
            ["[2026-10-11 17:30:05.120] INFO  first", "[2026-10-11 17:30:05.120] ERROR second"],
            File.ReadAllLines(LogPath));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
