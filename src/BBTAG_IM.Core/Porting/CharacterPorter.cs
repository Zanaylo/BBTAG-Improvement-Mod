using System.Text;
using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Palettes;
using BBTAG_IM.Core.Porting.Moves;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Compiling;
using BBTAG_IM.Core.Sprites;

namespace BBTAG_IM.Core.Porting;

public sealed record PortRequest(
    IHanteiGameFiles Source,
    string Character,
    string GameData,
    string SlotCode,
    string ImagePrefix,
    string SpritePrefix,
    bool ForeignBody,
    RosterEntry? Roster = null)
{
    public string OutputCode => Roster?.Code ?? SlotCode;
}

public sealed record PortedFile(string Path, byte[] Bytes);

public sealed class CharacterPorter(IFileSystem files, Func<string, ScriptDatabase> databases)
{
    private const string JonbinExtension = ".jonbin";
    private const string HipExtension = ".hip";
    private const string ArchiveFolder = "char";
    private const string BodyPaletteSuffix = "_00.hpl";
    private const string ScriptExtension = ".bin";
    private const string EffectScriptSuffix = "ea";
    private const int BasePalette = 0;
    private const int ColorDigits = 2;
    private const string CommandsSuffix = "_cmd_0.txt";
    private const string MovesSuffix = "_mv_0.txt";
    private const string VectorsFile = "../VectorTable.txt";
    private const string CpuSuffix = "_com_0.txt";
    private const string StatusSuffix = "_sc_0.txt";
    private const string CommandTableFile = "script/btl_StdCommdandTbl.txt";
    private const string DefinesFile = "script/btl_Define.txt";
    private const string MoveTableFile = "script/btl_StdMoveTbl.txt";
    private const string StatusTableFile = "script/btl_Std_BattleStatusTbl.txt";
    private const string CharacterNamesFile = "script/btl_Define_Chara.txt";

    private static readonly string[] DoubleFolders = ["charfhd", "char2x"];

    public IReadOnlyList<PortedFile> Port(PortRequest request)
    {
        var cast = ReadCast(request);
        List<PortedFile> ported =
        [
            Pictures(request, ArchiveFolder, cast, SheetPictures.Single),
            Collision(request, cast),
            Colors(request, cast.Palettes),
            Scripts(request, cast.Patterns),
        ];

        if (request.Roster is { } roster)
            ported.Add(new PortedFile(RosterManifest.PathOf(roster), RosterManifest.Write(roster)));

        ported.AddRange(DoubleFolders
            .Where(folder => request.Roster is not null || files.FileExists(GameArchive(request, folder, "img")))
            .Select(folder => Pictures(request, folder, cast, SheetPictures.Double)));
        return ported;
    }

    private delegate byte[] PictureWriter(CgImage image, ReadOnlySpan<byte> palette);

    private sealed record Cast(IReadOnlyDictionary<int, Ha6Pattern> Patterns, IReadOnlyList<byte[]> Palettes, Dictionary<int, CgImage> Images);

    private Cast ReadCast(PortRequest request)
    {
        var character = request.Character;
        var patterns = ReadPatterns(request, character);
        var sheet = CgFile.Read(ReadSource(request, character, character + ".cg"));
        var palettes = PalFile.Read(ReadSource(request, character, character + ".pal"));
        return new Cast(patterns, palettes, ReadImages(patterns, sheet, request.ImagePrefix));
    }

    private static string ArchiveName(string code, string kind) => $"char_{code}_{kind}.pac";

    private static string OutputPath(string folder, string name) => Path.Combine("data", folder, name);

    private string GameArchive(PortRequest request, string folder, string kind) =>
        Path.Combine(request.GameData, folder, ArchiveName(request.SlotCode, kind));

    private static byte[] ReadSource(PortRequest request, string character, string file) =>
        request.Source.Read(HanteiFileName.InCharacter(character, file));

