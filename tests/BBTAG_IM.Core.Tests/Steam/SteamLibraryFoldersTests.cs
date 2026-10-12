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
    public void ParsePaths_ReadsEveryLibraryPath()
    {
        Assert.Equal([@"C:\Program Files (x86)\Steam", @"D:\Jogos\steam"], SteamLibraryFolders.ParsePaths(LibraryFolders));
    }

    [Fact]
    public void ParsePaths_ReturnsEmptyForUnrelatedText()
    {
        Assert.Empty(SteamLibraryFolders.ParsePaths("\"libraryfolders\" { }"));
    }

    [Fact]
    public void ParseInstallDirectory_ReadsInstallDir()
    {
        Assert.Equal("BBTAG", SteamLibraryFolders.ParseInstallDirectory(AppManifest));
    }

    [Fact]
    public void ParseInstallDirectory_ReturnsNullWhenMissing()
    {
        Assert.Null(SteamLibraryFolders.ParseInstallDirectory("\"AppState\" { \"appid\" \"702890\" }"));
    }
}
