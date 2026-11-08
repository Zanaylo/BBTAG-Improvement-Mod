using BBTAG_IM.Core.Porting;

namespace BBTAG_IM.Core.Tests.Porting;

public class PatternAliasesTests
{
    [Theory]
    [InlineData(138, 3)]
    [InlineData(192, 2)]
    [InlineData(222, 131)]
    public void Parse_CrushAnimationOfTheTemplate_UsesANativeAttack(int template, int native)
    {
        // Act
        IReadOnlyDictionary<int, int> aliases = PatternAliases.Parse(string.Empty);

        // Assert
        Assert.Equal(native, aliases[template]);
    }

    [Fact]
    public void Parse_VectorTable_ReadsTheOldNumberFromTheNote()
    {
        // Arrange
        string table = "const def_VLP_Atama1\t\t\t= 606;\t// 300 note\nconst def_VLP_Hara1 = 607; // 301 note\n";

        // Act
        IReadOnlyDictionary<int, int> aliases = PatternAliases.Parse(table);

        // Assert
        Assert.Equal(606, aliases[300]);
        Assert.Equal(607, aliases[301]);
    }

    [Fact]
    public void Parse_LineWithoutANote_IsIgnored()
    {
        // Act
        IReadOnlyDictionary<int, int> aliases = PatternAliases.Parse("const def_VLP_Uki = 620;\n");

        // Assert
        Assert.DoesNotContain(620, aliases.Values);
    }

    [Fact]
    public void Parse_AnyTable_MapsTheTemplateDashToTheNativeDash()
    {
        // Act
        IReadOnlyDictionary<int, int> aliases = PatternAliases.Parse("");

        // Assert
        Assert.Equal(43, aliases[45]);
    }
}