    private IReadOnlyDictionary<int, Ha6Pattern> ReadPatterns(PortRequest request, string characterCode)
    {
        var loadList = Encoding.Latin1.GetString(ReadSource(request, characterCode, characterCode + "_0.txt"));
        List<IReadOnlyDictionary<int, Ha6Pattern>> character = [];
        IReadOnlyDictionary<int, Ha6Pattern> shared = new Dictionary<int, Ha6Pattern>();

        foreach (var file in Ha6Stack.Files(loadList))
        {
            var patterns = Ha6File.Read(ReadSource(request, characterCode, file));

            if (Ha6Stack.IsSharedTemplate(file))
                shared = patterns;
            else
                character.Add(patterns);
        }

        return Ha6File.Merge(character, shared);
    }

    private static Dictionary<int, CgImage> ReadImages(IReadOnlyDictionary<int, Ha6Pattern> patterns, CgFile sheet, string prefix)
    {
        Dictionary<int, CgImage> images = [];

        foreach (var layer in patterns.Values.SelectMany(pattern => pattern.Frames).SelectMany(frame => frame.Layers))
        {
            if (layer.UsesPat || layer.Sprite < 0 || images.ContainsKey(layer.Sprite) || sheet.Image(layer.Sprite) is not { } image)
                continue;

            images[layer.Sprite] = image with { Name = SheetPictures.Name(image.Name, prefix) };
        }

        return images;
    }

    private PortedFile Pictures(PortRequest request, string folder, Cast cast, PictureWriter writer)
    {
        var loose = cast.Images.Values
            .Select(image => new ArchiveFile(image.Name + HipExtension, writer(image, cast.Palettes[BasePalette])))
            .ToList();

        var archive = request.Roster is null ? Merged(GameArchive(request, folder, "img"), loose) : LooseArchive.Write(loose);
        return new PortedFile(OutputPath(folder, ArchiveName(request.OutputCode, "img")), archive);
    }

    private PortedFile Collision(PortRequest request, Cast cast)
    {
        List<ArchiveFile> loose = [];

        foreach (var pattern in cast.Patterns.Values)
        {
            for (var frame = 0; frame < pattern.Frames.Count; frame++)
            {
                var bytes = FrameJonbin.Build(pattern.Frames[frame], sprite => Sheet(cast.Images, sprite));
                loose.Add(new ArchiveFile(SpriteRemap.FrameName(request.SpritePrefix, pattern.Id, frame) + JonbinExtension, bytes));
            }
        }

        return new PortedFile(OutputPath(ArchiveFolder, ArchiveName(request.OutputCode, "col")), Merged(GameArchive(request, ArchiveFolder, "col"), loose));
    }

    private static SheetImage? Sheet(Dictionary<int, CgImage> images, int sprite) =>
        images.TryGetValue(sprite, out var image) ? new SheetImage(image.Name, image.CanvasWidth, image.CanvasHeight) : null;

    private PortedFile Colors(PortRequest request, IReadOnlyList<byte[]> palettes)
    {
        var archive = GameArchive(request, ArchiveFolder, "pal");
        var loose = LooseArchive.Read(files.ReadAllBytes(archive))
            .Where(file => file.Name.EndsWith(BodyPaletteSuffix, StringComparison.OrdinalIgnoreCase))
            .Select(file => (file.Name, Color: ColorOf(file.Name)))
            .Where(entry => entry.Color < palettes.Count)
            .Select(entry => new ArchiveFile(entry.Name, HplWriter.Write(palettes[entry.Color])))
            .ToList();

        return new PortedFile(OutputPath(ArchiveFolder, ArchiveName(request.OutputCode, "pal")), Merged(archive, loose));
    }

    private static int ColorOf(string name) => int.Parse(name.AsSpan(name.Length - BodyPaletteSuffix.Length - ColorDigits, ColorDigits));

    private PortedFile Scripts(PortRequest request, IReadOnlyDictionary<int, Ha6Pattern> patterns)
    {
        var durations = patterns.ToDictionary(entry => entry.Key, entry => entry.Value.Frames.Select(frame => frame.Duration).ToArray());
        var remap = new SpriteRemap(durations, keepOtherSprites: !request.ForeignBody, request.SpritePrefix, Aliases(request));
        var native = NativeMoves(request, patterns);
        var archive = GameArchive(request, ArchiveFolder, "scr");
        var loose = ScriptArchive.Scripts(files.ReadAllBytes(archive))
            .Where(script => request.Roster is not null || !request.ForeignBody || !IsEffectScript(script.Name))
            .Select(script => new ArchiveFile(script.Name.Replace(request.SlotCode, request.OutputCode) + ScriptExtension, Rebuild(script, remap, request, native)))
            .ToList();
        var bytes = request.Roster is null ? Merged(archive, loose) : LooseArchive.Write(loose);

        return new PortedFile(OutputPath(ArchiveFolder, ArchiveName(request.OutputCode, "scr")), bytes);
    }

