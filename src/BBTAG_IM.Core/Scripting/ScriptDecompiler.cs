using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting;

public sealed class ScriptDecompiler
{
    private const string Indent = "    ";
    private const string NewLine = "\n";
    private const string DefaultScriptName = "Script";
    private const string UnnamedPrefix = "unnamed_";
    private const string DuplicateMark = "_";
    private const int ConstantType = 0;
    private const int SlotType = 2;
    private const int OperationIndex = 0;

    private readonly ScriptDatabase database;
    private readonly IScriptDialect dialect;
    private HashSet<string> functions = [];

    public ScriptDecompiler(ScriptDatabase database, ScriptStyle style = ScriptStyle.Readable)
        : this(database, new PythonDialect(database, style))
    {
    }

    public ScriptDecompiler(ScriptDatabase database, IScriptDialect dialect)
    {
        this.database = database;
        this.dialect = dialect;
    }

    public string Decompile(ScriptBinary script, string scriptName = DefaultScriptName)
    {
        var text = new StringBuilder(dialect.Begin(scriptName));
        var instructions = script.Instructions;
        var identifiers = new Dictionary<string, int>(StringComparer.Ordinal);
        functions = instructions.Where(instruction => ScriptCommands.StartsFunction(instruction.Id)).Select(instruction => instruction.TextAt(0)).ToHashSet(StringComparer.Ordinal);
        var start = 0;
        var unnamed = 0;

        while (start < instructions.Count)
        {
            if (!ScriptCommands.StartsFunction(instructions[start].Id))
                throw new InvalidScriptException($"The command at 0x{instructions[start].Offset:X} is outside of any state or subroutine.");

            var end = start + 1;

            while (end < instructions.Count && !ScriptCommands.StartsFunction(instructions[end].Id))
                end++;

            if (start > 0)
                text.Append(dialect.FunctionSeparator);

            AppendFunction(text, instructions, start, end, identifiers, ref unnamed);
            start = end;
        }

        return text.Append(dialect.End).ToString();
    }

    private void AppendFunction(StringBuilder text, IReadOnlyList<ScriptInstruction> instructions, int start, int end, Dictionary<string, int> identifiers, ref int unnamed)
    {
        var opener = instructions[start];
        var (structured, raw) = dialect.StacksBlocks ? StackedFunction(instructions, start, end) : (Structured(instructions, start, end), false);
        var name = opener.TextAt(0);
        var identifier = dialect.FunctionIdentifier(name);
        var decorator = ScriptDecorators.For(opener.Id, raw || structured is null);
        var outer = IndentOf(dialect.BodyDepth - 1);
        string? realName = null;

        if (identifier is null)
        {
            realName = name;
            identifier = UnnamedPrefix + unnamed++.ToString(CultureInfo.InvariantCulture);
        }
        else if (dialect.UniqueFunctionNames && identifiers.TryGetValue(identifier, out var seen))
        {
            identifiers[identifier] = seen + 1;
            realName = name;
            identifier += DuplicateMark + (seen + 1).ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            identifiers[identifier] = 1;
        }

        foreach (var line in dialect.FunctionStart(decorator, realName, identifier))
            text.Append(outer).Append(line).Append(NewLine);

        foreach (var line in structured ?? Raw(instructions, start + 1, end))
            text.Append(line).Append(NewLine);

        if (dialect.FunctionEnd is { } functionEnd)
            text.Append(outer).Append(functionEnd).Append(NewLine);
    }

    private List<string>? Structured(IReadOnlyList<ScriptInstruction> instructions, int start, int end)
    {
        var lines = new List<string>();
        var index = start + 1;
        var closer = ScriptCommands.CloserOf(instructions[start].Id)!.Value;

        return Block(lines, instructions, end, dialect.BodyDepth, ref index, closer) && index == end ? lines : null;
    }

    private (List<string>? Lines, bool Raw) StackedFunction(IReadOnlyList<ScriptInstruction> instructions, int start, int end)
    {
        var endsWithCloser = end - start >= 2 && instructions[end - 1].Id == ScriptCommands.CloserOf(instructions[start].Id);

        if (endsWithCloser && Stacked(instructions, start + 1, end - 1) is { } closed)
            return (closed, false);

        return (Stacked(instructions, start + 1, end), true);
    }

