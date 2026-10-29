using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Scripting.CSharp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.CSharp;

public class CSharpCompilerTests
{
    private const int SlotType = 2;
    private const int ConstantType = 0;

    public static TheoryData<byte[]> Scripts => new()
    {
        new ScriptBytes()
            .State("NmlAtk5A")
            .Command(15, 10).Command(ScriptTestDatabase.Damage, 1200).Command(16)
            .Command(4, SlotType, 51).Command(ScriptTestDatabase.Damage, 1).Command(5)
            .Command(56).Command(ScriptTestDatabase.Damage, 2).Command(57)
            .Command(54, ConstantType, -5).Command(55)
            .Command(4, SlotType, 2).Command(5).Command(56).Command(57)
            .Command(2, "a\"bé", 3)
            .Command(1)
            .Build(),
        new ScriptBytes()
            .Subroutine("MatchInit")
            .Command(14001, "NmlAtk5A", 0x07).Command(ScriptTestDatabase.SharedGatling, "NmlAtk5A").Command(14002)
            .Command(14001, "Other", 0x5F).Command(14002)
            .Command(ScriptTestDatabase.CheckInput, 172)
            .Command(ScriptTestDatabase.CheckInput, 0x1F)
            .Command(41, SlotType, 3, ConstantType, 5)
            .Command(40, 5, SlotType, 3, ConstantType, 2)
            .Command(49, 0, SlotType, 3, ConstantType, 1)
            .Command(47, 14, SlotType, 3, ConstantType, 2, SlotType, 4)
            .Command(9)
            .State("NmlAtk5A").Command(1)
            .State("NmlAtk5A").Command(1)
            .State("event").Command(1)
            .State("Effect.01").Command(1)
            .Build(),
        new ScriptBytes()
            .State("Odd")
            .Command(ScriptTestDatabase.Damage, 1).Command(56).Command(57)
            .Command(1)
            .Build(),
    };

    [Theory]
    [MemberData(nameof(Scripts))]
    public void Compile_DecompiledCSharp_GivesTheSameBytes(byte[] bytes)
    {
        // Arrange
        ScriptDatabase database = CreateDatabase();
        string source = new ScriptDecompiler(database, new CSharpDialect(database)).Decompile(ScriptBinary.Read(bytes, database), "scr_brg");

        // Act
        CompiledScript compiled = new ScriptCompiler(database, ScriptLanguage.CSharp).Compile(source);

        // Assert
        Assert.Equal(bytes, compiled.Bytes);
    }

    [Fact]
    public void Compile_NameOfAndComments_AreUnderstood()
    {
        // Arrange
        string source = """
            using BBScript;

            namespace Mods;

            public partial class scr_brg : Script
            {
                // a comment
                [State]
                public void NmlAtk2B()
                {
                    /* another */
                    SharedGatling(nameof(NmlAtk5B));
                }

                [State]
                public void NmlAtk5B() { }
            }
            """;
        byte[] expected = new ScriptBytes()
            .State("NmlAtk2B").Command(ScriptTestDatabase.SharedGatling, "NmlAtk5B").Command(1)
            .State("NmlAtk5B").Command(1)
            .Build();

        // Act
        CompiledScript compiled = new ScriptCompiler(CreateDatabase(), ScriptLanguage.CSharp).Compile(source);

        // Assert
        Assert.Equal(expected, compiled.Bytes);
    }

    [Theory]
    [InlineData("        Damag(1);", 6)]
    [InlineData("        Damage(Input.NOPE);", 6)]
    [InlineData("        Damage(Foo.Bar);", 6)]
    [InlineData("        SharedGatling(NmlAtk5X);", 6)]
    [InlineData("        Damage(1, () => { });", 6)]
    [InlineData("        Damage(1)", 7)]
    public void FindMistakes_Mistake_IsReportedWithItsLine(string statement, int line)
    {
        // Arrange
        string source = $$"""
            public partial class scr_brg : Script
            {
                [State]
                public void A()
                {
            {{statement}}
                }
            }
            """;

        // Act
        IReadOnlyList<ScriptCompileException> mistakes = new ScriptCompiler(CreateDatabase(), ScriptLanguage.CSharp).FindMistakes(source);

        // Assert
        Assert.Equal(line, Assert.Single(mistakes).Line);
    }

    private static ScriptDatabase CreateDatabase() => ScriptTestDatabase.Create().WithTagNames();
}
