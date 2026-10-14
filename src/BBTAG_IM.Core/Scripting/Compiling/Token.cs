namespace BBTAG_IM.Core.Scripting.Compiling;

public enum TokenKind
{
    Name,
    Number,
    Text,
    Symbol,
    NewLine,
    Indent,
    Dedent,
    End,
}

public sealed record Token(TokenKind Kind, string Text, int Line, long Number = 0)
{
    public bool Is(TokenKind kind, string text) => Kind == kind && Text == text;

    public bool IsSymbol(string symbol) => Is(TokenKind.Symbol, symbol);

    public bool IsKeyword(string keyword) => Is(TokenKind.Name, keyword);

    public string Describe() => Kind switch
    {
        TokenKind.NewLine => "the end of the line",
        TokenKind.Indent => "an indented line",
        TokenKind.Dedent => "the end of the block",
        TokenKind.End => "the end of the file",
        _ => $"\"{Text}\"",
    };
}
