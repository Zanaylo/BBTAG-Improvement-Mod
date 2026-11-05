using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Tests.Game;

public class RecoveryReportsTests
{
    private const uint Address = 0x00444A47;
    private const uint OtherAddress = 0x00444A64;

    [Fact]
    public void IsFirst_FirstErrorAtAnAddress_ReturnsTrue()
    {
        // Arrange
        RecoveryReports reports = CreateRecoveryReports();

        // Act
        bool first = reports.IsFirst(matchNumber: 1, Address);

        // Assert
        Assert.True(first);
    }

    [Fact]
    public void IsFirst_SameAddressInTheSameMatch_ReturnsFalse()
    {
        // Arrange
        RecoveryReports reports = CreateRecoveryReports();
        reports.IsFirst(matchNumber: 1, Address);

        // Act
        bool first = reports.IsFirst(matchNumber: 1, Address);

        // Assert
        Assert.False(first);
    }

    [Fact]
    public void IsFirst_OtherAddressInTheSameMatch_ReturnsTrue()
    {
        // Arrange
        RecoveryReports reports = CreateRecoveryReports();
        reports.IsFirst(matchNumber: 1, Address);

        // Act
        bool first = reports.IsFirst(matchNumber: 1, OtherAddress);

        // Assert
        Assert.True(first);
    }

    [Fact]
    public void IsFirst_SameAddressInANewMatch_ReturnsTrue()
    {
        // Arrange
        RecoveryReports reports = CreateRecoveryReports();
        reports.IsFirst(matchNumber: 1, Address);

        // Act
        bool first = reports.IsFirst(matchNumber: 2, Address);

        // Assert
        Assert.True(first);
    }

    public RecoveryReports CreateRecoveryReports() => new();
}
