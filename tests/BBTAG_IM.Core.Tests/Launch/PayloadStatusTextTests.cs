using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Core.Tests.Launch;

public class PayloadStatusTextTests
{
    [Fact]
    public void Describe_EveryKnownStatus_HasItsOwnText()
    {
        // Arrange
        PayloadStatus[] statuses = Enum.GetValues<PayloadStatus>();

        // Act
        string[] texts = statuses.Select(PayloadStatusText.Describe).ToArray();

        // Assert
        Assert.Equal(statuses.Length, texts.Distinct().Count());
        Assert.DoesNotContain(texts, text => text.Contains("unknown status", StringComparison.Ordinal));
    }

    [Fact]
    public void Describe_UnknownStatus_ShowsItsNumber()
    {
        // Act
        string text = PayloadStatusText.Describe((PayloadStatus)1234);

        // Assert
        Assert.Equal("The mod stopped with an unknown status (1234).", text);
    }
}
