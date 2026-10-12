using BBTAG_IM.Core.Improvement;
using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Tests.Improvement;

public class ImprovementModContractTests
{
    [Fact]
    public void FindIn_ModShippingTheImprovementDll_ReturnsThatMod()
    {
        // Arrange
        ModFolder overhaul = Mod("BBTAG-OverHaul");
        ModFolder improvement = Mod("BBTAG-Improvement-Mod", "imageprovider.dll", "improvementmod.DLL");

        // Act
        ModFolder? found = ImprovementModContract.FindIn([overhaul, improvement]);

        // Assert
        Assert.Same(improvement, found);
    }

    [Fact]
    public void FindIn_NoModShipsTheImprovementDll_ReturnsNull()
    {
        // Arrange
        ModFolder other = Mod("Other", "OtherPlugin.dll");

        // Act
        ModFolder? found = ImprovementModContract.FindIn([other]);

        // Assert
        Assert.Null(found);
    }

    private static ModFolder Mod(string name, params string[] plugins)
    {
        string root = $@"D:\BBTAG\mods\{name}";
        return new ModFolder(name, root, [], null, null, plugins.Select(plugin => Path.Combine(root, plugin)).ToArray());
    }
}
