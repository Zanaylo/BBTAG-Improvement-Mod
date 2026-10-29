using System.Globalization;
using System.Security;
using System.Text;
using BBTAG_IM.Core.Scripting.EditorHelp;

namespace BBTAG_IM.Core.Scripting.CSharp;

public static class CSharpApi
{
    public const string FileName = "BBScript.g.cs";

    private const int SlotCount = 1024;
    private const string Indent = "    ";
    private const string ActionType = "Action";
    private const string BodyParameter = "body";

    private static readonly HashSet<int> BlockCommands = [ScriptCommands.Upon, ScriptCommands.ObjectFunction, ScriptCommands.StateRegister];

    private static readonly HashSet<string> ReservedNames =
    [
        CSharpDialect.ValueType, CSharpDialect.BaseClass, CSharpNames.NormalInput, CSharpNames.Input, CSharpNames.MoveCondition,
        CSharpNames.Upon, CSharpNames.HitAnimation, CSharpNames.Slot, ActionType, "State", "Subroutine", "RawState", "RawSubroutine",
    ];

    private const string Preamble = """
        #nullable disable
        using System;

        namespace BBScript;

        /// <summary>
        /// <para>[State] marks a state the game can enter: a move, a reaction, an effect.</para>
        /// <para>[State("name")] gives the game's name when it is not a C# name.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class StateAttribute : Attribute
        {
            public StateAttribute() { }

            public StateAttribute(string name) { }
        }

        /// <summary>
        /// <para>[Subroutine] marks a block of commands that states run with callSubroutine.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class SubroutineAttribute : Attribute
        {
            public SubroutineAttribute() { }

            public SubroutineAttribute(string name) { }
        }

        /// <summary>
        /// <para>[RawState] is a state written one command per line, closing commands included.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class RawStateAttribute : Attribute
        {
            public RawStateAttribute() { }

            public RawStateAttribute(string name) { }
        }

        /// <summary>
        /// <para>[RawSubroutine] is a subroutine written one command per line, closing commands included.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Method)]
        public sealed class RawSubroutineAttribute : Attribute
        {
            public RawSubroutineAttribute() { }

            public RawSubroutineAttribute(string name) { }
        }

        /// <summary>
        /// <para>A number or a SLOT. Comparisons and math between them work as in the game: true is 1, false is 0.</para>
        /// </summary>
        public readonly struct Value
        {
            public static implicit operator Value(int number) => default;

            public static implicit operator Value(bool flag) => default;

            public static Value operator +(Value left, Value right) => default;

            public static Value operator -(Value left, Value right) => default;

            public static Value operator *(Value left, Value right) => default;

            public static Value operator /(Value left, Value right) => default;

            public static Value operator %(Value left, Value right) => default;

            public static Value operator &(Value left, Value right) => default;

            public static Value operator |(Value left, Value right) => default;

            public static Value operator ==(Value left, Value right) => default;

            public static Value operator !=(Value left, Value right) => default;

            public static Value operator <(Value left, Value right) => default;

            public static Value operator >(Value left, Value right) => default;

            public static Value operator <=(Value left, Value right) => default;

            public static Value operator >=(Value left, Value right) => default;

            public static Value operator !(Value value) => default;

            public static Value operator ~(Value value) => default;

            public static Value operator -(Value value) => default;

            public static Value operator +(Value value) => default;

            public static bool operator true(Value value) => false;

            public static bool operator false(Value value) => false;

            public override bool Equals(object other) => false;

            public override int GetHashCode() => 0;
        }

        """;

    public static string Write(ScriptDatabase database, ScriptDocs docs, ScriptUsage usage)
    {
        var api = new StringBuilder(Preamble);

        AppendEnum(api, CSharpNames.NormalInput, "The input of a move registration: Move_Register(..., NormalInput._5A, ...).", database.Inputs.Normal.Named, true, usage, UsageKind.NormalInput);
        AppendEnum(api, CSharpNames.Input, "An input for Move_Input_ and CheckInput.", database.Inputs.Motions.Named, true, usage, UsageKind.MoveInput);
        AppendEnum(api, CSharpNames.MoveCondition, "When a registered move can be used: Move_AirGround_(MoveCondition.GROUND).", Described(database.MoveConditions), true, usage, UsageKind.MoveCondition);
        AppendEnum(api, CSharpNames.HitAnimation, "The animation the opponent is hit with.", Described(database.HitAnimations), false, usage, UsageKind.HitAnimation);
        AppendUpons(api, database, docs, usage);
        AppendSlots(api, database, docs);
        AppendScript(api, database, docs, usage);

        return api.ToString();
    }

