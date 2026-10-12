using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Pe;

public class ImportTableTests
{
    [Fact]
    public void FindSlotRva_ImportedFunctionWithDllNameInAnotherCase_ReturnsItsAddressTableSlot()
    {
        // Arrange
        FileImage image = ImageWithImports(out TestImportLayout layout);

        // Act
        uint? slot = ImportTable.FindSlotRva(image, image.Headers, "kernel32.dll", "CreateFileW");

        // Assert
        Assert.Equal(layout.SlotRvas[TestPeTables.SlotKey("KERNEL32.dll", "CreateFileW")], slot);
    }

    [Theory]
    [InlineData("KERNEL32.dll", "ReadFile")]
    [InlineData("ADVAPI32.dll", "CreateFileW")]
    public void FindSlotRva_FunctionOrDllNotImported_ReturnsNull(string dll, string function)
    {
        // Arrange
        FileImage image = ImageWithImports(out _);

        // Act
        uint? slot = ImportTable.FindSlotRva(image, image.Headers, dll, function);

        // Assert
        Assert.Null(slot);
    }

    [Fact]
    public void FindSlotRva_ImageWithoutImportDirectory_ReturnsNull()
    {
        // Arrange
        FileImage image = new(new TestPeBuilder().Build());

        // Act
        uint? slot = ImportTable.FindSlotRva(image, image.Headers, "KERNEL32.dll", "CreateFileW");

        // Assert
        Assert.Null(slot);
    }

    private static FileImage ImageWithImports(out TestImportLayout layout)
    {
        TestPeBuilder builder = new();
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
}
