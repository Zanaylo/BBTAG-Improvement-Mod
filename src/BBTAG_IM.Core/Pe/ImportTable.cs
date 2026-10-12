namespace BBTAG_IM.Core.Pe;

public static class ImportTable
{
    private const int NameField = 12;
    private const int FirstThunkField = 16;
    private const int HintSize = sizeof(ushort);

    public static uint? FindSlotRva(IImageReader image, PeHeaders headers, string dllName, string functionName)
    {
        var directory = headers.Directory(PeConstants.ImportDirectoryIndex);

        if (!directory.IsPresent)
            return null;

        var descriptor = FindDescriptor(image, directory.VirtualAddress, dllName);

        if (descriptor is null)
            return null;

        return FindSlot(image, descriptor.Value, functionName);
    }

    private static uint? FindDescriptor(IImageReader image, uint descriptor, string dllName)
    {
        for (; image.ReadUInt32(descriptor + NameField) != 0; descriptor += PeConstants.ImportDescriptorSize)
        {
            if (string.Equals(image.ReadAsciiString(image.ReadUInt32(descriptor + NameField)), dllName, StringComparison.OrdinalIgnoreCase))
                return descriptor;
        }

        return null;
    }

    private static uint? FindSlot(IImageReader image, uint descriptor, string functionName)
    {
        var names = image.ReadUInt32(descriptor);
        var slots = image.ReadUInt32(descriptor + FirstThunkField);

        if (names == 0)
            return null;

        for (uint index = 0; ; index++)
        {
            var entry = image.ReadUInt32(names + index * PeConstants.AddressSize);

            if (entry == 0)
                return null;

            if ((entry & PeConstants.ImportByOrdinalFlag) != 0)
                continue;

            if (image.ReadAsciiString(entry + HintSize) == functionName)
                return slots + index * PeConstants.AddressSize;
        }
    }
}
