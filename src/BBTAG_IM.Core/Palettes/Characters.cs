namespace BBTAG_IM.Core.Palettes;

public static class Characters
{
    public static IReadOnlyList<string> Names { get; } =
    [
        "Ragna", "Jin", "Noel", "Rachel", "Tager", "Hakumen", "Nu", "Hazama", "Makoto", "Platinum",
        "Izayoi", "Azrael", "Nine", "Es", "Mai", "Jubei", "Yu", "Yosuke", "Chie", "Yukiko",
        "Kanji", "Naoto", "Mitsuru", "Akihiko", "Aegis", "Labrys", "Hyde", "Linne", "Waldstein", "Carmine",
        "Orie", "Gordeau", "Merkava", "Vatista", "Yuzuriha", "Mika", "Ruby", "Weiss", "Blake", "Yang",
        "NaotoKurogane", "Teddie", "Seth", "Heart", "Celica", "Susanoo", "Adachi", "Elizabeth", "Akatsuki", "Hilda",
        "Neo", "Yumi", "Blitztank",
    ];

    public static IReadOnlyList<string> ImageCodes { get; } =
    [
        "brg", "bjn", "bno", "brc", "btg", "bha", "bny", "bhz", "bmk", "bpt",
        "biz", "baz", "bph", "bes", "bma", "bjb", "pbc", "pyo", "pce", "pyu",
        "pka", "pna", "pmi", "pak", "pag", "pla", "uhy", "uli", "uwa", "uca",
        "uor", "ugo", "ume", "uva", "uyu", "umi", "rrb", "rwi", "rbl", "ryn",
        "bnt", "pku", "use", "ahe", "bce", "bsu", "pad", "pel", "uak", "uhi",
        "rne", "kym", "ubl",
    ];

    public const string NumberedPrefix = "Character ";

    private const int LongestCode = 4;

    public static string? NameOf(int index) => index >= 0 && index < Names.Count ? Names[index] : null;

    public static bool IsCode(string text) =>
        text.Length is > 0 and <= LongestCode && text.All(character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character));

    public static bool IsNumbered(string text) =>
        text.StartsWith(NumberedPrefix, StringComparison.OrdinalIgnoreCase) && int.TryParse(text[NumberedPrefix.Length..], out _);

    public static string? ImageCodeOf(string folder)
    {
        if (IndexOf(folder) is { } index)
            return ImageCodes[index];

        return IsCode(folder) ? folder : null;
    }

    public static string? FolderOfImageCode(string code)
    {
        if (!IsCode(code))
            return null;

        for (var index = 0; index < ImageCodes.Count; index++)
        {
            if (ImageCodes[index] == code)
                return Names[index];
        }

        return code;
    }

    public static int? IndexOf(string name)
    {
        var index = Names.ToList().FindIndex(candidate => candidate.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? null : index;
    }
}
