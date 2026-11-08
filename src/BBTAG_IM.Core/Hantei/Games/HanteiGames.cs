using BBTAG_IM.Core.IO;

namespace BBTAG_IM.Core.Hantei.Games;

public static class HanteiGames
{
    public const string Uni2Title = "UNDER NIGHT IN-BIRTH II Sys:Celes";
    public const string MbtlTitle = "MELTY BLOOD: TYPE LUMINA";

    private const string Uni2Letter = "u";
    private const string MbtlLetter = "m";

    public static HanteiGame Open(string path, IFileSystem files)
    {
        var root = RootOf(path, files);

        if (files.FileExists(Path.Combine(root, Uni2Archives.ArchiveFolder, Uni2Archives.DataIndex)))
            return new HanteiGame(Uni2Title, root, Uni2Archives.Open(files, root), Uni2Letter);

        if (files.FileExists(Path.Combine(root, MbtlArchives.ExecutableName)))
            return new HanteiGame(MbtlTitle, root, MbtlArchives.Open(files, root), MbtlLetter);

        if (files.DirectoryExists(Path.Combine(root, HanteiFileName.DataFolder)))
            return new HanteiGame(Path.GetFileName(root), root, new UnpackedGameFiles(files, root));

        throw new InvalidHanteiDataException($"{path} is not UNDER NIGHT IN-BIRTH II, MELTY BLOOD TYPE LUMINA or a folder with their files.");
    }

    private static string RootOf(string path, IFileSystem files)
    {
        var folder = files.FileExists(path) ? Path.GetDirectoryName(path) ?? path : Path.TrimEndingDirectorySeparator(path);

        return Path.GetFileName(folder).Equals(HanteiFileName.DataFolder, StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(folder) ?? folder
            : folder;
    }
}
