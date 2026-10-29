using System.Globalization;
using System.Text;
using BBTAG_IM.Core.Scripting.Compiling;

namespace BBTAG_IM.Core.Scripting.CSharp;

public enum CSharpTokenKind
{
    Name,
    Number,
    Text,
    Symbol,
    End,
}

public sealed record CSharpToken(CSharpTokenKind Kind, string Text, long Number, int Line)
{
    public bool Is(string symbol) => Kind == CSharpTokenKind.Symbol && Text == symbol;

    public bool IsName(string name) => Kind == CSharpTokenKind.Name && Text == name;
}

public static class CSharpLexer
{
    private const string HexPrefix = "0x";

    private static readonly string[] Symbols =
    [
        "=>", "==", "!=", "<=", ">=", "&&", "||", "{", "}", "(", ")", "[", "]", ";", ",", ".", "=", "<", ">", "+", "-", "*",
        "/", "%", "&", "|", "!", "~", ":",
    ];

    public static IReadOnlyList<CSharpToken> Tokenize(string source)
    {
        var tokens = new List<CSharpToken>();
        var line = 1;
        var position = 0;

        while (position < source.Length)
        {
            var letter = source[position];

            if (letter == '\n')
            {
                line++;
                position++;
                continue;
            }

            if (char.IsWhiteSpace(letter))
            {
                position++;
                continue;
            }

            if (StartsWith(source, position, "//"))
            {
                position = LineEnd(source, position);
                continue;
            }

            if (StartsWith(source, position, "/*"))
            {
                position = BlockCommentEnd(source, position, ref line);
                continue;
            }

            if (letter == '"')
            {
                tokens.Add(ReadText(source, ref position, line));
                continue;
            }

            if (char.IsAsciiDigit(letter))
            {
                tokens.Add(ReadNumber(source, ref position, line));
                continue;
            }

            if (letter == '@' || char.IsAsciiLetter(letter) || letter == '_')
            {
                tokens.Add(ReadName(source, ref position, line));
                continue;
            }

            var symbol = Symbols.FirstOrDefault(candidate => StartsWith(source, position, candidate))
                ?? throw new ScriptCompileException($"'{letter}' cannot be used here.", line);
            tokens.Add(new CSharpToken(CSharpTokenKind.Symbol, symbol, 0, line));
            position += symbol.Length;
        }

        tokens.Add(new CSharpToken(CSharpTokenKind.End, string.Empty, 0, line));
        return tokens;
    }

    private static CSharpToken ReadName(string source, ref int position, int line)
    {
        var start = position;

        if (source[position] == '@')
            position++;

        while (position < source.Length && (char.IsAsciiLetterOrDigit(source[position]) || source[position] == '_'))
            position++;

        var name = source[start..position].TrimStart('@');

        if (name.Length == 0)
            throw new ScriptCompileException("'@' must be followed by a name.", line);

        return new CSharpToken(CSharpTokenKind.Name, name, 0, line);
    }

    private static CSharpToken ReadNumber(string source, ref int position, int line)
    {
        var start = position;
        var hex = StartsWith(source, position, HexPrefix) || StartsWith(source, position, "0X");

        if (hex)
            position += HexPrefix.Length;

        while (position < source.Length && (hex ? char.IsAsciiHexDigit(source[position]) : char.IsAsciiDigit(source[position])))
            position++;

        var digits = source[(hex ? start + HexPrefix.Length : start)..position];

        while (position < source.Length && "uUlL".Contains(source[position]))
            position++;

        var parsed = hex
            ? long.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var number)
            : long.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);

        if (!parsed)
            throw new ScriptCompileException($"{source[start..position]} is not a number scripts can use.", line);

        return new CSharpToken(CSharpTokenKind.Number, source[start..position], number, line);
    }

    private static CSharpToken ReadText(string source, ref int position, int line)
    {
        var text = new StringBuilder();
        position++;

        while (position < source.Length && source[position] != '"')
        {
            if (source[position] == '\n')
                throw new ScriptCompileException("This text has no closing quote.", line);

            if (source[position] != '\\')
            {
                text.Append(source[position++]);
                continue;
            }

            text.Append(Escape(source, ref position, line));
        }

        if (position >= source.Length)
            throw new ScriptCompileException("This text has no closing quote.", line);

        position++;
        return new CSharpToken(CSharpTokenKind.Text, text.ToString(), 0, line);
    }

    private static char Escape(string source, ref int position, int line)
    {
        if (position + 1 >= source.Length)
            throw new ScriptCompileException("This text ends with a lone backslash.", line);

        var code = source[position + 1];
        position += 2;

        switch (code)
        {
            case '\\':
                return '\\';
            case '"':
                return '"';
            case '\'':
                return '\'';
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case '0':
                return '\0';
            case 'u':
                return Unicode(source, ref position, line);
            default:
                throw new ScriptCompileException($"\\{code} is not an escape scripts understand. Use \\u00XX for any letter.", line);
        }
    }

    private static char Unicode(string source, ref int position, int line)
    {
        const int Digits = 4;

        if (position + Digits > source.Length || !int.TryParse(source.AsSpan(position, Digits), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            throw new ScriptCompileException("\\u needs four hex digits.", line);

        position += Digits;
        return (char)value;
    }

    private static int LineEnd(string source, int position)
    {
        var end = source.IndexOf('\n', position);
        return end < 0 ? source.Length : end;
    }

    private static int BlockCommentEnd(string source, int position, ref int line)
    {
        var end = source.IndexOf("*/", position + 2, StringComparison.Ordinal);
        var stop = end < 0 ? source.Length : end + 2;
        line += source.AsSpan(position, stop - position).Count('\n');
        return stop;
    }

    private static bool StartsWith(string source, int position, string text) =>
        string.CompareOrdinal(source, position, text, 0, text.Length) == 0;
}
