namespace BBTAG_IM.Core.Scripting.Compiling;

public sealed class CommandEncoder(ScriptDatabase database)
{
    private const int ConstantType = 0;
    private const int SlotType = 2;
    private const string Attributes = "HBFPT";

    private static readonly HashSet<int> AttributeCommands = [11058, 22019];

    public void Write(ScriptBuffer buffer, CommandDefinition command, IReadOnlyList<Expression> arguments, int line)
    {
        buffer.MarkLine(line);
        buffer.WriteInt32(command.Id);

        if (AttributeCommands.Contains(command.Id) && arguments is [TextExpression letters])
        {
            WriteAttributes(buffer, command, letters.Text, line);
            return;
        }

        if (IsBlob(command, arguments))
        {
            buffer.WriteBytes(EncodeText((TextExpression)arguments[0], command.Size - sizeof(int)));
            return;
        }

        var pairedCount = command.Arguments.Count - command.SlotTypeArguments.Count;

        if (arguments.Count == pairedCount)
        {
            WritePaired(buffer, command, arguments);
            return;
        }

        if (arguments.Count == command.Arguments.Count)
        {
            WriteRaw(buffer, command, arguments);
            return;
        }

        throw new ScriptCompileException($"{command.Name} takes {pairedCount} values but was given {arguments.Count}.", line);
    }

    public (int Type, int Value) Operand(Expression expression)
    {
        if (expression is NameExpression name)
        {
            return database.SlotNumber(name.Name) is { } slot
                ? (SlotType, slot)
                : throw new ScriptCompileException($"{name.Name} is not a SLOT.", name.Line);
        }

        return ConstantFolding.Evaluate(expression) is { } number
            ? (ConstantType, ToInt32(number, expression.Line))
            : throw new ScriptCompileException("Expected a SLOT or a number here.", expression.Line);
    }

    private void WritePaired(ScriptBuffer buffer, CommandDefinition command, IReadOnlyList<Expression> arguments)
    {
        var visible = 0;

        for (var index = 0; index < command.Arguments.Count; index++)
        {
            if (command.SlotTypeArguments.Contains(index))
            {
                var (type, value) = Operand(arguments[visible++]);
                buffer.WriteInt32(type);
                buffer.WriteInt32(value);
                index++;
                continue;
            }

            WriteValue(buffer, command, command.Arguments[index], arguments[visible], visible, namesAllowed: true);
            visible++;
        }
    }

    private void WriteRaw(ScriptBuffer buffer, CommandDefinition command, IReadOnlyList<Expression> arguments)
    {
        for (var index = 0; index < command.Arguments.Count; index++)
            WriteValue(buffer, command, command.Arguments[index], arguments[index], index, namesAllowed: !command.HasSlotArguments);
    }

    private void WriteValue(ScriptBuffer buffer, CommandDefinition command, CommandArgument argument, Expression expression, int visibleIndex, bool namesAllowed)
    {
        if (argument.Kind == ArgumentKind.Text)
        {
            if (expression is not TextExpression text)
                throw new ScriptCompileException($"{command.Name} expects text in quotes here.", expression.Line);

            buffer.WriteBytes(EncodeText(text, argument.Length));
            return;
        }

        if (expression is NameExpression name)
        {
            buffer.WriteInt32(namesAllowed ? NamedValue(command, name, visibleIndex) : throw NotANumber(command, name));
            return;
        }

        if (ConstantFolding.Evaluate(expression) is not { } number)
            throw new ScriptCompileException($"{command.Name} expects a number here.", expression.Line);

        buffer.WriteInt32(ToInt32(number, expression.Line));
    }

    private int NamedValue(CommandDefinition command, NameExpression name, int visibleIndex)
    {
        var value = ValueKinds.Of(command.Id, visibleIndex) switch
        {
            ValueKind.Input => database.Inputs.ValueOf(name.Name),
            ValueKind.Upon => database.UponNumber(name.Name),
            ValueKind.HitAnimation => database.HitAnimations.ValueOf(name.Name),
            ValueKind.MoveCondition => database.MoveConditions.ValueOf(name.Name),
            _ => null,
        };

        return value ?? throw NotANumber(command, name);
    }

    private static bool IsBlob(CommandDefinition command, IReadOnlyList<Expression> arguments) =>
        arguments is [TextExpression]
        && !command.HasSlotArguments
        && command.Arguments.All(argument => argument.Kind != ArgumentKind.Text);

    private static void WriteAttributes(ScriptBuffer buffer, CommandDefinition command, string letters, int line)
    {
        if (command.Arguments.Count != Attributes.Length || letters.Any(letter => !Attributes.Contains(letter, StringComparison.Ordinal)))
            throw new ScriptCompileException($"{command.Name} attributes can only use the letters {Attributes}.", line);

        foreach (var attribute in Attributes)
            buffer.WriteInt32(letters.Contains(attribute, StringComparison.Ordinal) ? 1 : 0);
    }

    private static byte[] EncodeText(TextExpression text, int length)
    {
        try
        {
            return ScriptText.Encode(text.Text, length);
        }
        catch (ArgumentException exception)
        {
            throw new ScriptCompileException(exception.Message, text.Line);
        }
    }

    private static int ToInt32(long number, int line) =>
        number is >= int.MinValue and <= uint.MaxValue
            ? unchecked((int)number)
            : throw new ScriptCompileException($"{number} does not fit in 32 bits.", line);

    private static ScriptCompileException NotANumber(CommandDefinition command, NameExpression name) =>
        new($"{name.Name} is not a value {command.Name} understands here.", name.Line);
}
