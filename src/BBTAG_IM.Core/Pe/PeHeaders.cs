using System.Buffers.Binary;
using System.Text;

namespace BBTAG_IM.Core.Pe;

public sealed record PeHeaders(
    ushort Machine,
    uint TimeDateStamp,
    ushort FileCharacteristics,
    uint ImageBase,
    uint SizeOfImage,
    uint SizeOfHeaders,
    uint AddressOfEntryPoint,
    IReadOnlyList<PeDataDirectory> DataDirectories,
    IReadOnlyList<PeSection> Sections)
{
    private const int NumberOfSectionsField = 2;
    private const int TimeDateStampField = 4;
    private const int SizeOfOptionalHeaderField = 16;
    private const int FileCharacteristicsField = 18;

    private const int EntryPointField = 16;
    private const int ImageBaseField = 28;
    private const int SizeOfImageField = 56;
    private const int SizeOfHeadersField = 60;
    private const int DirectoryCountField = 92;
    private const int DirectoriesField = 96;

    private const int SectionVirtualSizeField = 8;
    private const int SectionVirtualAddressField = 12;
    private const int SectionRawSizeField = 16;
    private const int SectionRawPointerField = 20;
    private const int SectionCharacteristicsField = 36;

    public bool RelocationsStripped => (FileCharacteristics & PeConstants.FileRelocsStripped) != 0;

    public PeDataDirectory Directory(int index) =>
        index < DataDirectories.Count ? DataDirectories[index] : default;

    public PeSection? SectionContaining(uint rva) => Sections.FirstOrDefault(section => section.ContainsRva(rva));

    public static PeHeaders Parse(ReadOnlySpan<byte> image)
    {
        if (UInt16At(image, 0) != PeConstants.DosSignature)
            throw new InvalidPeImageException("The file is not a Windows executable (missing MZ signature).");

        var ntHeaders = (int)UInt32At(image, PeConstants.DosNewHeaderOffsetField);

        if (UInt32At(image, ntHeaders) != PeConstants.NtSignature)
            throw new InvalidPeImageException("The file is not a Windows executable (missing PE signature).");

        var fileHeader = ntHeaders + PeConstants.NtSignatureSize;
        var machine = UInt16At(image, fileHeader);

        if (machine != PeConstants.MachineI386)
            throw new InvalidPeImageException($"Only 32-bit x86 executables are supported (machine 0x{machine:X4}).");

        var optionalHeader = fileHeader + PeConstants.FileHeaderSize;

        if (UInt16At(image, optionalHeader) != PeConstants.OptionalHeader32Magic)
            throw new InvalidPeImageException("Only PE32 optional headers are supported.");

        var sectionTable = optionalHeader + UInt16At(image, fileHeader + SizeOfOptionalHeaderField);

        return new PeHeaders(
            machine,
            UInt32At(image, fileHeader + TimeDateStampField),
            UInt16At(image, fileHeader + FileCharacteristicsField),
            UInt32At(image, optionalHeader + ImageBaseField),
            UInt32At(image, optionalHeader + SizeOfImageField),
            UInt32At(image, optionalHeader + SizeOfHeadersField),
            UInt32At(image, optionalHeader + EntryPointField),
            ReadDirectories(image, optionalHeader),
            ReadSections(image, sectionTable, UInt16At(image, fileHeader + NumberOfSectionsField)));
    }

    private static PeDataDirectory[] ReadDirectories(ReadOnlySpan<byte> image, int optionalHeader)
    {
        var count = (int)UInt32At(image, optionalHeader + DirectoryCountField);
        var directories = new PeDataDirectory[count];

        for (var index = 0; index < count; index++)
        {
            var entry = optionalHeader + DirectoriesField + index * PeConstants.DataDirectorySize;
            directories[index] = new PeDataDirectory(UInt32At(image, entry), UInt32At(image, entry + PeConstants.AddressSize));
        }

        return directories;
    }

    private static PeSection[] ReadSections(ReadOnlySpan<byte> image, int sectionTable, int count)
    {
        var sections = new PeSection[count];

        for (var index = 0; index < count; index++)
        {
            var header = sectionTable + index * PeConstants.SectionHeaderSize;
            sections[index] = new PeSection(
                SectionName(Slice(image, header, PeConstants.SectionNameSize)),
                UInt32At(image, header + SectionVirtualAddressField),
                UInt32At(image, header + SectionVirtualSizeField),
                UInt32At(image, header + SectionRawPointerField),
                UInt32At(image, header + SectionRawSizeField),
                UInt32At(image, header + SectionCharacteristicsField));
        }

        return sections;
    }

    private static string SectionName(ReadOnlySpan<byte> field)
    {
        var terminator = field.IndexOf((byte)0);
        return Encoding.ASCII.GetString(terminator < 0 ? field : field[..terminator]);
    }

    private static ushort UInt16At(ReadOnlySpan<byte> image, int offset) =>
        BinaryPrimitives.ReadUInt16LittleEndian(Slice(image, offset, sizeof(ushort)));

    private static uint UInt32At(ReadOnlySpan<byte> image, int offset) =>
        BinaryPrimitives.ReadUInt32LittleEndian(Slice(image, offset, sizeof(uint)));

    private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> image, int offset, int length)
    {
        if (offset < 0 || offset + length > image.Length)
            throw new InvalidPeImageException("The executable headers are truncated.");

        return image.Slice(offset, length);
    }
}
