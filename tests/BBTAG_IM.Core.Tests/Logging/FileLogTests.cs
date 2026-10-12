using BBTAG_IM.Core.Logging;

namespace BBTAG_IM.Core.Tests.Logging;

public sealed class FileLogTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "BBTAG_IM.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Fact]
    public void Info_AppendsTimestampedLinesAndCreatesFolder()
    {
        var path = Path.Combine(_directory, "logs", "BBTAG_IM.log");
        var log = new FileLog(path, new FixedClock(new DateTimeOffset(2026, 10, 11, 17, 30, 5, 120, TimeSpan.Zero)));

        log.Info("first");
        log.Error("second");

        Assert.Equal(
            ["[2026-10-11 17:30:05.120] INFO  first", "[2026-10-11 17:30:05.120] ERROR second"],
            File.ReadAllLines(path));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