    private List<string>? Stacked(IReadOnlyList<ScriptInstruction> instructions, int first, int last)
    {
        var lines = new List<string>();
        var open = new Stack<OpenBlock>();
        var afterIf = new HashSet<int>();
        var comparison = (Index: -1, Expression: string.Empty);

        for (var index = first; index < last; index++)
        {
            var instruction = instructions[index];
            var depth = dialect.BodyDepth + open.Count;

            if (ScriptCommands.IsStackCloser(instruction.Id))
            {
                Close(lines, open, afterIf, instruction, depth);
                continue;
            }

            if (instruction.Id == ScriptCommands.Else && afterIf.Contains(depth))
            {
                afterIf.Remove(depth);
                lines.AddRange(dialect.ElseStart.Select(line => IndentOf(depth) + line));
                open.Push(new OpenBlock(ScriptCommands.EndElse, depth, lines.Count, IsCondition: false));
                afterIf.Remove(depth + 1);
                continue;
            }

            afterIf.Remove(depth);

            if (ScriptCommands.CloserOf(instruction.Id) is { } closer && instruction.Id != ScriptCommands.Else
                && StackedHeader(instruction, lines, comparison.Index == index - 1 ? comparison.Expression : null) is { } header)
            {
                if (header.Kind == BlockKind.Call && (lines.Count > 0 || dialect.BlankLineBeforeFirstBlock))
                    lines.Add(string.Empty);

                lines.AddRange(header.Lines.Select(line => IndentOf(depth) + line));
                open.Push(new OpenBlock(closer, depth, lines.Count, header.Kind == BlockKind.Condition));
                afterIf.Remove(depth + 1);
                continue;
            }

            var statement = ScriptCommands.CloserOf(instruction.Id) is null ? Statement(instruction) : Call(instruction, paired: true);
            lines.Add(IndentOf(depth) + dialect.Statement(statement));

            if (TestedValue(instruction) is { } tested)
                comparison = (index, tested);
        }

        if (open.Count > 0)
            return null;

        if (lines.Count == 0 && dialect.EmptyBlock is { } empty)
            lines.Add(IndentOf(dialect.BodyDepth) + empty);

        return lines;
    }

    private void Close(List<string> lines, Stack<OpenBlock> open, HashSet<int> afterIf, ScriptInstruction closer, int depth)
    {
        if (open.Count == 0)
        {
            lines.Add(IndentOf(depth) + dialect.Statement(Call(closer, paired: true)));
            return;
        }

        var block = open.Pop();
        afterIf.Remove(block.Depth + 1);

        if (closer.Id != block.Closer)
            lines.Add(IndentOf(block.Depth + 1) + dialect.Statement(Call(closer, paired: true)));
        else if (lines.Count == block.FirstLine && dialect.EmptyBlock is { } empty)
            lines.Add(IndentOf(block.Depth + 1) + empty);

        if (block.IsCondition)
            afterIf.Add(block.Depth);
    }

    private (BlockKind Kind, IReadOnlyList<string> Lines)? StackedHeader(ScriptInstruction instruction, List<string> lines, string? comparison)
    {
        var mergesComparison = instruction.Id is ScriptCommands.If or ScriptCommands.IfNot
            && IsSlot(instruction, 0) && SlotOf(instruction, 0) == 0;

        if (comparison is null || !mergesComparison)
            return Header(instruction);

        lines.RemoveAt(lines.Count - 1);
        return (BlockKind.Condition, dialect.IfStart(instruction.Id == ScriptCommands.IfNot, comparison, constant: false));
    }

    private string? TestedValue(ScriptInstruction instruction)
    {
        if (instruction.Id == ScriptCommands.Operation)
            return ScriptOperators.IsKnown(instruction.Values[OperationIndex].Number) && Expression(instruction) is { } expression ? $"({expression})" : null;

        if (instruction.Id is ScriptCommands.OperationInto or ScriptCommands.ModifyValue or ScriptCommands.StoreValue || ScriptCommands.CloserOf(instruction.Id) is not null || ScriptCommands.IsCloser(instruction.Id))
            return null;

        return Call(instruction, paired: true);
    }

    private List<string> Raw(IReadOnlyList<ScriptInstruction> instructions, int start, int end)
    {
        var indent = IndentOf(dialect.BodyDepth);
        var lines = new List<string>();

        for (var index = start; index < end; index++)
            lines.Add(indent + dialect.Statement(Call(instructions[index], paired: false)));

        if (lines.Count == 0 && dialect.EmptyBlock is { } empty)
            lines.Add(indent + empty);

        return lines;
    }

