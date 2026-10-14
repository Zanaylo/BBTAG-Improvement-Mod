namespace BBTAG_IM.Core.Scripting.Compiling;

public static class ConstantFolding
{
    public static long? Evaluate(Expression expression) => expression switch
    {
        NumberExpression number => number.Value,
        UnaryExpression unary => Unary(unary),
        BinaryExpression binary => Binary(binary),
        _ => null,
    };

    public static bool IsConstant(Expression expression) => Evaluate(expression) is not null;

    private static long? Unary(UnaryExpression unary)
    {
        if (Evaluate(unary.Operand) is not { } operand)
            return null;

        return unary.Operator switch
        {
            "-" => -operand,
            "+" => operand,
            "~" => ~operand,
            _ => null,
        };
    }

    private static long? Binary(BinaryExpression binary)
    {
        if (Evaluate(binary.Left) is not { } left || Evaluate(binary.Right) is not { } right)
            return null;

        return binary.Operator switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" when right != 0 => left / right,
            "%" when right != 0 => left % right,
            "&" => left & right,
            "|" => left | right,
            _ => null,
        };
    }
}
