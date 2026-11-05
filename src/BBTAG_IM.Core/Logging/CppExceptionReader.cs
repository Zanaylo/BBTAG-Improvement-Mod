using BBTAG_IM.Core.Memory;

namespace BBTAG_IM.Core.Logging;

public static class CppExceptionReader
{
    public const uint CppExceptionCode = 0xE06D7363;

    private const string StdExceptionTypeName = ".?AVexception@std@@";
    private const int CodeField = 0;
    private const int ParameterCountField = 16;
    private const int ThrownObjectField = 24;
    private const int ThrowInfoField = 28;
    private const int RecordLength = 32;
    private const uint ParametersNeeded = 3;
    private const int CatchableTypesField = 12;
    private const int TypeDescriptorField = 4;
    private const int TypeNameField = 8;
    private const int WhatTextField = 4;
    private const int TypeNameLimit = 64;
    private const int WhatTextLimit = 256;
    private const uint CatchableTypeLimit = 32;

    public static string? Message(IGameMemory memory, uint exceptionRecord)
    {
        if (!memory.CanRead(exceptionRecord, RecordLength))
            return null;

        if (memory.ReadUInt32(exceptionRecord + CodeField) != CppExceptionCode)
            return null;

        if (memory.ReadUInt32(exceptionRecord + ParameterCountField) < ParametersNeeded)
            return null;

        var thrownObject = memory.ReadUInt32(exceptionRecord + ThrownObjectField);
        var throwInfo = memory.ReadUInt32(exceptionRecord + ThrowInfoField);

        if (!IsStdException(memory, throwInfo))
            return null;

        return memory.ReadPointer(thrownObject + WhatTextField) is { } whatText ? ReadTextIfMapped(memory, whatText, WhatTextLimit) : null;
    }

    private static bool IsStdException(IGameMemory memory, uint throwInfo)
    {
        if (memory.ReadPointer(throwInfo + CatchableTypesField) is not { } catchableTypes)
            return false;

        if (!memory.CanRead(catchableTypes, sizeof(uint)))
            return false;

        var count = Math.Min(memory.ReadUInt32(catchableTypes), CatchableTypeLimit);

        for (var index = 1u; index <= count; index++)
        {
            if (TypeName(memory, catchableTypes + index * sizeof(uint)) == StdExceptionTypeName)
                return true;
        }

        return false;
    }

    private static string? TypeName(IGameMemory memory, uint catchableTypeSlot)
    {
        if (memory.ReadPointer(catchableTypeSlot) is not { } catchableType)
            return null;

        if (memory.ReadPointer(catchableType + TypeDescriptorField) is not { } descriptor)
            return null;

        return ReadTextIfMapped(memory, descriptor + TypeNameField, TypeNameLimit);
    }

    private static string? ReadTextIfMapped(IGameMemory memory, uint address, int limit)
    {
        for (var length = limit; length > 0; length--)
        {
            if (memory.CanRead(address, length))
                return memory.ReadText(address, length);
        }

        return null;
    }
}
