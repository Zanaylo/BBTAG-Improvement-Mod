namespace BBTAG_IM.Core.Scripting.CSharp;

public static class CSharpNames
{
    public const string NormalInput = "NormalInput";
    public const string Input = "Input";
    public const string MoveCondition = "MoveCondition";
    public const string Upon = "Upon";
    public const string HitAnimation = "HitAnimation";
    public const string Slot = "Slot";

    private const char Verbatim = '@';
    private const char MemberDigitMark = '_';

    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
        "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    ];

    public static IReadOnlyDictionary<string, string> Prefixes { get; } = new Dictionary<string, string>
    {
        [NormalInput] = InputNames.Prefix,
        [Input] = InputNames.Prefix,
        [MoveCondition] = "MOVE_",
        [Upon] = ScriptDatabase.UponPrefix,
        [HitAnimation] = string.Empty,
        [Slot] = ScriptDatabase.SlotPrefix,
    };

    public static string? Identifier(string name)
    {
        var mangled = FunctionNames.Mangle(name);

        if (!IsPlainIdentifier(mangled) || FunctionNames.FromIdentifier(mangled) != name)
            return null;

        return Escaped(mangled);
    }

    public static string FunctionName(string identifier) => FunctionNames.FromIdentifier(identifier.TrimStart(Verbatim));

    public static string? Member(string canonical, string prefix)
    {
        if (!canonical.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || canonical.Length == prefix.Length)
            return null;

        var rest = canonical[prefix.Length..];
        var member = char.IsAsciiDigit(rest[0]) ? MemberDigitMark + rest : rest;

        return IsPlainIdentifier(member) ? Escaped(member) : null;
    }

    public static string Canonical(string member, string prefix)
    {
        var name = member.TrimStart(Verbatim);

        if (name.Length > 1 && name[0] == MemberDigitMark && char.IsAsciiDigit(name[1]))
            name = name[1..];

        return prefix + name;
    }

    public static string Escaped(string identifier) => Keywords.Contains(identifier) ? Verbatim + identifier : identifier;

    public static bool IsPlainIdentifier(string text) =>
        text.Length > 0
        && (char.IsAsciiLetter(text[0]) || text[0] == '_')
        && text.All(letter => char.IsAsciiLetterOrDigit(letter) || letter == '_');
}
