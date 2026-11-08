using System.Text;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Scripting.Live;

public class MoveListTests
{
    public const uint Player = 0x30000000;
    public const uint List = Player + 0x8FFC;
    public const uint Count = List + 0x16C14;
    public const uint Current = List + 0x16C18;
    public const uint Names = List + 0x16C1C;
    public const uint NameCount = Names + 0x1F40;

    private readonly FakeGameMemory _memoryMock = new FakeGameMemory().Map(Player, 0x22000);

    [Fact]
    public void StartOf_NamesInTheMiddle_ReturnsWhereTheyBegin()
    {
        // Arrange
        Register(_memoryMock, "Neutral", "FWalk", "NmlAtk5A", "NmlAtk2A", "FDash");
        MoveList moveList = CreateMoveList();

        // Act
        int? start = moveList.StartOf(["NmlAtk5A", "NmlAtk2A"]);

        // Assert
        Assert.Equal(2, start);
    }

    [Fact]
    public void StartOf_NamesNotTogether_ReturnsNull()
    {
        // Arrange
        Register(_memoryMock, "Neutral", "NmlAtk5A", "FWalk", "NmlAtk2A");
        MoveList moveList = CreateMoveList();

        // Act
        int? start = moveList.StartOf(["NmlAtk5A", "NmlAtk2A"]);

        // Assert
        Assert.Null(start);
    }

    [Fact]
    public void StartOf_NoNames_ReturnsNull()
    {
        // Arrange
        Register(_memoryMock, "Neutral");
        MoveList moveList = CreateMoveList();

        // Act
        int? start = moveList.StartOf([]);

        // Assert
        Assert.Null(start);
    }

    [Fact]
    public void Truncate_Count_DropsTheLaterMovesAndTheirNames()
    {
        // Arrange
        Register(_memoryMock, "Neutral", "FWalk", "NmlAtk5A");
        _memoryMock.Int32At(Current, 2);
        MoveList moveList = CreateMoveList();

        // Act
        moveList.Truncate(1);

        // Assert
        Assert.Equal([1, -1, 1], [_memoryMock.Int32From(Count), _memoryMock.Int32From(Current), _memoryMock.Int32From(NameCount)]);
    }

    [Fact]
    public void Append_Records_WritesThemAfterTheLastMove()
    {
        // Arrange
        Register(_memoryMock, "Neutral", "FWalk", "FDash");
        MoveList moveList = CreateMoveList();
        byte[] tail = moveList.Read(2, 1);
        moveList.Truncate(1);

        // Act
        moveList.Append(tail);

        // Assert
        Assert.Equal(["Neutral", "FDash"], [moveList.NameAt(0), moveList.NameAt(1)]);
        Assert.Equal(2, moveList.Count);
    }

    [Fact]
    public void IndexNames_Moves_ListsEveryNameWithItsIndexAndSortsThem()
    {
        // Arrange
        Register(_memoryMock, "Neutral", "AutoFDash", "FDash");
        _memoryMock.Int32At(NameCount, 0);
        MoveList moveList = CreateMoveList();

        // Act
        moveList.IndexNames();

        // Assert
        Assert.Equal(3, _memoryMock.Int32From(NameCount));
        Assert.Equal(["Neutral", "AutoFDash", "FDash"], Enumerable.Range(0, 3).Select(index => Text(_memoryMock, Names + (uint)index * 32)));
        Assert.Equal([0, 1, 2], Enumerable.Range(0, 3).Select(index => _memoryMock.Int32From(Names + 0x1900 + (uint)index * 4)));
        Assert.Equal([1, 2, 0], Enumerable.Range(0, 3).Select(index => _memoryMock.Int32From(Names + 0x1C20 + (uint)index * 4)));
    }

    [Fact]
    public void IndexNames_UnderscoreAndCapitals_SortsByByteLikeTheGame()
    {
        // Arrange
        Register(_memoryMock, "Air_MidAssault", "AirJumpAssault", "AirAssault");
        MoveList moveList = CreateMoveList();

        // Act
        moveList.IndexNames();

        // Assert
        Assert.Equal([2, 1, 0], Enumerable.Range(0, 3).Select(index => _memoryMock.Int32From(Names + 0x1C20 + (uint)index * 4)));
    }

    public MoveList CreateMoveList() => new(_memoryMock, Player);

    internal static void Register(FakeGameMemory memory, params string[] names)
    {
        int count = memory.Int32From(Count);

        foreach (string name in names)
        {
            byte[] field = new byte[32];
            Encoding.Latin1.GetBytes(name, field);
            memory.BytesAt(List + (uint)(count * 0x2B4), new byte[0x2B4]).BytesAt(List + (uint)(count * 0x2B4), field);
            count++;
        }

        memory.Int32At(Count, count).Int32At(NameCount, count);
    }

    private static string Text(FakeGameMemory memory, uint address) =>
        Encoding.Latin1.GetString(memory.BytesFrom(address, 32)).TrimEnd('\0');
}
