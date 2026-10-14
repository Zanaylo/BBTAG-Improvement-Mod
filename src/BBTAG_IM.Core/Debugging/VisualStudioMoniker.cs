using System.Globalization;

namespace BBTAG_IM.Core.Debugging;

public static class VisualStudioMoniker
{
    private const string Prefix = "!VisualStudio.DTE.";
    private const char ProcessSeparator = ':';

    public static int? ProcessIdOf(string displayName)
    {
        if (!displayName.StartsWith(Prefix, StringComparison.Ordinal))
            return null;

        var separator = displayName.LastIndexOf(ProcessSeparator);

        if (separator < 0)
            return null;

        return int.TryParse(displayName.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var processId)
            ? processId
            : null;
    }
}
