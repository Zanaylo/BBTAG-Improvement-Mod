using BBTAG_IM.Core.Mods;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModRefreshMessageTests
{
    [Fact]
    public void From_SameMods_SaysNothingChanged()
    {
        string message = ModRefreshMessage.From(["A", "B"], ["B", "A"]);

        Assert.Equal("The mods folder was read again. Nothing was added or removed.", message);
    }

    [Fact]
    public void From_NewMods_NamesThem()
    {
        string message = ModRefreshMessage.From(["A"], ["A", "B", "C"]);

        Assert.Equal("The mods folder was read again. New: B, C.", message);
    }

    [Fact]
    public void From_RemovedMods_NamesThem()
    {
        string message = ModRefreshMessage.From(["A", "B"], ["A"]);

        Assert.Equal("The mods folder was read again. Gone: B.", message);
    }

    [Fact]
    public void From_NewAndRemovedMods_NamesBoth()
    {
        string message = ModRefreshMessage.From(["A", "B"], ["A", "C"]);

        Assert.Equal("The mods folder was read again. New: C. Gone: B.", message);
    }

    [Fact]
    public void From_NameInOtherCase_IsTheSameMod()
    {
        string message = ModRefreshMessage.From(["FB-Kohaku"], ["fb-kohaku"]);

        Assert.Equal("The mods folder was read again. Nothing was added or removed.", message);
    }
}
