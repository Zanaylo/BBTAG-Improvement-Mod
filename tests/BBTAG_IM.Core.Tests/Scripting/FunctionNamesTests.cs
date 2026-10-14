using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public class FunctionNamesTests
{
    [Fact]
    public void ToIdentifier_NameStartingWithDigit_AddsThePrefix()
    {
        // Arrange
        string name = "032rock";

        // Act
        string? identifier = FunctionNames.ToIdentifier(name);

        // Assert
        Assert.Equal("__032rock", identifier);
    }

    [Fact]
    public void ToIdentifier_NameWithDashAndSpace_ReplacesThem()
    {
        // Arrange
        string name = "Act-2 B";

        // Act
        string? identifier = FunctionNames.ToIdentifier(name);

        // Assert
        Assert.Equal("Act__ds__2__sp__B", identifier);
    }

    [Fact]
    public void ToIdentifier_NameWithDot_ReturnsNull()
    {
        // Arrange
        string name = "Effect.01";

        // Act
        string? identifier = FunctionNames.ToIdentifier(name);

        // Assert
        Assert.Null(identifier);
    }

    [Fact]
    public void FromIdentifier_MangledName_ReturnsTheGameName()
    {
        // Arrange
        string identifier = "__5A__qu__";

        // Act
        string name = FunctionNames.FromIdentifier(identifier);

        // Assert
        Assert.Equal("5A?", name);
    }

    [Fact]
    public void IsIdentifier_Keyword_ReturnsFalse()
    {
        // Arrange
        string text = "pass";

        // Act
        bool identifier = FunctionNames.IsIdentifier(text);

        // Assert
        Assert.False(identifier);
    }
}
