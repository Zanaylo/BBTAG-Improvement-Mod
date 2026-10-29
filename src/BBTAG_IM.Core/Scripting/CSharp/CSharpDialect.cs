using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting.CSharp;

public sealed class CSharpDialect(ScriptDatabase database) : IScriptDialect
{
    public const string ValueType = "Value";
    public const string BaseClass = "Script";
    public const string Namespace = "BBScript";
    public const string BlockLambda = "() =>";

    private const string OpenBrace = "{";
    private const string CloseBrace = "}";
    private const string CloseCallBlock = "});";
    private const char Quote = '"';
    private const char FirstPrintable = ' ';
    private const char LastPrintable = '~';

    public string FunctionSeparator => "\n";

    public int BodyDepth => 2;

    public bool UniqueFunctionNames => true;

    public bool BlankLineBeforeFirstBlock => false;

    public string? FunctionEnd => CloseBrace;

    public string? EmptyBlock => null;

    public IReadOnlyList<string> ElseStart { get; } = ["else", OpenBrace];

    public string End => CloseBrace + "\n";

    public static string ClassName(string scriptName) => CSharpNames.Identifier(scriptName) ?? CSharpNames.Escaped(FunctionNames.Mangle(scriptName));

    public string Begin(string scriptName) =>
        $"using {Namespace};\n\npublic partial class {ClassName(scriptName)} : {BaseClass}\n{OpenBrace}\n";

    public string? FunctionIdentifier(string name) => CSharpNames.Identifier(name) is { } identifier && !CSharpApi.IsReserved(identifier) ? identifier : null;

    public IEnumerable<string> FunctionStart(string decorator, string? realName, string identifier)
    {
        yield return realName is null ? $"[{decorator}]" : $"[{decorator}({Text(realName)})]";
        yield return $"public void {identifier}()";
        yield return OpenBrace;
    }

    public string Statement(string text) => text + ";";

    public string CommandName(CommandDefinition command) => CSharpApi.CommandMethodName(database, command) ?? CommandDefinition.UnknownName(command.Id);

    public IReadOnlyList<string> IfStart(bool negated, string condition, bool constant)
    {
        var test = constant ? $"({ValueType})({condition})" : condition;
        return [$"if ({(negated ? "!" : string.Empty)}{test})", OpenBrace];
    }

    public string? BlockEnd(BlockKind kind) => kind == BlockKind.Condition ? CloseBrace : CloseCallBlock;

    public IReadOnlyList<string>? CallBlockStart(ScriptInstruction instruction, Func<ScriptInstruction, string> call)
    {
        var text = call(instruction);
        return [$"{text[..^1]}, {BlockLambda}", OpenBrace];
    }

    public string Value(NamedValueRequest request)
    {
        var (type, canonical) = request.Kind switch
        {
            ValueKind.Input when request.Command == ScriptCommands.StateRegister => (CSharpNames.NormalInput, InputName(database.Inputs.Normal.NameOf(request.Value), request.Value)),
            ValueKind.Input => (CSharpNames.Input, InputName(database.Inputs.Motions.NameOf(request.Value), request.Value)),
            ValueKind.Upon => (CSharpNames.Upon, Prefixed(ScriptDatabase.UponPrefix, database.Upons.NameOf(request.Value))),
            ValueKind.HitAnimation => (CSharpNames.HitAnimation, database.HitAnimations.NameOf(request.Value)),
            ValueKind.MoveCondition => (CSharpNames.MoveCondition, database.MoveConditions.NameOf(request.Value)),
            _ => (null, null),
        };

        if (type is null)
            return request.Plain;

        var member = canonical is null ? null : CSharpNames.Member(canonical, CSharpNames.Prefixes[type]);
        return member is null ? Cast(type, request.Plain) : $"{type}.{member}";
    }

    public string Slot(int slot)
    {
        var canonical = Prefixed(ScriptDatabase.SlotPrefix, database.Slots.NameOf(slot)) ?? ScriptDatabase.SlotPrefix + slot.ToString(CultureInfo.InvariantCulture);
        var member = CSharpNames.Member(canonical, ScriptDatabase.SlotPrefix) ?? CSharpNames.Member(ScriptDatabase.SlotPrefix + slot.ToString(CultureInfo.InvariantCulture), ScriptDatabase.SlotPrefix);
        return $"{CSharpNames.Slot}.{member}";
    }

    public string Text(string text)
    {
        var literal = new StringBuilder(text.Length + 2).Append(Quote);

        foreach (var letter in text)
            literal.Append(Escaped(letter));

        return literal.Append(Quote).ToString();
    }

    public string Operation(ScriptOperator operation, string left, string right) => operation switch
    {
        ScriptOperator.And => $"{left} && {right}",
        ScriptOperator.Or => $"{left} || {right}",
        _ => ScriptOperators.Format(operation, left, right),
    };

    public string? StateReference(string name) => FunctionIdentifier(name);

    private string? InputName(string? name, int value) => name is not null && database.Inputs.ValueOf(name) == value ? name : null;

    private static string? Prefixed(string prefix, string? name) => name is null ? null : prefix + name;

    private static string Cast(string type, string plain) => plain.StartsWith('-') ? $"({type})({plain})" : $"({type}){plain}";

    private static string Escaped(char letter) => letter switch
    {
        '\\' => @"\\",
        Quote => "\\\"",
        '\n' => @"\n",
        '\r' => @"\r",
        '\t' => @"\t",
        >= FirstPrintable and <= LastPrintable => letter.ToString(),
        _ => @"\u" + ((int)letter).ToString("x4", CultureInfo.InvariantCulture),
    };
}
