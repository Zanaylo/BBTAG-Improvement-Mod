using System.Text.Json.Serialization;
using BBTAG_IM.Core.Mods.Updates;
using BBTAG_IM.Core.Updates;

namespace BBTAG_IM.Core.Settings;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LauncherSettings))]
[JsonSerializable(typeof(ImprovementSettings))]
[JsonSerializable(typeof(UpdaterSettings))]
[JsonSerializable(typeof(ModSource))]
internal sealed partial class SettingsJson : JsonSerializerContext;
