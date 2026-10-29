using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.CSharp;
using BBTAG_IM.Core.Scripting.EditorHelp;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.CSharp;

public class CSharpApiTests
{
    private readonly ScriptDocs _docs = new(
        new Dictionary<int, CommandDoc> { [ScriptTestDatabase.Damage] = new("Sets the damage & <more>.", "(x)", "") },
        new Dictionary<int, string> { [51] = "Is in Overdrive." },
        new Dictionary<int, string> { [10] = "Attack hitting the opponent" });

    [Fact]
    public void Write_Command_HasItsDocsAndTypedParameters()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("    /// <para>Sets the damage &amp; &lt;more&gt;.</para>\n", api);
        Assert.Contains("    public static void Damage(int value) { }\n", api);
        Assert.Contains("    public static Value CheckInput(Input input) => default;\n", api);
    }

    [Fact]
    public void Write_CommandWithSlotArguments_HasThePairedAndTheRawForm()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("    public static void If(Value value) { }\n", api);
        Assert.Contains("    public static void If(int value1, int value2) { }\n", api);
    }

    [Fact]
    public void Write_BlockCommands_TakeTheBodyAsALambda()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("    public static void upon(Upon upon, Action body) { }\n", api);
        Assert.Contains("    public static void Move_Register(Action state, NormalInput input, Action body) { }\n", api);
        Assert.Contains("    public static void Move_Register(string state, NormalInput input, Action body) { }\n", api);
    }

    [Fact]
    public void Write_StateArgument_TakesTheMethodOrItsName()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("    public static void SharedGatling(Action state) { }\n", api);
        Assert.Contains("    public static void SharedGatling(string state) { }\n", api);
    }

    [Fact]
    public void Write_NamedValues_AreDocumentedEnumMembers()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("public enum NormalInput\n{\n", api);
        Assert.Contains("    /// <para>0x7 (7): 5A on the ground.</para>\n    /// </summary>\n    _5A = 7,\n", api);
        Assert.Contains("    _236 = 172,\n", api);
        Assert.Contains("    GROUND = 8192,\n", api);
        Assert.Contains("    Hit = 10,\n", api);
        Assert.Contains("    AIR_VERTICAL = 10,\n", api);
    }

    [Fact]
    public void Write_Slots_AreDocumentedProperties()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("    /// <para>SLOT 51: Is in Overdrive.</para>\n    /// </summary>\n    public static Value IsInOverdrive { get; set; }\n", api);
        Assert.Contains("    public static Value _3 { get; set; }\n", api);
    }

    [Fact]
    public void Write_NamedCommand_AlsoHasItsNumberName()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("    public static void Unknown9000(int value) { }\n", api);
    }

    [Fact]
    public void Write_Always_HasTheValueTypeAndTheAttributes()
    {
        // Act
        string api = CSharpApi.Write(CreateDatabase(), _docs, ScriptUsage.Empty);

        // Assert
        Assert.Contains("public readonly struct Value", api);
        Assert.Contains("public static bool operator true(Value value)", api);
        Assert.Contains("public sealed class StateAttribute : Attribute", api);
        Assert.Contains("public abstract class Script", api);
    }

    private static ScriptDatabase CreateDatabase() =>
        ScriptTestDatabase.Create()
            .WithGameSizes(new Dictionary<int, int>
            {
                [ScriptTestDatabase.Damage] = 8, [4] = 12, [43] = 8, [ScriptTestDatabase.SharedGatling] = 36, [15] = 8, [16] = 4, [14001] = 40, [14002] = 4,
            })
            .WithTagNames();
}
