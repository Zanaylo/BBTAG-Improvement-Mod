using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class ScriptActivationTests
{
    private static readonly TimeSpan FirstIdle = TimeSpan.FromSeconds(1);
    private static readonly BridgeStatus Working = new(1, 1, 0);
    private static readonly BridgeStatus Idle = new(0, 0, 0);
    private static readonly BridgeStatus OnlyWaiting = new(0, 2, 2);

    public ScriptActivation CreateScriptActivation() => new();

    [Fact]
    public void Observe_ScriptsWorking_IsTurningOn()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();

        // Act
        ActivationState state = activation.Observe(Working, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Fact]
    public void Observe_IdleForLessThanTheQuietPeriod_IsTurningOn()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(Idle, FirstIdle);

        // Act
        ActivationState state = activation.Observe(Idle, FirstIdle + ScriptActivation.QuietPeriod / 2);

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Fact]
    public void Observe_IdleForTheWholeQuietPeriod_IsOn()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(Idle, FirstIdle);

        // Act
        ActivationState state = activation.Observe(Idle, FirstIdle + ScriptActivation.QuietPeriod);

        // Assert
        Assert.Equal(ActivationState.On, state);
    }

    [Fact]
    public void Observe_WorkingAgainDuringTheQuietPeriod_RestartsTheQuietPeriod()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(Idle, FirstIdle);
        activation.Observe(Working, TimeSpan.FromSeconds(2));

        // Act
        ActivationState state = activation.Observe(Idle, FirstIdle + ScriptActivation.QuietPeriod);

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Fact]
    public void Observe_OnlyWaitingScriptsForTheWholeQuietPeriod_IsStuck()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(OnlyWaiting, FirstIdle);

        // Act
        ActivationState state = activation.Observe(OnlyWaiting, FirstIdle + ScriptActivation.QuietPeriod);

        // Assert
        Assert.Equal(ActivationState.Stuck, state);
    }

    [Fact]
    public void Observe_WaitingEndsDuringTheQuietPeriod_RestartsTheQuietPeriod()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(OnlyWaiting, FirstIdle);
        activation.Observe(Idle, TimeSpan.FromSeconds(2));

        // Act
        ActivationState state = activation.Observe(Idle, FirstIdle + ScriptActivation.QuietPeriod);

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Fact]
    public void Reset_AfterTheQuietPeriodStarted_RestartsTheQuietPeriod()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(Idle, FirstIdle);
        activation.Reset();

        // Act
        ActivationState state = activation.Observe(Idle, FirstIdle + ScriptActivation.QuietPeriod);

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 2, 2)]
    public void Observe_LimitReachedBeforeTheQuietPeriodEnded_TimesOut(int pending, int running, int waiting)
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();

        // Act
        ActivationState state = activation.Observe(new BridgeStatus(pending, running, waiting), ScriptActivation.Limit);

        // Assert
        Assert.Equal(ActivationState.TimedOut, state);
    }
}
