using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Palettes;

public sealed record CharacterPalettes(string Character, IReadOnlyList<ImplPalette> Palettes, IReadOnlyList<string> Problems)
{
    public ImplPalette? Find(string name) =>
        Palettes.FirstOrDefault(palette => palette.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

public sealed class PaletteLibrary(IFileSystem fileSystem, string modFolder)
{
    public const string FolderName = "palettes";
    public const string GameColors = "Game colors";

    private static readonly char[] NameBreakers = [.. Path.GetInvalidFileNameChars(), '/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    public string Folder => Path.Combine(modFolder, FolderName);

    public static bool IsUsableName(string name) =>
        name.Length > 0
        && !char.IsWhiteSpace(name[0])
        && name[0] != '.'
        && name.IndexOfAny(NameBreakers) < 0
        && !name.Equals(GameColors, StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> CharactersWithPalettes()
    {
        if (!fileSystem.DirectoryExists(Folder))
            return [];

        var folders = fileSystem.EnumerateDirectories(Folder).Select(path => Path.GetFileName(path)!).ToArray();
        var gameCharacters = Characters.Names.Where(name => folders.Contains(name, StringComparer.OrdinalIgnoreCase));
        var moddedCharacters = folders
            .Where(folder => Characters.IsCode(folder) || Characters.IsNumbered(folder))
            .Order(StringComparer.OrdinalIgnoreCase);

        return [.. gameCharacters, .. moddedCharacters];
    }

    public CharacterPalettes Load(string character)
    {
        var folder = CharacterFolder(character);

        if (!fileSystem.DirectoryExists(folder))
            return new CharacterPalettes(character, [], []);

        List<ImplPalette> palettes = [];
        List<string> problems = [];

        foreach (var path in PalettePaths(folder).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            if (Read(path, problems) is not { } palette)
                continue;

            if (Problem(character, palette, palettes) is { } problem)
                problems.Add(problem);
            else
                palettes.Add(palette);
        }

        return new CharacterPalettes(character, palettes, problems);
    }

    public string Save(string character, ImplPalette palette)
    {
        var folder = CharacterFolder(character);
        var fileName = palette.Name + ImplPalette.Extension;
        var existing = fileSystem.DirectoryExists(folder)
            ? PalettePaths(folder).FirstOrDefault(path => Path.GetFileName(path).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            : null;
        var path = existing ?? Path.Combine(folder, fileName);

        fileSystem.WriteAllBytes(path, palette.ToBytes());
        return path;
    }

    private string CharacterFolder(string character) => Path.Combine(Folder, character);

    private IEnumerable<string> PalettePaths(string folder) => fileSystem.EnumerateFilesRecursively(folder).Where(IsPalette);

    private ImplPalette? Read(string path, List<string> problems)
    {
        try
        {
            return ImplPalette.Parse(Path.GetFileNameWithoutExtension(path), fileSystem.ReadAllBytes(path));
        }
        catch (InvalidPaletteException exception)
        {
            problems.Add(exception.Message);
        }
        catch (IOException exception)
        {
            problems.Add($"{Path.GetFileName(path)} could not be read: {exception.Message}");
        }

        return null;
    }

    private static string? Problem(string character, ImplPalette palette, IReadOnlyList<ImplPalette> accepted)
    {
        if (Characters.IndexOf(character) is { } index && palette.CharacterIndex != index)
            return $"{palette.Name}{ImplPalette.Extension} is a palette of {Characters.NameOf(palette.CharacterIndex) ?? "an unknown character"}, so the {character} folder skips it.";

        if (accepted.Any(other => other.Name.Equals(palette.Name, StringComparison.OrdinalIgnoreCase)))
            return $"{character} has two palettes named {palette.Name}. Only the first one is used.";

        return null;
    }

    private static bool IsPalette(string path) =>
        Path.GetExtension(path).Equals(ImplPalette.Extension, StringComparison.OrdinalIgnoreCase);
}
