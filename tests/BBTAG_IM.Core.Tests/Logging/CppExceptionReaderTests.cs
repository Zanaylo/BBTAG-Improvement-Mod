using System.Text;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Tests.Support;

namespace BBTAG_IM.Core.Tests.Logging;

public class CppExceptionReaderTests
{
    private const uint Record = 0x00100000;
    private const uint ThrownObject = 0x00200000;
    private const uint ThrowInfo = 0x00300000;
    private const uint CatchableTypes = 0x00300100;
    private const uint RuntimeErrorType = 0x00300200;
    private const uint ExceptionType = 0x00300300;
    private const uint RuntimeErrorDescriptor = 0x00300400;
    private const uint ExceptionDescriptor = 0x00300500;
    private const uint WhatText = 0x00400000;
    private const uint MsvcMagic = 0x19930520;
    private const string RuntimeErrorName = ".?AVruntime_error@std@@";
    private const string ExceptionName = ".?AVexception@std@@";
    private const string SteamworksError = "can not initialize steamworks.";

    [Fact]
    public void Message_StdExceptionThrown_ReturnsItsText()
    {
        // Arrange
        FakeGameMemory memory = CreateThrownException(RuntimeErrorName, ExceptionName);

        // Act
        string? message = CppExceptionReader.Message(memory, Record);

        // Assert
        Assert.Equal(SteamworksError, message);
    }

    [Fact]
    public void Message_OtherExceptionCode_ReturnsNull()
    {
        // Arrange
        FakeGameMemory memory = CreateThrownException(RuntimeErrorName, ExceptionName).UInt32At(Record, 0xC0000005);

        // Act
        string? message = CppExceptionReader.Message(memory, Record);

        // Assert
        Assert.Null(message);
    }

    [Fact]
    public void Message_TooFewParameters_ReturnsNull()
    {
        // Arrange
        FakeGameMemory memory = CreateThrownException(RuntimeErrorName, ExceptionName).UInt32At(Record + 16, 2);

        // Act
        string? message = CppExceptionReader.Message(memory, Record);

        // Assert
        Assert.Null(message);
    }

    [Fact]
    public void Message_ThrownTypeIsNotAStdException_ReturnsNull()
    {
        // Arrange
        FakeGameMemory memory = CreateThrownException(".?AVGameError@@", ".?AVBaseError@@");

        // Act
        string? message = CppExceptionReader.Message(memory, Record);

        // Assert
        Assert.Null(message);
    }

    [Fact]
    public void Message_TextPointerUnreadable_ReturnsNull()
    {
        // Arrange
        FakeGameMemory memory = CreateThrownException(RuntimeErrorName, ExceptionName).UInt32At(ThrownObject + 4, 0x00900000);

        // Act
        string? message = CppExceptionReader.Message(memory, Record);

        // Assert
        Assert.Null(message);
    }

    [Fact]
    public void Message_RecordUnreadable_ReturnsNull()
    {
        // Arrange
        FakeGameMemory memory = new();

        // Act
        string? message = CppExceptionReader.Message(memory, Record);

        // Assert
        Assert.Null(message);
    }

    private static FakeGameMemory CreateThrownException(string thrownName, string baseName) =>
        new FakeGameMemory()
            .Map(Record, 0x50)
            .Map(ThrownObject, 0x10)
            .Map(ThrowInfo, 0x600)
            .Map(WhatText, 0x40)
            .UInt32At(Record, CppExceptionReader.CppExceptionCode)
            .UInt32At(Record + 16, 3)
            .UInt32At(Record + 20, MsvcMagic)
            .UInt32At(Record + 24, ThrownObject)
            .UInt32At(Record + 28, ThrowInfo)
            .UInt32At(ThrownObject + 4, WhatText)
            .BytesAt(WhatText, Encoding.ASCII.GetBytes(SteamworksError + "\0"))
            .UInt32At(ThrowInfo + 12, CatchableTypes)
            .UInt32At(CatchableTypes, 2)
            .UInt32At(CatchableTypes + 4, RuntimeErrorType)
            .UInt32At(CatchableTypes + 8, ExceptionType)
            .UInt32At(RuntimeErrorType + 4, RuntimeErrorDescriptor)
            .UInt32At(ExceptionType + 4, ExceptionDescriptor)
            .BytesAt(RuntimeErrorDescriptor + 8, Encoding.ASCII.GetBytes(thrownName + "\0"))
            .BytesAt(ExceptionDescriptor + 8, Encoding.ASCII.GetBytes(baseName + "\0"));
}
