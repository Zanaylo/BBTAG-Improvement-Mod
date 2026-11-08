using BBTAG_IM.Core.Roster;

namespace BBTAG_IM.Core.Mods;

public sealed class CharacterFileAliases
{
    private readonly IReadOnlyList<CharacterManifest> _manifests;

    public CharacterFileAliases(IEnumerable<CharacterManifest> manifests) =>
        _manifests = manifests.Where(manifest => manifest.FilesFrom is not null).ToArray();

    public string? AliasOf(string key)
    {
        foreach (var manifest in _manifests)
        {
            if (key.Contains(manifest.Code, StringComparison.OrdinalIgnoreCase))
                return key.Replace(manifest.Code, manifest.FilesFrom, StringComparison.OrdinalIgnoreCase);
        }

        return null;
    }
}
