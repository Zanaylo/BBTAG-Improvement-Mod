using System.Numerics;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Memory;

public class GameMemoryExtensionsTests
{
    private const uint Address = 0x2000;

    private readonly FakeGameMemory _memoryMock = new();

    [Fact]
    public void ReadMatrix_SixteenFloats_FillsTheRowsInOrder()
    {
        // Arrange
        _memoryMock.Map(Address, 64);

        for (uint index = 0; index < 16; index++)
            _memoryMock.SingleAt(Address + index * 4, index + 1);

        // Act
        Matrix4x4 matrix = _memoryMock.ReadMatrix(Address);

        // Assert
        Assert.Equal(new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16), matrix);
    }

    [Fact]
    public void ReadUInt32_LittleEndianBytes_ReturnsTheValue()
    {
        // Arrange
        _memoryMock.Map(Address, 4).BytesAt(Address, [0x00, 0x91, 0xE5, 0x00]);

        // Act
        uint value = _memoryMock.ReadUInt32(Address);

        // Assert
        Assert.Equal(0x00E59100u, value);
    }

    [Fact]
    public void ReadPointer_AddressNotReadable_ReturnsNull()
    {
        // Act
        uint? pointer = _memoryMock.ReadPointer(Address);

        // Assert
        Assert.Null(pointer);
    }

    [Fact]
    public void ReadPointer_FieldHoldsZero_ReturnsNull()
    {
        // Arrange
        _memoryMock.Map(Address, 4);

        // Act
        uint? pointer = _memoryMock.ReadPointer(Address);

        // Assert
        Assert.Null(pointer);
    }

    [Fact]
    public void ReadPointer_FieldHoldsAnAddress_ReturnsIt()
    {
        // Arrange
        _memoryMock.Map(Address, 4).UInt32At(Address, 0x0157D3F8);

        // Act
        uint? pointer = _memoryMock.ReadPointer(Address);

        // Assert
        Assert.Equal(0x0157D3F8u, pointer);
    }

    [Fact]
    public void WriteInt32_Value_WritesItLittleEndian()
    {
        // Arrange
        _memoryMock.Map(Address, 4);

        // Act
        _memoryMock.WriteInt32(Address, 0x14);

        // Assert
        Assert.Equal([0x14, 0x00, 0x00, 0x00], _memoryMock.BytesFrom(Address, 4));
    }

    [Fact]
    public void ReadText_FieldEndedByZero_ReturnsTheTextBeforeTheZero()
    {
        // Arrange
        _memoryMock.Map(Address, 32).BytesAt(Address, "takemikazuchi\0old"u8);

        // Act
        string text = _memoryMock.ReadText(Address, 32);

        // Assert
        Assert.Equal("takemikazuchi", text);
    }

    [Fact]
    public void ReadText_FieldFilledToTheEnd_ReturnsTheWholeField()
    {
        // Arrange
        _memoryMock.Map(Address, 4).BytesAt(Address, "boss"u8);

        // Act
        string text = _memoryMock.ReadText(Address, 4);

        // Assert
        Assert.Equal("boss", text);
    }

    [Fact]
    public void AddressLoadedBy_GetterThatLoadsAnAddress_ReturnsTheAddress()
    {
        // Arrange
        _memoryMock.Map(Address, 6).BytesAt(Address, [0xB8, 0xD0, 0xDA, 0xE5, 0x00, 0xC3]);

        // Act
        uint? loaded = _memoryMock.AddressLoadedBy(Address);

        // Assert
        Assert.Equal(0x00E5DAD0u, loaded);
    }

    [Fact]
    public void AddressLoadedBy_GetterOverwrittenByAJump_ReturnsNull()
    {
        // Arrange
        _memoryMock.Map(Address, 6).BytesAt(Address, [0xE9, 0x00, 0x00, 0x00, 0x30, 0xC3]);

        // Act
        uint? loaded = _memoryMock.AddressLoadedBy(Address);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public void AddressLoadedBy_GetterNotReadable_ReturnsNull()
    {
        // Act
        uint? loaded = _memoryMock.AddressLoadedBy(Address);

        // Assert
        Assert.Null(loaded);
    }
}
