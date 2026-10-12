namespace BBTAG_IM.Core.Pe;

public static class ExportTable
{
    private const int NumberOfNamesField = 24;
    private const int AddressOfFunctionsField = 28;
    private const int AddressOfNamesField = 32;
    private const int AddressOfNameOrdinalsField = 36;

    public static uint? FindFunctionRva(IImageReader image, PeHeaders headers, string functionName)
    {
        var directory = headers.Directory(PeConstants.ExportDirectoryIndex);

        if (!directory.IsPresent)
            return null;

        var exports = directory.VirtualAddress;
        var count = image.ReadUInt32(exports + NumberOfNamesField);
        var names = image.ReadUInt32(exports + AddressOfNamesField);

        for (uint index = 0; index < count; index++)
        {
            if (image.ReadAsciiString(image.ReadUInt32(names + index * PeConstants.AddressSize)) != functionName)
                continue;

            var ordinal = image.ReadUInt16(image.ReadUInt32(exports + AddressOfNameOrdinalsField) + index * sizeof(ushort));
            return image.ReadUInt32(image.ReadUInt32(exports + AddressOfFunctionsField) + ordinal * (uint)PeConstants.AddressSize);
        }

        return null;
    }
}
