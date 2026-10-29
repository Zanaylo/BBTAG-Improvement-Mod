using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Tests.Mods;

public class CharacterFileRulesTests
{
    [Fact]
    public void For_Code_PutsTheCodeInEveryFileName()
    {
        // Act
        IReadOnlyList<RequiredFile> files = CharacterFileRules.For("mko");

        // Assert
        Assert.All(files.SelectMany(file => file.Choices), key => Assert.Contains("mko", Path.GetFileName(key)));
    }

    [Fact]
    public void For_Code_AsksForTheScriptArchive()
    {
        // Act
        IReadOnlyList<RequiredFile> files = CharacterFileRules.For("mko");

        // Assert
        Assert.Contains(files, file => file.Name == @"data\char\char_mko_scr.pac");
    }

    [Fact]
    public void For_Code_AcceptsTheSpritesFromAnySpriteFolder()
    {
        // Act
        IReadOnlyList<RequiredFile> files = CharacterFileRules.For("mko");

        // Assert
        RequiredFile sprites = Assert.Single(files, file => file.Name == @"data\char\char_mko_img.pac");
        Assert.Equal([@"data\char\char_mko_img.pac", @"data\charfhd\char_mko_img.pac", @"data\char2x\char_mko_img.pac"], sprites.Choices);
    }

    [Fact]
    public void For_Code_AsksForTheBattleSubtitlesInEveryLanguage()
    {
        // Act
        IReadOnlyList<RequiredFile> files = CharacterFileRules.For("mko");

        // Assert
        Assert.Equal(9, files.Count(file => file.Name.EndsWith("btl_subtitle_mko.pac", StringComparison.Ordinal)));
    }

    [Fact]
    public void For_Code_AsksForTheSoundEffects()
    {
        // Act
        IReadOnlyList<RequiredFile> files = CharacterFileRules.For("mko");

        // Assert
        Assert.Contains(files, file => file.Name == @"data\sound\se\semko.pac");
    }

    [Fact]
    public void For_Code_AsksForTheAstralHeatBackground()
    {
        // Act
        IReadOnlyList<RequiredFile> files = CharacterFileRules.For("mko");

        // Assert
        Assert.Contains(files, file => file.Name == @"data\bg\astral\bg_exastralmko.pac");
        Assert.Contains(files, file => file.Name == @"data\bg\astral\bg_exastralmko_img.pac");
    }

    [Fact]
    public void For_Code_AsksForEveryFileOnce()
    {
        // Act
        IReadOnlyList<RequiredFile> files = CharacterFileRules.For("mko");

        // Assert
        Assert.Equal(50, files.Select(file => file.Name).Distinct().Count());
    }
}
