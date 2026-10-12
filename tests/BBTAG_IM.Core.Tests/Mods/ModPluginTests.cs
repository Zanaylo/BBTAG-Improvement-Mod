using BBTAG_IM.Core.Mods;
using BBTAG_IM.Core.Pe;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Mods;

public class ModPluginTests : IDisposable
{
    private const uint StartRva = 0x1010;

    private readonly List<string> _temporaryFiles = [];

    public void Dispose() => _temporaryFiles.ForEach(File.Delete);

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

    [Fact]
    public void IsPlugin_DllFileExportingTheStartFunction_ReturnsTrue()
    {
        // Arrange
        string path = TemporaryFile(DllBytesExporting(ModPlugin.StartExport));

        // Act
        bool isPlugin = ModPlugin.IsPlugin(path);

        // Assert
        Assert.True(isPlugin);
    }

    [Fact]
    public void IsPlugin_LibraryWithOtherExports_ReturnsFalse()
    {
        // Arrange
        string path = TemporaryFile(DllBytesExporting("igCreateContext"));

        // Act
        bool isPlugin = ModPlugin.IsPlugin(path);

        // Assert
        Assert.False(isPlugin);
    }

    [Fact]
    public void IsPlugin_FileThatIsNotAnExecutable_ReturnsFalse()
    {
        // Arrange
        string path = TemporaryFile([1, 2, 3]);

        // Act
        bool isPlugin = ModPlugin.IsPlugin(path);

        // Assert
        Assert.False(isPlugin);
    }

    [Fact]
    public void IsPlugin_FileMissing_ReturnsFalse()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");

        // Act
        bool isPlugin = ModPlugin.IsPlugin(path);

        // Assert
        Assert.False(isPlugin);
    }

    private string TemporaryFile(byte[] bytes)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dll");
        File.WriteAllBytes(path, bytes);
        _temporaryFiles.Add(path);
        return path;
    }

    private static FileImage DllExporting(params string[] functions) => new(DllBytesExporting(functions));

    private static byte[] DllBytesExporting(params string[] functions)
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
        return builder.Build();
    }
}
