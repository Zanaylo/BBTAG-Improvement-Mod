using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting;

public class InputNamesTests
{
    private const int NormalInputCommand = 14001;

    [Fact]
    public void NameOf_NormalInput_JoinsDirectionAndButton()
    {
        // Arrange
        InputNames inputs = CreateInputNames();

        // Act
        string? name = inputs.NameOf(NormalInputCommand, (1 << 8) + 4, hex: true);

        // Assert
        Assert.Equal("INPUT_5A", name);
    }

    [Fact]
    public void NameOf_UnnamedMotion_ReturnsHexFallback()
    {
        // Arrange
        InputNames inputs = CreateInputNames();

        // Act
        string? name = inputs.NameOf(ScriptTestDatabase.CheckInput, 0x1F, hex: true);

        // Assert
        Assert.Equal("INPUT_0x1f", name);
    }

    [Fact]
    public void NameOf_DecimalFallbackThatMeansAnotherMotion_ReturnsNull()
    {
        // Arrange
        InputNames inputs = new(new NameTable([new(186, "INPUT_22")]), ButtonByteInputs.Empty);

        // Act
        string? name = inputs.NameOf(ScriptTestDatabase.CheckInput, 22, hex: false);

        // Assert
        Assert.Null(name);
    }

    [Fact]
    public void ValueOf_NormalInputName_ReturnsButtonAndDirection()
    {
        // Arrange
        InputNames inputs = CreateInputNames();

        // Act
        int? value = inputs.ValueOf("INPUT_2B");

        // Assert
        Assert.Equal((2 << 8) + 1, value);
    }

    [Fact]
    public void ValueOf_NotAnInput_ReturnsNull()
    {
        // Arrange
        InputNames inputs = CreateInputNames();

        // Act
        int? value = inputs.ValueOf("SLOT_2");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void NameOf_NormalInputWithTagEncoding_UsesTheTagName()
    {
        // Arrange
        InputNames inputs = new(NameTable.Empty, TagNormalInputs.Instance);

        // Act
        string? name = inputs.NameOf(NormalInputCommand, 0x10, hex: true);

        // Assert
        Assert.Equal("INPUT_J5A", name);
    }

    [Fact]
    public void Named_MotionsAndNormalInputs_ListsBoth()
    {
        // Arrange
        InputNames inputs = new(new NameTable([new(172, "INPUT_236")]), TagNormalInputs.Instance);

        // Act
        IReadOnlyList<string> names = inputs.Named.Select(value => value.Name).ToList();

        // Assert
        Assert.Contains("INPUT_236", names);
        Assert.Contains("INPUT_5A", names);
    }

    public InputNames CreateInputNames() => ScriptTestDatabase.Create().Inputs;
}
