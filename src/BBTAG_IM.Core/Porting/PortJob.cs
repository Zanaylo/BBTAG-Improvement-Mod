using BBTAG_IM.Core.Hantei.Games;

namespace BBTAG_IM.Core.Porting;

public sealed record PortJob(HanteiGame Game, HanteiCharacter Character, string Series)
{
    public const string ForeignSpritePrefix = "Port_";
    public const string ImagePrefix = "Fbp";

    private const string ModPrefix = "FB";
    private const string CodePrefix = "x";
    private const string Digits = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int CharacterNumberStart = 3;

    public string Code => CodePrefix + Game.CodeLetter + Digits[int.Parse(Character.Code.AsSpan(CharacterNumberStart)) % Digits.Length];

    public string ModName => $"{ModPrefix}-{string.Concat(Character.Name.Where(char.IsAsciiLetterOrDigit))}";

    public IReadOnlyList<string> Arguments(string gameData, string modsFolder, string databaseFolder) =>
    [
        "port", Game.Root, Character.Code, gameData, RosterManifest.FilesTemplate, ImagePrefix,
        "--foreign", "--roster", Code, "--name", Character.Name, "--series", Series,
        "--out", Path.Combine(modsFolder, ModName), "--db", databaseFolder,
    ];
}
