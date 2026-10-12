using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Pe;

public class PeTablesTests
{
    private static FileImage ImageWithImports(out TestImportLayout layout)
    {
        var builder = new TestPeBuilder();
        builder.AddSection(".text", new byte[0x10], TestPeBuilder.CodeCharacteristics);
        TestImportLayout? built = null;
        builder.AddSection(".idata", rva =>
        {
            built = TestPeTables.Imports(rva,
            [
                ("USER32.dll", ["MessageBoxW"]),
                ("KERNEL32.dll", ["#7", "CreateFileA", "CreateFileW"]),
            ]);
            return built.Data;
        });
        layout = built!;
        builder.WithDirectory(PeConstants.ImportDirectoryIndex, layout.DirectoryRva, layout.DirectorySize);
        return new FileImage(builder.Build());
    }

    [Fact]
    public void FindSlotRva_ReturnsImportAddressTableSlot()
    {
        var image = ImageWithImports(out var layout);

        var slot = ImportTable.FindSlotRva(image, image.Headers, "kernel32.dll", "CreateFileW");

        Assert.Equal(layout.SlotRvas[TestPeTables.SlotKey("KERNEL32.dll", "CreateFileW")], slot);
    }

    [Fact]
    public void FindSlotRva_ReturnsNullForMissingFunction()
    {
        var image = ImageWithImports(out _);

        Assert.Null(ImportTable.FindSlotRva(image, image.Headers, "KERNEL32.dll", "ReadFile"));
    }

    [Fact]
    public void FindSlotRva_ReturnsNullForMissingDll()
    {
        var image = ImageWithImports(out _);

        Assert.Null(ImportTable.FindSlotRva(image, image.Headers, "ADVAPI32.dll", "CreateFileW"));
    }

    [Fact]
    public void FindSlotRva_ReturnsNullWithoutImportDirectory()
    {
        var image = new FileImage(new TestPeBuilder().Build());

        Assert.Null(ImportTable.FindSlotRva(image, image.Headers, "KERNEL32.dll", "CreateFileW"));
    }

    [Fact]
    public void FindFunctionRva_ReturnsExportedFunction()
    {
        var builder = new TestPeBuilder();
        builder.AddSection(".text", new byte[0x40], TestPeBuilder.CodeCharacteristics);
        TestExportLayout? layout = null;
        builder.AddSection(".edata", rva =>
        {
            layout = TestPeTables.Exports(rva, "BBTAG_IM.dll", [("Start", 0x1010), ("Stop", 0x1020)]);
            return layout.Data;
        });
        builder.WithDirectory(PeConstants.ExportDirectoryIndex, layout!.DirectoryRva, layout.DirectorySize);
        var image = new FileImage(builder.Build());

        Assert.Equal(0x1010u, ExportTable.FindFunctionRva(image, image.Headers, "Start"));
        Assert.Equal(0x1020u, ExportTable.FindFunctionRva(image, image.Headers, "Stop"));
        Assert.Null(ExportTable.FindFunctionRva(image, image.Headers, "Missing"));
    }

    [Fact]
    public void FindFunctionRva_ReturnsNullWithoutExportDirectory()
    {
        var image = new FileImage(new TestPeBuilder().Build());

        Assert.Null(ExportTable.FindFunctionRva(image, image.Headers, "Start"));
    }

    [Fact]
    public void ReadAddressSites_ReadsHighLowEntriesAcrossPages()
    {
        uint[] sites = [0x1004, 0x1ffc, 0x2010, 0x2014, 0x5000];
        var builder = new TestPeBuilder();
        builder.AddSection(".text", new byte[0x5000], TestPeBuilder.CodeCharacteristics);
        var relocations = TestPeTables.Relocations(sites);
        var rva = builder.AddSection(".reloc", relocations);
        builder.WithDirectory(PeConstants.BaseRelocationDirectoryIndex, rva, (uint)relocations.Length);
        var image = new FileImage(builder.Build());

        var read = RelocationTable.ReadAddressSites(image, image.Headers);

        Assert.Equal(sites, read.Order());
    }

    [Fact]
    public void ReadAddressSites_ReturnsEmptyWithoutRelocationDirectory()
    {
        var image = new FileImage(new TestPeBuilder().Build());

        Assert.Empty(RelocationTable.ReadAddressSites(image, image.Headers));
    }
}
