using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Roster;

public sealed record CharacterManifest(
    string Code,
    string Template,
    string ShortName,
    SpriteFolder? SpriteFolder,
    Series Series,
    CharacterNames Names);
