using BBTAG_IM.Core.Hitboxes.Editing;

namespace BBTAG_IM.Core.Tests.Hitboxes.Editing;

public class SavedFilesTests
{
    private const string RagnaFolder = @"D:\BBTAG\mods\DEV\data\char\char_brg_col";
    private const string JinFolder = @"D:\BBTAG\mods\DEV\data\char\char_bjn_col";
    private const string CharacterFolder = @"D:\BBTAG\mods\DEV\data\char";

    [Fact]
    public void CommonFolder_OneFile_ReturnsItsFolder()
    {
        // Act
        string? folder = SavedFiles.CommonFolder([RagnaFolder + @"\rg211_11.jonbin"]);

        // Assert
        Assert.Equal(RagnaFolder, folder);
    }

    [Fact]
    public void CommonFolder_FilesOfOneCharacter_ReturnsTheirFolder()
    {
        // Act
        string? folder = SavedFiles.CommonFolder([RagnaFolder + @"\rg211_11.jonbin", RagnaFolder + @"\rg211_12.jonbin"]);

        // Assert
        Assert.Equal(RagnaFolder, folder);
    }

    [Fact]
    public void CommonFolder_FilesOfTwoCharacters_ReturnsTheFolderHoldingBoth()
    {
        // Act
        string? folder = SavedFiles.CommonFolder([RagnaFolder + @"\rg211_11.jonbin", JinFolder + @"\jn211_11.jonbin"]);

        // Assert
        Assert.Equal(CharacterFolder, folder);
    }

    [Fact]
    public void CommonFolder_FoldersThatOnlyStartAlike_DoesNotStopAtTheShorterName()
    {
        // Act
        string? folder = SavedFiles.CommonFolder([CharacterFolder + @"\a.jonbin", CharacterFolder + @"2\b.jonbin"]);

        // Assert
        Assert.Equal(@"D:\BBTAG\mods\DEV\data", folder);
    }

    [Fact]
    public void CommonFolder_NoFiles_ReturnsNull()
    {
        // Act
        string? folder = SavedFiles.CommonFolder([]);

        // Assert
        Assert.Null(folder);
    }
}