    public static string? CommandMethodName(ScriptDatabase database, CommandDefinition command)
    {
        var name = database.CallName(command);
        return ReservedNames.Contains(name) ? null : CSharpNames.Escaped(name);
    }

    public static bool IsReserved(string identifier) => ReservedNames.Contains(identifier);

    private static IEnumerable<NamedValue> Described(NameTable table) =>
        table.Named.Select(value => value with { Description = table.DescriptionOf(value.Value) ?? string.Empty });

    private static void AppendEnum(StringBuilder api, string type, string summary, IEnumerable<NamedValue> values, bool hex, ScriptUsage usage, UsageKind kind)
    {
        AppendDocs(api, string.Empty, summary);
        api.Append("public enum ").Append(type).Append("\n{\n");

        foreach (var value in Members(values, CSharpNames.Prefixes[type]))
        {
            AppendDocs(api, Indent, HelpText.Value(value.Value, hex, value.Description, usage, new UsageKey(kind, value.Value)));
            api.Append(Indent).Append(value.Name).Append(" = ").Append(Number(value.Value)).Append(",\n");
        }

        api.Append("}\n\n");
    }

    private static void AppendUpons(StringBuilder api, ScriptDatabase database, ScriptDocs docs, ScriptUsage usage)
    {
        AppendDocs(api, string.Empty, "An event a state reacts to: upon(Upon.Landing, () => { ... }).");
        api.Append("public enum ").Append(CSharpNames.Upon).Append("\n{\n");
        var upons = database.Upons.Named.Select(value => value with { Name = ScriptDatabase.UponPrefix + value.Name });

        foreach (var value in Members(upons, ScriptDatabase.UponPrefix))
        {
            AppendDocs(api, Indent, HelpText.Upon(value.Value, docs, usage));
            api.Append(Indent).Append(value.Name).Append(" = ").Append(Number(value.Value)).Append(",\n");
        }

        api.Append("}\n\n");
    }

    private static void AppendSlots(StringBuilder api, ScriptDatabase database, ScriptDocs docs)
    {
        AppendDocs(api, string.Empty, "The script's variables (SLOTs): Slot._3 = 5; if (Slot.IsInOverdrive) { ... }.");
        api.Append("public static class ").Append(CSharpNames.Slot).Append("\n{\n");
        var named = database.Slots.Named.Select(value => new NamedValue(value.Value, ScriptDatabase.SlotPrefix + value.Name));
        var numbered = Enumerable.Range(0, SlotCount).Select(slot => new NamedValue(slot, ScriptDatabase.SlotPrefix + Number(slot)));

        foreach (var slot in Members(named.Concat(numbered), ScriptDatabase.SlotPrefix))
        {
            AppendDocs(api, Indent, HelpText.Slot(slot.Value, docs));
            api.Append(Indent).Append("public static Value ").Append(slot.Name).Append(" { get; set; }\n\n");
        }

        api.Append("}\n\n");
    }

    private static void AppendScript(StringBuilder api, ScriptDatabase database, ScriptDocs docs, ScriptUsage usage)
    {
        AppendDocs(api, string.Empty, "Every BBTAG script is a class that inherits Script: its commands are the methods below.");
        api.Append("public abstract class ").Append(CSharpDialect.BaseClass).Append("\n{\n");

        foreach (var command in database.Commands.OrderBy(command => command.Id))
        {
            var help = HelpText.Command(command, docs.Command(command.Id), usage);
            var unknown = CommandDefinition.UnknownName(command.Id);

            if (CommandMethodName(database, command) is { } name && name != unknown)
                AppendCommand(api, command, name, help);

            AppendCommand(api, command, unknown, help);
        }

        api.Append("}\n");
    }

