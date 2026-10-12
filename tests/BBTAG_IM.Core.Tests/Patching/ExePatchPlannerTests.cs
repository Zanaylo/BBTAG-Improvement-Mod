using System.Buffers.Binary;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Patching;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Patching;

public class ExePatchPlannerTests
{
    private const uint TextRva = 0x1000;
    private const int AddressSiteOffset = 0x10;
    private const int SecondAddressSiteOffset = 0x40;
    private const uint CaveRva = 0x4000;
    private const int FunctionOffset = 0x100;
    private const uint DataRva = 0x2000;

    private static readonly byte[] SmallCave = [0xC3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    private static FileImage Build(
        Action<byte[]>? editText = null,
        byte[]? caveSection = null,
        uint timeDateStamp = TestPeBuilder.DefaultTimeDateStamp,
        int textLength = 0x200,
        bool includeData = true,
        Action<byte[]>? editData = null)
    {
        var text = new byte[textLength];
        BinaryPrimitives.WriteUInt32LittleEndian(text.AsSpan(AddressSiteOffset), 0x00401234);
        BinaryPrimitives.WriteUInt32LittleEndian(text.AsSpan(SecondAddressSiteOffset), 0x00402000);
        text[FunctionOffset - 1] = 0xCC;
        editText?.Invoke(text);

        var builder = new TestPeBuilder().WithTimeDateStamp(timeDateStamp);
        builder.AddSection(".text", text, TestPeBuilder.CodeCharacteristics);

        if (includeData)
        {
            var data = new byte[0x100];
            editData?.Invoke(data);
            builder.AddSection(".data", data);
        }

        var relocations = TestPeTables.Relocations([TextRva + AddressSiteOffset, TextRva + SecondAddressSiteOffset]);
        var relocationRva = builder.AddSection(".reloc", relocations);
        builder.WithDirectory(PeConstants.BaseRelocationDirectoryIndex, relocationRva, (uint)relocations.Length);

        if (caveSection is not null)
            builder.AddSection(".extend", caveSection, TestPeBuilder.CodeCharacteristics | 0x80000000);

        return new FileImage(builder.Build());
    }

    [Fact]
    public void Plan_IsEmptyForIdenticalExecutables()
    {
        var plan = ExePatchPlanner.Plan(Build(), Build());

        Assert.True(plan.IsEmpty);
        Assert.Equal(TestPeBuilder.DefaultImageBase, plan.PreferredBase);
    }

    [Fact]
    public void Plan_WritesEveryChangedRun()
    {
        var modded = Build(text =>
        {
            text[0x100] = 0x90;
            text[0x101] = 0x90;
            text[0x150] = 0xC3;
        });

        var plan = ExePatchPlanner.Plan(Build(), modded);

        Assert.Collection(plan.Writes,
            write => Assert.Equal(new PatchWrite(TextRva + 0x100, [0x90, 0x90], [], []), write, PatchWriteComparer.Instance),
            write => Assert.Equal(new PatchWrite(TextRva + 0x150, [0xC3], [], []), write, PatchWriteComparer.Instance));
        Assert.Equal(3, plan.PatchedByteCount);
    }

    [Fact]
    public void Plan_IgnoresHeaderDifferences()
    {
        var stock = Build();
        var modded = Build();
        modded.Bytes[TestPeBuilder.PeHeaderOffset + PeConstants.NtSignatureSize + 18] |= (byte)PeConstants.FileRelocsStripped;

        Assert.True(ExePatchPlanner.Plan(stock, new FileImage(modded.Bytes)).IsEmpty);
    }

    [Fact]
    public void Plan_WidensWriteOverPartiallyPatchedAddressAndKeepsItRelocatable()
    {
        var modded = Build(text => text[AddressSiteOffset + 1] = 0x22);

        var write = Assert.Single(ExePatchPlanner.Plan(Build(), modded).Writes);

        Assert.Equal(new PatchWrite(TextRva + AddressSiteOffset, [0x34, 0x22, 0x40, 0x00], [0], []), write, PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_StopsRelocatingAddressOverwrittenWithNonAddress()
    {
        var modded = Build(text => BinaryPrimitives.WriteUInt32LittleEndian(text.AsSpan(SecondAddressSiteOffset), 0x90909090));

        var write = Assert.Single(ExePatchPlanner.Plan(Build(), modded).Writes);

        Assert.Equal(new PatchWrite(TextRva + SecondAddressSiteOffset, [0x90, 0x90, 0x90, 0x90], [], []), write, PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_AddsNewSectionsWithAddressesFoundFromJumpsIntoThem()
    {
        byte[] cave = [0xA1, 0x10, 0x40, 0x40, 0x00, 0xC3];
        var jumpDisplacement = (int)(CaveRva - (TextRva + FunctionOffset + 5));
        var modded = Build(text =>
        {
            text[FunctionOffset] = 0xE9;
            BinaryPrimitives.WriteInt32LittleEndian(text.AsSpan(FunctionOffset + 1), jumpDisplacement);
        }, cave);

        var plan = ExePatchPlanner.Plan(Build(), modded);

        var write = Assert.Single(plan.Writes);
        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xE9, 0xFB, 0x2E, 0x00, 0x00], [], [1]), write, PatchWriteComparer.Instance);
        Assert.Equal(new AddressRange(CaveRva, 0x1000), plan.NewSectionRvas);
        var section = Assert.Single(plan.Sections);
        Assert.Equal(".extend", section.Name);
        Assert.Equal(CaveRva, section.Rva);
        Assert.Equal((uint)cave.Length, section.VirtualSize);
        Assert.True(section.IsExecutable);
        Assert.Equal(cave, section.Bytes.AsSpan(0, cave.Length).ToArray());
        Assert.Equal([1], section.AddressFields);
        Assert.Empty(section.BranchFields);
    }

    [Fact]
    public void Plan_RelocatesNewCodePointingIntoNewSections()
    {
        var modded = Build(text => new byte[] { 0xB8, 0x10, 0x40, 0x40, 0x00 }.CopyTo(text, FunctionOffset), SmallCave);

        var write = Assert.Single(ExePatchPlanner.Plan(Build(), modded).Writes);

        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xB8, 0x10, 0x40, 0x40, 0x00], [1], []), write, PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_KeepsNewImmediatesInsideTheStockImageAsConstants()
    {
        var modded = Build(text => new byte[] { 0xB8, 0x01, 0x10, 0x40, 0x00 }.CopyTo(text, FunctionOffset), SmallCave);

        var write = Assert.Single(ExePatchPlanner.Plan(Build(), modded).Writes);

        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xB8, 0x01, 0x10, 0x40], [], []), write, PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_RelocatesNewAbsoluteMemoryOperands()
    {
        var modded = Build(text => new byte[] { 0xA1, 0x00, 0x10, 0x40, 0x00 }.CopyTo(text, FunctionOffset));

        var write = Assert.Single(ExePatchPlanner.Plan(Build(), modded).Writes);

        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xA1, 0x00, 0x10, 0x40, 0x00], [1], []), write, PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_RelocatesAlignedDataPointersIntoNewSections()
    {
        var modded = Build(caveSection: SmallCave, editData: data => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x20), 0x00404000));

        var write = Assert.Single(ExePatchPlanner.Plan(Build(), modded).Writes);

        Assert.Equal(new PatchWrite(DataRva + 0x20, [0x00, 0x40, 0x40, 0x00], [0], []), write, PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_RejectsDifferentBuild()
    {
        var exception = Assert.Throws<IncompatibleExecutableException>(() => ExePatchPlanner.Plan(Build(), Build(timeDateStamp: 0x12345678)));

        Assert.Contains("build", exception.Message);
    }

    [Fact]
    public void Plan_RejectsMovedSections()
    {
        Assert.Throws<IncompatibleExecutableException>(() => ExePatchPlanner.Plan(Build(), Build(textLength: 0x1200)));
    }

    [Fact]
    public void Plan_RejectsMissingSections()
    {
        Assert.Throws<IncompatibleExecutableException>(() => ExePatchPlanner.Plan(Build(), Build(includeData: false)));
    }

    private sealed class PatchWriteComparer : IEqualityComparer<PatchWrite>
    {
        public static readonly PatchWriteComparer Instance = new();

        public bool Equals(PatchWrite? x, PatchWrite? y) =>
            x is not null && y is not null && x.Rva == y.Rva && x.Bytes.SequenceEqual(y.Bytes) && x.AddressFields.SequenceEqual(y.AddressFields)
            && x.BranchFields.SequenceEqual(y.BranchFields);

        public int GetHashCode(PatchWrite obj) => obj.Rva.GetHashCode();
    }
}
