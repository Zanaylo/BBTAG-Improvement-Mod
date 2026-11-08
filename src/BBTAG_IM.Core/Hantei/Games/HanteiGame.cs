using System.Text;

namespace BBTAG_IM.Core.Hantei.Games;

public sealed record HanteiCharacter(string Code, string Name);

public sealed class HanteiGame(string title, string root, IHanteiGameFiles files, string codeLetter = HanteiGame.UnpackedLetter)
{
    public const string UnpackedLetter = "p";

    private const string NamesFile = "___English/System/BtlCharaTbl_str.ini";
    private const string CharacterPrefix = "chr";
    private const string PatternExtension = ".ha6";
    private const char FolderSeparator = '/';
    private const char NameSeparator = '=';
    private const int NumberDigits = 3;
    private const int PathParts = 3;

    private static readonly string[] RequiredFiles = [".cg", ".pal", "_0.txt"];

    public string Title { get; } = title;

    public string Root { get; } = root;

    public string CodeLetter { get; } = codeLetter;

    public IHanteiGameFiles Files { get; } = files;

    public IReadOnlyList<HanteiCharacter> Characters()
    {
        var names = ReadNames();
        return Files.Names
            .Select(CodeOf)
            .OfType<string>()
            .Where(HasEveryFile)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(code => new HanteiCharacter(code, names.GetValueOrDefault(code[CharacterPrefix.Length..], code)))
            .ToArray();
    }

    private static string? CodeOf(string name)
    {
        var parts = name.Split(FolderSeparator);

        if (parts.Length != PathParts || !parts[0].Equals(HanteiFileName.DataFolder, StringComparison.OrdinalIgnoreCase))
            return null;

        var code = parts[1];
        return IsCharacterCode(code) && parts[2].Equals(code + PatternExtension, StringComparison.OrdinalIgnoreCase) ? code : null;
    }

    private static bool IsCharacterCode(string code) =>
        code.Length == CharacterPrefix.Length + NumberDigits
        && code.StartsWith(CharacterPrefix, StringComparison.OrdinalIgnoreCase)
        && code.AsSpan(CharacterPrefix.Length).IndexOfAnyExceptInRange('0', '9') < 0;

    private bool HasEveryFile(string code) => RequiredFiles.All(file => Files.Contains(HanteiFileName.InCharacter(code, code + file)));

    private Dictionary<string, string> ReadNames()
    {
        if (!Files.Contains(NamesFile))
            return [];

        Dictionary<string, string> names = [];

        foreach (var line in Encoding.Latin1.GetString(Files.Read(NamesFile)).Split('\n'))
        {
            var separator = line.IndexOf(NameSeparator);

            if (separator > 0)
                names[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        return names;
    }
}
