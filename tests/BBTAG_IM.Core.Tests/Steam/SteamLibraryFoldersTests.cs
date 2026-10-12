using BBTAG_IM.Core.Steam;

namespace BBTAG_IM.Core.Tests.Steam;

public class SteamLibraryFoldersTests
{
    private const string LibraryFolders = """
        "libraryfolders"
        {
        	"0"
        	{
        		"path"		"C:\\Program Files (x86)\\Steam"
        		"label"		""
        		"apps"
        		{
        			"228980"		"420917285"
        		}
        	}
        	"1"
        	{
        		"path"		"D:\\Jogos\\steam"
        	}
        }
        """;

    private const string AppManifest = """
        "AppState"
        {
        	"appid"		"702890"
        	"name"		"BlazBlue: Cross Tag Battle"
        	"installdir"		"BBTAG"
        }
        """;

    [Fact]
    public void ParsePaths_LibraryFoldersFile_ReturnsEveryLibraryPath()
    {
        // Act
        IReadOnlyList<string> paths = SteamLibraryFolders.ParsePaths(LibraryFolders);

        // Assert
        Assert.Equal([@"C:\Program Files (x86)\Steam", @"D:\Jogos\steam"], paths);
    }

    [Fact]
    public void ParsePaths_TextWithoutPaths_ReturnsEmpty()
    {
        // Act
        IReadOnlyList<string> paths = SteamLibraryFolders.ParsePaths("\"libraryfolders\" { }");

        // Assert
        Assert.Empty(paths);
    }

    [Fact]
    public void ParseInstallDirectory_AppManifest_ReturnsTheInstallFolderName()
    {
        // Act
        string? installDirectory = SteamLibraryFolders.ParseInstallDirectory(AppManifest);

        // Assert
        Assert.Equal("BBTAG", installDirectory);
    }

    [Fact]
    public void ParseInstallDirectory_ManifestWithoutInstallDir_ReturnsNull()
    {
        // Act
        string? installDirectory = SteamLibraryFolders.ParseInstallDirectory("\"AppState\" { \"appid\" \"702890\" }");

        // Assert
        Assert.Null(installDirectory);
    }
}
