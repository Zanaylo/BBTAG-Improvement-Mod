using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.Scripting.EditorHelp;

public static class ScriptStubs
{
    public const string FileName = "__builtins__.pyi";
    public const string EditorSettings = "# pyright: reportRedeclaration=false, reportUnboundVariable=false, reportPossiblyUnboundVariable=false";

    private const int SlotCount = 256;
    private const int UponCount = 128;
    private const int InputCount = 0x200;
    private const string Indent = "    ";
    private const string Quotes = "\"\"\"";
    private const string Paragraph = "\n\n";

    private const string Header = """
        These are the commands and names BBTAG scripts can use, for the editor to show what each one does.
        The BBTAG Improvement Mod writes this file again every time a script is opened: changes to it are lost.
        """;

    private static readonly (string Name, string Doc)[] Decorators =
    [
        (ScriptDecorators.State, "@State: a state the game can enter (a move, a reaction, an effect). @State('name') gives a name that is not a Python name."),
        (ScriptDecorators.Subroutine, "@Subroutine: a block of commands that states run with callSubroutine."),
        (ScriptDecorators.RawState, "@RawState: a state written one command per line, closing commands included."),
        (ScriptDecorators.RawSubroutine, "@RawSubroutine: a subroutine written one command per line, closing commands included."),
        (ScriptDecorators.StateRegister, "@StateRegister: registers a move inside MatchInit. The parameter is its input, such as INPUT_5A or INPUT_SPECIALMOVE."),
    ];

    public static string Write(ScriptDatabase database, ScriptDocs docs, ScriptUsage? usage = null)
    {
        usage ??= ScriptUsage.Empty;
        var stub = new StringBuilder();
        stub.Append(Docstring(Header)).Append("\n\nfrom typing import Any, Callable, overload\n\n");
        stub.Append("StateName = str | Callable[..., Any]\n");
        stub.Append("UponName = int | Callable[..., Any]\n");

        foreach (var (name, doc) in Decorators)
            stub.Append("\n\ndef ").Append(name).Append("(*arguments: Any) -> Any:\n").Append(Indent).Append(Docstring(doc)).Append('\n');

        foreach (var command in database.Commands.OrderBy(command => command.Id))
            AppendCommand(stub, database, command, HelpText.Command(command, docs.Command(command.Id), usage));

        stub.Append('\n');

        foreach (var command in database.Commands.OrderBy(command => command.Id))
            AppendAlias(stub, database, command);

        AppendConstants(stub, Inputs(database, usage));
        AppendConstants(stub, Values(database.MoveConditions, hex: true, usage, UsageKind.MoveCondition));
        AppendConstants(stub, Values(database.HitAnimations, hex: false, usage, UsageKind.HitAnimation));
        AppendConstants(stub, Upons(database, docs, usage));
        AppendConstants(stub, Slots(database, docs));

        return stub.ToString();
    }

    private static void AppendCommand(StringBuilder stub, ScriptDatabase database, CommandDefinition command, string text)
    {
        var name = database.CallName(command);
        var returns = ScriptCommands.WritesSlotZero.Contains(command.Id) ? "int" : "None";
        var docstring = Docstring(text);
        var forms = command.HasSlotArguments ? [PairedParameters(command), RawParameters(command)] : new[] { PairedParameters(command) };

        foreach (var parameters in forms)
        {
            stub.Append("\n\n");

            if (forms.Length > 1)
                stub.Append("@overload\n");

            stub.Append("def ").Append(name).Append('(').Append(Signature(parameters)).Append(") -> ").Append(returns).Append(":\n");
            stub.Append(Indent).Append(docstring).Append('\n');
        }
    }

    private static void AppendAlias(StringBuilder stub, ScriptDatabase database, CommandDefinition command)
    {
        var name = database.CallName(command);
        var unknown = CommandDefinition.UnknownName(command.Id);

        if (name != unknown)
            stub.Append('\n').Append(unknown).Append(" = ").Append(name);
    }

    private static void AppendConstants(StringBuilder stub, IEnumerable<(string Name, string Doc)> constants)
    {
        stub.Append('\n');

        foreach (var (name, doc) in constants)
        {
            if (FunctionNames.IsIdentifier(name))
                stub.Append('\n').Append(name).Append(": int\n").Append(Docstring(doc)).Append('\n');
        }
    }

