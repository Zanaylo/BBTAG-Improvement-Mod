namespace BBTAG_IM.Core.Pe;

public static class RelocationTable
{
    public static IReadOnlySet<uint> ReadAddressSites(IImageReader image, PeHeaders headers)
    {
        var directory = headers.Directory(PeConstants.BaseRelocationDirectoryIndex);
        var sites = new HashSet<uint>();

        if (!directory.IsPresent)
            return sites;

        var end = directory.VirtualAddress + directory.Size;

        for (var block = directory.VirtualAddress; block + PeConstants.RelocationBlockHeaderSize <= end;)
        {
            var page = image.ReadUInt32(block);
            var blockSize = image.ReadUInt32(block + PeConstants.AddressSize);

            if (blockSize < PeConstants.RelocationBlockHeaderSize)
                break;

            ReadBlock(image, block, blockSize, page, sites);
            block += blockSize;
        }

        return sites;
    }

    private static void ReadBlock(IImageReader image, uint block, uint blockSize, uint page, HashSet<uint> sites)
    {
        for (var entry = block + PeConstants.RelocationBlockHeaderSize; entry < block + blockSize; entry += sizeof(ushort))
        {
            var value = image.ReadUInt16(entry);

            if (value >> PeConstants.RelocationTypeShift == PeConstants.RelocationTypeHighLow)
                sites.Add(page + (uint)(value & PeConstants.RelocationOffsetMask));
        }
    }
}
