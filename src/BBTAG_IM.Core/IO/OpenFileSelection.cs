namespace BBTAG_IM.Core.IO;

public static class OpenFileSelection
{
    private const char Separator = '\0';

    public static IReadOnlyList<string> Paths(string answer)
    {
        var parts = answer.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length <= 1)
            return parts;

        var folder = parts[0];
        return parts.Skip(1).Select(name => Path.Combine(folder, name)).ToArray();
    }
}
