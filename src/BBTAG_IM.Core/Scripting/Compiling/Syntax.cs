namespace BBTAG_IM.Core.Scripting.Compiling;

public abstract record Expression(int Line);

public sealed record NameExpression(string Name, int Line) : Expression(Line);

public sealed record NumberExpression(long Value, int Line) : Expression(Line);

public sealed record TextExpression(string Text, int Line) : Expression(Line);

public sealed record UnaryExpression(string Operator, Expression Operand, int Line) : Expression(Line);

public sealed record BinaryExpression(string Operator, Expression Left, Expression Right, int Line) : Expression(Line);

public sealed record CallExpression(string Name, IReadOnlyList<Expression> Arguments, int Line) : Expression(Line);

public abstract record Statement(int Line);

public sealed record ExpressionStatement(Expression Value, int Line) : Statement(Line);

public sealed record AssignStatement(NameExpression Target, Expression Value, int Line) : Statement(Line);

public sealed record IfStatement(Expression Test, IReadOnlyList<Statement> Body, IReadOnlyList<Statement> Otherwise, int Line, int ElseLine) : Statement(Line);

public sealed record PassStatement(int Line) : Statement(Line);

public sealed record Decorator(string Name, IReadOnlyList<Expression> Arguments, int Line);

public sealed record FunctionStatement(
    string Name,
    IReadOnlyList<Decorator> Decorators,
    IReadOnlyList<string> Parameters,
    IReadOnlyList<Statement> Body,
    int Line,
    int EndLine) : Statement(Line);
