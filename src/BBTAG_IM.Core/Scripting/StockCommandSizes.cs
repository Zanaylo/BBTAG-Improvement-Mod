using System.Globalization;

namespace BBTAG_IM.Core.Scripting;

public static class StockCommandSizes
{
    private const string ResourceName = "BBTAG_IM.Core.Scripting.Data.command_sizes.tsv";
    private const char Separator = '\t';

    public static IReadOnlyDictionary<int, int> Load()
    {
        using var stream = typeof(StockCommandSizes).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidScriptDatabaseException($"{ResourceName} is missing from the mod.");
        using var reader = new StreamReader(stream);
        var sizes = new Dictionary<int, int>();

        while (reader.ReadLine() is { } line)
        {
            if (line.Split(Separator) is [var id, var size])
                sizes[int.Parse(id, CultureInfo.InvariantCulture)] = int.Parse(size, CultureInfo.InvariantCulture);
        }

        return sizes;
    }
}
