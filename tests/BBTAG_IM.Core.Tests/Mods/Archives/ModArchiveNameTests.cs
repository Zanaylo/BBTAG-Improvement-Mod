using BBTAG_IM.Core.Mods.Archives;

namespace BBTAG_IM.Core.Tests.Mods.Archives;

public class ModArchiveNameTests
{
    [Theory]
    [InlineData(@"D:\mods\Foo.zip")]
    [InlineData(@"D:\mods\Foo.ZIP")]
    [InlineData(@"D:\mods\Foo.rar")]
    [InlineData(@"D:\mods\Foo.7z")]
    [InlineData(@"D:\mods\Foo.tar")]
    [InlineData(@"D:\mods\Foo.tar.gz")]
    [InlineData(@"D:\mods\Foo.tgz")]
    public void IsArchive_SupportedExtension_ReturnsTrue(string path)
    {
        // Act
        bool isArchive = ModArchiveName.IsArchive(path);

        // Assert
        Assert.True(isArchive);
    }

    [Theory]
    [InlineData(@"D:\mods\Foo")]
    [InlineData(@"D:\mods\Foo.txt")]
    [InlineData(@"D:\mods\Foo.gz")]
    public void IsArchive_AnythingElse_ReturnsFalse(string path)
    {
        // Act
        bool isArchive = ModArchiveName.IsArchive(path);

        // Assert
        Assert.False(isArchive);
    }

    [Theory]
    [InlineData(@"D:\mods\Foo.zip", "Foo")]
    [InlineData(@"D:\mods\Foo.Bar v1.2.rar", "Foo.Bar v1.2")]
    [InlineData(@"D:\mods\Foo.TAR.GZ", "Foo")]
    public void FolderOf_Archive_DropsOnlyTheExtension(string path, string expected)
    {
        // Act
        string folder = ModArchiveName.FolderOf(path);

        // Assert
        Assert.Equal(expected, folder);
    }
}
