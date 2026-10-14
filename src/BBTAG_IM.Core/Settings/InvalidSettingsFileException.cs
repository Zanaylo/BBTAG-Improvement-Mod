namespace BBTAG_IM.Core.Settings;

public sealed class InvalidSettingsFileException(string path, long? lineIndex) : IOException(Describe(path, lineIndex))
{
    private static string Describe(string path, long? lineIndex)
    {
        var where = lineIndex is { } index ? $" on line {index + 1}" : string.Empty;
        return $"{Path.GetFileName(path)} has a mistake{where}, so it was not read. Fix it or delete it. It is not saved over until then.";
    }
}
