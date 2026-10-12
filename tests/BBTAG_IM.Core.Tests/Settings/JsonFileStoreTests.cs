using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Settings;
using Bogus;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace BBTAG_IM.Core.Tests.Settings;

public class JsonFileStoreTests
{
    private const string LauncherDirectory = @"D:\BBTAG";
    private const string ModDirectory = @"D:\BBTAG\mods\BBTAG-Improvement-Mod";
    private const string LauncherSettingsPath = @"D:\BBTAG\BBTAG_IM.json";
    private const string ImprovementSettingsPath = @"D:\BBTAG\mods\BBTAG-Improvement-Mod\ImprovementMod.json";

    private readonly Faker _faker = new();

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    public JsonFileStore<LauncherSettings> CreateLauncherStore() => SettingsStores.Launcher(_fileSystemMock, LauncherDirectory);

    public JsonFileStore<ImprovementSettings> CreateImprovementStore() => SettingsStores.Improvement(_fileSystemMock, ModDirectory);

    [Fact]
    public void Load_NoFile_ReturnsDefaults()
    {
        // Arrange
        JsonFileStore<LauncherSettings> store = CreateLauncherStore();
        _fileSystemMock.FileExists(LauncherSettingsPath).Returns(false);

        // Act
        LauncherSettings settings = store.Load();

        // Assert
        Assert.Equal(((string?)null, true, true, 0), (settings.CheatEnginePath, settings.RunCheatEngineAsAdmin, settings.ShowCheatEngineWindow, settings.ScriptSelections.Count));
    }

    [Fact]
    public void Load_BrokenFile_ReturnsDefaults()
    {
        // Arrange
        JsonFileStore<LauncherSettings> store = CreateLauncherStore();
        FileContains(LauncherSettingsPath, "{ not json");

        // Act
        LauncherSettings settings = store.Load();

        // Assert
        Assert.Empty(settings.ScriptSelections);
    }

    [Fact]
    public void Load_FileWithoutSomeKeys_KeepsTheDefaultsOfTheMissingKeys()
    {
        // Arrange
        JsonFileStore<LauncherSettings> store = CreateLauncherStore();
        FileContains(LauncherSettingsPath, "{ \"CheatEnginePath\": null }");

        // Act
        LauncherSettings settings = store.Load();

        // Assert
        Assert.Equal((true, true), (settings.RunCheatEngineAsAdmin, settings.ShowCheatEngineWindow));
    }

    [Fact]
    public void Load_FileTurningOffAdminAndWindow_ReadsBothChoices()
    {
        // Arrange
        JsonFileStore<LauncherSettings> store = CreateLauncherStore();
        FileContains(LauncherSettingsPath, "{ \"RunCheatEngineAsAdmin\": false, \"ShowCheatEngineWindow\": false }");

        // Act
        LauncherSettings settings = store.Load();

        // Assert
        Assert.Equal((false, false), (settings.RunCheatEngineAsAdmin, settings.ShowCheatEngineWindow));
    }

    [Fact]
    public void Save_LauncherSettings_WritesWhatLoadReadsBack()
    {
        // Arrange
        JsonFileStore<LauncherSettings> store = CreateLauncherStore();
        string tableName = _faker.System.FileName("CT");
        List<int> ids = _faker.Make(3, () => _faker.Random.Int(1, 30000)).Distinct().ToList();
        string cheatEnginePath = _faker.System.FilePath();
        LauncherSettings saved = new() { CheatEnginePath = cheatEnginePath, ShowCheatEngineWindow = false, ScriptSelections = { [tableName] = ids } };
        CaptureWrites(LauncherSettingsPath);

        // Act
        store.Save(saved);
        LauncherSettings loaded = store.Load();

        // Assert
        Assert.Equal(cheatEnginePath, loaded.CheatEnginePath);
        Assert.False(loaded.ShowCheatEngineWindow);
        Assert.Equal(ids, loaded.ScriptSelections[tableName]);
    }

    [Fact]
    public void Load_ImprovementSettingsWithoutFile_LeavesTheFrameLimiterOff()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        _fileSystemMock.FileExists(ImprovementSettingsPath).Returns(false);

