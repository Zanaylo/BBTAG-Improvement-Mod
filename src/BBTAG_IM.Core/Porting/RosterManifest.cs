using System.Text.Json;

namespace BBTAG_IM.Core.Porting;

public sealed record RosterEntry(string Code, string Name, string Series)
{
    public const string DefaultSeries = "Others";
}

public static class RosterManifest
{
    public const string EngineTemplate = "brg";
    public const string FilesTemplate = "uhy";

    private const string ManifestFolder = "characters";
    private const string ManifestExtension = ".json";

    public static string PathOf(RosterEntry roster) => Path.Combine(ManifestFolder, roster.Code + ManifestExtension);

    public static byte[] Write(RosterEntry roster)
    {
        using var stream = new MemoryStream();

        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("Code", roster.Code);
            json.WriteString("Template", EngineTemplate);
            json.WriteString("FilesFrom", FilesTemplate);
            json.WriteString("ShortName", "U_" + roster.Name.ToUpperInvariant().Replace(' ', '_'));
            json.WriteString("Series", roster.Series);
            json.WriteStartObject("Names");
            json.WriteString("Full", roster.Name);
            json.WriteString("Short", roster.Name.ToUpperInvariant());
            json.WriteString("Select", roster.Name.ToUpperInvariant());
            json.WriteEndObject();
            json.WriteEndObject();
        }

        return stream.ToArray();
    }
}