    private static void AppendCommand(StringBuilder api, CommandDefinition command, string name, string help)
    {
        var returns = ScriptCommands.WritesSlotZero.Contains(command.Id) ? CSharpDialect.ValueType : "void";
        var body = returns == "void" ? " { }" : " => default;";

        foreach (var parameters in Forms(command))
        {
            AppendDocs(api, Indent, help);
            api.Append(Indent).Append("public static ").Append(returns).Append(' ').Append(name)
                .Append('(').Append(string.Join(", ", Unique(parameters).Select(parameter => $"{parameter.Type} {parameter.Name}"))).Append(')')
                .Append(body).Append("\n\n");
        }
    }

    private static IEnumerable<List<(string Type, string Name)>> Forms(CommandDefinition command)
    {
        var paired = Variants(Paired(command)).ToList();

        foreach (var form in paired)
            yield return form;

        if (BlockCommands.Contains(command.Id))
        {
            foreach (var form in paired)
                yield return [.. form, (ActionType, BodyParameter)];
        }

        if (command.HasSlotArguments)
            yield return Raw(command);
    }

    private static List<(string[] Types, string Name)> Paired(CommandDefinition command)
    {
        var parameters = new List<(string[], string)>();
        var visible = 0;

        for (var index = 0; index < command.Arguments.Count; index++)
        {
            if (command.SlotTypeArguments.Contains(index))
            {
                parameters.Add(([CSharpDialect.ValueType], "value"));
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

    private static (string[] Types, string Name) Parameter(CommandDefinition command, CommandArgument argument, int visible)
    {
        if (argument.Kind == ArgumentKind.Text)
            return ValueKinds.IsStateReference(command.Id, visible) ? ([ActionType, "string"], "state") : (["string"], "text");

        if (argument.Kind == ArgumentKind.Unsigned)
            return (["long"], "value");

        return ValueKinds.Of(command.Id, visible) switch
        {
            ValueKind.Input => ([command.Id == ScriptCommands.StateRegister ? CSharpNames.NormalInput : CSharpNames.Input], "input"),
            ValueKind.Upon => ([CSharpNames.Upon], "upon"),
            ValueKind.HitAnimation => ([CSharpNames.HitAnimation], "animation"),
            ValueKind.MoveCondition => ([CSharpNames.MoveCondition], "condition"),
            _ => (["int"], "value"),
        };
    }

    private static IEnumerable<List<(string Type, string Name)>> Variants(List<(string[] Types, string Name)> parameters)
    {
        IEnumerable<List<(string, string)>> forms = [[]];

        foreach (var (types, name) in parameters)
            forms = forms.SelectMany(form => types.Select(type => new List<(string, string)>(form) { (type, name) })).ToList();

        return forms;
    }

    private static List<(string Type, string Name)> Raw(CommandDefinition command) =>
        command.Arguments.Select(argument => argument.Kind switch
        {
            ArgumentKind.Text => ("string", "text"),
            ArgumentKind.Unsigned => ("long", "value"),
            _ => ("int", "value"),
        }).ToList();

    private static IEnumerable<(string Type, string Name)> Unique(List<(string Type, string Name)> parameters)
    {
        var repeated = parameters.GroupBy(parameter => parameter.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        var counters = new Dictionary<string, int>();

        foreach (var (type, name) in parameters)
        {
            if (!repeated.Contains(name))
            {
                yield return (type, CSharpNames.Escaped(name));
                continue;
            }

            counters[name] = counters.GetValueOrDefault(name) + 1;
            yield return (type, name + Number(counters[name]));
        }
    }

    private static IEnumerable<NamedValue> Members(IEnumerable<NamedValue> values, string prefix) =>
        values
            .Select(value => CSharpNames.Member(value.Name, prefix) is { } member ? value with { Name = member } : null)
            .OfType<NamedValue>()
            .DistinctBy(value => value.Name, StringComparer.Ordinal);

    private static void AppendDocs(StringBuilder api, string indent, string text)
    {
        api.Append(indent).Append("/// <summary>\n");

        foreach (var paragraph in text.ReplaceLineEndings("\n").Split("\n\n"))
        {
            var lines = paragraph.Split('\n').Select(line => SecurityElement.Escape(line));
            api.Append(indent).Append("/// <para>").Append(string.Join("<br/>", lines)).Append("</para>\n");
        }

        api.Append(indent).Append("/// </summary>\n");
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
