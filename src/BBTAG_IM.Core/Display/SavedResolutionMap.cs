namespace BBTAG_IM.Core.Display;

public sealed class SavedResolutionMap(IReadOnlyList<Resolution> native, IReadOnlyList<Resolution> running)
{
    private const string ResolutionSetting = "iDisplayResolution";

    public static bool IsResolutionSetting(string? name) => string.Equals(name, ResolutionSetting, StringComparison.OrdinalIgnoreCase);

    public int ToRunning(int saved) => PositionOfSameSize(saved, native, running);

    public int ToSaved(int position) => PositionOfSameSize(position, running, native);

    public string ToSavedText(string position) => int.TryParse(position, out var value) ? ToSaved(value).ToString() : position;

    private static int PositionOfSameSize(int position, IReadOnlyList<Resolution> from, IReadOnlyList<Resolution> to)
    {
        if (position < 0 || position >= from.Count)
            return position;

        var sameSize = to.ToList().IndexOf(from[position]);
        return sameSize < 0 ? position : sameSize;
    }
}
