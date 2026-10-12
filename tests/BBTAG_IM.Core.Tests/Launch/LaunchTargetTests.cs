using BBTAG_IM.Core.Launch;
using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Launch;

public class LaunchTargetTests
{
    private const string GameDirectory = @"D:\Steam\steamapps\common\BBTAG";
    private const string GameExecutable = @"D:\Steam\steamapps\common\BBTAG\BBTAG.exe";
    private const string FixedBaseExecutable = @"D:\Mods\Overhaul\BBTAG.exe";
    private const string RelocatableExecutable = @"D:\Mods\Relocatable\BBTAG.exe";

    private static readonly PeHeaders FixedBaseHeaders = PeHeaders.Parse(
        new TestPeBuilder().WithFileCharacteristics(PeConstants.FileRelocsStripped).Build());

    private static readonly PeHeaders RelocatableHeaders = PeHeaders.Parse(new TestPeBuilder().Build());

    [Fact]
    public void Choose_NoModShipsAnExecutable_RunsTheInstalledGame()
    {
        // Arrange
        ModFolder[] mods = [Mod("Data", null)];

        // Act
        LaunchTarget target = LaunchTarget.Choose(GameDirectory, mods, ReadHeaders);

        // Assert
        Assert.Equal(new LaunchTarget(GameExecutable, null), target);
    }

    [Fact]
    public void Choose_ModShipsAFixedBaseExecutable_RunsItAsItIs()
    {
        // Arrange
        ModFolder overhaul = Mod("Overhaul", FixedBaseExecutable);

        // Act
        LaunchTarget target = LaunchTarget.Choose(GameDirectory, [Mod("Data", null), overhaul], ReadHeaders);

        // Assert
        Assert.Equal(new LaunchTarget(FixedBaseExecutable, overhaul), target);
    }

    [Fact]
    public void Choose_ModShipsARelocatableExecutable_RunsTheInstalledGame()
    {
        // Arrange
        ModFolder[] mods = [Mod("Relocatable", RelocatableExecutable)];

        // Act
        LaunchTarget target = LaunchTarget.Choose(GameDirectory, mods, ReadHeaders);

        // Assert
        Assert.Equal(new LaunchTarget(GameExecutable, null), target);
    }

    [Fact]
    public void Choose_RelocatableExecutableBeforeAFixedBaseOne_OnlyLooksAtTheFirst()
    {
        // Arrange
        ModFolder[] mods = [Mod("Relocatable", RelocatableExecutable), Mod("Overhaul", FixedBaseExecutable)];

        // Act
        LaunchTarget target = LaunchTarget.Choose(GameDirectory, mods, ReadHeaders);

        // Assert
        Assert.Equal(GameExecutable, target.ExecutablePath);
    }

    [Theory]
    [InlineData(GameExecutable, true)]
    [InlineData(@"d:\steam\steamapps\common\bbtag\bbtag.EXE", true)]
    [InlineData(@"D:\Steam\steamapps\common\BBTAG\mods\..\BBTAG.exe", true)]
    [InlineData(FixedBaseExecutable, false)]
    public void IsInstalledGame_ExecutablePath_ComparesFullPaths(string executablePath, bool expected)
    {
        // Act
        bool installed = LaunchTarget.IsInstalledGame(executablePath, GameDirectory);

        // Assert
        Assert.Equal(expected, installed);
    }

    private static ModFolder Mod(string name, string? executablePath) =>
        new(name, $@"D:\Mods\{name}", [], executablePath, null, []);

    private static PeHeaders ReadHeaders(string path) => path == FixedBaseExecutable ? FixedBaseHeaders : RelocatableHeaders;
}
