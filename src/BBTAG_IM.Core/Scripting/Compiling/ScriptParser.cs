namespace BBTAG_IM.Core.Scripting.Compiling;

public sealed class ScriptParser
{
    private static readonly string[] ComparisonSymbols = ["==", "!=", "<", ">", "<=", ">="];

    private readonly IReadOnlyList<Token> tokens;
    private int position;

    private ScriptParser(IReadOnlyList<Token> tokens)
    {
        this.tokens = tokens;
    }

    public static IReadOnlyList<FunctionStatement> Parse(string source) => new ScriptParser(ScriptLexer.Tokenize(source)).Module();

    private Token Current => tokens[position];

    private List<FunctionStatement> Module()
    {
        var functions = new List<FunctionStatement>();

        while (Current.Kind != TokenKind.End)
        {
            if (Current.Kind == TokenKind.NewLine)
            {
                position++;
                continue;
            }

            if (Current.Kind == TokenKind.Text)
            {
                SkipTextLine();
                continue;
            }

            if (!Current.IsSymbol("@") && !Current.IsKeyword(ScriptKeywords.Def))
                throw Error("Only states and subroutines can be written outside of a function.");

            functions.Add(Function());
        }

        return functions;
    }

    private FunctionStatement Function()
    {
        var decorators = new List<Decorator>();

        while (Current.IsSymbol("@"))
            decorators.Add(DecoratorLine());

        var line = Expect(TokenKind.Name, ScriptKeywords.Def).Line;
        var name = ExpectName().Text;
        ExpectSymbol("(");
        var parameters = new List<string>();

        while (!Current.IsSymbol(")"))
        {
            parameters.Add(ExpectName().Text);

            if (!Current.IsSymbol(")"))
                ExpectSymbol(",");
        }

        ExpectSymbol(")");
        ExpectSymbol(":");
        var body = Block();
        var endLine = body.Count > 0 ? LastLine(body[^1]) : line;
        return new FunctionStatement(name, decorators, parameters, body, line, endLine);
    }

    private Decorator DecoratorLine()
    {
        var line = ExpectSymbol("@").Line;
        var name = ExpectName().Text;
        var arguments = Current.IsSymbol("(") ? CallArguments() : [];
        Expect(TokenKind.NewLine);
        return new Decorator(name, arguments, line);
    }

    private List<Statement> Block()
    {
        if (Current.Kind != TokenKind.NewLine)
        {
            var statement = SimpleStatement();
            Expect(TokenKind.NewLine);
            return [statement];
        }

        Expect(TokenKind.NewLine);
        Expect(TokenKind.Indent);
        var statements = new List<Statement>();

        while (Current.Kind != TokenKind.Dedent && Current.Kind != TokenKind.End)
        {
            if (Current.Kind == TokenKind.NewLine)
            {
                position++;
                continue;
            }

            if (StatementOrNothing() is { } statement)
                statements.Add(statement);
        }

        if (Current.Kind == TokenKind.Dedent)
            position++;

        return statements;
    }

    private Statement? StatementOrNothing()
    {
        if (Current.IsSymbol("@") || Current.IsKeyword(ScriptKeywords.Def))
            return Function();

        if (Current.IsKeyword(ScriptKeywords.If))
            return If();

        if (Current.Kind == TokenKind.Text && tokens[position + 1].Kind == TokenKind.NewLine)
        {
            SkipTextLine();
            return null;
        }

        var statement = SimpleStatement();
        Expect(TokenKind.NewLine);
        return statement;
    }

    private IfStatement If()
    {
        var line = Current.Line;
        position++;
        var test = Expression();
        ExpectSymbol(":");
        var body = Block();

        if (Current.IsKeyword(ScriptKeywords.Elif))
        {
            var elseLine = Current.Line;
            return new IfStatement(test, body, [If()], line, elseLine);
        }

        if (!Current.IsKeyword(ScriptKeywords.Else))
            return new IfStatement(test, body, [], line, line);

        var otherwiseLine = Current.Line;
        position++;
        ExpectSymbol(":");
        return new IfStatement(test, body, Block(), line, otherwiseLine);
    }

    private Statement SimpleStatement()
    {
        var line = Current.Line;

        if (Current.IsKeyword(ScriptKeywords.Pass))
        {
            position++;
            return new PassStatement(line);
        }

        var value = Expression();

        if (!Current.IsSymbol("="))
            return new ExpressionStatement(value, line);

        if (value is not NameExpression target)
            throw Error("Only a SLOT can be given a value.");

        position++;
        return new AssignStatement(target, Expression(), line);
    }

    private Expression Expression() => Or();

