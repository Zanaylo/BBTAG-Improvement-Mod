using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptRenamingTests
{
    private const string PythonSource = "@State\ndef A():\n    Damage(15)\n";

    private static readonly ScriptDatabase Before = ScriptTestDatabase.Create();
    private static readonly ScriptDatabase After = Renamed(Before, ScriptTestDatabase.Damage, "SetDamage");

    private static ScriptDatabase Renamed(ScriptDatabase database, int id, string name) =>
        new(
            database.Commands.Select(command => command.Id == id ? command with { Name = name } : command),
            database.Slots,
            database.Upons,
            database.HitAnimations,
            database.MoveConditions,
            database.Inputs);

    [Fact]
    public void Rewrite_PythonScript_UsesTheNewNames()
    {
        // Act
        string rewritten = ScriptRenaming.Rewrite(PythonSource, ScriptLanguage.Python, Before, After, "scr_brg");

        // Assert
        Assert.Contains("    SetDamage(15)", rewritten);
    }

    [Fact]
    public void Rewrite_PythonScript_CompilesToTheSameBytes()
    {
        // Arrange
        byte[] original = new ScriptCompiler(Before).Compile(PythonSource).Bytes;

        // Act
        string rewritten = ScriptRenaming.Rewrite(PythonSource, ScriptLanguage.Python, Before, After, "scr_brg");

        // Assert
        Assert.Equal(original, new ScriptCompiler(After).Compile(rewritten).Bytes);
    }

    [Fact]
    public void Rewrite_CSharpScript_UsesTheNewNames()
    {
        // Arrange
        byte[] bytes = new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 15).Command(1).Build();
        string source = ScriptLanguages.Decompiler(Before, ScriptLanguage.CSharp).Decompile(ScriptBinary.Read(bytes, Before), "scr_brg");

        // Act
        string rewritten = ScriptRenaming.Rewrite(source, ScriptLanguage.CSharp, Before, After, "scr_brg");

        // Assert
        Assert.Contains("SetDamage(15)", rewritten);
        Assert.Equal(bytes, new ScriptCompiler(After, ScriptLanguage.CSharp).Compile(rewritten).Bytes);
    }

    [Fact]
    public void Rewrite_PythonToTheMangoLayout_WritesThatLayout()
    {
        // Arrange
        string source = "@State\ndef A():\n    if not SLOT_IsInOverdrive:\n        Damage(15)\n";

        // Act
        string rewritten = ScriptRenaming.Rewrite(source, ScriptLanguage.Python, Before, Before, "scr_brg", ScriptStyle.Mango);

        // Assert
        Assert.Contains("    if (not SLOT_51):\n        Damage(15)\n", rewritten);
    }

    [Fact]
    public void Rewrite_ScriptWithAMistake_Throws()
    {
        // Act
        Exception? exception = Record.Exception(() => ScriptRenaming.Rewrite("@State\ndef A():\n    NotACommand(1)\n", ScriptLanguage.Python, Before, After, "scr_brg"));

        // Assert
        Assert.IsType<ScriptCompileException>(exception);
    }
}
