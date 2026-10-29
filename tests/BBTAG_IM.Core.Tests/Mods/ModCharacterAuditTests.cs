using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModCharacterAuditTests
{
    private const string Game = @"D:\BBTAG";
    private const string OverhaulRoot = @"D:\BBTAG\mods\Overhaul";
    private const string OverhaulData = @"D:\BBTAG\mods\Overhaul\data";
    private const string DevRoot = @"D:\BBTAG\mods\DEV";
    private const string DevData = @"D:\BBTAG\mods\DEV\data";

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Run_RosterCharacterWithEveryFile_ReportsNothing()
    {
        // Arrange
        ModFolder overhaul = ModWithFiles("Overhaul", OverhaulRoot, OverhaulData, CharacterFileRules.For("mko").Select(file => file.Name).ToArray());
        ModCharacterAudit audit = CreateModCharacterAudit();

        // Act
        IReadOnlyList<CharacterFileReport> reports = audit.Run([overhaul], ["mko"]);

        // Assert
        Assert.Empty(reports);
    }

    [Fact]
    public void Run_RosterCharacterWithOnlyItsScript_ReportsTheOtherFiles()
    {
        // Arrange
        ModFolder overhaul = ModWithFiles("Overhaul", OverhaulRoot, OverhaulData, @"data\char\char_mko_scr.pac");
        ModCharacterAudit audit = CreateModCharacterAudit();

        // Act
        IReadOnlyList<CharacterFileReport> reports = audit.Run([overhaul], ["mko"]);

        // Assert
        CharacterFileReport report = Assert.Single(reports);
        Assert.Equal(("mko", 49), (report.Code, report.Missing.Count));
    }

    [Fact]
    public void Run_BossInTheRoster_LeavesItOut()
    {
        // Arrange
        ModFolder overhaul = ModWithFiles("Overhaul", OverhaulRoot, OverhaulData);
        ModCharacterAudit audit = CreateModCharacterAudit();

        // Act
        IReadOnlyList<CharacterFileReport> reports = audit.Run([overhaul], ["bta", "bcr"]);

        // Assert
        Assert.Empty(reports);
    }

    [Fact]
    public void Run_GameCharacter_UsesTheGameFiles()
    {
        // Arrange
        _fileSystemMock.FileExists(Arg.Is<string>(path => path.StartsWith(Game + @"\data\", StringComparison.Ordinal))).Returns(true);
        ModCharacterAudit audit = CreateModCharacterAudit();

        // Act
        IReadOnlyList<CharacterFileReport> reports = audit.Run([], ["brg"]);

        // Assert
        Assert.Empty(reports);
    }

    [Fact]
    public void Run_CharacterScriptBuiltInTheDevFolder_CountsItsScriptFolder()
    {
        // Arrange
        string scriptFolder = DevRoot + @"\data\char\char_rar_scr";
        ModFolder dev = ModWithFiles("DEV", DevRoot, DevData, @"data\char\char_rar_scr\scr_rar.bin");
        _fileSystemMock.DirectoryExists(scriptFolder).Returns(true);
        _fileSystemMock.EnumerateFiles(scriptFolder).Returns([scriptFolder + @"\scr_rar.bin"]);
        ModCharacterAudit audit = CreateModCharacterAudit();

        // Act
        IReadOnlyList<CharacterFileReport> reports = audit.Run([dev], ["rar"]);

        // Assert
        CharacterFileReport report = Assert.Single(reports);
        Assert.DoesNotContain(@"data\char\char_rar_scr.pac", report.Missing);
    }

    public ModCharacterAudit CreateModCharacterAudit() => new(_fileSystemMock, Game);

    private ModFolder ModWithFiles(string name, string root, string dataRoot, params string[] keys)
    {
        _fileSystemMock.EnumerateFilesRecursively(dataRoot).Returns(keys.Select(key => Path.Combine(root, key)).ToArray());
        return new ModFolder(name, root, [new ModDataRoot(dataRoot, "data")], null, null, []);
    }
}
