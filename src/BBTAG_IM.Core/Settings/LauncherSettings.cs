using System.Text.Json.Serialization;
using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Settings;

public sealed class LauncherSettings
{
    public const string FileName = "BBTAG_IM.json";

    public string? CheatEnginePath { get; set; }

    public string? ModsFolder { get; set; }

    public bool WaitForDebugger { get; set; }

    public bool RunCheatEngineAsAdmin { get; set; } = true;

    public bool ShowCheatEngineWindow { get; set; } = true;

    public bool WriteLogs { get; set; }

    public bool PlayOnStart { get; set; }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, List<int>> ScriptSelections { get; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public HashSet<string> DisabledMods { get; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public List<string> ModOrder { get; } = [];

    public string ModsDirectory(string launcherDirectory) => string.IsNullOrWhiteSpace(ModsFolder)
        ? Path.Combine(launcherDirectory, ModSelection.ModsFolderName)
        : Path.GetFullPath(ModsFolder, launcherDirectory);

    public bool IsModEnabled(string modName) => !DisabledMods.Contains(modName);

    public void EnableMod(string modName, bool enabled)
    {
        if (enabled)
            DisabledMods.Remove(modName);
        else
            DisabledMods.Add(modName);
    }

    public IReadOnlyList<ModFolder> EnabledOf(IEnumerable<ModFolder> mods) => InLoadOrder(mods).Where(mod => IsModEnabled(mod.Name)).ToArray();

    public IReadOnlyList<ModFolder> InLoadOrder(IEnumerable<ModFolder> mods) => mods.OrderBy(mod => LoadRankOf(mod.Name)).ToArray();

    public void MoveMod(IReadOnlyList<string> loadOrder, string modName, ModMove move)
    {
        var order = loadOrder.ToList();
        var from = order.FindIndex(name => IsSameMod(name, modName));
        var to = from + (int)move;

        if (from >= 0 && to >= 0 && to < order.Count)
            (order[from], order[to]) = (order[to], order[from]);

        ModOrder.Clear();
        ModOrder.AddRange(order);
    }

    private int LoadRankOf(string modName)
    {
        var rank = ModOrder.FindIndex(name => IsSameMod(name, modName));
        return rank < 0 ? int.MaxValue : rank;
    }

    private static bool IsSameMod(string name, string other) => string.Equals(name, other, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<int>? SelectionFor(string tableName) => ScriptSelections.GetValueOrDefault(tableName);

    public void Remember(string tableName, IEnumerable<int> scriptIds) => ScriptSelections[tableName] = scriptIds.ToList();
}
