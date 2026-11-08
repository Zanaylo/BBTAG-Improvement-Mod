using System.Text;
using System.Text.Json;
using BBTAG_IM.Core.Porting;

namespace BBTAG_IM.Core.Tests.Porting;

public class RosterManifestTests
{
    private static readonly RosterEntry Kohaku = new("xm4", "Kohaku", "Others");

    [Fact]
    public void PathOf_Entry_IsInTheCharactersFolder()
    {
        // Act
        string path = RosterManifest.PathOf(Kohaku);

        // Assert
        Assert.Equal(Path.Combine("characters", "xm4.json"), path);
    }

    [Fact]
    public void Write_Entry_TakesTheEngineAndFilesTemplates()
    {
        // Act
        using JsonDocument manifest = JsonDocument.Parse(Encoding.UTF8.GetString(RosterManifest.Write(Kohaku)));

        // Assert
        Assert.Equal(RosterManifest.EngineTemplate, manifest.RootElement.GetProperty("Template").GetString());
        Assert.Equal(RosterManifest.FilesTemplate, manifest.RootElement.GetProperty("FilesFrom").GetString());
    }

    [Fact]
    public void Write_NameWithSpace_MakesTheShortNameOneWord()
    {
        // Arrange
        RosterEntry entry = new("xm7", "Hisui Maid", "Others");

        // Act
        using JsonDocument manifest = JsonDocument.Parse(Encoding.UTF8.GetString(RosterManifest.Write(entry)));

        // Assert
        Assert.Equal("U_HISUI_MAID", manifest.RootElement.GetProperty("ShortName").GetString());
    }
}
