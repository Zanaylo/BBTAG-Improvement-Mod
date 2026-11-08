using BBTAG_IM.Core.Mods.Archives;

namespace BBTAG_IM.Core.Tests.Mods.Archives;

public class ArchiveEntryPathTests
{
    private const string Folder = @"D:\mods\.unpacking-Foo";

    [Theory]
    [InlineData("data/char/file.pac", @"D:\mods\.unpacking-Foo\data\char\file.pac")]
    [InlineData(@"data\file.pac", @"D:\mods\.unpacking-Foo\data\file.pac")]
    [InlineData("data/../file.pac", @"D:\mods\.unpacking-Foo\file.pac")]
    public void Inside_EntryInsideTheFolder_ReturnsItsPath(string key, string expected)
    {
        // Act
        string path = ArchiveEntryPath.Inside(Folder, key);

        // Assert
        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData("../evil.dll")]
    [InlineData(@"..\evil.dll")]
    [InlineData("data/../../evil.dll")]
    [InlineData("../.unpacking-Foo-sibling/evil.dll")]
    [InlineData(@"C:\Windows\evil.dll")]
    [InlineData("/Windows/evil.dll")]
    public void Inside_EntryOutsideTheFolder_ThrowsModArchive(string key)
    {
        // Act
        Exception? exception = Record.Exception(() => ArchiveEntryPath.Inside(Folder, key));

        // Assert
        Assert.IsType<ModArchiveException>(exception);
    }
}
