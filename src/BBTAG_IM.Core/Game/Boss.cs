using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Game;

public sealed record BossSprites(SpriteFolder Folder, string Directory);

public sealed record Boss(string Name, string Code, string Stage, BossSprites? Sprites)
{
    public bool IsInstalled(IFileSystem fileSystem, IEnumerable<string> dataFolders) =>
        Sprites is not { } sprites
        || dataFolders.Any(folder => fileSystem.FileExists(Path.Combine(folder, sprites.Directory, $"char_{Code}_vri.pac")));

    public BossFight FindIn(CharacterRoster roster, StageTable stages)
    {
        var character = roster.IndexOf(Code)
            ?? throw new BossNotReadyException($"{Name} is not in the character list yet. Wait until the table scripts are on.");
        var stage = stages.IndexOf(Stage)
            ?? throw new BossNotReadyException($"The {Stage} stage of {Name} is not in the stage list yet. Wait until the table scripts are on.");

        return new BossFight(this, character, stage);
    }
}
