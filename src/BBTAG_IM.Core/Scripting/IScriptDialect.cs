namespace BBTAG_IM.Core.Scripting;

public enum BlockKind
{
    Condition,
    Call,
}

public sealed record NamedValueRequest(ValueKind Kind, int Command, int Value, bool Hex, string Plain);

public interface IScriptDialect
{
    string FunctionSeparator { get; }

    int BodyDepth { get; }

    bool UniqueFunctionNames { get; }

    bool BlankLineBeforeFirstBlock { get; }

    bool StacksBlocks { get; }

    string? FunctionEnd { get; }

    string? EmptyBlock { get; }

    IReadOnlyList<string> ElseStart { get; }

    string End { get; }

    string Begin(string scriptName);

    string? FunctionIdentifier(string name);

    IEnumerable<string> FunctionStart(string decorator, string? realName, string identifier);

    string Statement(string text);

    string CommandName(CommandDefinition command);

    IReadOnlyList<string> IfStart(bool negated, string condition, bool constant);

    string? BlockEnd(BlockKind kind);

    IReadOnlyList<string>? CallBlockStart(ScriptInstruction instruction, Func<ScriptInstruction, string> call);

    string Value(NamedValueRequest request);

    string Slot(int slot);

    string Text(string text);

    string Operation(ScriptOperator operation, string left, string right);

    string? StateReference(string name);
}
