using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting.Compiling;

public sealed class ScriptLexer
{
    private const int TabWidth = 8;
    private const string TripleSingle = "'''";
    private const string TripleDouble = "\"\"\"";

    private static readonly string[] Symbols =
        ["==", "!=", "<=", ">=", "(", ")", ",", ":", "=", "@", "+", "-", "*", "/", "%", "&", "|", "~", "<", ">", "[", "]"];

    private readonly string source;
    private readonly List<Token> tokens = [];
    private readonly Stack<int> indents = new([0]);
    private int position;
    private int line = 1;
    private int brackets;

    private ScriptLexer(string source)
    {
        this.source = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }

    public static IReadOnlyList<Token> Tokenize(string source) => new ScriptLexer(source).Run();

    private List<Token> Run()
    {
        var lineStart = true;

        while (position < source.Length)
        {
            if (lineStart && brackets == 0)
            {
                lineStart = SkippedBlankLine();

                if (lineStart)
                    continue;
            }

            var letter = source[position];

            if (letter == '\n')
            {
                position++;

                if (brackets == 0)
                {
                    tokens.Add(new Token(TokenKind.NewLine, "\n", line));
                    lineStart = true;
                }

                line++;
                continue;
            }

            if (letter is ' ' or '\t')
            {
                position++;
                continue;
            }

            if (letter == '#')
            {
                SkipComment();
                continue;
            }

            if (letter == '\\' && Peek(1) == '\n')
            {
                position += 2;
                line++;
                continue;
            }

            ReadToken(letter);
        }

        if (tokens.Count > 0 && tokens[^1].Kind != TokenKind.NewLine)
            tokens.Add(new Token(TokenKind.NewLine, "\n", line));

        while (indents.Count > 1)
        {
            indents.Pop();
            tokens.Add(new Token(TokenKind.Dedent, string.Empty, line));
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, line));
        return tokens;
    }

    private bool SkippedBlankLine()
    {
        var column = 0;

        while (position < source.Length && source[position] is ' ' or '\t')
        {
            column = source[position] == '\t' ? (column / TabWidth + 1) * TabWidth : column + 1;
            position++;
        }

        if (position >= source.Length || source[position] is '\n' or '#')
        {
            if (position < source.Length && source[position] == '#')
                SkipComment();

            if (position < source.Length)
            {
                position++;
                line++;
            }

            return true;
        }

        if (column > indents.Peek())
        {
            indents.Push(column);
            tokens.Add(new Token(TokenKind.Indent, string.Empty, line));
            return false;
        }

        while (column < indents.Peek())
        {
            indents.Pop();
            tokens.Add(new Token(TokenKind.Dedent, string.Empty, line));
        }

        if (column != indents.Peek())
            throw new ScriptCompileException("This line's indentation does not match any outer block.", line);

        return false;
    }

    private void ReadToken(char letter)
    {
        if (char.IsAsciiLetter(letter) || letter == '_')
        {
            ReadName();
            return;
        }

        if (char.IsAsciiDigit(letter))
        {
            ReadNumber();
            return;
        }

        if (letter is '\'' or '"')
        {
            ReadText(letter);
            return;
        }

        foreach (var symbol in Symbols)
        {
            if (string.CompareOrdinal(source, position, symbol, 0, symbol.Length) != 0)
                continue;

            if (symbol is "(" or "[")
                brackets++;
            else if (symbol is ")" or "]")
                brackets = Math.Max(0, brackets - 1);

            tokens.Add(new Token(TokenKind.Symbol, symbol, line));
            position += symbol.Length;
            return;
        }

        throw new ScriptCompileException($"\"{letter}\" is not allowed here.", line);
    }

    private void ReadName()
    {
        var start = position;

        while (position < source.Length && (char.IsAsciiLetterOrDigit(source[position]) || source[position] == '_'))
            position++;

        tokens.Add(new Token(TokenKind.Name, source[start..position], line));
    }

    private void ReadNumber()
    {
        var start = position;
        var hex = source[position] == '0' && Peek(1) is 'x' or 'X';

        if (hex)
            position += 2;

        while (position < source.Length && (char.IsAsciiLetterOrDigit(source[position]) || source[position] == '_'))
            position++;

        var text = source[start..position];
        var digits = (hex ? text[2..] : text).Replace("_", string.Empty, StringComparison.Ordinal);

        if (position < source.Length && source[position] == '.')
            throw new ScriptCompileException($"Scripts only use whole numbers, not \"{text}.\".", line);

        var parsed = hex
            ? long.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var number)
            : long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out number);

        if (!parsed || digits.Length == 0)
            throw new ScriptCompileException($"\"{text}\" is not a number.", line);

        tokens.Add(new Token(TokenKind.Number, text, line, number));
    }

    private void ReadText(char quote)
    {
        var startLine = line;
        var triple = quote == '\'' ? TripleSingle : TripleDouble;
        var isTriple = string.CompareOrdinal(source, position, triple, 0, triple.Length) == 0;
        position += isTriple ? triple.Length : 1;
        var text = new StringBuilder();

        while (true)
        {
            if (position >= source.Length)
                throw new ScriptCompileException("This text is never closed with a quote.", startLine);

            var letter = source[position];

            if (isTriple && string.CompareOrdinal(source, position, triple, 0, triple.Length) == 0)
            {
                position += triple.Length;
                break;
            }

            if (!isTriple && letter == quote)
            {
                position++;
                break;
            }

            if (letter == '\n')
            {
                if (!isTriple)
                    throw new ScriptCompileException("This text is never closed with a quote.", startLine);

                line++;
            }

            if (letter == '\\')
            {
                text.Append(Escape());
                continue;
            }

            text.Append(letter);
            position++;
        }

        tokens.Add(new Token(TokenKind.Text, text.ToString(), startLine));
    }

    private char Escape()
    {
        var letter = Peek(1);
        position += 2;

        switch (letter)
        {
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case '0':
                return '\0';
            case 'x':
                if (position + 2 > source.Length || !byte.TryParse(source.AsSpan(position, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
                    throw new ScriptCompileException("\\x must be followed by two hex digits.", line);
                position += 2;
                return (char)value;
            case '\\' or '\'' or '"':
                return letter;
            default:
                throw new ScriptCompileException($"\"\\{letter}\" is not a known escape.", line);
        }
    }

    private void SkipComment()
    {
        while (position < source.Length && source[position] != '\n')
            position++;
    }

    private char Peek(int distance) => position + distance < source.Length ? source[position + distance] : '\0';
}
