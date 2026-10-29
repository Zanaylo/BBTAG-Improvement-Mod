using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Roster;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Roster;

public class ModCharacterManifestsTests
{
    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();

    [Fact]
    public void Load_ManifestInTheCharactersFolder_ReadsIt()
    {
        // Arrange
        ModFolder mod = ModWithManifests("Red", ("rar.json", Json("rar")));

        // Act
        ManifestLoad load = ModCharacterManifests.Load([mod], _fileSystemMock);

        // Assert
        Assert.Equal("rar", Assert.Single(load.Manifests).Code);
        Assert.Empty(load.Problems);
    }

    [Fact]
    public void Load_BrokenManifest_ReportsItAndKeepsTheOthers()
    {
        // Arrange
        ModFolder mod = ModWithManifests("Red", ("bad.json", "{"), ("rar.json", Json("rar")));

        // Act
        ManifestLoad load = ModCharacterManifests.Load([mod], _fileSystemMock);

        // Assert
        Assert.Equal("rar", Assert.Single(load.Manifests).Code);
        Assert.Contains(@"D:\Mods\Red\characters\bad.json", Assert.Single(load.Problems));
    }

    [Fact]
    public void Load_FileThatIsNotJson_IsIgnored()
    {
        // Arrange
        ModFolder mod = ModWithManifests("Red", ("notes.txt", "hello"));

        // Act
        ManifestLoad load = ModCharacterManifests.Load([mod], _fileSystemMock);

        // Assert
        Assert.Empty(load.Manifests);
        Assert.Empty(load.Problems);
    }

    [Fact]
    public void Load_ModWithoutCharactersFolder_ReadsNothing()
    {
        // Arrange
        ModFolder mod = new("Plain", @"D:\Mods\Plain", [], null, null, []);

        // Act
        ManifestLoad load = ModCharacterManifests.Load([mod], _fileSystemMock);

        // Assert
        Assert.Empty(load.Manifests);
    }

    [Fact]
    public void Load_TwoMods_KeepsTheModOrder()
    {
        // Arrange
        ModFolder first = ModWithManifests("First", ("zzz.json", Json("zzz")));
        ModFolder second = ModWithManifests("Second", ("aaa.json", Json("aaa")));

        // Act
        ManifestLoad load = ModCharacterManifests.Load([first, second], _fileSystemMock);

        // Assert
        Assert.Equal(["zzz", "aaa"], load.Manifests.Select(manifest => manifest.Code));
    }

    private ModFolder ModWithManifests(string name, params (string File, string Text)[] files)
    {
        string root = $@"D:\Mods\{name}";
        string folder = Path.Combine(root, "characters");
        _fileSystemMock.DirectoryExists(folder).Returns(true);
        _fileSystemMock.EnumerateFiles(folder).Returns(files.Select(file => Path.Combine(folder, file.File)).ToArray());

        foreach ((string file, string text) in files)
            _fileSystemMock.ReadAllText(Path.Combine(folder, file)).Returns(text);

        return new ModFolder(name, root, [], null, null, []);
    }

    private static string Json(string code) => $$"""{ "Code": "{{code}}", "Template": "brg", "Series": "BlazBlue" }""";
}
