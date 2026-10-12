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
    private const uint ExtendCharacteristics = TestPeBuilder.CodeCharacteristics | 0x80000000;

    private static readonly byte[] SmallCave = [0xC3, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] CaveReadingItsOwnData = [0xA1, 0x10, 0x40, 0x40, 0x00, 0xC3];

    [Fact]
    public void Plan_IdenticalExecutables_IsEmptyAndKeepsThePreferredBase()
    {
        // Arrange
        FileImage stock = Build();

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(stock, Build());

        // Assert
        Assert.True(plan.IsEmpty);
        Assert.Equal(TestPeBuilder.DefaultImageBase, plan.PreferredBase);
    }

    [Fact]
    public void Plan_TwoChangedRuns_WritesEachRun()
    {
        // Arrange
        FileImage modded = Build(text =>
        {
            text[0x100] = 0x90;
            text[0x101] = 0x90;
            text[0x150] = 0xC3;
        });

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(
            [new PatchWrite(TextRva + 0x100, [0x90, 0x90], [], []), new PatchWrite(TextRva + 0x150, [0xC3], [], [])],
            plan.Writes,
            PatchWriteComparer.Instance);
        Assert.Equal(3, plan.PatchedByteCount);
    }

    [Fact]
    public void Plan_OnlyTheHeaderDiffers_IsEmpty()
    {
        // Arrange
        FileImage modded = Build();
        modded.Bytes[TestPeBuilder.PeHeaderOffset + PeConstants.NtSignatureSize + 18] |= (byte)PeConstants.FileRelocsStripped;

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), new FileImage(modded.Bytes));

        // Assert
        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Plan_OneByteOfARelocatedAddressChanged_WritesTheWholeAddressAndKeepsItRelocatable()
    {
        // Arrange
        FileImage modded = Build(text => text[AddressSiteOffset + 1] = 0x22);

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(new PatchWrite(TextRva + AddressSiteOffset, [0x34, 0x22, 0x40, 0x00], [0], []), Assert.Single(plan.Writes), PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_RelocatedAddressOverwrittenWithCode_StopsRelocatingIt()
    {
        // Arrange
        FileImage modded = Build(text => BinaryPrimitives.WriteUInt32LittleEndian(text.AsSpan(SecondAddressSiteOffset), 0x90909090));

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(new PatchWrite(TextRva + SecondAddressSiteOffset, [0x90, 0x90, 0x90, 0x90], [], []), Assert.Single(plan.Writes), PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_JumpIntoANewSection_RetargetsTheJump()
    {
        // Arrange
        FileImage modded = Build(JumpToCave, CaveReadingItsOwnData);

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xE9, 0xFB, 0x2E, 0x00, 0x00], [], [1]), Assert.Single(plan.Writes), PatchWriteComparer.Instance);
        Assert.Equal(new AddressRange(CaveRva, 0x1000), plan.NewSectionRvas);
    }

    [Fact]
    public void Plan_JumpIntoANewSection_AddsTheSectionWithTheAddressesItsCodeUses()
    {
        // Arrange
        FileImage modded = Build(JumpToCave, CaveReadingItsOwnData);

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        PatchSection section = Assert.Single(plan.Sections);
        Assert.Equal((".extend", CaveRva, (uint)CaveReadingItsOwnData.Length, true), (section.Name, section.Rva, section.VirtualSize, section.IsExecutable));
        Assert.Equal(CaveReadingItsOwnData, section.Bytes.AsSpan(0, CaveReadingItsOwnData.Length).ToArray());
        Assert.Equal([1], section.AddressFields);
        Assert.Empty(section.BranchFields);
    }

    [Fact]
    public void Plan_NewCodePointingIntoANewSection_RelocatesThePointer()
    {
        // Arrange
        FileImage modded = Build(text => new byte[] { 0xB8, 0x10, 0x40, 0x40, 0x00 }.CopyTo(text, FunctionOffset), SmallCave);

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xB8, 0x10, 0x40, 0x40, 0x00], [1], []), Assert.Single(plan.Writes), PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_NewImmediateThatLooksLikeAStockAddress_KeepsItAsAConstant()
    {
        // Arrange
        FileImage modded = Build(text => new byte[] { 0xB8, 0x01, 0x10, 0x40, 0x00 }.CopyTo(text, FunctionOffset), SmallCave);

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xB8, 0x01, 0x10, 0x40], [], []), Assert.Single(plan.Writes), PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_NewAbsoluteMemoryOperand_RelocatesIt()
    {
        // Arrange
        FileImage modded = Build(text => new byte[] { 0xA1, 0x00, 0x10, 0x40, 0x00 }.CopyTo(text, FunctionOffset));

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(new PatchWrite(TextRva + FunctionOffset, [0xA1, 0x00, 0x10, 0x40, 0x00], [1], []), Assert.Single(plan.Writes), PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_AlignedDataPointerIntoANewSection_RelocatesIt()
    {
        // Arrange
        FileImage modded = Build(caveSection: SmallCave, editData: data => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x20), 0x00404000));

        // Act
        ExePatchPlan plan = ExePatchPlanner.Plan(Build(), modded);

        // Assert
        Assert.Equal(new PatchWrite(DataRva + 0x20, [0x00, 0x40, 0x40, 0x00], [0], []), Assert.Single(plan.Writes), PatchWriteComparer.Instance);
    }

    [Fact]
    public void Plan_DifferentBuild_ThrowsIncompatibleExecutable()
    {
        // Arrange
        FileImage otherBuild = Build(timeDateStamp: 0x12345678);

        // Act
        Exception? exception = Record.Exception(() => ExePatchPlanner.Plan(Build(), otherBuild));

        // Assert
        Assert.Contains("build", Assert.IsType<IncompatibleExecutableException>(exception).Message);
    }

    [Fact]
    public void Plan_SectionsMoved_ThrowsIncompatibleExecutable()
    {
        // Arrange
        FileImage moved = Build(textLength: 0x1200);

        // Act
        Exception? exception = Record.Exception(() => ExePatchPlanner.Plan(Build(), moved));

        // Assert
        Assert.IsType<IncompatibleExecutableException>(exception);
    }

    [Fact]
    public void Plan_SectionMissing_ThrowsIncompatibleExecutable()
    {
        // Arrange
        FileImage missing = Build(includeData: false);

        // Act
        Exception? exception = Record.Exception(() => ExePatchPlanner.Plan(Build(), missing));

        // Assert
        Assert.IsType<IncompatibleExecutableException>(exception);
    }

    private static void JumpToCave(byte[] text)
    {
        text[FunctionOffset] = 0xE9;
        BinaryPrimitives.WriteInt32LittleEndian(text.AsSpan(FunctionOffset + 1), (int)(CaveRva - (TextRva + FunctionOffset + 5)));
    }

    private static FileImage Build(
        Action<byte[]>? editText = null,
        byte[]? caveSection = null,
        uint timeDateStamp = TestPeBuilder.DefaultTimeDateStamp,
        int textLength = 0x200,
        bool includeData = true,
        Action<byte[]>? editData = null)
    {
        byte[] text = new byte[textLength];
        BinaryPrimitives.WriteUInt32LittleEndian(text.AsSpan(AddressSiteOffset), 0x00401234);
        BinaryPrimitives.WriteUInt32LittleEndian(text.AsSpan(SecondAddressSiteOffset), 0x00402000);
        text[FunctionOffset - 1] = 0xCC;
        editText?.Invoke(text);

        TestPeBuilder builder = new TestPeBuilder().WithTimeDateStamp(timeDateStamp);
        builder.AddSection(".text", text, TestPeBuilder.CodeCharacteristics);

        if (includeData)
        {
            byte[] data = new byte[0x100];
            editData?.Invoke(data);
            builder.AddSection(".data", data);
        }

        byte[] relocations = TestPeTables.Relocations([TextRva + AddressSiteOffset, TextRva + SecondAddressSiteOffset]);
        uint relocationRva = builder.AddSection(".reloc", relocations);
        builder.WithDirectory(PeConstants.BaseRelocationDirectoryIndex, relocationRva, (uint)relocations.Length);

        if (caveSection is not null)
            builder.AddSection(".extend", caveSection, ExtendCharacteristics);

        return new FileImage(builder.Build());
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
