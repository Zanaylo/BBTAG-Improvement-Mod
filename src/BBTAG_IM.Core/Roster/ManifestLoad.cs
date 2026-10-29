namespace BBTAG_IM.Core.Roster;

public sealed record ManifestLoad(IReadOnlyList<CharacterManifest> Manifests, IReadOnlyList<string> Problems);
