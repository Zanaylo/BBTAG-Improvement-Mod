using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Pe;

public class ExportTableTests
{
    [Theory]
    [InlineData("Start", 0x1010u)]
    [InlineData("Stop", 0x1020u)]
    public void FindFunctionRva_ExportedFunction_ReturnsItsRva(string function, uint expected)
    {
        // Arrange
        FileImage image = ImageExporting(("Start", 0x1010), ("Stop", 0x1020));

        // Act
        uint? rva = ExportTable.FindFunctionRva(image, image.Headers, function);

        // Assert
        Assert.Equal(expected, rva);
    }

    [Fact]
    public void FindFunctionRva_FunctionNotExported_ReturnsNull()
    {
        // Arrange
        FileImage image = ImageExporting(("Start", 0x1010));

        // Act
        uint? rva = ExportTable.FindFunctionRva(image, image.Headers, "Missing");

        // Assert
        Assert.Null(rva);
    }

    [Fact]
    public void FindFunctionRva_ImageWithoutExportDirectory_ReturnsNull()
    {
        // Arrange
        FileImage image = new(new TestPeBuilder().Build());

        // Act
        uint? rva = ExportTable.FindFunctionRva(image, image.Headers, "Start");

        // Assert
        Assert.Null(rva);
    }

    private static FileImage ImageExporting(params (string Name, uint Rva)[] functions)
    {
        TestPeBuilder builder = new();
        builder.AddSection(".text", new byte[0x40], TestPeBuilder.CodeCharacteristics);
        TestExportLayout? layout = null;
        builder.AddSection(".edata", rva =>
        {
            layout = TestPeTables.Exports(rva, "BBTAG_IM.dll", functions);
            return layout.Data;
        });
        builder.WithDirectory(PeConstants.ExportDirectoryIndex, layout!.DirectoryRva, layout.DirectorySize);
        return new FileImage(builder.Build());
    }
}
