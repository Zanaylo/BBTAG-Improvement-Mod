using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class ScriptActivationTests
{
    private static readonly TimeSpan FirstIdle = TimeSpan.FromSeconds(1);

    public ScriptActivation CreateScriptActivation() => new();

    [Fact]
    public void Observe_CheatEngineBusy_IsTurningOn()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();

        // Act
        ActivationState state = activation.Observe(true, TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Fact]
    public void Observe_IdleForLessThanTheQuietPeriod_IsTurningOn()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(false, FirstIdle);

        // Act
        ActivationState state = activation.Observe(false, FirstIdle + ScriptActivation.QuietPeriod / 2);

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Fact]
    public void Observe_IdleForTheWholeQuietPeriod_IsOn()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(false, FirstIdle);

        // Act
        ActivationState state = activation.Observe(false, FirstIdle + ScriptActivation.QuietPeriod);

        // Assert
        Assert.Equal(ActivationState.On, state);
    }

    [Fact]
    public void Observe_BusyAgainDuringTheQuietPeriod_RestartsTheQuietPeriod()
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();
        activation.Observe(false, FirstIdle);
        activation.Observe(true, TimeSpan.FromSeconds(2));

        // Act
        ActivationState state = activation.Observe(false, FirstIdle + ScriptActivation.QuietPeriod);

        // Assert
        Assert.Equal(ActivationState.TurningOn, state);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Observe_LimitReachedBeforeTheQuietPeriodEnded_TimesOut(bool busy)
    {
        // Arrange
        ScriptActivation activation = CreateScriptActivation();

        // Act
        ActivationState state = activation.Observe(busy, ScriptActivation.Limit);

        // Assert
        Assert.Equal(ActivationState.TimedOut, state);
    }
}
