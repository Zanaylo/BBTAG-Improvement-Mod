using BBTAG_IM.Core.Game;

namespace BBTAG_IM.Core.Hitboxes.Editing;

public static class SpriteDirectories
{
    public const string Char = "char";
    public const string CharFhd = "charfhd";
    public const string CharFhdExtra = "charfhd_add_ex";
    public const string Char2x = "char2x";
    public const string Char2xExtra = "char2x_add_ex";

    private const float CharPixelsPerUnit = 1f;
    private const float CharFhdPixelsPerUnit = 1.5f;
    private const float Char2xPixelsPerUnit = 2f;

    private static readonly string[] All = [Char2x, Char2xExtra, CharFhd, CharFhdExtra, Char];

    public static IReadOnlyList<string> SearchOrder(SpriteFolder folder)
    {
        var matching = Matching(folder);
        return [.. matching, .. All.Except(matching)];
    }

    public static float DefaultPixelsPerUnit(string directory) => directory switch
    {
        Char2x or Char2xExtra => Char2xPixelsPerUnit,
        CharFhd or CharFhdExtra => CharFhdPixelsPerUnit,
        _ => CharPixelsPerUnit,
    };

    private static string[] Matching(SpriteFolder folder) => folder switch
    {
        SpriteFolder.Char2x => [Char2x, Char2xExtra],
        SpriteFolder.CharFhd => [CharFhd, CharFhdExtra],
        _ => [Char],
    };
}