        // Act
        ImprovementSettings settings = store.Load();

        // Assert
        Assert.False(settings.FrameLimiter);
    }

    [Fact]
    public void Save_ImprovementSettings_WritesTheFileInsideTheModFolder()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        CaptureWrites(ImprovementSettingsPath);

        // Act
        store.Save(new ImprovementSettings { FrameLimiter = true });
        ImprovementSettings loaded = store.Load();

        // Assert
        Assert.True(loaded.FrameLimiter);
    }

    [Fact]
    public void Save_ImprovementSettingsWithPalettes_ReadsThePalettesBack()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        ImprovementSettings saved = new() { HitboxViewer = false };
        saved.ChoosePalette("Aegis", 3, "Minato");
        CaptureWrites(ImprovementSettingsPath);

        // Act
        store.Save(saved);
        ImprovementSettings loaded = store.Load();

        // Assert
        Assert.Equal((false, "Minato"), (loaded.HitboxViewer, loaded.PaletteFor("aegis", 3)));
    }

    [Fact]
    public void Save_ImprovementSettingsWithHitboxStylesAndAnAuthor_ReadsThemBack()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        ImprovementSettings saved = new() { HitboxOrigins = false, PaletteCreator = "Painter" };
        saved.Hitboxes.Shown = false;
        saved.Hurtboxes.Fill = 0.5f;
        CaptureWrites(ImprovementSettingsPath);

        // Act
        store.Save(saved);
        ImprovementSettings loaded = store.Load();

        // Assert
        Assert.Equal((false, false, 0.5f, "Painter"), (loaded.HitboxOrigins, loaded.Hitboxes.Shown, loaded.Hurtboxes.Fill, loaded.PaletteCreator));
    }

    [Fact]
    public void Save_ImprovementSettingsWithAMenuFontSize_ReadsItBack()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        CaptureWrites(ImprovementSettingsPath);

        // Act
        store.Save(new ImprovementSettings { MenuFontSize = 14 });
        ImprovementSettings loaded = store.Load();

        // Assert
        Assert.Equal(14f, loaded.MenuFontSize);
    }

    [Fact]
    public void Load_ImprovementSettingsWithoutFile_SizesTheMenuFontByTheScreen()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        _fileSystemMock.FileExists(ImprovementSettingsPath).Returns(false);

        // Act
        ImprovementSettings settings = store.Load();

        // Assert
        Assert.Null(settings.MenuFontSize);
    }

    [Fact]
    public void TryLoad_NoFile_ReturnsDefaults()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        _fileSystemMock.FileExists(ImprovementSettingsPath).Returns(false);

        // Act
        ImprovementSettings? settings = store.TryLoad();

        // Assert
        Assert.True(settings?.HitboxViewer);
    }

    [Fact]
    public void TryLoad_FileCutHalfWayThroughAWrite_ReturnsNull()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        FileContains(ImprovementSettingsPath, "{ \"FrameLimiter\": tr");

        // Act
        ImprovementSettings? settings = store.TryLoad();

        // Assert
        Assert.Null(settings);
    }

    [Fact]
    public void TryLoad_FileLockedByAWriter_ReturnsNull()
    {
        // Arrange
        JsonFileStore<ImprovementSettings> store = CreateImprovementStore();
        _fileSystemMock.FileExists(ImprovementSettingsPath).Returns(true);
        _fileSystemMock.ReadAllText(ImprovementSettingsPath).Throws(new IOException("The file is in use."));

        // Act
        ImprovementSettings? settings = store.TryLoad();

        // Assert
        Assert.Null(settings);
    }

    private void FileContains(string path, string text)
    {
        _fileSystemMock.FileExists(path).Returns(true);
        _fileSystemMock.ReadAllText(path).Returns(text);
    }

    private void CaptureWrites(string path) =>
        _fileSystemMock.When(fileSystem => fileSystem.WriteAllText(path, Arg.Any<string>()))
            .Do(call => FileContains(path, call.ArgAt<string>(1)));
}
