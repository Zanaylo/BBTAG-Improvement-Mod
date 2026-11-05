using System.Text.Json.Nodes;

namespace BBTAG_IM.Core.Scripting.Importing;

public static class SlotNameImport
{
    private const string VariablePrefix = "VAR_";
    private const int LastSharedSlot = 100;

    public static NameImportResult Merge(string slotsJson, BbcfTable table)
    {
        var slots = DatabaseJson.Parse(slotsJson);
        var claims = new NameClaims(UsedNames(slots));
        var added = 0;

        foreach (var (slot, variable) in table.Variables.Where(pair => pair.Key <= LastSharedSlot).OrderBy(pair => pair.Key))
        {
            var key = DatabaseJson.Key(slot);
            var name = WithoutPrefix(variable);

            if (slots.ContainsKey(key) || !claims.TryClaim(name, ScriptDatabase.SlotPrefix + key))
                continue;

            slots[key] = name;
            added++;
        }

        return new NameImportResult(DatabaseJson.WriteInNumberOrder(slots), added, added, claims.Skipped);
    }

    private static IEnumerable<string> UsedNames(JsonObject slots) =>
        slots.Select(entry => entry.Value is JsonValue value && value.TryGetValue<string>(out var name) ? name : null).OfType<string>();

    private static string WithoutPrefix(string variable) =>
        variable.StartsWith(VariablePrefix, StringComparison.Ordinal) ? variable[VariablePrefix.Length..] : variable;
}
