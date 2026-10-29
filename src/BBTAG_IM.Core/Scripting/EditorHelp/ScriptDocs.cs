namespace BBTAG_IM.Core.Scripting.EditorHelp;

public sealed class ScriptDocs(
    IReadOnlyDictionary<int, CommandDoc> commands,
    IReadOnlyDictionary<int, string> slots,
    IReadOnlyDictionary<int, string> upons)
{
    public static readonly ScriptDocs Empty = new(new Dictionary<int, CommandDoc>(), new Dictionary<int, string>(), new Dictionary<int, string>());

    public IReadOnlyDictionary<int, CommandDoc> Commands => commands;

    public IReadOnlyDictionary<int, string> Slots => slots;

    public IReadOnlyDictionary<int, string> Upons => upons;

    public CommandDoc? Command(int id) => commands.GetValueOrDefault(id);

    public string? Slot(int slot) => slots.GetValueOrDefault(slot);

    public string? Upon(int upon) => upons.GetValueOrDefault(upon);
}
