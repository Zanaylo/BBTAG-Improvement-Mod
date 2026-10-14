using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class BridgeScriptTests
{
    private const int GameProcessId = 4242;

    [Fact]
    public void PipeName_GameProcessId_IsUniquePerGame()
    {
        // Act
        string pipeName = BridgeScript.PipeName(GameProcessId);

        // Assert
        Assert.Equal("BBTAG_IM_4242", pipeName);
    }

    [Fact]
    public void For_GameProcessId_AttachesToTheGameAndConnectsToTheLauncherPipe()
    {
        // Act
        string lua = BridgeScript.For(GameProcessId);

        // Assert
        Assert.Contains("openProcess(4242)", lua);
        Assert.Contains("connectToPipe(\"BBTAG_IM_4242\")", lua);
    }

    [Fact]
    public void For_GameProcessId_GivesFormVariablesADefaultBeforeAttaching()
    {
        // Act
        string lua = BridgeScript.For(GameProcessId);

        // Assert
        int fallback = lua.IndexOf("formVariables = setmetatable({}, { __index = function() return 0 end })", StringComparison.Ordinal);
        Assert.InRange(fallback, 0, lua.IndexOf("openProcess(", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(@"""show"" then\s+unhideMainCEwindow\(\)")]
    [InlineData(@"""hide"" then\s+hideAllCEWindows\(\)")]
    public void For_GameProcessId_AnswersTheWindowCommands(string handlerPattern)
    {
        // Act
        string lua = BridgeScript.For(GameProcessId);

        // Assert
        Assert.Matches(handlerPattern, lua);
    }

    [Theory]
    [InlineData(@"""status"" then\s+return bbtagImStatus\(\)")]
    [InlineData(@"""failures"" then\s+return bbtagImFailureReport\(\)")]
    [InlineData(@"""cancel"" then\s+bbtagImCancelWaits = true")]
    public void For_GameProcessId_AnswersTheActivationCommands(string handlerPattern)
    {
        // Act
        string lua = BridgeScript.For(GameProcessId);

        // Assert
        Assert.Matches(handlerPattern, lua);
    }

    [Fact]
    public void For_GameProcessId_HoldsEveryScriptActivationUntilTheBridgeReleasesIt()
    {
        // Act
        string lua = BridgeScript.For(GameProcessId);

        // Assert
        Assert.Matches(@"if record\.Type == vtAutoAssembler then\s+record\.OnActivate = bbtagImGate", lua);
    }

    [Fact]
    public void For_GameProcessId_KeepsTheReasonCheatEngineGivesForAFailedScript()
    {
        // Act
        string lua = BridgeScript.For(GameProcessId);

        // Assert
        Assert.Matches(@"function bbtagImRecordFailure\(failed, reason, reasonText\)\s+bbtagImFailures\[failed\.ID\] = bbtagImReason\(reasonText\)", lua);
        Assert.Contains("record.OnActivationFailure = bbtagImRecordFailure", lua);
    }

    [Fact]
    public void For_GameProcessId_ReplacesTheTableWaitWithOneTheBridgeCanCount()
    {
        // Act
        string lua = BridgeScript.For(GameProcessId);

        // Assert
        Assert.Contains("GeneralTools.WaitForCondition = bbtagImWaitForCondition", lua);
    }
}
