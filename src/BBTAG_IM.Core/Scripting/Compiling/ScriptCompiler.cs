namespace BBTAG_IM.Core.Scripting.Compiling;

public sealed class ScriptCompiler
{
    private const string SlotZeroName = "SLOT_0";
    private const int SlotZero = 0;
    private const int TablePlaceholder = unchecked((int)0xFADEF00D);

    private static readonly NameExpression SlotZeroValue = new(SlotZeroName, 0);

    private readonly ScriptDatabase database;
    private readonly CommandEncoder encoder;
    private ScriptBuffer buffer = new();

    public ScriptCompiler(ScriptDatabase database)
    {
        this.database = database;
        encoder = new CommandEncoder(database);
    }

    public CompiledScript Compile(string source)
    {
        var functions = ScriptParser.Parse(source);
        var roots = functions.Select(Root).ToList();
        var states = roots.Where(root => root.IsState).ToList();

        buffer = new ScriptBuffer();
        buffer.WriteInt32(states.Count);

        foreach (var state in states)
        {
            buffer.WriteBytes(EncodeName(state.Name, state.Function.Line));
            buffer.WriteInt32(TablePlaceholder);
        }

        var dataStart = buffer.Length;
        var tableIndex = 0;

        foreach (var root in roots)
        {
            if (root.IsState)
                buffer.PatchInt32(sizeof(int) + tableIndex++ * ScriptBinary.StateEntryLength + ScriptBinary.NameLength, buffer.Length - dataStart);

            WriteRoot(root);
        }

        return buffer.ToCompiledScript();
    }

    private RootFunction Root(FunctionStatement function)
    {
        if (function.Decorators.Count != 1)
            throw new ScriptCompileException($"{function.Name} needs one decorator such as @State or @Subroutine.", function.Line);

        if (function.Parameters.Count > 0)
            throw new ScriptCompileException($"{function.Name} cannot take parameters.", function.Line);

        var decorator = function.Decorators[0];
        var (start, raw) = decorator.Name.ToLowerInvariant() switch
        {
            "state" => (ScriptCommands.StartState, false),
            "subroutine" => (ScriptCommands.StartSubroutine, false),
            "rawstate" => (ScriptCommands.StartState, true),
            "rawsubroutine" => (ScriptCommands.StartSubroutine, true),
            "macro" => throw new ScriptCompileException("Macros are not supported yet.", decorator.Line),
            _ => throw new ScriptCompileException($"@{decorator.Name} is not a known decorator.", decorator.Line),
        };

        var name = decorator.Arguments switch
        {
            [] => FunctionNames.FromIdentifier(function.Name),
            [TextExpression text] => text.Text,
            _ => throw new ScriptCompileException($"@{decorator.Name} only takes the real name in quotes.", decorator.Line),
        };

        return new RootFunction(function, name, start, raw);
    }

    private void WriteRoot(RootFunction root)
    {
        WriteCommand(root.Start, [new TextExpression(root.Name, root.Function.Line)], root.Function.Line);
        WriteBody(root.Function.Body);

        if (!root.Raw)
            WriteCommand(ScriptCommands.CloserOf(root.Start)!.Value, [], root.Function.EndLine);
    }

    private void WriteBody(IReadOnlyList<Statement> body)
    {
        foreach (var statement in body)
            WriteStatement(statement);
    }

    private void WriteStatement(Statement statement)
    {
        switch (statement)
        {
            case PassStatement:
                return;
            case ExpressionStatement { Value: TextExpression }:
                return;
            case ExpressionStatement { Value: CallExpression call }:
                WriteCall(call);
                return;
            case ExpressionStatement { Value: var value } when IsOperation(value):
                WriteAssign(SlotZeroValue with { Line = value.Line }, value);
                return;
            case ExpressionStatement expression:
                throw new ScriptCompileException("This line does nothing. Call a command or give a SLOT a value.", expression.Line);
            case AssignStatement assign:
                WriteAssign(assign.Target, assign.Value);
                return;
            case IfStatement conditional:
                WriteIf(conditional);
                return;
            case FunctionStatement function:
                WriteInnerFunction(function);
                return;
            default:
                throw new ScriptCompileException("This kind of line is not supported.", statement.Line);
        }
    }

    private void WriteIf(IfStatement conditional)
    {
        var negated = conditional.Test is UnaryExpression { Operator: ScriptKeywords.Not };
        var test = negated ? ((UnaryExpression)conditional.Test).Operand : conditional.Test;
        var condition = IsSimple(test) ? test : Evaluate(test);
        var (opener, closer) = negated ? (ScriptCommands.IfNot, ScriptCommands.EndIfNot) : (ScriptCommands.If, ScriptCommands.EndIf);

        WriteCommand(opener, [condition], conditional.Line);
        WriteBody(conditional.Body);
        WriteCommand(closer, [], LastLineOf(conditional.Body, conditional.Line));

        if (conditional.Otherwise.Count == 0)
            return;

        WriteCommand(ScriptCommands.Else, [], conditional.ElseLine);
        WriteBody(conditional.Otherwise);
        WriteCommand(ScriptCommands.EndElse, [], LastLineOf(conditional.Otherwise, conditional.ElseLine));
    }

