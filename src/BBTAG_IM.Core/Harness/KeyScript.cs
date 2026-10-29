using System.Globalization;

namespace BBTAG_IM.Core.Harness;

public sealed record KeyStep(IReadOnlyList<int> Keys, int HoldFrames, int GapFrames)
{
    public bool Equals(KeyStep? other) =>
        other is not null && Keys.SequenceEqual(other.Keys) && HoldFrames == other.HoldFrames && GapFrames == other.GapFrames;

    public override int GetHashCode() => HashCode.Combine(Keys.Count, HoldFrames, GapFrames);
}

public sealed class InvalidKeyScriptException(string message) : Exception(message);

public sealed record KeyScript(IReadOnlyList<KeyStep> Steps)
{
    public const int DefaultHoldFrames = 4;
    public const int DefaultGapFrames = 8;

    private const string HoldOption = "hold=";
    private const string GapOption = "gap=";
    private const char StepSeparator = ',';
    private const char ChordSeparator = '+';
    private const char HoldSeparator = '*';

    public int Frames => Steps.Sum(step => step.HoldFrames + step.GapFrames);

    public static KeyScript Parse(IEnumerable<string> words)
    {
        var holdFrames = DefaultHoldFrames;
        var gapFrames = DefaultGapFrames;
        List<string> tokens = [];

        foreach (var word in words)
        {
            if (Option(word, HoldOption) is { } hold)
                holdFrames = hold;
            else if (Option(word, GapOption) is { } gap)
                gapFrames = gap;
            else
                tokens.AddRange(word.Split(StepSeparator, StringSplitOptions.RemoveEmptyEntries));
        }

        if (tokens.Count == 0)
            throw new InvalidKeyScriptException("keys needs at least one key");

        return new KeyScript(tokens.Select(token => Step(token, holdFrames, gapFrames)).ToArray());
    }

    private static KeyStep Step(string token, int holdFrames, int gapFrames)
    {
        var parts = token.Split(HoldSeparator);
        var hold = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var own) ? own : holdFrames;
        var keys = parts[0].Split(ChordSeparator).Select(name => HarnessKeys.Key(name) ?? throw new InvalidKeyScriptException($"unknown key {name}")).ToArray();
        return new KeyStep(keys, hold, gapFrames);
    }

    private static int? Option(string word, string prefix) =>
        word.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        && int.TryParse(word[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
}
