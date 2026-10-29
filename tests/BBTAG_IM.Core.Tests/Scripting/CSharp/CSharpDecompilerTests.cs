using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.CSharp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.CSharp;

public class CSharpDecompilerTests
{
    private const int SlotType = 2;
    private const int ConstantType = 0;

    [Fact]
    public void Decompile_StateWithUponIfAndElse_WritesACSharpClass()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("NmlAtk5A")
            .Command(15, 10).Command(ScriptTestDatabase.Damage, 1200).Command(16)
            .Command(4, SlotType, 51).Command(ScriptTestDatabase.Damage, 1).Command(5)
            .Command(56).Command(ScriptTestDatabase.Damage, 2).Command(57)
            .Command(54, ConstantType, 5).Command(5 + 50)
            .Command(1)
            .Build();
        string expected = """
            using BBScript;

            public partial class scr_brg : Script
            {
                [State]
                public void NmlAtk5A()
                {
                    upon(Upon.Hit, () =>
                    {
                        Damage(1200);
                    });
                    if (Slot.IsInOverdrive)
                    {
                        Damage(1);
                    }
                    else
                    {
                        Damage(2);
                    }
                    if (!(Value)(5))
                    {
                    }
                }
            }

            """;

        // Act
        string text = CreateDecompiler().Decompile(Read(bytes), "scr_brg");

        // Assert
        Assert.Equal(expected.ReplaceLineEndings("\n"), text);
    }

    [Fact]
    public void Decompile_MoveRegistration_UsesTheStateAndTheTypedInput()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .Subroutine("MatchInit")
            .Command(14001, "NmlAtk5A", 0x07).Command(ScriptTestDatabase.SharedGatling, "NmlAtk5A").Command(14002)
            .Command(14001, "Other", 0x5F).Command(14002)
            .Command(9)
            .State("NmlAtk5A").Command(1)
            .Build();

        // Act
        string text = CreateDecompiler().Decompile(Read(bytes), "scr_brg");

        // Assert
        Assert.Contains("        Move_Register(NmlAtk5A, NormalInput._5A, () =>\n        {\n            SharedGatling(NmlAtk5A);\n        });\n", text);
        Assert.Contains("        Move_Register(\"Other\", (NormalInput)0x5f, () =>\n", text);
    }

    [Fact]
    public void Decompile_InputsAndAssignments_UseTypesAndCSharpOperators()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .Subroutine("Check")
            .Command(ScriptTestDatabase.CheckInput, 172)
            .Command(ScriptTestDatabase.CheckInput, 0x1F)
            .Command(41, SlotType, 3, ConstantType, 5)
            .Command(40, 5, SlotType, 3, ConstantType, 2)
            .Command(9)
            .Build();

        // Act
        string text = CreateDecompiler().Decompile(Read(bytes), "scr_brg");

        // Assert
        Assert.Contains("        Slot._0 = CheckInput(Input._236);\n", text);
        Assert.Contains("        Slot._0 = CheckInput((Input)0x1f);\n", text);
        Assert.Contains("        Slot._3 = 5;\n", text);
        Assert.Contains("        Slot._0 = Slot._3 && 2;\n", text);
    }

    [Fact]
    public void Decompile_DuplicateAndOddNames_StayValidCSharp()
    {
        // Arrange
        byte[] bytes = new ScriptBytes()
            .State("Twice").Command(1)
            .State("Twice").Command(1)
            .State("Effect.01").Command(1)
            .State("event").Command(1)
            .Build();

        // Act
        string text = CreateDecompiler().Decompile(Read(bytes), "scr_brg");

        // Assert
        Assert.Contains("    [State]\n    public void Twice()\n", text);
        Assert.Contains("    [State(\"Twice\")]\n    public void Twice_2()\n", text);
        Assert.Contains("    [State(\"Effect.01\")]\n    public void unnamed_0()\n", text);
        Assert.Contains("    [State]\n    public void @event()\n", text);
    }

    [Fact]
    public void Decompile_TextWithOddLetters_UsesCSharpEscapes()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(2, "a\"bé", 3).Command(1).Build();

        // Act
        string text = CreateDecompiler().Decompile(Read(bytes), "scr_brg");

        // Assert
        Assert.Contains("sprite(\"a\\\"b\\u00e9\", 3);", text);
    }

    private static ScriptDecompiler CreateDecompiler()
    {
        ScriptDatabase database = ScriptTestDatabase.Create().WithTagNames();
        return new ScriptDecompiler(database, new CSharpDialect(database));
    }

    private static ScriptBinary Read(byte[] bytes) => ScriptBinary.Read(bytes, ScriptTestDatabase.Create().WithTagNames());
}
