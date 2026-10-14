namespace BBTAG_IM.Core.Scripting;

public enum ScriptOperator
{
    Add = 0,
    Subtract = 1,
    Multiply = 2,
    Divide = 3,
    Modulo = 4,
    And = 5,
    Or = 6,
    BitAnd = 7,
    BitOr = 8,
    Equal = 9,
    Greater = 10,
    Less = 11,
    GreaterOrEqual = 12,
    LessOrEqual = 13,
    NotBoth = 14,
    NotEqual = 15,
}

public static class ScriptOperators
{
    private static readonly Dictionary<ScriptOperator, string> Symbols = new()
    {
        [ScriptOperator.Add] = "+",
        [ScriptOperator.Subtract] = "-",
        [ScriptOperator.Multiply] = "*",
        [ScriptOperator.Divide] = "/",
        [ScriptOperator.Modulo] = "%",
        [ScriptOperator.And] = "and",
        [ScriptOperator.Or] = "or",
        [ScriptOperator.BitAnd] = "&",
        [ScriptOperator.BitOr] = "|",
        [ScriptOperator.Equal] = "==",
        [ScriptOperator.Greater] = ">",
        [ScriptOperator.Less] = "<",
        [ScriptOperator.GreaterOrEqual] = ">=",
        [ScriptOperator.LessOrEqual] = "<=",
        [ScriptOperator.NotEqual] = "!=",
    };

    private static readonly Dictionary<string, ScriptOperator> BySymbol = Symbols.ToDictionary(pair => pair.Value, pair => pair.Key);

    public static bool IsKnown(int value) => Enum.IsDefined((ScriptOperator)value);

    public static string Format(ScriptOperator operation, string left, string right) =>
        operation == ScriptOperator.NotBoth ? $"~({left} & {right})" : $"{left} {Symbols[operation]} {right}";

    public static ScriptOperator? FromSymbol(string symbol) => BySymbol.TryGetValue(symbol, out var operation) ? operation : null;
}
