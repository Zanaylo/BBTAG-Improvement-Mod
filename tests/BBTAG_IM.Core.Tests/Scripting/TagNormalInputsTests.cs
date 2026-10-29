using BBTAG_IM.Core.Scripting;

namespace BBTAG_IM.Core.Tests.Scripting;

public class TagNormalInputsTests
{
    [Theory]
    [InlineData(0x03, "INPUT_1A")]
    [InlineData(0x07, "INPUT_5A")]
    [InlineData(0x10, "INPUT_J5A")]
    [InlineData(0x16, "INPUT_2B")]
    [InlineData(0x1A, "INPUT_6B")]
    [InlineData(0x2B, "INPUT_5C")]
    [InlineData(0x34, "INPUT_J5C")]
    [InlineData(0x4A, "INPUT_J9D")]
    public void NameOf_ButtonAndDirection_IsTheNumpadName(int value, string expected)
    {
        // Act
        string? name = TagNormalInputs.Instance.NameOf(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0x02, "INPUT_SPECIALMOVE")]
    [InlineData(0x5D, "INPUT_THROW")]
    [InlineData(0x61, "INPUT_BACKTHROW")]
    [InlineData(0x64, "INPUT_REVERSAL")]
    [InlineData(0x66, "INPUT_CRUSH_TRIGGER")]
    [InlineData(0x68, "INPUT_DISTORTION")]
    [InlineData(0x69, "INPUT_ASTRAL")]
    public void NameOf_SystemInput_IsItsName(int value, string expected)
    {
        // Act
        string? name = TagNormalInputs.Instance.NameOf(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x5F)]
    public void NameOf_ValueWithoutAKnownMeaning_ReturnsNull(int value)
    {
        // Act
        string? name = TagNormalInputs.Instance.NameOf(value);

        // Assert
        Assert.Null(name);
    }

    [Theory]
    [InlineData("INPUT_5A", 0x07)]
    [InlineData("input_j2b", 0x1F)]
    [InlineData("INPUT_THROW", 0x5D)]
    public void ValueOf_Name_ReturnsTheGameValue(string name, int expected)
    {
        // Act
        int? value = TagNormalInputs.Instance.ValueOf(name);

        // Assert
        Assert.Equal(expected, value);
    }

    [Fact]
    public void Named_EveryValue_HasADescription()
    {
        // Act
        IReadOnlyList<NamedValue> named = TagNormalInputs.Instance.Named.ToList();

        // Assert
        Assert.Equal(4 * 18 + 9, named.Count);
        Assert.All(named, value => Assert.False(string.IsNullOrWhiteSpace(value.Description)));
    }
}
