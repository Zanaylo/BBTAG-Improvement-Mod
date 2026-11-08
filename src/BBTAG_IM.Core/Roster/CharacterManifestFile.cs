namespace BBTAG_IM.Core.Roster;

internal sealed record CharacterManifestFile(
    string? Code,
    string? Template,
    string? ShortName,
    string? SpriteFolder,
    string? Series,
    CharacterNamesFile? Names,
    string? FilesFrom = null);

internal sealed record CharacterNamesFile(string? Full, string? Short, string? Select);
