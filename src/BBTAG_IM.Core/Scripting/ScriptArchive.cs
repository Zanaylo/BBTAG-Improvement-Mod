using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Scripting;

public sealed record ScriptFile(string Name, byte[] Bytes);

public static class ScriptArchive
{
    private const string BinaryExtension = ".bin";
    private const string ScriptPrefix = "scr_";
    private const string EffectsSuffix = "ea";
    private const int ShortestEffectsName = 4;

    public static IReadOnlyList<ScriptFile> Scripts(byte[] archive)
    {
        var dataOffset = FpacArchive.DataOffset(archive);

        return FpacArchive.Entries(archive.AsSpan(0, dataOffset))
            .Where(entry => entry.Name.EndsWith(BinaryExtension, StringComparison.OrdinalIgnoreCase))
            .Select(entry => new ScriptFile(Path.GetFileNameWithoutExtension(entry.Name), archive.AsSpan((int)entry.Offset, entry.Size).ToArray()))
            .ToList();
    }

    public static string? CharacterCode(string scriptName)
    {
        if (!scriptName.StartsWith(ScriptPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var rest = scriptName[ScriptPrefix.Length..].ToLowerInvariant();

        if (rest.Length >= ShortestEffectsName && rest.EndsWith(EffectsSuffix, StringComparison.Ordinal))
            return rest[..^EffectsSuffix.Length];

        return rest.Length == 0 ? null : rest;
    }
}