    private bool Block(List<string> lines, IReadOnlyList<ScriptInstruction> instructions, int end, int depth, ref int index, int closer)
    {
        var indent = IndentOf(depth);
        var first = lines.Count;
        var afterIf = false;

        while (index < end)
        {
            var instruction = instructions[index];

            if (instruction.Id == closer)
            {
                index++;

                if (lines.Count == first && dialect.EmptyBlock is { } empty)
                    lines.Add(indent + empty);

                return true;
            }

            if (ScriptCommands.StartsFunction(instruction.Id))
                return false;

            if (instruction.Id == ScriptCommands.Else)
            {
                if (!afterIf)
                    return false;

                lines.AddRange(dialect.ElseStart.Select(line => indent + line));
                index++;

                if (!Block(lines, instructions, end, depth + 1, ref index, ScriptCommands.EndElse))
                    return false;

                AddBlockEnd(lines, indent, BlockKind.Condition);
                afterIf = false;
                continue;
            }

            if (ScriptCommands.IsCloser(instruction.Id))
                return false;

            if (ScriptCommands.CloserOf(instruction.Id) is { } innerCloser)
            {
                if (Header(instruction) is not { } header)
                    return false;

                if (header.Kind == BlockKind.Call && (lines.Count > first || dialect.BlankLineBeforeFirstBlock))
                    lines.Add(string.Empty);

                lines.AddRange(header.Lines.Select(line => indent + line));
                index++;

                if (!Block(lines, instructions, end, depth + 1, ref index, innerCloser))
                    return false;

                AddBlockEnd(lines, indent, header.Kind);
                afterIf = instruction.Id is ScriptCommands.If or ScriptCommands.IfNot;
                continue;
            }

            lines.Add(indent + dialect.Statement(Statement(instruction)));
            afterIf = false;
            index++;
        }

        return false;
    }

    private void AddBlockEnd(List<string> lines, string indent, BlockKind kind)
    {
        if (dialect.BlockEnd(kind) is { } blockEnd)
            lines.Add(indent + blockEnd);
    }

    private (BlockKind Kind, IReadOnlyList<string> Lines)? Header(ScriptInstruction instruction)
    {
        if (instruction.Id is ScriptCommands.If or ScriptCommands.IfNot)
        {
            if (Operand(instruction, 0) is not { } condition)
                return null;

            return (BlockKind.Condition, dialect.IfStart(instruction.Id == ScriptCommands.IfNot, condition, instruction.Values[0].Number == ConstantType));
        }

        if (instruction.Id is not (ScriptCommands.Upon or ScriptCommands.ObjectFunction or ScriptCommands.StateRegister))
            return null;

        return dialect.CallBlockStart(instruction, call => Call(call, paired: true)) is { } lines ? (BlockKind.Call, lines) : null;
    }

    private string Statement(ScriptInstruction instruction)
    {
        var assignment = instruction.Id switch
        {
            ScriptCommands.Operation => OperationAssignment(instruction),
            ScriptCommands.OperationInto => OperationIntoAssignment(instruction),
            ScriptCommands.ModifyValue => ModifyAssignment(instruction),
            ScriptCommands.StoreValue => StoreAssignment(instruction),
            _ => null,
        };

        if (assignment is not null)
            return assignment;

        var call = Call(instruction, paired: true);
        return ScriptCommands.WritesSlotZero.Contains(instruction.Id) ? $"{dialect.Slot(0)} = {call}" : call;
    }

    private string? OperationAssignment(ScriptInstruction instruction) =>
        Expression(instruction) is { } expression ? $"{dialect.Slot(0)} = {expression}" : null;

    private string? OperationIntoAssignment(ScriptInstruction instruction)
    {
        if (Expression(instruction) is not { } expression || !IsSlot(instruction, 5))
            return null;

        if (SlotOf(instruction, 5) == 0 || SameOperand(instruction, 1, 5))
            return null;

        return $"{Operand(instruction, 5)} = {expression}";
    }

    private string? ModifyAssignment(ScriptInstruction instruction)
    {
        if (Expression(instruction) is not { } expression || !IsSlot(instruction, 1) || SlotOf(instruction, 1) == 0)
            return null;

        return $"{Operand(instruction, 1)} = {expression}";
    }

