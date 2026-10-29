namespace BBTAG_IM.Core.Scripting;

public sealed class ScriptDatabase
{
    public const string SlotPrefix = "SLOT_";
    public const string UponPrefix = "upon_";

    public static readonly ScriptDatabase Unnamed = new([], NameTable.Empty, NameTable.Empty, NameTable.Empty, NameTable.Empty, InputNames.Empty);

    private readonly Dictionary<int, CommandDefinition> byId;
    private readonly Dictionary<string, CommandDefinition> byName;

    public ScriptDatabase(
        IEnumerable<CommandDefinition> commands,
        NameTable slots,
        NameTable upons,
        NameTable hitAnimations,
        NameTable moveConditions,
        InputNames inputs)
    {
        byId = commands.ToDictionary(command => command.Id);
        byName = new Dictionary<string, CommandDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var command in byId.Values)
            byName.TryAdd(command.Name, command);

        Slots = slots;
        Upons = upons;
        HitAnimations = hitAnimations;
        MoveConditions = moveConditions;
        Inputs = inputs;
    }

    public NameTable Slots { get; }

    public NameTable Upons { get; }

    public NameTable HitAnimations { get; }

    public NameTable MoveConditions { get; }

    public InputNames Inputs { get; }

    public IEnumerable<CommandDefinition> Commands => byId.Values;

    public CommandDefinition? Find(int id) => byId.GetValueOrDefault(id);

    public CommandDefinition? Find(string name)
    {
        if (byName.TryGetValue(name, out var command))
            return command;

        return CommandDefinition.IdFromUnknownName(name) is { } id ? Find(id) : null;
    }

    public string CallName(CommandDefinition command) =>
        FunctionNames.IsIdentifier(command.Name) && Find(command.Name)?.Id == command.Id
            ? command.Name
            : CommandDefinition.UnknownName(command.Id);

    public CommandDefinition Command(int id) =>
        Find(id) ?? throw new UnknownCommandException(id);

    public string SlotName(int slot) => SlotPrefix + (Slots.NameOf(slot) ?? slot.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public int? SlotNumber(string name)
    {
        if (!name.StartsWith(SlotPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = name[SlotPrefix.Length..];
        return Slots.ValueOf(rest) ?? (int.TryParse(rest, out var number) ? number : null);
    }

    public string UponName(int upon) => UponPrefix + (Upons.NameOf(upon) ?? upon.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public int? UponNumber(string name)
    {
        if (!name.StartsWith(UponPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = name[UponPrefix.Length..];
        return Upons.ValueOf(rest) ?? (int.TryParse(rest, out var number) ? number : null);
    }

    public ScriptDatabase WithGameSizes(IReadOnlyDictionary<int, int> sizes)
    {
        var commands = sizes.Select(pair => GameCommand(pair.Key, pair.Value));
        return new ScriptDatabase(commands, Slots, Upons, HitAnimations, MoveConditions, Inputs);
    }

    private CommandDefinition GameCommand(int id, int size)
    {
        if (Find(id) is not { } known)
            return new CommandDefinition(id, CommandDefinition.UnknownName(id), CommandFormat.ForSize(size), new HashSet<int>(), HexArguments.None);

        return known.Size == size
            ? known
            : known with { Arguments = CommandFormat.ForSize(size), SlotTypeArguments = new HashSet<int>(), Hex = HexArguments.None };
    }

    public ScriptDatabase WithTagNames() =>
        new(
            byId.Values.Select(WithTagName),
            Slots,
            Upons,
            HitAnimations,
            MoveConditions.With(TagMoveConditions.Table),
            Inputs.With(TagMoveInputs.Table, TagNormalInputs.Instance));

    private CommandDefinition WithTagName(CommandDefinition command)
    {
        if (!TagCommandNames.Names.TryGetValue(command.Id, out var name))
            return command;

        if (command.Name != CommandDefinition.UnknownName(command.Id) || byName.ContainsKey(name))
            return command;

        return command with { Name = name };
    }

    public ScriptDatabase WithCharacterNames(NameTable slots, NameTable upons) =>
        new(byId.Values, Slots.With(slots), Upons.With(upons), HitAnimations, MoveConditions, Inputs);
}
