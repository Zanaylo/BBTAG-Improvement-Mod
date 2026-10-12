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

    private const string NumberSeparator = "_";
    private const int FirstNumber = 2;

    private static readonly char[] NameBreakers = [.. Path.GetInvalidFileNameChars(), '/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    public string Folder => Path.Combine(modFolder, FolderName);

    public static bool IsUsableName(string name) =>
        name.Length > 0
        && !char.IsWhiteSpace(name[0])
        && name[0] != '.'
        && name.IndexOfAny(NameBreakers) < 0
        && !name.Equals(GameColors, StringComparison.OrdinalIgnoreCase)
        && !RandomPalette.IsChoice(name);

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

        try
        {
            return Read(character, PalettePaths(folder).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).ToArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new CharacterPalettes(character, [], [$"The {character} palettes could not be read: {exception.Message}"]);
        }
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

    public ImplPalette Open(string path) => ImplPalette.Parse(Path.GetFileNameWithoutExtension(path), fileSystem.ReadAllBytes(path));

    public string Import(ImplPalette palette, string character)
    {
        var name = FreeName(character, palette.Name);
        var index = Characters.IndexOf(character) ?? palette.CharacterIndex;
        Save(character, ImplPalette.Create(name, index, palette.Creator, palette.Description, palette.Colors()));
        return name;
    }

    public void Export(ImplPalette palette, string path) => fileSystem.WriteAllBytes(path, palette.ToBytes());

    public int Delete(string character, string name)
    {
        var folder = CharacterFolder(character);

        if (!fileSystem.DirectoryExists(folder))
            return 0;

        var named = PalettePaths(folder)
            .Where(path => Path.GetFileNameWithoutExtension(path).Equals(name, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var path in named)
            fileSystem.DeleteFile(path);

        return named.Length;
    }

    private string FreeName(string character, string name)
    {
        var folder = CharacterFolder(character);

        if (!fileSystem.DirectoryExists(folder))
            return name;

        var taken = PalettePaths(folder).Select(Path.GetFileNameWithoutExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var free = name;

        for (var number = FirstNumber; taken.Contains(free); number++)
            free = $"{name}{NumberSeparator}{number}";

        return free;
    }

    private string CharacterFolder(string character) => Path.Combine(Folder, character);

    private IEnumerable<string> PalettePaths(string folder) => fileSystem.EnumerateFilesRecursively(folder).Where(IsPalette);

    private CharacterPalettes Read(string character, IReadOnlyList<string> paths)
    {
        List<ImplPalette> palettes = [];
        List<string> problems = [];

        foreach (var path in paths)
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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
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
