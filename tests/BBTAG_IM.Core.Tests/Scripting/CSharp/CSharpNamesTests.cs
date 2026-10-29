using BBTAG_IM.Core.Scripting.CSharp;

namespace BBTAG_IM.Core.Tests.Scripting.CSharp;

public class CSharpNamesTests
{
    [Theory]
    [InlineData("NmlAtk5A", "NmlAtk5A")]
    [InlineData("5A", "__5A")]
    [InlineData("event", "@event")]
    [InlineData("pass", "pass")]
    [InlineData("Dash-Cancel", "Dash__ds__Cancel")]
    public void Identifier_FunctionName_IsAValidCSharpName(string name, string expected)
    {
        // Act
        string? identifier = CSharpNames.Identifier(name);

        // Assert
        Assert.Equal(expected, identifier);
    }

    [Fact]
    public void Identifier_NameThatCannotBeAnIdentifier_ReturnsNull()
    {
        // Act
        string? identifier = CSharpNames.Identifier("Effect.01");

        // Assert
        Assert.Null(identifier);
    }

    [Theory]
    [InlineData("@event", "event")]
    [InlineData("__5A", "5A")]
    [InlineData("NmlAtk5A", "NmlAtk5A")]
    public void FunctionName_Identifier_GivesTheGameName(string identifier, string expected)
    {
        // Act
        string name = CSharpNames.FunctionName(identifier);

        // Assert
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData("INPUT_5A", "INPUT_", "_5A")]
    [InlineData("INPUT_J5A", "INPUT_", "J5A")]
    [InlineData("MOVE_0x3083", "MOVE_", "_0x3083")]
    [InlineData("upon_Hit", "upon_", "Hit")]
    [InlineData("SLOT_3", "SLOT_", "_3")]
    [InlineData("AIR_VERTICAL", "", "AIR_VERTICAL")]
    public void Member_CanonicalName_IsTheEnumMember(string canonical, string prefix, string expected)
    {
        // Act
        string? member = CSharpNames.Member(canonical, prefix);

        // Assert
        Assert.Equal(expected, member);
    }

    [Theory]
    [InlineData("_5A", "INPUT_", "INPUT_5A")]
    [InlineData("_0x3083", "MOVE_", "MOVE_0x3083")]
    [InlineData("@class", "upon_", "upon_class")]
    [InlineData("IsInOverdrive", "SLOT_", "SLOT_IsInOverdrive")]
    public void Canonical_Member_IsTheScriptName(string member, string prefix, string expected)
    {
        // Act
        string canonical = CSharpNames.Canonical(member, prefix);

        // Assert
        Assert.Equal(expected, canonical);
    }

    [Fact]
    public void Member_NameWithoutTheprefix_ReturnsNull()
    {
        // Act
        string? member = CSharpNames.Member("SLOT_3", "INPUT_");

        // Assert
        Assert.Null(member);
    }
}
