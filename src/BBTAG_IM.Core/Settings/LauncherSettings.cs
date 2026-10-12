using System.Text.Json.Serialization;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Settings;

public sealed class LauncherSettings
{
    public const string FileName = "BBTAG_IM.json";

    public string? CheatEnginePath { get; set; }

    public bool RunCheatEngineAsAdmin { get; set; } = true;

    public bool ShowCheatEngineWindow { get; set; } = true;

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, List<int>> ScriptSelections { get; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public HashSet<string> DisabledMods { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsModEnabled(string modName) => !DisabledMods.Contains(modName);

    public void EnableMod(string modName, bool enabled)
    {
        if (enabled)
            DisabledMods.Remove(modName);
        else
            DisabledMods.Add(modName);
    }

    public IReadOnlyList<ModFolder> EnabledOf(IEnumerable<ModFolder> mods) => mods.Where(mod => IsModEnabled(mod.Name)).ToArray();

    public IReadOnlyList<int>? SelectionFor(string tableName) => ScriptSelections.GetValueOrDefault(tableName);

    public void Remember(string tableName, IEnumerable<int> scriptIds) => ScriptSelections[tableName] = scriptIds.ToList();
}
