using System.Text.Json.Serialization;

namespace BBTAG_IM.Core.Roster;

[JsonSerializable(typeof(CharacterManifestFile))]
internal sealed partial class RosterJson : JsonSerializerContext;
