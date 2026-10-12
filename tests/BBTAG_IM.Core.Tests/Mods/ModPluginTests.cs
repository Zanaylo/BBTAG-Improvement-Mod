using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModPluginTests
{
    private const uint StartRva = 0x1010;

    [Fact]
    public void FindStart_DllExportingTheStartFunction_ReturnsItsRva()
    {
        // Arrange
        FileImage dll = DllExporting(ModPlugin.StartExport, "Other");

        // Act
        uint? start = ModPlugin.FindStart(dll);

        // Assert
        Assert.Equal(StartRva, start);
    }

    [Fact]
    public void FindStart_DllWithOtherExports_ReturnsNull()
    {
        // Arrange
        FileImage dll = DllExporting("DllRegisterServer");

        // Act
        uint? start = ModPlugin.FindStart(dll);

        // Assert
        Assert.Null(start);
    }

    [Fact]
    public void FindStart_DllWithoutExports_ReturnsNull()
    {
        // Arrange
        FileImage dll = new(new TestPeBuilder().Build());

        // Act
        uint? start = ModPlugin.FindStart(dll);

        // Assert
        Assert.Null(start);
    }

    private static FileImage DllExporting(params string[] functions)
    {
        TestPeBuilder builder = new();
        builder.AddSection(".text", new byte[0x40], TestPeBuilder.CodeCharacteristics);
        TestExportLayout? layout = null;
        builder.AddSection(".edata", rva =>
        {
            layout = TestPeTables.Exports(rva, "Plugin.dll", functions.Select((name, index) => (name, StartRva + (uint)index * 0x10)).ToArray());
            return layout.Data;
        });
        builder.WithDirectory(PeConstants.ExportDirectoryIndex, layout!.DirectoryRva, layout.DirectorySize);
        return new FileImage(builder.Build());
    }
}
