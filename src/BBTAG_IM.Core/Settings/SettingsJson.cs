using System.Text.Json.Serialization;

namespace BBTAG_IM.Core.Settings;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LauncherSettings))]
[JsonSerializable(typeof(ImprovementSettings))]
internal sealed partial class SettingsJson : JsonSerializerContext;
