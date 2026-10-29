using BBTAG_IM.Core.Scripting.Compiling;

namespace BBTAG_IM.Core.Scripting.CSharp;

public sealed class CSharpParser
{
    private const string NameOf = "nameof";
    private const string True = "true";
    private const string False = "false";

    private static readonly HashSet<string> Modifiers = ["public", "private", "protected", "internal", "static", "partial", "sealed", "abstract", "override", "virtual", "new"];

    private static readonly string[][] BinaryLevels =
    [
        ["||"], ["&&"], ["|"], ["&"], ["==", "!="], ["<", ">", "<=", ">="], ["+", "-"], ["*", "/", "%"],
    ];

    private static readonly Dictionary<string, string> ScriptOperators = new()
    {
        ["||"] = ScriptKeywords.Or,
        ["&&"] = ScriptKeywords.And,
    };

    private readonly IReadOnlyList<CSharpToken> tokens;
    private int position;

    private CSharpParser(IReadOnlyList<CSharpToken> tokens)
    {
        this.tokens = tokens;
    }

    public static IReadOnlyList<FunctionStatement> Parse(string source) => new CSharpParser(CSharpLexer.Tokenize(source)).Module();

    private CSharpToken Current => tokens[position];

    private List<FunctionStatement> Module()
    {
        var functions = new List<FunctionStatement>();

        while (Current.IsName("using"))
            SkipPast(";");

        if (Current.IsName("namespace"))
        {
            position++;
            QualifiedName();

            if (Current.Is("{"))
                throw Error("Write the namespace as 'namespace Name;' so the script stays one class.");

            Expect(";");
        }

        while (Current.Kind != CSharpTokenKind.End)
            functions.AddRange(Class());

        return functions;
    }

    private List<FunctionStatement> Class()
    {
        SkipAttributes();
        SkipModifiers();

        if (!Current.IsName("class"))
            throw Error("Only one class with the states and subroutines can be written here.");

        position++;
        ExpectName();

        if (Current.Is(":"))
        {
            position++;
            QualifiedName();
        }

        Expect("{");
        var functions = new List<FunctionStatement>();

        while (!Current.Is("}"))
        {
            if (Current.Kind == CSharpTokenKind.End)
                throw Error("The class has no closing '}'.");

            functions.Add(Function());
        }

        position++;
        return functions;
    }

    private FunctionStatement Function()
    {
        var decorators = Attributes();
        var line = Current.Line;
        SkipModifiers();

        if (!Current.IsName("void"))
            throw Error("States and subroutines are written as 'void Name()' with [State] or [Subroutine] above.");

        position++;
        var name = ExpectName();
        Expect("(");
        Expect(")");
        var (body, endLine) = Block();

        return new FunctionStatement(name, decorators, [], body, decorators.Count > 0 ? decorators[0].Line : line, endLine);
    }

    private List<Decorator> Attributes()
    {
        var decorators = new List<Decorator>();

        while (Current.Is("["))
        {
            position++;
            var line = Current.Line;
            var name = ExpectName();
            var arguments = new List<Expression>();

            if (Current.Is("("))
            {
                position++;

                while (!Current.Is(")"))
                {
                    arguments.Add(Expression());

                    if (!Current.Is(")"))
                        Expect(",");
                }

                position++;
            }

            Expect("]");
            decorators.Add(new Decorator(name, arguments, line));
        }

        return decorators;
    }

    private (List<Statement> Body, int EndLine) Block()
    {
        Expect("{");
        var body = new List<Statement>();

        while (!Current.Is("}"))
        {
            if (Current.Kind == CSharpTokenKind.End)
                throw Error("This block has no closing '}'.");

            if (Statement() is { } statement)
                body.Add(statement);
        }

        var endLine = Current.Line;
        position++;
        return (body, endLine);
    }

    private Statement? Statement()
    {
        if (Current.Is(";"))
        {
            position++;
            return null;
        }

        if (Current.IsName("if"))
            return If();

        var line = Current.Line;
        var expression = Expression();

        if (Current.Is("="))
        {
            position++;

            if (expression is not NameExpression target)
                throw new ScriptCompileException("Only a SLOT (Slot.Name) can be given a value.", line);

            var value = Expression();
            Expect(";");
            return new AssignStatement(target, value, line);
        }

        if (expression is CallExpression call && call.Arguments.Count > 0 && call.Arguments[^1] is LambdaExpression lambda)
        {
            Expect(";");
            return new BlockStatement(call with { Arguments = call.Arguments.SkipLast(1).ToList() }, lambda.Body, line, lambda.EndLine);
        }

        Expect(";");
        return new ExpressionStatement(expression, line);
    }

    private IfStatement If()
    {
        var line = Current.Line;
        position++;
        Expect("(");
        var test = Expression();
        Expect(")");
        var (body, _) = Block();

        if (!Current.IsName("else"))
            return new IfStatement(test, body, [], line, line);

        var elseLine = Current.Line;
        position++;

        if (Current.IsName("if"))
            return new IfStatement(test, body, [If()], line, elseLine);

        var (otherwise, _) = Block();
        return new IfStatement(test, body, otherwise.Count == 0 ? [new PassStatement(elseLine)] : otherwise, line, elseLine);
    }

