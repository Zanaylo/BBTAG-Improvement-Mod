namespace BBTAG_IM.Core.Scripting;

public static class CommandFormat
{
    private const int IdLength = sizeof(int);

    public static IReadOnlyList<CommandArgument> Parse(string format)
    {
        var arguments = new List<CommandArgument>();
        var count = 0;

        foreach (var letter in format)
        {
            if (char.IsAsciiDigit(letter))
            {
                count = count * 10 + (letter - '0');
                continue;
            }

            AddArguments(arguments, letter, count == 0 ? 1 : count, format);
            count = 0;
        }

        if (count != 0)
            throw new InvalidScriptDatabaseException($"The command format \"{format}\" ends with a number.");

        return arguments;
    }

    public static IReadOnlyList<CommandArgument> ForSize(int size)
    {
        var length = size - IdLength;

        if (length < 0)
            throw new InvalidScriptDatabaseException($"A command cannot be {size} bytes long.");

        if (length % sizeof(int) == 0)
            return Enumerable.Repeat(CommandArgument.Integer, length / sizeof(int)).ToArray();

        return [CommandArgument.Text(length)];
    }

    public static int SizeOf(IReadOnlyList<CommandArgument> arguments) => IdLength + arguments.Sum(argument => argument.Length);

    private static void AddArguments(List<CommandArgument> arguments, char letter, int count, string format)
    {
        switch (letter)
        {
            case 'i':
                arguments.AddRange(Enumerable.Repeat(CommandArgument.Integer, count));
                return;
            case 'I':
                arguments.AddRange(Enumerable.Repeat(CommandArgument.Unsigned, count));
                return;
            case 's':
                arguments.Add(CommandArgument.Text(count));
                return;
            default:
                throw new InvalidScriptDatabaseException($"The command format \"{format}\" uses '{letter}', which is not supported.");
        }
    }
}
