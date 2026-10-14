using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting;

public sealed class ScriptDecompiler(ScriptDatabase database)
{
    private const string Indent = "    ";
    private const string SlotZero = "SLOT_0";
    private const int ConstantType = 0;
    private const int SlotType = 2;
    private const int OperationIndex = 0;

    public string Decompile(ScriptBinary script)
    {
        var text = new StringBuilder();
        var instructions = script.Instructions;
        var start = 0;
        var unnamed = 0;

        while (start < instructions.Count)
        {
            if (!ScriptCommands.StartsFunction(instructions[start].Id))
                throw new InvalidScriptException($"The command at 0x{instructions[start].Offset:X} is outside of any state or subroutine.");

            var end = start + 1;

            while (end < instructions.Count && !ScriptCommands.StartsFunction(instructions[end].Id))
                end++;

            if (text.Length > 0)
                text.Append('\n').Append('\n');

            AppendFunction(text, instructions, start, end, ref unnamed);
            start = end;
        }

        return text.ToString();
    }

    private void AppendFunction(StringBuilder text, IReadOnlyList<ScriptInstruction> instructions, int start, int end, ref int unnamed)
    {
        var opener = instructions[start];
        var structured = Structured(instructions, start, end);
        var name = opener.TextAt(0);
        var identifier = FunctionNames.ToIdentifier(name);
        var decorator = ScriptDecorators.For(opener.Id, structured is null);

        if (identifier is null)
        {
            text.Append('@').Append(decorator).Append('(').Append(ScriptLiterals.Text(name)).Append(")\n");
            identifier = "unnamed_" + unnamed++.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            text.Append('@').Append(decorator).Append('\n');
        }

        text.Append(ScriptKeywords.Def).Append(' ').Append(identifier).Append("():\n");

        foreach (var line in structured ?? Raw(instructions, start + 1, end))
            text.Append(line).Append('\n');
    }

    private List<string>? Structured(IReadOnlyList<ScriptInstruction> instructions, int start, int end)
    {
        var lines = new List<string>();
        var index = start + 1;
        var closer = ScriptCommands.CloserOf(instructions[start].Id)!.Value;

        return Block(lines, instructions, end, 1, ref index, closer) && index == end ? lines : null;
    }

    private List<string> Raw(IReadOnlyList<ScriptInstruction> instructions, int start, int end)
    {
        var lines = new List<string>();

        for (var index = start; index < end; index++)
            lines.Add(Indent + Call(instructions[index], paired: false));

        if (lines.Count == 0)
            lines.Add(Indent + ScriptKeywords.Pass);

        return lines;
    }

    private bool Block(List<string> lines, IReadOnlyList<ScriptInstruction> instructions, int end, int depth, ref int index, int closer)
    {
        var indent = string.Concat(Enumerable.Repeat(Indent, depth));
        var first = lines.Count;
        var afterIf = false;

        while (index < end)
        {
            var instruction = instructions[index];

            if (instruction.Id == closer)
            {
                index++;

                if (lines.Count == first)
                    lines.Add(indent + ScriptKeywords.Pass);

                return true;
            }

            if (ScriptCommands.StartsFunction(instruction.Id))
                return false;

            if (instruction.Id == ScriptCommands.Else)
            {
                if (!afterIf)
                    return false;

                lines.Add(indent + ScriptKeywords.Else + ":");
                index++;

                if (!Block(lines, instructions, end, depth + 1, ref index, ScriptCommands.EndElse))
                    return false;

                afterIf = false;
                continue;
            }

            if (ScriptCommands.IsCloser(instruction.Id))
                return false;

            if (ScriptCommands.CloserOf(instruction.Id) is { } innerCloser)
            {
                if (Header(instruction) is not { } header)
                    return false;

                if (instruction.Id is ScriptCommands.Upon or ScriptCommands.ObjectFunction or ScriptCommands.StateRegister)
                    lines.Add(string.Empty);

                foreach (var headerLine in header)
                    lines.Add(indent + headerLine);

                index++;

                if (!Block(lines, instructions, end, depth + 1, ref index, innerCloser))
                    return false;

                afterIf = instruction.Id is ScriptCommands.If or ScriptCommands.IfNot;
                continue;
            }

            lines.Add(indent + Statement(instruction));
            afterIf = false;
            index++;
        }

        return false;
    }

    private string[]? Header(ScriptInstruction instruction)
    {
        switch (instruction.Id)
        {
            case ScriptCommands.If:
            case ScriptCommands.IfNot:
                var condition = Operand(instruction, 0);
                if (condition is null)
                    return null;
                var keyword = instruction.Id == ScriptCommands.If ? ScriptKeywords.If : ScriptKeywords.If + " " + ScriptKeywords.Not;
                return [$"{keyword} {condition}:"];
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
        return ScriptCommands.WritesSlotZero.Contains(instruction.Id) ? $"{SlotZero} = {call}" : call;
    }

    private string? OperationAssignment(ScriptInstruction instruction) =>
        Expression(instruction) is { } expression ? $"{SlotZero} = {expression}" : null;

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

        return ScriptOperators.Format((ScriptOperator)operation, left, right);
    }

    private string Call(ScriptInstruction instruction, bool paired)
    {
        var arguments = paired && CanPair(instruction) ? PairedArguments(instruction) : RawArguments(instruction);
        return $"{CallName(instruction.Command)}({string.Join(", ", arguments)})";
    }

    private string CallName(CommandDefinition command) =>
        FunctionNames.IsIdentifier(command.Name) && database.Find(command.Name)?.Id == command.Id
            ? command.Name
            : CommandDefinition.UnknownName(command.Id);

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
            return ScriptLiterals.Text(instruction.TextAt(index));

        var named = ValueKinds.Of(instruction.Id, visibleIndex) switch
        {
            ValueKind.Input => database.Inputs.NameOf(instruction.Id, value.Number, instruction.Command.Hex.Contains(visibleIndex)),
            ValueKind.Upon => database.UponName(value.Number),
            ValueKind.HitAnimation => database.HitAnimations.NameOf(value.Number),
            ValueKind.MoveCondition => database.MoveConditions.NameOf(value.Number),
            _ => null,
        };

        return named is not null && FunctionNames.IsIdentifier(named) ? named : Plain(instruction, index, visibleIndex);
    }

    private static string Plain(ScriptInstruction instruction, int index, int visibleIndex)
    {
        var value = instruction.Values[index];

        if (value.IsText)
            return ScriptLiterals.Text(instruction.TextAt(index));

        var unsigned = instruction.Command.Arguments[index].Kind == ArgumentKind.Unsigned;
        return ScriptLiterals.Number(value.Number, instruction.Command.Hex.Contains(visibleIndex), unsigned);
    }

    private string? Operand(ScriptInstruction instruction, int typeIndex) =>
        instruction.Values[typeIndex].Number switch
        {
            ConstantType => ScriptLiterals.Number(instruction.Values[typeIndex + 1].Number, hex: false, unsigned: false),
            SlotType => database.SlotName(instruction.Values[typeIndex + 1].Number),
            _ => null,
        };

    private static bool IsSlot(ScriptInstruction instruction, int typeIndex) => instruction.Values[typeIndex].Number == SlotType;

    private static int SlotOf(ScriptInstruction instruction, int typeIndex) => instruction.Values[typeIndex + 1].Number;

    private static bool SameOperand(ScriptInstruction instruction, int first, int second) =>
        instruction.Values[first].Number == instruction.Values[second].Number
        && instruction.Values[first + 1].Number == instruction.Values[second + 1].Number;
}
