namespace BBTAG_IM.Core.Scripting;

public static class ScriptKeywords
{
    public const string Def = "def";
    public const string If = "if";
    public const string Elif = "elif";
    public const string Else = "else";
    public const string Not = "not";
    public const string And = "and";
    public const string Or = "or";
    public const string Pass = "pass";

    private static readonly HashSet<string> All =
    [
        "False", "None", "True", And, "as", "assert", "async", "await", "break", "class", "continue", Def, "del",
        Elif, Else, "except", "finally", "for", "from", "global", If, "import", "in", "is", "lambda", "nonlocal",
        Not, Or, Pass, "raise", "return", "try", "while", "with", "yield",
    ];

    public static bool Contains(string word) => All.Contains(word);
}
