using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Roster;

public static class ModCharacterManifests
{
    public const string FolderName = "characters";

    private const string ManifestExtension = ".json";

    public static ManifestLoad Load(IEnumerable<ModFolder> modsByPriority, IFileSystem fileSystem)
    {
        List<CharacterManifest> manifests = [];
        List<string> problems = [];

        foreach (var file in modsByPriority.SelectMany(mod => ManifestFiles(mod, fileSystem)))
        {
            try
            {
                manifests.Add(CharacterManifestReader.Parse(fileSystem.ReadAllText(file), file));
            }
            catch (InvalidCharacterManifestException exception)
            {
                problems.Add(exception.Message);
            }
        }

        return new ManifestLoad(manifests, problems);
    }

    private static IEnumerable<string> ManifestFiles(ModFolder mod, IFileSystem fileSystem)
    {
        var folder = Path.Combine(mod.RootPath, FolderName);

        if (!fileSystem.DirectoryExists(folder))
            return [];

        return fileSystem.EnumerateFiles(folder)
            .Where(file => Path.GetExtension(file).Equals(ManifestExtension, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase);
    }
}