    private string? StoreAssignment(ScriptInstruction instruction)
    {
        if (!IsSlot(instruction, 0) || Operand(instruction, 2) is not { } value)
            return null;

        return $"{Operand(instruction, 0)} = {value}";
    }

    private string? Expression(ScriptInstruction instruction)
    {
        var operation = instruction.Values[OperationIndex].Number;

        if (!ScriptOperators.IsKnown(operation) || Operand(instruction, 1) is not { } left || Operand(instruction, 3) is not { } right)
            return null;

        return dialect.Operation((ScriptOperator)operation, left, right);
    }

    private string Call(ScriptInstruction instruction, bool paired)
    {
        var arguments = paired && CanPair(instruction) ? PairedArguments(instruction) : RawArguments(instruction);
        return $"{dialect.CommandName(instruction.Command)}({string.Join(", ", arguments)})";
    }

    private bool CanPair(ScriptInstruction instruction) =>
        instruction.Command.SlotTypeArguments.All(typeIndex =>
            typeIndex + 1 < instruction.Values.Count && instruction.Values[typeIndex].Number is ConstantType or SlotType);

    private List<string> PairedArguments(ScriptInstruction instruction)
    {
        var arguments = new List<string>();

        for (var index = 0; index < instruction.Values.Count; index++)
        {
            if (instruction.Command.SlotTypeArguments.Contains(index))
            {
                arguments.Add(Operand(instruction, index)!);
                index++;
                continue;
            }

            arguments.Add(Value(instruction, index, arguments.Count));
        }

        return arguments;
    }

    private List<string> RawArguments(ScriptInstruction instruction)
    {
        var arguments = new List<string>();

        for (var index = 0; index < instruction.Values.Count; index++)
            arguments.Add(instruction.Command.HasSlotArguments ? Plain(instruction, index, index) : Value(instruction, index, index));

        return arguments;
    }

    private string Value(ScriptInstruction instruction, int index, int visibleIndex)
    {
        var value = instruction.Values[index];

        if (value.IsText)
            return StateReference(instruction, index, visibleIndex) ?? dialect.Text(instruction.TextAt(index));

        var hex = instruction.Command.Hex.Contains(visibleIndex);
        return dialect.Value(new NamedValueRequest(ValueKinds.Of(instruction.Id, visibleIndex), instruction.Id, value.Number, hex, Plain(instruction, index, visibleIndex)));
    }

    private string? StateReference(ScriptInstruction instruction, int index, int visibleIndex)
    {
        if (!ValueKinds.IsStateReference(instruction.Id, visibleIndex))
            return null;

        var name = instruction.TextAt(index);
        return functions.Contains(name) ? dialect.StateReference(name) : null;
    }

    private string Plain(ScriptInstruction instruction, int index, int visibleIndex)
    {
        var value = instruction.Values[index];

        if (value.IsText)
            return dialect.Text(instruction.TextAt(index));

        var unsigned = instruction.Command.Arguments[index].Kind == ArgumentKind.Unsigned;
        return ScriptLiterals.Number(value.Number, instruction.Command.Hex.Contains(visibleIndex), unsigned);
    }

    private string? Operand(ScriptInstruction instruction, int typeIndex) =>
        instruction.Values[typeIndex].Number switch
        {
            ConstantType => ScriptLiterals.Number(instruction.Values[typeIndex + 1].Number, hex: false, unsigned: false),
            SlotType => dialect.Slot(instruction.Values[typeIndex + 1].Number),
            _ => null,
        };

    private sealed record OpenBlock(int Closer, int Depth, int FirstLine, bool IsCondition);

    private static string IndentOf(int depth) => depth <= 0 ? string.Empty : string.Concat(Enumerable.Repeat(Indent, depth));

    private static bool IsSlot(ScriptInstruction instruction, int typeIndex) => instruction.Values[typeIndex].Number == SlotType;

    private static int SlotOf(ScriptInstruction instruction, int typeIndex) => instruction.Values[typeIndex + 1].Number;

    private static bool SameOperand(ScriptInstruction instruction, int first, int second) =>
        instruction.Values[first].Number == instruction.Values[second].Number
        && instruction.Values[first + 1].Number == instruction.Values[second + 1].Number;
}