    private static IEnumerable<(string Name, string Doc)> Inputs(ScriptDatabase database, ScriptUsage usage)
    {
        var normalNames = database.Inputs.Normal.Named.Select(value => value.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var named = database.Inputs.Named.ToList();
        var names = named.Select(value => value.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var numbers = Enumerable.Range(0, InputCount)
            .Select(value => new NamedValue(value, InputNames.Fallback(value, hex: true), "No name is known for this input yet."))
            .Where(value => !names.Contains(value.Name));

        return named.Concat(numbers).Select(value => (value.Name, HelpText.Value(
            value.Value,
            hex: true,
            value.Description,
            usage,
            new UsageKey(normalNames.Contains(value.Name) ? UsageKind.NormalInput : UsageKind.MoveInput, value.Value))));
    }

    private static IEnumerable<(string Name, string Doc)> Values(NameTable table, bool hex, ScriptUsage usage, UsageKind kind) =>
        table.Named.Select(value => (value.Name, HelpText.Value(value.Value, hex, table.DescriptionOf(value.Value), usage, new UsageKey(kind, value.Value))));

    private static IEnumerable<(string Name, string Doc)> Upons(ScriptDatabase database, ScriptDocs docs, ScriptUsage usage) =>
        Numbered(database.Upons, UponCount, ScriptDatabase.UponPrefix, upon => HelpText.Upon(upon, docs, usage));

    private static IEnumerable<(string Name, string Doc)> Slots(ScriptDatabase database, ScriptDocs docs) =>
        Numbered(database.Slots, SlotCount, ScriptDatabase.SlotPrefix, slot => HelpText.Slot(slot, docs));

    private static IEnumerable<(string Name, string Doc)> Numbered(NameTable names, int count, string prefix, Func<int, string> doc) =>
        names.Named.Select(value => (prefix + value.Name, doc(value.Value)))
            .Concat(Enumerable.Range(0, count).Select(number => (prefix + Number(number), doc(number))))
            .DistinctBy(constant => constant.Item1);

    private static List<(string Name, string Type)> PairedParameters(CommandDefinition command)
    {
        var parameters = new List<(string, string)>();
        var visible = 0;

        for (var index = 0; index < command.Arguments.Count; index++)
        {
            if (command.SlotTypeArguments.Contains(index))
            {
                parameters.Add(("value", "int"));
                index++;
            }
            else
            {
                parameters.Add(Parameter(command, command.Arguments[index], visible));
            }

            visible++;
        }

        return parameters;
    }

    private static List<(string Name, string Type)> RawParameters(CommandDefinition command) =>
        command.Arguments.Select(argument => argument.Kind == ArgumentKind.Text ? ("text", "str") : ("value", "int")).ToList();

    private static (string Name, string Type) Parameter(CommandDefinition command, CommandArgument argument, int visible)
    {
        if (argument.Kind == ArgumentKind.Text)
            return ValueKinds.IsStateReference(command.Id, visible) ? ("state", "StateName") : ("text", "str");

        return ValueKinds.Of(command.Id, visible) switch
        {
            ValueKind.Input => ("input", "int"),
            ValueKind.Upon => ("upon", "UponName"),
            ValueKind.HitAnimation => ("animation", "int"),
            ValueKind.MoveCondition => ("condition", "int"),
            _ => ("value", "int"),
        };
    }

    private static string Signature(List<(string Name, string Type)> parameters)
    {
        if (parameters.Count == 0)
            return string.Empty;

        var repeated = parameters.GroupBy(parameter => parameter.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        var counters = new Dictionary<string, int>();
        var written = new List<string>();

        foreach (var (name, type) in parameters)
        {
            var unique = name;

            if (repeated.Contains(name))
            {
                counters[name] = counters.GetValueOrDefault(name) + 1;
                unique = name + Number(counters[name]);
            }

            written.Add($"{unique}: {type}");
        }

        return string.Join(", ", written) + ", /";
    }

    private static string Docstring(string text)
    {
        var escaped = text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        return Quotes + escaped.ReplaceLineEndings("\n") + Quotes;
    }

    private static string Number(int value) => HelpText.Number(value);
}
