using System.Globalization;

namespace BBTAG_IM.Core.Updates;

public sealed record UpdaterCommandLine(int? AfterProcess)
{
    private const string AfterOption = "--after";

    public static string[] After(int processId) => [AfterOption, processId.ToString(CultureInfo.InvariantCulture)];

    public static UpdaterCommandLine Parse(IReadOnlyList<string> arguments)
    {
        var isAfter = arguments.Count == 2 && arguments[0] == AfterOption;
        var hasProcess = int.TryParse(isAfter ? arguments[1] : null, NumberStyles.None, CultureInfo.InvariantCulture, out var processId);

        return new UpdaterCommandLine(hasProcess ? processId : null);
    }
}
