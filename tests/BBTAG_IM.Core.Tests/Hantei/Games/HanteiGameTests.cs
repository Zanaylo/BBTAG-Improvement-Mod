using System.Text;
using BBTAG_IM.Core.Hantei.Games;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Hantei.Games;

public class HanteiGameTests
{
    private const string NamesFile = "___English/System/BtlCharaTbl_str.ini";

    private readonly IHanteiGameFiles _files = Substitute.For<IHanteiGameFiles>();

    [Fact]
    public void Characters_FullSet_TakesTheNameFromTheTable()
    {
        // Arrange
        ArrangeCharacter("chr004");
        ArrangeNames("[data]\r\n000 = Arcueid\r\n004 = Kohaku\r\n");

        // Act
        IReadOnlyList<HanteiCharacter> characters = InstantiateHanteiGame().Characters();

        // Assert
        Assert.Equal([new HanteiCharacter("chr004", "Kohaku")], characters);
    }

    [Fact]
    public void Characters_NoNameTable_ShowsTheCode()
    {
        // Arrange
        ArrangeCharacter("chr004");

        // Act
        IReadOnlyList<HanteiCharacter> characters = InstantiateHanteiGame().Characters();

        // Assert
        Assert.Equal("chr004", characters[0].Name);
    }

    [Theory]
    [InlineData(".cg")]
    [InlineData(".pal")]
    [InlineData("_0.txt")]
    public void Characters_MissingFile_LeavesTheCharacterOut(string missing)
    {
        // Arrange
        ArrangeCharacter("chr004");
        _files.Contains("data/chr004/chr004" + missing).Returns(false);

        // Act
        IReadOnlyList<HanteiCharacter> characters = InstantiateHanteiGame().Characters();

        // Assert
        Assert.Empty(characters);
    }

    [Fact]
    public void Characters_OtherFiles_AreNotCharacters()
    {
        // Arrange
        _files.Names.Returns(["data/BaseData.HA6", "data/chr004/chr004_7.ha6", "data/chrxyz/chrxyz.ha6"]);

        // Act
        IReadOnlyList<HanteiCharacter> characters = InstantiateHanteiGame().Characters();

        // Assert
        Assert.Empty(characters);
    }

    [Fact]
    public void Characters_AnyOrder_SortsByCode()
    {
        // Arrange
        _files.Names.Returns(["data/chr002/chr002.ha6", "data/chr001/chr001.ha6"]);
        _files.Contains(Arg.Any<string>()).Returns(true);
        _files.Contains(NamesFile).Returns(false);

        // Act
        IReadOnlyList<HanteiCharacter> characters = InstantiateHanteiGame().Characters();

        // Assert
        Assert.Equal(["chr001", "chr002"], characters.Select(character => character.Code));
    }

    private HanteiGame InstantiateHanteiGame() => new("Game", @"X:\game", _files);

    private void ArrangeCharacter(string code)
    {
        _files.Names.Returns([$"data/{code}/{code}.ha6"]);
        _files.Contains(Arg.Any<string>()).Returns(true);
        _files.Contains(NamesFile).Returns(false);
    }

    private void ArrangeNames(string text)
    {
        _files.Contains(NamesFile).Returns(true);
        _files.Read(NamesFile).Returns(Encoding.ASCII.GetBytes(text));
    }
}