    private void WriteInnerFunction(FunctionStatement function)
    {
        if (function.Decorators is [{ Name: var decorator }] && decorator.Equals(ScriptDecorators.StateRegister, StringComparison.OrdinalIgnoreCase))
        {
            if (function.Parameters.Count != 1)
                throw new ScriptCompileException($"@{ScriptDecorators.StateRegister} needs the input as its only parameter.", function.Line);

            var name = new TextExpression(FunctionNames.FromIdentifier(function.Name), function.Line);
            WriteBlock(ScriptCommands.StateRegister, [name, new NameExpression(function.Parameters[0], function.Line)], function);
            return;
        }

        if (function.Decorators.Count > 0 || function.Parameters.Count > 0)
            throw new ScriptCompileException($"{function.Name} cannot have decorators or parameters here.", function.Line);

        if (database.UponNumber(function.Name) is { } upon)
        {
            WriteBlock(ScriptCommands.Upon, [new NumberExpression(upon, function.Line)], function);
            return;
        }

        if (ObjectFunctionTarget(function.Name) is { } target)
        {
            WriteBlock(ScriptCommands.ObjectFunction, [new NumberExpression(target, function.Line)], function);
            return;
        }

        throw new ScriptCompileException($"{function.Name} is not an upon or a function for another object.", function.Line);
    }

    private void WriteBlock(int opener, IReadOnlyList<Expression> arguments, FunctionStatement function)
    {
        WriteCommand(opener, arguments, function.Line);
        WriteBody(function.Body);
        WriteCommand(ScriptCommands.CloserOf(opener)!.Value, [], function.EndLine);
    }

    private int? ObjectFunctionTarget(string name)
    {
        var prefix = database.Command(ScriptCommands.ObjectFunction).Name + "_";

        return name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && int.TryParse(name.AsSpan(prefix.Length), out var target)
            ? target
            : null;
    }

    private void WriteAssign(NameExpression target, Expression value)
    {
        if (database.SlotNumber(target.Name) is not { } targetSlot)
            throw new ScriptCompileException($"{target.Name} is not a SLOT, so it cannot be given a value.", target.Line);

        if (value is CallExpression call)
        {
            WriteCall(call);

            if (targetSlot != SlotZero)
                WriteCommand(ScriptCommands.StoreValue, [target, SlotZeroValue with { Line = value.Line }], value.Line);

            return;
        }

        if (IsSimple(value))
        {
            WriteCommand(ScriptCommands.StoreValue, [target, value], target.Line);
            return;
        }

        var (operation, left, right) = Operation(value);
        var leftOperand = IsSimple(left) ? left : Evaluate(left);
        var rightOperand = IsSimple(right) ? right : EvaluateSecond(right, leftOperand);
        var operationValue = new NumberExpression((int)operation, value.Line);

        if (targetSlot == SlotZero)
            WriteCommand(ScriptCommands.Operation, [operationValue, leftOperand, rightOperand], value.Line);
        else if (leftOperand is NameExpression leftName && database.SlotNumber(leftName.Name) == targetSlot)
            WriteCommand(ScriptCommands.ModifyValue, [operationValue, target, rightOperand], value.Line);
        else
            WriteCommand(ScriptCommands.OperationInto, [operationValue, leftOperand, rightOperand, target], value.Line);
    }

    private NameExpression Evaluate(Expression expression)
    {
        var slotZero = SlotZeroValue with { Line = expression.Line };

        if (expression is CallExpression call)
            WriteCall(call);
        else
            WriteAssign(slotZero, expression);

        return slotZero;
    }

    private NameExpression EvaluateSecond(Expression expression, Expression first)
    {
        if (first is NameExpression name && database.SlotNumber(name.Name) == SlotZero)
            throw new ScriptCompileException("Both sides of this need SLOT_0. Work out one side on its own line first.", expression.Line);

        return Evaluate(expression);
    }

    private (ScriptOperator Operation, Expression Left, Expression Right) Operation(Expression value)
    {
        if (value is UnaryExpression { Operator: "~", Operand: BinaryExpression { Operator: "&" } inverted })
            return (ScriptOperator.NotBoth, inverted.Left, inverted.Right);

        if (value is BinaryExpression binary && ScriptOperators.FromSymbol(binary.Operator) is { } operation)
            return (operation, binary.Left, binary.Right);

        throw new ScriptCompileException("This operation is not supported by scripts.", value.Line);
    }

    private void WriteCall(CallExpression call)
    {
        var command = database.Find(call.Name) ?? throw new ScriptCompileException($"{call.Name} is not a known command.", call.Line);
        var complex = call.Arguments.Where(argument => !IsSimpleArgument(argument)).ToList();

        if (complex.Count > 1)
            throw new ScriptCompileException($"{call.Name} has more than one value that needs working out. Work them out on their own lines first.", call.Line);

        var arguments = call.Arguments.Select(argument => complex.Contains(argument) ? Evaluate(argument) : argument).ToList();
        encoder.Write(buffer, command, arguments, call.Line);
    }

    private void WriteCommand(int id, IReadOnlyList<Expression> arguments, int line) =>
        encoder.Write(buffer, database.Command(id), arguments, line);

    private static bool IsSimple(Expression expression) =>
        expression is NameExpression or NumberExpression or UnaryExpression { Operator: "-" or "+", Operand: NumberExpression };

    private static bool IsSimpleArgument(Expression expression) =>
        expression is TextExpression || IsSimple(expression) || ConstantFolding.IsConstant(expression);

    private static bool IsOperation(Expression expression) => expression is BinaryExpression or UnaryExpression { Operator: "~" };

    private static int LastLineOf(IReadOnlyList<Statement> body, int fallback) => body.Count > 0 ? body[^1].Line : fallback;

    private static byte[] EncodeName(string name, int line)
    {
        try
        {
            return ScriptText.Encode(name, ScriptBinary.NameLength);
        }
        catch (ArgumentException exception)
        {
            throw new ScriptCompileException(exception.Message, line);
        }
    }

    private sealed record RootFunction(FunctionStatement Function, string Name, int Start, bool Raw)
    {
        public bool IsState => Start == ScriptCommands.StartState;
    }
}
