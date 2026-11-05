using System.Text.RegularExpressions;

namespace BBTAG_IM.Core.Logging;

public sealed record ShownMessage(string Line, string? Replaces);

public sealed partial class RepeatedMessages
{
    private const string AnyNumber = "#";

    private readonly Dictionary<string, Shown> _shown = [];

    public ShownMessage Show(string time, string message)
    {
        var key = StandaloneNumber().Replace(message, AnyNumber);
        var earlier = _shown.GetValueOrDefault(key);
        var count = (earlier?.Count ?? 0) + 1;
        var line = count == 1 ? $"{time}  {message}" : $"{time}  {message} (x{count})";
        _shown[key] = new Shown(line, count);
        return new ShownMessage(line, earlier?.Line);
    }

    [GeneratedRegex(@"(?<![\w.])\d+(?!\w)")]
    private static partial Regex StandaloneNumber();

    private sealed record Shown(string Line, int Count);
}
