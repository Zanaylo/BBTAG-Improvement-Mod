using System.Globalization;
using BBTAG_IM.Core.Scripting.EditorHelp;

namespace BBTAG_IM.Core.Scripting;

public sealed class PythonDialect(ScriptDatabase database, ScriptStyle style) : IScriptDialect
{
    private const string BlankLine = "\n\n";

    public string FunctionSeparator => BlankLine;

    public int BodyDepth => 1;

    public bool UniqueFunctionNames => false;

    public bool BlankLineBeforeFirstBlock => true;

    public string? FunctionEnd => null;

    public string? EmptyBlock => ScriptKeywords.Pass;

    public IReadOnlyList<string> ElseStart { get; } = [ScriptKeywords.Else + ":"];

    public string End => string.Empty;

    public string Begin(string scriptName) => style == ScriptStyle.Readable ? ScriptStubs.EditorSettings + BlankLine : string.Empty;

    public string? FunctionIdentifier(string name) => FunctionNames.ToIdentifier(name);

    public IEnumerable<string> FunctionStart(string decorator, string? realName, string identifier)
    {
        yield return realName is null ? "@" + decorator : $"@{decorator}({ScriptLiterals.Text(realName)})";
        yield return $"{ScriptKeywords.Def} {identifier}():";
    }

    public string Statement(string text) => text;

    public string CommandName(CommandDefinition command) => database.CallName(command);

    public IReadOnlyList<string> IfStart(bool negated, string condition, bool constant) =>
        [$"{(negated ? ScriptKeywords.If + " " + ScriptKeywords.Not : ScriptKeywords.If)} {condition}:"];

    public string? BlockEnd(BlockKind kind) => null;

    public IReadOnlyList<string>? CallBlockStart(ScriptInstruction instruction, Func<ScriptInstruction, string> call)
    {
        switch (instruction.Id)
        {
            case ScriptCommands.Upon:
                var upon = database.UponName(instruction.Values[0].Number);
                return FunctionNames.IsIdentifier(upon) ? [$"{ScriptKeywords.Def} {upon}():"] : null;
            case ScriptCommands.ObjectFunction:
                var objectFunction = $"{instruction.Command.Name}_{instruction.Values[0].Number.ToString(CultureInfo.InvariantCulture)}";
                return instruction.Values[0].Number >= 0 && FunctionNames.IsIdentifier(objectFunction) ? [$"{ScriptKeywords.Def} {objectFunction}():"] : null;
            case ScriptCommands.StateRegister:
                var name = FunctionNames.ToIdentifier(instruction.TextAt(0));
                var input = database.Inputs.NameOf(instruction.Id, instruction.Values[1].Number, instruction.Command.Hex.Contains(1));
                if (name is null || input is null || !FunctionNames.IsIdentifier(input))
                    return null;
                return ["@" + ScriptDecorators.StateRegister, $"{ScriptKeywords.Def} {name}({input}):"];
            default:
                return null;
        }
    }

    public string Value(NamedValueRequest request)
    {
        var named = request.Kind switch
        {
            ValueKind.Input => database.Inputs.NameOf(request.Command, request.Value, request.Hex),
            ValueKind.Upon => database.UponName(request.Value),
            ValueKind.HitAnimation => database.HitAnimations.NameOf(request.Value),
            ValueKind.MoveCondition => database.MoveConditions.NameOf(request.Value),
            _ => null,
        };

        return named is not null && FunctionNames.IsIdentifier(named) ? named : request.Plain;
    }

    public string Slot(int slot) => database.SlotName(slot);

    public string Text(string text) => ScriptLiterals.Text(text);

    public string Operation(ScriptOperator operation, string left, string right) => ScriptOperators.Format(operation, left, right);

    public string? StateReference(string name) => style == ScriptStyle.Readable ? FunctionNames.ToIdentifier(name) : null;
}