    private Expression Expression() => Binary(0);

    private Expression Binary(int level)
    {
        if (level == BinaryLevels.Length)
            return Unary();

        var left = Binary(level + 1);

        while (Current.Kind == CSharpTokenKind.Symbol && BinaryLevels[level].Contains(Current.Text))
        {
            var line = Current.Line;
            var symbol = Current.Text;
            position++;
            var right = Binary(level + 1);
            left = new BinaryExpression(ScriptOperators.GetValueOrDefault(symbol, symbol), left, right, line);
        }

        return left;
    }

    private Expression Unary()
    {
        var line = Current.Line;

        if (Current.Is("!"))
        {
            position++;
            return new UnaryExpression(ScriptKeywords.Not, Unary(), line);
        }

        if (Current.Is("-") || Current.Is("+") || Current.Is("~"))
        {
            var symbol = Current.Text;
            position++;
            return new UnaryExpression(symbol, Unary(), line);
        }

        if (IsCast())
        {
            position++;
            var type = ExpectName();
            position++;
            return Cast(type, Unary(), line);
        }

        return Primary();
    }

    private bool IsCast() =>
        Current.Is("(")
        && tokens[position + 1].Kind == CSharpTokenKind.Name
        && (tokens[position + 1].Text == CSharpDialect.ValueType || CSharpNames.Prefixes.ContainsKey(tokens[position + 1].Text))
        && tokens[position + 2].Is(")");

    private static Expression Cast(string type, Expression operand, int line)
    {
        if (type == CSharpDialect.ValueType || ConstantFolding.IsConstant(operand))
            return operand;

        throw new ScriptCompileException($"Only a number can be turned into a {type}, such as ({type})0x5F.", line);
    }

    private Expression Primary()
    {
        var token = Current;

        switch (token.Kind)
        {
            case CSharpTokenKind.Number:
                position++;
                return new NumberExpression(token.Number, token.Line);
            case CSharpTokenKind.Text:
                position++;
                return new TextExpression(token.Text, token.Line);
            case CSharpTokenKind.Name:
                return NameOrCall();
        }

        if (token.Is("("))
        {
            if (tokens[position + 1].Is(")") && tokens[position + 2].Is("=>"))
                return Lambda();

            position++;
            var inner = Expression();
            Expect(")");
            return inner;
        }

        throw Error($"'{token.Text}' cannot be used here.");
    }

    private Expression NameOrCall()
    {
        var line = Current.Line;
        var name = ExpectName();

        if (name is True or False)
            return new NumberExpression(name == True ? 1 : 0, line);

        if (Current.Is("."))
        {
            position++;
            var member = ExpectName();

            if (!CSharpNames.Prefixes.TryGetValue(name, out var prefix))
                throw new ScriptCompileException($"{name}.{member} is not a script value. Use NormalInput, Input, MoveCondition, Upon, HitAnimation or Slot.", line);

            return new NameExpression(CSharpNames.Canonical(member, prefix), line);
        }

        if (!Current.Is("("))
            return new NameExpression(name, line);

        position++;
        var arguments = new List<Expression>();

        while (!Current.Is(")"))
        {
            arguments.Add(Expression());

            if (!Current.Is(")"))
                Expect(",");
        }

        position++;

        if (name == NameOf)
            return NameOfFunction(arguments, line);

        return new CallExpression(name, arguments, line);
    }

    private static TextExpression NameOfFunction(List<Expression> arguments, int line) =>
        arguments is [NameExpression function]
            ? new TextExpression(CSharpNames.FunctionName(function.Name), line)
            : throw new ScriptCompileException("nameof needs the name of a state or subroutine.", line);

    private LambdaExpression Lambda()
    {
        var line = Current.Line;
        position += 3;
        var (body, endLine) = Block();
        return new LambdaExpression(body, line, endLine);
    }

    private void SkipAttributes() => Attributes();

    private void SkipModifiers()
    {
        while (Current.Kind == CSharpTokenKind.Name && Modifiers.Contains(Current.Text))
            position++;
    }

    private void QualifiedName()
    {
        ExpectName();

        while (Current.Is("."))
        {
            position++;
            ExpectName();
        }
    }

    private void SkipPast(string symbol)
    {
        while (!Current.Is(symbol) && Current.Kind != CSharpTokenKind.End)
            position++;

        Expect(symbol);
    }

    private string ExpectName()
    {
        if (Current.Kind != CSharpTokenKind.Name)
            throw Error($"A name was expected here, not '{Current.Text}'.");

        return tokens[position++].Text;
    }

    private void Expect(string symbol)
    {
        if (!Current.Is(symbol))
            throw Error($"'{symbol}' was expected here.");

        position++;
    }

    private ScriptCompileException Error(string message) => new(message, Current.Line);
}

public sealed record LambdaExpression(IReadOnlyList<Statement> Body, int Line, int EndLine) : Expression(Line);
