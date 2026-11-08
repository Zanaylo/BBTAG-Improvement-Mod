using BBTAG_IM.Core.Porting;

namespace BBTAG_IM.Core.Tests.Porting;

public class Ha6StackTests
{
    [Fact]
    public void Files_LoadList_ReadsTheDataFilesInOrder()
    {
        // Arrange
        string text = "[System]\r\nProjectName=chr000\r\n[DataFile]\r\nFileNum= 3\r\nFile00=_temp.ha6\r\nFile01=chr000.ha6\r\nFile02=../BaseData.ha6\r\n[BmpcutFile]\r\nFileNum= 1\r\nFile00=chr000.cg\r\n";

        // Act
        IReadOnlyList<string> files = Ha6Stack.Files(text);

        // Assert
        Assert.Equal(["_temp.ha6", "chr000.ha6", "../BaseData.ha6"], files);
    }

    [Theory]
    [InlineData("../BaseData.ha6", true)]
    [InlineData("../basedata.HA6", true)]
    [InlineData("chr000.ha6", false)]
    public void IsSharedTemplate_FileName_TellsTheCommonFile(string file, bool expected)
    {
        // Act
        bool shared = Ha6Stack.IsSharedTemplate(file);

        // Assert
        Assert.Equal(expected, shared);
    }
}
