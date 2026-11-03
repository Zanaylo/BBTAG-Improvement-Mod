namespace BBTAG_IM.Core.Scripting;

public static class StockCommandSizes
{
    private const string ResourceName = "BBTAG_IM.Core.Scripting.Data.command_sizes.tsv";

    public static IReadOnlyDictionary<int, int> Load()
    {
        using var stream = typeof(StockCommandSizes).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidScriptDatabaseException($"{ResourceName} is missing from the mod.");
        using var reader = new StreamReader(stream);

        return CommandSizesText.Read(reader.ReadToEnd());
    }
}
