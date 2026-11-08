namespace BBTAG_IM.Core.Hantei.Games;

public static class HanteiFileName
{
    public const string DataFolder = "data";

    private const char Separator = '/';
    private const char WindowsSeparator = '\\';
    private const string ParentFolder = "../";

    public static string Normalize(string name) => name.Replace(WindowsSeparator, Separator);

    public static string InCharacter(string character, string file)
    {
        var name = Normalize(file);

        return name.StartsWith(ParentFolder, StringComparison.Ordinal)
            ? $"{DataFolder}{Separator}{name[ParentFolder.Length..]}"
            : $"{DataFolder}{Separator}{character}{Separator}{name}";
    }
}
