namespace BBTAG_IM.Core.Scripting;

public enum ArgumentKind
{
    Integer,
    Unsigned,
    Text,
}

public readonly record struct CommandArgument(ArgumentKind Kind, int Length)
{
    public static readonly CommandArgument Integer = new(ArgumentKind.Integer, sizeof(int));

    public static readonly CommandArgument Unsigned = new(ArgumentKind.Unsigned, sizeof(uint));

    public static CommandArgument Text(int length) => new(ArgumentKind.Text, length);
}
