using BBTAG_IM.Core.Hantei.Games;
using BBTAG_IM.Core.Hitboxes.Editing;
using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Porting;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Porting;

public class CharacterPorterTests
{
    private const string Game = @"X:\game";

    private readonly IFileSystem _files = Substitute.For<IFileSystem>();
    private readonly IHanteiGameFiles _source = Substitute.For<IHanteiGameFiles>();
    private readonly ScriptDatabase _database = ScriptTestDatabase.Create();

    [Fact]
    public void Port_Frame_WritesItsJonbinUnderTheSpriteName()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Action_", foreign: false));

        // Assert
        Jonbin jonbin = Jonbin.Read(Entry(ported, @"char\char_uhy_col.pac", "Action_000_00.jonbin"));
        Assert.Equal(["Uni000_000.bmp"], jonbin.Images);
    }

    [Fact]
    public void Port_Image_WritesItInEveryPictureFolder()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Action_", foreign: false));

        // Assert
        Assert.NotEmpty(Entry(ported, @"char\char_uhy_img.pac", "Uni000_000.hip"));
        Assert.NotEmpty(Entry(ported, @"charfhd\char_uhy_img.pac", "Uni000_000.hip"));
        Assert.NotEmpty(Entry(ported, @"char2x\char_uhy_img.pac", "Uni000_000.hip"));
    }

    [Fact]
    public void Port_BodyColor_ReplacesOnlyTheBodyPalette()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Action_", foreign: false));

        // Assert
        Assert.Equal(1056, Entry(ported, @"char\char_uhy_pal.pac", "uhy00_00.hpl").Length);
        Assert.Equal(4, Entry(ported, @"char\char_uhy_pal.pac", "uhy00_01.hpl").Length);
    }

    [Fact]
    public void Port_SlotWithShortPaletteNames_ReplacesItsBodyPalette()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();
        _files.ReadAllBytes(Arg.Is<string>(path => path.EndsWith("_pal.pac"))).Returns(LooseArchive.Write([new ArchiveFile("rg00_00.hpl", [0, 0, 0, 0])]));

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Action_", foreign: false));

        // Assert
        Assert.Equal(1056, Entry(ported, @"char\char_uhy_pal.pac", "rg00_00.hpl").Length);
    }

    [Fact]
    public void Port_NativeBody_TakesTheDurationFromTheSource()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Action_", foreign: false));

        // Assert
        Assert.Contains("sprite('Action_000_00', 6)", ScriptText(ported));
    }

    [Fact]
    public void Port_ForeignBody_UsesTheOtherSpritePrefix()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Port_", foreign: true));

        // Assert
        Assert.Contains("sprite('Port_000_00', 6)", ScriptText(ported));
        Assert.NotEmpty(Entry(ported, @"char\char_uhy_col.pac", "Port_000_00.jonbin"));
    }

    [Fact]
    public void Port_ForeignBody_LeavesTheEffectScriptAlone()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Port_", foreign: true));

        // Assert
        byte[] effects = Entry(ported, @"char\char_uhy_scr.pac", "scr_uhyea.bin");
        Assert.Equal(EffectScript(), effects);
    }

    [Fact]
    public void Port_SlotWithoutAPictureFolder_SkipsThatFolder()
    {
        // Arrange
        CharacterPorter porter = InstantiateCharacterPorter();
        _files.FileExists(Arg.Is<string>(path => path.Contains("charfhd"))).Returns(false);

        // Act
        IReadOnlyList<PortedFile> ported = porter.Port(Request("Action_", foreign: false));

        // Assert
        Assert.DoesNotContain(ported, file => file.Path.Contains("charfhd"));
        Assert.Contains(ported, file => file.Path.Contains("char2x"));
    }

    private static byte[] EffectScript() => new ScriptBytes().State("Eff").Command(2, "Action_000_00", 7).Command(1).Build();

    private PortRequest Request(string spritePrefix, bool foreign) => new(_source, "chr000", Game, "uhy", "Uni", spritePrefix, foreign);

    private static byte[] Entry(IReadOnlyList<PortedFile> ported, string archive, string name) =>
        LooseArchive.Read(ported.Single(file => file.Path.EndsWith(archive, StringComparison.OrdinalIgnoreCase)).Bytes)
            .Single(file => file.Name == name).Bytes;

    private CharacterPorter InstantiateCharacterPorter()
    {
        byte[] tile = Enumerable.Repeat((byte)1, 256).ToArray();
        byte[] pattern = new Ha6Bytes().Pattern(0, "Stand", body => body.Frame(frame => frame.Anim(anim => anim.Tag("AFGX", 0, 0, 0).Tag("AFD6")))).Build();
        byte[] template = new Ha6Bytes().Build();
        byte[] sheet = new CgBytes().Image("000_000.bmp", 64, 0, 0, 15, 15, [tile], [(0, 0)]).Build();
        byte[] palettes = new byte[16 + 1024];
        BitConverter.TryWriteBytes(palettes.AsSpan(12), 1);
        byte[] script = new ScriptBytes().State("CmnActStand").Command(2, "Action_000_00", 7).Command(1).Build();

        _source.Read("data/chr000/chr000_0.txt").Returns("[DataFile]\nFile00=chr000.ha6\nFile01=../BaseData.ha6\n"u8.ToArray());
        _source.Read("data/chr000/chr000.ha6").Returns(pattern);
        _source.Read("data/BaseData.ha6").Returns(template);
        _source.Read("data/chr000/chr000.cg").Returns(sheet);
        _source.Read("data/chr000/chr000.pal").Returns(palettes);
        _files.FileExists(Arg.Any<string>()).Returns(true);
        _files.ReadAllBytes(Arg.Is<string>(path => path.EndsWith("_img.pac") || path.EndsWith("_col.pac"))).Returns(LooseArchive.Write([new ArchiveFile("old", [1])]));
        _files.ReadAllBytes(Arg.Is<string>(path => path.EndsWith("_pal.pac"))).Returns(LooseArchive.Write([new ArchiveFile("uhy00_00.hpl", [0, 0, 0, 0]), new ArchiveFile("uhy00_01.hpl", [0, 0, 0, 0])]));
        _files.ReadAllBytes(Arg.Is<string>(path => path.EndsWith("_scr.pac"))).Returns(LooseArchive.Write([new ArchiveFile("scr_uhy.bin", script), new ArchiveFile("scr_uhyea.bin", EffectScript())]));

        return new CharacterPorter(_files, _ => _database);
    }

    private string ScriptText(IReadOnlyList<PortedFile> ported)
    {
        byte[] script = Entry(ported, @"char\char_uhy_scr.pac", "scr_uhy.bin");
        return ScriptLanguages.Decompiler(_database, ScriptLanguage.Python).Decompile(ScriptBinary.Read(script, _database), "scr_uhy");
    }
}
