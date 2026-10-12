using BBTAG_IM.Core.Launch;

namespace BBTAG_IM.Core.Tests.Launch;

public class PayloadStatusTextTests
{
    [Fact]
    public void Describe_GivesEveryStatusItsOwnText()
    {
        var texts = Enum.GetValues<PayloadStatus>().Select(PayloadStatusText.Describe).ToArray();

        Assert.All(texts, text => Assert.False(string.IsNullOrWhiteSpace(text)));
        Assert.Equal(texts.Length, texts.Distinct().Count());
    }

    [Fact]
    public void Describe_ExplainsIncompatibleExecutable()
    {
        Assert.Contains("BBTAG.exe", PayloadStatusText.Describe(PayloadStatus.IncompatibleExecutable));
    }

    [Fact]
    public void Describe_HandlesUnknownStatus()
    {
        Assert.Contains("1234", PayloadStatusText.Describe((PayloadStatus)1234));
    }
}
