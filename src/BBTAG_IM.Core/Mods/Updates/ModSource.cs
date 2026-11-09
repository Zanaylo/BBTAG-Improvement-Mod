using System.Text.Json.Serialization;

namespace BBTAG_IM.Core.Mods.Updates;

public sealed class ModSource
{
    public string Repository { get; set; } = "";

    public string? Version { get; set; }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
}
