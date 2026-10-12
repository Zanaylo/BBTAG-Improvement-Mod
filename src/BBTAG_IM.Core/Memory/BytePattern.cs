using System.Globalization;

namespace BBTAG_IM.Core.Memory;

public sealed class BytePattern
{
    private const string Wildcard = "??";
    private const int HexDigitsPerByte = 2;

    private readonly byte?[] _bytes;
    private readonly int _anchor;

    private BytePattern(byte?[] bytes)
    {
        _bytes = bytes;
        _anchor = Array.FindIndex(bytes, value => value is not null);
    }

    public int Length => _bytes.Length;

    public static BytePattern Parse(string text)
    {
        var bytes = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ParseToken).ToArray();

        if (bytes.All(value => value is null))
            throw new ArgumentException($"The byte pattern '{text}' has no fixed byte.", nameof(text));

        return new BytePattern(bytes);
    }

    public IReadOnlyList<int> FindAll(ReadOnlySpan<byte> bytes)
    {
        List<int> offsets = [];
        var anchorByte = _bytes[_anchor]!.Value;
        var searchFrom = _anchor;

        while (searchFrom < bytes.Length)
        {
            var found = bytes[searchFrom..].IndexOf(anchorByte);

            if (found < 0)
                break;

            var start = searchFrom + found - _anchor;
            searchFrom += found + 1;

            if (start + _bytes.Length <= bytes.Length && MatchesAt(bytes, start))
                offsets.Add(start);
        }

        return offsets;
    }

    private bool MatchesAt(ReadOnlySpan<byte> bytes, int start)
    {
        for (var index = 0; index < _bytes.Length; index++)
        {
            if (_bytes[index] is { } expected && bytes[start + index] != expected)
                return false;
        }

        return true;
    }

    private static byte? ParseToken(string token)
    {
        if (token == Wildcard)
            return null;

        if (token.Length != HexDigitsPerByte || !byte.TryParse(token, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            throw new ArgumentException($"'{token}' is not a byte of a pattern.", nameof(token));

        return value;
    }
}
