using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Tests.Game;

public class RecoveryBudgetTests
{
    [Fact]
    public void TrySpend_FirstErrorOfAMatch_IsAllowed()
    {
        // Arrange
        RecoveryBudget budget = CreateRecoveryBudget();

        // Act
        bool allowed = budget.TrySpend(matchNumber: 1);

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void TrySpend_MoreErrorsThanTheLimitInOneMatch_RefusesTheRest()
    {
        // Arrange
        RecoveryBudget budget = CreateRecoveryBudget();

        for (int error = 0; error < 3; error++)
            budget.TrySpend(matchNumber: 1);

        // Act
        bool allowed = budget.TrySpend(matchNumber: 1);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void TrySpend_NewMatch_StartsCountingAgain()
    {
        // Arrange
        RecoveryBudget budget = CreateRecoveryBudget();

        for (int error = 0; error < 3; error++)
            budget.TrySpend(matchNumber: 1);

        // Act
        bool allowed = budget.TrySpend(matchNumber: 2);

        // Assert
        Assert.True(allowed);
    }

    public RecoveryBudget CreateRecoveryBudget() => new(perMatch: 3);
}
