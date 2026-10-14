using BBTAG_IM.Core.Debugging;

namespace BBTAG_IM.Core.Tests.Debugging;

public class VisualStudioMonikerTests
{
    [Theory]
    [InlineData("!VisualStudio.DTE.18.0:17980", 17980)]
    [InlineData("!VisualStudio.DTE.17.0:420", 420)]
    public void ProcessIdOf_VisualStudioEntry_ReturnsItsProcessId(string displayName, int expected)
    {
        // Act
        int? processId = VisualStudioMoniker.ProcessIdOf(displayName);

        // Assert
        Assert.Equal(expected, processId);
    }

    [Theory]
    [InlineData("!{0002DF01-0000-0000-C000-000000000046}")]
    [InlineData(@"C:\Users\Player\Documents\Book.xlsx")]
    [InlineData("!VisualStudio.DTE.18.0:")]
    [InlineData("!VisualStudio.DTE.18.0:abc")]
    [InlineData("!VisualStudio.Solution.18.0:17980")]
    public void ProcessIdOf_OtherEntry_ReturnsNull(string displayName)
    {
        // Act
        int? processId = VisualStudioMoniker.ProcessIdOf(displayName);

        // Assert
        Assert.Null(processId);
    }
}
