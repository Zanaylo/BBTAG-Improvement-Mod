using System.Text.Json;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Palettes;

namespace BBTAG_IM.Core.Roster;

public static class CharacterManifestReader
{
    public static CharacterManifest Parse(string json, string source)
    {
        var file = Deserialize(json, source);
        var code = RequiredCode(file.Code, "Code", source);
        var defaultName = code.ToUpperInvariant();

        return new CharacterManifest(
            code,
            RequiredCode(file.Template, "Template", source),
            file.ShortName ?? defaultName,
            OptionalSpriteFolder(file.SpriteFolder, source),
            RequiredSeries(file.Series, source),
            new CharacterNames(file.Names?.Full ?? defaultName, file.Names?.Short ?? defaultName, file.Names?.Select ?? defaultName),
            file.FilesFrom);
    }

    private static CharacterManifestFile Deserialize(string json, string source)
    {
        try
        {
            return JsonSerializer.Deserialize(json, RosterJson.Default.CharacterManifestFile)
                ?? throw new InvalidCharacterManifestException($"{source} is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidCharacterManifestException($"{source} is not valid JSON: {exception.Message}", exception);
        }
    }

    private static string RequiredCode(string? value, string field, string source)
    {
        if (value is null)
            throw new InvalidCharacterManifestException($"{source} has no {field}.");

        if (!Characters.IsCode(value))
            throw new InvalidCharacterManifestException($"{source}: {field} \"{value}\" must be 1 to 4 lowercase letters or digits.");

        return value;
    }

    private static Series RequiredSeries(string? value, string source)
    {
        if (value is null)
            throw new InvalidCharacterManifestException($"{source} has no Series. Use one of: {string.Join(", ", Enum.GetNames<Series>())}.");

        return Enum.TryParse<Series>(value, out var series) && Enum.IsDefined(series)
            ? series
            : throw new InvalidCharacterManifestException($"{source}: Series \"{value}\" is unknown. Use one of: {string.Join(", ", Enum.GetNames<Series>())}.");
    }

    private static SpriteFolder? OptionalSpriteFolder(string? value, string source)
    {
        if (value is null)
            return null;

        return Enum.TryParse<SpriteFolder>(value, out var folder) && Enum.IsDefined(folder)
            ? folder
            : throw new InvalidCharacterManifestException($"{source}: SpriteFolder \"{value}\" is unknown. Use one of: {string.Join(", ", Enum.GetNames<SpriteFolder>())}.");
    }
}
