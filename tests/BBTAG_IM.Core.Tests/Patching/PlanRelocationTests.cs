using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Tests.Patching;

public class PlanRelocationTests
{
    private const uint PreferredBase = 0x400000;
    private const uint ImageBase = 0x5A0000;
    private const uint NewSectionRva = 0x1DEB000;
    private const uint NewSectionsAddress = 0x2600000;

    private static readonly AddressRange NewSections = new(NewSectionRva, 0x400000);

    private static ExePatchPlan PlanWith(PatchWrite? write = null, PatchSection? section = null) =>
        new(PreferredBase, NewSections, write is null ? [] : [write], section is null ? [] : [section]);

    [Fact]
    public void BytesFor_MovesImageAddressesByBaseDelta()
    {
        var write = new PatchWrite(0x274530, [0x30, 0x17, 0xAC, 0x00], [0], []);
        var relocation = new PlanRelocation(PlanWith(write), ImageBase, NewSectionsAddress);

        Assert.Equal([0x30, 0x17, 0xC6, 0x00], relocation.BytesFor(write));
    }

    [Fact]
    public void BytesFor_MovesNewSectionAddressesToWhereTheyWereAllocated()
    {
        var write = new PatchWrite(0x4C7397, [0xB8, 0x88, 0x71, 0x33, 0x02], [1], []);
        var relocation = new PlanRelocation(PlanWith(write), ImageBase, NewSectionsAddress);

        Assert.Equal([0xB8, 0x88, 0xC1, 0x74, 0x02], relocation.BytesFor(write));
    }

    [Fact]
    public void BytesFor_RetargetsJumpsIntoNewSections()
    {
        var write = new PatchWrite(0x21130, [0xE9, 0x4B, 0x5F, 0xF1, 0x01], [], [1]);
        var relocation = new PlanRelocation(PlanWith(write), ImageBase, NewSectionsAddress);

        Assert.Equal([0xE9, 0x4B, 0xAF, 0x18, 0x02], relocation.BytesFor(write));
    }

    [Fact]
    public void BytesFor_RetargetsJumpsLeavingNewSections()
    {
        var code = new byte[0x14C0AD];
        new byte[] { 0xE9, 0x91, 0xA0, 0x0E, 0xFE }.CopyTo(code, 0x14C0A8);
        var section = new PatchSection(".extend", NewSectionRva, 0x400000, code, true, [], [0x14C0A9]);
        var relocation = new PlanRelocation(PlanWith(section: section), ImageBase, NewSectionsAddress);

        var bytes = relocation.BytesFor(section);

        Assert.Equal([0xE9, 0x91, 0x50, 0xE7, 0xFD], bytes.AsSpan(0x14C0A8, 5).ToArray());
    }

    [Fact]
    public void BytesFor_KeepsJumpsThatStayInsideTheirRegion()
    {
        var write = new PatchWrite(0x1000, [0xE9, 0x10, 0x00, 0x00, 0x00], [], [1]);
        var relocation = new PlanRelocation(PlanWith(write), ImageBase, NewSectionsAddress);

        Assert.Equal([0xE9, 0x10, 0x00, 0x00, 0x00], relocation.BytesFor(write));
    }

    [Fact]
    public void BytesFor_ReturnsOriginalBytesAtPreferredLayout()
    {
        var write = new PatchWrite(0x21130, [0xE9, 0x4B, 0x5F, 0xF1, 0x01, 0x00, 0x10, 0x40, 0x00], [5], [1]);
        var relocation = new PlanRelocation(PlanWith(write), PreferredBase, PreferredBase + NewSectionRva);

        Assert.Equal(write.Bytes, relocation.BytesFor(write));
    }

    [Fact]
    public void AddressOf_PlacesSectionsRelativeToTheNewSectionsBlock()
    {
        var section = new PatchSection(".extra", NewSectionRva + 0x1000, 0x100, [], false, [], []);
        var relocation = new PlanRelocation(PlanWith(section: section), ImageBase, NewSectionsAddress);

        Assert.Equal(NewSectionsAddress + 0x1000, relocation.AddressOf(section));
    }
}
