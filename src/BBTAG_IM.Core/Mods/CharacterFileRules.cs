namespace BBTAG_IM.Core.Mods;

public static class CharacterFileRules
{
    private const string CodeMark = "{c}";
    private const string LanguageMark = "{l}";

    private static readonly string[] Languages = ["chi", "eng", "fre", "ger", "ita", "jpn", "kor", "sim", "spa"];

    private static readonly string[] SpriteFolders = [@"data\char", @"data\charfhd", @"data\char2x"];

    private static readonly string[] SpriteArchives = [@"char_{c}_img.pac", @"char_{c}_vri.pac"];

    private static readonly string[] CommonArchives = ["a", "b", "d", "e", "f", "g", "h", "m", "s"];

    private static readonly string[] SingleFiles =
    [
        @"data\char\char_{c}_col.pac",
        @"data\char\char_{c}_pal.pac",
        @"data\char\char_{c}_scr.pac",
        @"data\bg\astral\bg_exastral{c}.pac",
        @"data\bg\astral\bg_exastral{c}_img.pac",
        @"data\common\cmn_nm\cmn_nm_b_{c}.pac",
        @"data\common\cmn_nm\cmn_nm_c_{c}.pac",
        @"data\common\cmn_nm_e_{c}.pac",
        @"data\eff3d\eff3d_{c}.pac",
        @"data\eff3d\eff3d_{c}_img.pac",
        @"data\etc\char_select_pal\char_select_{c}_pal.pac",
        @"data\etc\cockpit_sub_simg\cockpit_sub_simg_{c}.pac",
        @"data\particle\particle_dat_{c}.pac",
        @"data\particle\particle_img_{c}.pac",
        @"data\sound\se\se{c}.pac",
        @"data\sound\voice\result\vresult_{c}.pac",
        @"data\sound\voice\vbtl_{c}_0.pac",
        @"data\sound\voice\vbtldb_{c}_0.pac",
        @"data\sound\voice_eng\result\vresult_{c}.pac",
        @"data\sound\voice_eng\vbtl_{c}_0.pac",
        @"data\sound\voice_eng\vbtldb_{c}_0.pac",
    ];

    private static readonly string[] PerLanguageFiles =
    [
        @"data\common\cmn_nm\cmn_nm_d\{l}\cmn_nm_d_{c}.pac",
        @"data\etc\battle_subtitle_text\{l}\btl_subtitle_{c}.pac",
    ];

    private static readonly IReadOnlyList<RequiredFile> Templates =
    [
        .. SpriteArchives.Select(archive => new RequiredFile(SpriteFolders.Select(folder => Path.Join(folder, archive)).ToArray())),
        .. SingleFiles.Select(Single),
        .. CommonArchives.Select(letter => Single($@"data\common\cmn_ch_{letter}_{CodeMark}.pac")),
        .. PerLanguageFiles.SelectMany(file => Languages.Select(language => Single(file.Replace(LanguageMark, language)))),
    ];

    public static IReadOnlyList<RequiredFile> For(string code) =>
        Templates.Select(template => new RequiredFile(template.Choices.Select(choice => choice.Replace(CodeMark, code)).ToArray())).ToArray();

    private static RequiredFile Single(string key) => new([key]);
}
