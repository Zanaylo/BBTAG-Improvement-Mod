using BBTAG_IM.Core.Patching;

namespace BBTAG_IM.Core.Tests.Patching;

public class PlanRelocationTests
{
    private const uint PreferredBase = 0x400000;
    private const uint ImageBase = 0x5A0000;
    private const uint NewSectionRva = 0x1DEB000;
    private const uint NewSectionsAddress = 0x2600000;

    private static readonly AddressRange NewSections = new(NewSectionRva, 0x400000);

    public PlanRelocation CreatePlanRelocation(
        PatchWrite? write = null,
        PatchSection? section = null,
        uint imageBase = ImageBase,
        uint newSectionsAddress = NewSectionsAddress)
    {
        ExePatchPlan plan = new(PreferredBase, NewSections, write is null ? [] : [write], section is null ? [] : [section]);
        return new PlanRelocation(
            plan,
            imageBase,
            newSectionsAddress
        );
    }

    [Fact]
    public void BytesFor_AddressInsideTheImage_MovesItByTheBaseDelta()
    {
        // Arrange
        PatchWrite write = new(0x274530, [0x30, 0x17, 0xAC, 0x00], [0], []);
        PlanRelocation relocation = CreatePlanRelocation(write);

        // Act
        byte[] bytes = relocation.BytesFor(write);

        // Assert
        Assert.Equal([0x30, 0x17, 0xC6, 0x00], bytes);
    }

    [Fact]
    public void BytesFor_AddressInsideANewSection_MovesItToWhereTheSectionWasAllocated()
    {
        // Arrange
        PatchWrite write = new(0x4C7397, [0xB8, 0x88, 0x71, 0x33, 0x02], [1], []);
        PlanRelocation relocation = CreatePlanRelocation(write);

        // Act
        byte[] bytes = relocation.BytesFor(write);

        // Assert
        Assert.Equal([0xB8, 0x88, 0xC1, 0x74, 0x02], bytes);
    }

    [Fact]
    public void BytesFor_JumpIntoANewSection_IsRetargeted()
    {
        // Arrange
        PatchWrite write = new(0x21130, [0xE9, 0x4B, 0x5F, 0xF1, 0x01], [], [1]);
        PlanRelocation relocation = CreatePlanRelocation(write);

        // Act
        byte[] bytes = relocation.BytesFor(write);

        // Assert
        Assert.Equal([0xE9, 0x4B, 0xAF, 0x18, 0x02], bytes);
    }

    [Fact]
    public void BytesFor_JumpLeavingANewSection_IsRetargeted()
    {
        // Arrange
        byte[] code = new byte[0x14C0AD];
        new byte[] { 0xE9, 0x91, 0xA0, 0x0E, 0xFE }.CopyTo(code, 0x14C0A8);
        PatchSection section = new(".extend", NewSectionRva, 0x400000, code, true, [], [0x14C0A9]);
        PlanRelocation relocation = CreatePlanRelocation(section: section);

        // Act
        byte[] bytes = relocation.BytesFor(section);

        // Assert
        Assert.Equal([0xE9, 0x91, 0x50, 0xE7, 0xFD], bytes.AsSpan(0x14C0A8, 5).ToArray());
    }

    [Fact]
    public void BytesFor_JumpThatStaysInsideTheImage_IsKept()
    {
        // Arrange
        PatchWrite write = new(0x1000, [0xE9, 0x10, 0x00, 0x00, 0x00], [], [1]);
        PlanRelocation relocation = CreatePlanRelocation(write);

        // Act
        byte[] bytes = relocation.BytesFor(write);

        // Assert
        Assert.Equal([0xE9, 0x10, 0x00, 0x00, 0x00], bytes);
    }

    [Fact]
    public void BytesFor_GameLoadedAtThePreferredLayout_ReturnsTheOriginalBytes()
    {
        // Arrange
        PatchWrite write = new(0x21130, [0xE9, 0x4B, 0x5F, 0xF1, 0x01, 0x00, 0x10, 0x40, 0x00], [5], [1]);
        PlanRelocation relocation = CreatePlanRelocation(write, imageBase: PreferredBase, newSectionsAddress: PreferredBase + NewSectionRva);

        // Act
        byte[] bytes = relocation.BytesFor(write);

        // Assert
        Assert.Equal(write.Bytes, bytes);
    }

    [Fact]
    public void AddressOf_SecondNewSection_IsPlacedRelativeToTheNewSectionsBlock()
    {
        // Arrange
        PatchSection section = new(".extra", NewSectionRva + 0x1000, 0x100, [], false, [], []);
        PlanRelocation relocation = CreatePlanRelocation(section: section);

        // Act
        uint address = relocation.AddressOf(section);

        // Assert
        Assert.Equal(NewSectionsAddress + 0x1000, address);
    }
}