    private Expression Or()
    {
        var left = And();

        while (Current.IsKeyword(ScriptKeywords.Or))
        {
            var line = Current.Line;
            position++;
            left = new BinaryExpression(ScriptKeywords.Or, left, And(), line);
        }

        return left;
    }

    private Expression And()
    {
        var left = Not();

        while (Current.IsKeyword(ScriptKeywords.And))
        {
            var line = Current.Line;
            position++;
            left = new BinaryExpression(ScriptKeywords.And, left, Not(), line);
        }

        return left;
    }

    private Expression Not()
    {
        if (!Current.IsKeyword(ScriptKeywords.Not))
            return Comparison();

        var line = Current.Line;
        position++;
        return new UnaryExpression(ScriptKeywords.Not, Not(), line);
    }

    private Expression Comparison()
    {
        var left = BitOr();

        if (Current.Kind != TokenKind.Symbol || !ComparisonSymbols.Contains(Current.Text))
            return left;

        var symbol = Current;
        position++;
        var comparison = new BinaryExpression(symbol.Text, left, BitOr(), symbol.Line);

        if (Current.Kind == TokenKind.Symbol && ComparisonSymbols.Contains(Current.Text))
            throw Error("Compare two values at a time. Chained comparisons are not supported.");

        return comparison;
    }

    private Expression BitOr() => LeftAssociative(BitAnd, "|");

    private Expression BitAnd() => LeftAssociative(Arithmetic, "&");

    private Expression Arithmetic() => LeftAssociative(Term, "+", "-");

    private Expression Term() => LeftAssociative(Unary, "*", "/", "%");

    private Expression LeftAssociative(Func<Expression> operand, params string[] symbols)
    {
        var left = operand();

        while (Current.Kind == TokenKind.Symbol && symbols.Contains(Current.Text))
        {
            var symbol = Current;
            position++;
            left = new BinaryExpression(symbol.Text, left, operand(), symbol.Line);
        }

        return left;
    }

    private Expression Unary()
    {
        if (Current.Kind != TokenKind.Symbol || Current.Text is not ("-" or "+" or "~"))
            return Primary();

        var symbol = Current;
        position++;
        return new UnaryExpression(symbol.Text, Unary(), symbol.Line);
    }

    private Expression Primary()
    {
        var token = Current;

        switch (token.Kind)
        {
            case TokenKind.Number:
                position++;
                return new NumberExpression(token.Number, token.Line);
            case TokenKind.Text:
                position++;
                return new TextExpression(token.Text, token.Line);
            case TokenKind.Name when !ScriptKeywords.Contains(token.Text):
                position++;
                return Current.IsSymbol("(")
                    ? new CallExpression(token.Text, CallArguments(), token.Line)
                    : new NameExpression(token.Text, token.Line);
            case TokenKind.Symbol when token.Text == "(":
                position++;
                var inner = Expression();
                ExpectSymbol(")");
                return inner;
            default:
                throw Error($"Expected a value but found {token.Describe()}.");
        }
    }

    private List<Expression> CallArguments()
    {
        ExpectSymbol("(");
        var arguments = new List<Expression>();

        while (!Current.IsSymbol(")"))
        {
            arguments.Add(Expression());

            if (!Current.IsSymbol(")"))
                ExpectSymbol(",");
        }

        ExpectSymbol(")");
        return arguments;
    }

    private void SkipTextLine()
    {
        position++;
        Expect(TokenKind.NewLine);
    }

    private Token ExpectName()
    {
        if (Current.Kind != TokenKind.Name || ScriptKeywords.Contains(Current.Text))
            throw Error($"Expected a name but found {Current.Describe()}.");

        return tokens[position++];
    }

    private Token ExpectSymbol(string symbol) => Expect(TokenKind.Symbol, symbol);

    private Token Expect(TokenKind kind, string? text = null)
    {
        if (Current.Kind != kind || (text is not null && Current.Text != text))
            throw Error($"Expected {Describe(kind, text)} but found {Current.Describe()}.");

        return tokens[position++];
    }

    private static string Describe(TokenKind kind, string? text) =>
        text is not null && kind is TokenKind.Symbol or TokenKind.Name
            ? $"\"{text}\""
            : new Token(kind, text ?? string.Empty, 0).Describe();

    private ScriptCompileException Error(string message) => new(message, Current.Line);

    private static int LastLine(Statement statement) => statement switch
    {
        FunctionStatement function => function.EndLine,
        IfStatement conditional when conditional.Otherwise.Count > 0 => LastLine(conditional.Otherwise[^1]),
        IfStatement conditional when conditional.Body.Count > 0 => LastLine(conditional.Body[^1]),
        _ => statement.Line,
    };
}
