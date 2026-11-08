using BBTAG_IM.Core.Scripting.Live;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class ScriptMapHashTests
{
    [Fact]
    public void Of_Name_AddsTheFirstMiddleAndLastLetters()
    {
        // Arrange
        string name = "NmlAtkTaunt";

        // Act
        uint hash = ScriptMapHash.Of(name);

        // Assert
        Assert.Equal((uint)('N' + 'k' + 't'), hash);
    }

    [Fact]
    public void Of_OneLetter_CountsItThreeTimes()
    {
        // Arrange
        string name = "A";

        // Act
        uint hash = ScriptMapHash.Of(name);

        // Assert
        Assert.Equal((uint)('A' * 3), hash);
    }

    [Fact]
    public void Of_EmptyName_ReturnsZero()
    {
        // Arrange
        string name = string.Empty;

        // Act
        uint hash = ScriptMapHash.Of(name);

        // Assert
        Assert.Equal(0u, hash);
    }

    [Fact]
    public void Of_LetterAbove127_CountsItAsNegativeLikeTheGame()
    {
        // Arrange
        string name = "é";

        // Act
        uint hash = ScriptMapHash.Of(name);

        // Assert
        Assert.Equal(unchecked((uint)(-23 * 3)), hash);
    }
}