    private static bool IsEffectScript(string name) => name.EndsWith(EffectScriptSuffix, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<int, int>? Aliases(PortRequest request) =>
        request.ForeignBody && request.Source.Contains(HanteiFileName.InCharacter(request.Character, VectorsFile))
            ? PatternAliases.Parse(ReadText(request, request.Character, VectorsFile))
            : null;

    private static MoveScripts? NativeMoves(PortRequest request, IReadOnlyDictionary<int, Ha6Pattern> patterns)
    {
        var character = request.Character;

        if (request.Roster is null || !request.ForeignBody || !HasMoveTables(request.Source, character))
            return null;

        return MoveScripts.Build(
            ReadText(request, character, character + CommandsSuffix),
            ReadText(request, character, character + MovesSuffix),
            ReadText(request, character, VectorsFile),
            patterns,
            request.SpritePrefix,
            NativeCommandTable.Parse(ReadScript(request, CommandTableFile), ReadScript(request, DefinesFile)).WithCharacter(ReadText(request, character, character + CommandsSuffix)),
            ReadOptional(request, character, character + CpuSuffix),
            CharacterStats.Parse(ReadScript(request, MoveTableFile), ReadOptional(request, character, character + StatusSuffix), ReadScript(request, StatusTableFile), character, ReadScript(request, CharacterNamesFile)));
    }

    private static string ReadScript(PortRequest request, string file) =>
        request.Source.Contains(file) ? Encoding.Latin1.GetString(request.Source.Read(file)) : string.Empty;

    private static string ReadOptional(PortRequest request, string character, string file) =>
        ReadScript(request, HanteiFileName.InCharacter(character, file));

    private static bool HasMoveTables(IHanteiGameFiles source, string character) =>
        new[] { character + CommandsSuffix, character + MovesSuffix, VectorsFile }.All(file => source.Contains(HanteiFileName.InCharacter(character, file)));

    private static string ReadText(PortRequest request, string character, string file) => Encoding.Latin1.GetString(ReadSource(request, character, file));

    private byte[] Rebuild(ScriptFile script, SpriteRemap remap, PortRequest request, MoveScripts? native)
    {
        if (request.Roster is not null && IsEffectScript(script.Name))
            return native is null ? script.Bytes : WithObjects(script, native);

        var database = databases(script.Name);
        var mapped = remap.Apply(Decompile(script, database));
        var built = native is null ? Filtered(mapped, request) : MoveTemplate.Replace(ScriptFilters.Silence(mapped), native.Registrations, native.States, native.ReplacedStates, native.Stats);
        return new ScriptCompiler(database, ScriptLanguage.Python).Compile(built).Bytes;
    }

    private static string Filtered(string mapped, PortRequest request) =>
        request.ForeignBody ? ScriptFilters.Silence(ScriptFilters.DropSpecialMoves(mapped)) : mapped;

    private static string Decompile(ScriptFile script, ScriptDatabase database) =>
        ScriptLanguages.Decompiler(database, ScriptLanguage.Python).Decompile(ScriptBinary.Read(script.Bytes, database), script.Name);

    private byte[] WithObjects(ScriptFile script, MoveScripts native)
    {
        if (native.Objects.Length == 0)
            return script.Bytes;

        var database = databases(script.Name);
        return new ScriptCompiler(database, ScriptLanguage.Python).Compile($"{Decompile(script, database).TrimEnd()}\n\n\n{native.Objects}").Bytes;
    }

    private byte[] Merged(string archive, IReadOnlyList<ArchiveFile> loose) => LooseArchive.Merge(files.ReadAllBytes(archive), loose);
}
