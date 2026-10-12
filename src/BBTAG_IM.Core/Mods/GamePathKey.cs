namespace BBTAG_IM.Core.Mods;

public static class GamePathKey
{
    public const char Separator = '\\';

    private const char AlternateSeparator = '/';
    private const char DriveSeparator = ':';
    private const int DriveSeparatorIndex = 1;
    private const string ExtendedLengthPrefix = @"\\?\";
    private const string CurrentSegment = ".";
    private const string ParentSegment = "..";

    public static string? From(string? requestedPath, string gameRoot)
    {
        if (string.IsNullOrEmpty(requestedPath))
            return null;

        var path = requestedPath.Replace(AlternateSeparator, Separator);

        if (path.StartsWith(ExtendedLengthPrefix, StringComparison.Ordinal))
            path = path[ExtendedLengthPrefix.Length..];

        var relative = RelativeToGame(path, gameRoot.Replace(AlternateSeparator, Separator).TrimEnd(Separator));
        return relative is null ? null : Normalise(relative);
    }

    private static string? RelativeToGame(string path, string gameRoot)
    {
        if (path.StartsWith(Separator))
            return null;

        if (!HasDrive(path))
            return path;

        var prefix = gameRoot + Separator;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? path[prefix.Length..] : null;
    }

    private static bool HasDrive(string path) => path.Length > DriveSeparatorIndex && path[DriveSeparatorIndex] == DriveSeparator;

    private static string? Normalise(string relative)
    {
        var segments = new List<string>();

        foreach (var segment in relative.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == CurrentSegment)
                continue;

            if (segment != ParentSegment)
            {
                segments.Add(segment);
                continue;
            }

            if (segments.Count == 0)
                return null;

            segments.RemoveAt(segments.Count - 1);
        }

        return segments.Count > 1 ? string.Join(Separator, segments) : null;
    }
}
