using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Tests.Updates;

public class UpdaterCommandLineTests
{
    [Fact]
    public void Parse_NoArguments_IsAManualRun()
    {
        // Act
        UpdaterCommandLine commandLine = UpdaterCommandLine.Parse([]);

        // Assert
        Assert.Null(commandLine.AfterProcess);
    }

    [Fact]
    public void Parse_AfterArguments_ReadsTheProcess()
    {
        // Act
        UpdaterCommandLine commandLine = UpdaterCommandLine.Parse(UpdaterCommandLine.After(4242));

        // Assert
        Assert.Equal(4242, commandLine.AfterProcess);
    }

    [Theory]
    [InlineData("--after")]
    [InlineData("--after", "abc")]
    [InlineData("--other", "12")]
    public void Parse_UnknownArguments_IsAManualRun(params string[] arguments)
    {
        // Act
        UpdaterCommandLine commandLine = UpdaterCommandLine.Parse(arguments);

        // Assert
        Assert.Null(commandLine.AfterProcess);
    }
}
