using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class TestPeBuilder
{
    public const uint DefaultImageBase = 0x400000;
    public const uint DefaultTimeDateStamp = 0x62EB6F11;
    public const uint CodeCharacteristics = 0x60000020;
    public const uint DataCharacteristics = 0xC0000040;
    public const uint HeadersSize = 0x400;
    public const uint SectionAlignment = 0x1000;
    public const uint FileAlignment = 0x200;
    public const int PeHeaderOffset = 0x80;

    private const ushort ExecutableImageCharacteristics = 0x0102;
    private const ushort OptionalHeaderSize = 224;
    private const int DirectoryCount = 16;
    private const int MaximumImageSize = 0x100000;

    private const int EntryPointField = 16;
    private const int ImageBaseField = 28;
    private const int SectionAlignmentField = 32;
    private const int FileAlignmentField = 36;
    private const int SizeOfImageField = 56;
    private const int SizeOfHeadersField = 60;
    private const int DirectoryCountField = 92;
    private const int DirectoriesField = 96;

    private const int SectionVirtualSizeField = 8;
    private const int SectionVirtualAddressField = 12;
    private const int SectionRawSizeField = 16;
    private const int SectionRawPointerField = 20;
    private const int SectionCharacteristicsField = 36;

    private readonly List<TestSection> _sections = [];
    private readonly PeDataDirectory[] _directories = new PeDataDirectory[DirectoryCount];
    private uint _imageBase = DefaultImageBase;
    private uint _timeDateStamp = DefaultTimeDateStamp;
    private ushort _fileCharacteristics = ExecutableImageCharacteristics;
    private ushort _machine = PeConstants.MachineI386;
    private ushort _optionalHeaderMagic = PeConstants.OptionalHeader32Magic;
    private uint _entryPoint;

    public uint NextSectionRva
    {
        get
        {
            if (_sections.Count == 0)
                return SectionAlignment;

            var last = _sections[^1];
            return Align(last.Rva + Math.Max(last.VirtualSize, (uint)last.Data.Length), SectionAlignment);
        }
    }

    public uint AddSection(string name, byte[] data, uint characteristics = DataCharacteristics, uint? virtualSize = null)
    {
        var rva = NextSectionRva;
        _sections.Add(new TestSection(name, rva, data, characteristics, virtualSize ?? (uint)data.Length));
        return rva;
    }

    public uint AddSection(string name, Func<uint, byte[]> buildAtRva, uint characteristics = DataCharacteristics) =>
        AddSection(name, buildAtRva(NextSectionRva), characteristics);

    public TestPeBuilder WithDirectory(int index, uint rva, uint size)
    {
        _directories[index] = new PeDataDirectory(rva, size);
        return this;
    }

    public TestPeBuilder WithTimeDateStamp(uint value)
    {
        _timeDateStamp = value;
        return this;
    }

    public TestPeBuilder WithFileCharacteristics(ushort value)
    {
        _fileCharacteristics = value;
        return this;
    }

    public TestPeBuilder WithImageBase(uint value)
    {
        _imageBase = value;
        return this;
    }

    public TestPeBuilder WithEntryPoint(uint value)
    {
        _entryPoint = value;
        return this;
    }

    public TestPeBuilder WithMachine(ushort value)
    {
        _machine = value;
        return this;
    }

    public TestPeBuilder WithOptionalHeaderMagic(ushort value)
    {
        _optionalHeaderMagic = value;
        return this;
    }

    public byte[] Build()
    {
        var writer = new ByteWriter(MaximumImageSize);
        WriteDosHeader(writer);
        var optionalHeader = WriteFileHeader(writer);
        WriteOptionalHeader(writer, optionalHeader);
        WriteSections(writer, optionalHeader + OptionalHeaderSize);
        return writer.ToArray((int)Math.Max(writer.Length, HeadersSize));
    }

    public static uint Align(uint value, uint alignment) => (value + alignment - 1) / alignment * alignment;

    private static void WriteDosHeader(ByteWriter writer)
    {
        writer.UInt16At(0, PeConstants.DosSignature);
        writer.UInt32At(PeConstants.DosNewHeaderOffsetField, PeHeaderOffset);
        writer.UInt32At(PeHeaderOffset, PeConstants.NtSignature);
    }

    private int WriteFileHeader(ByteWriter writer)
    {
        var fileHeader = PeHeaderOffset + PeConstants.NtSignatureSize;
        writer.UInt16At(fileHeader, _machine);
        writer.UInt16At(fileHeader + 2, (ushort)_sections.Count);
        writer.UInt32At(fileHeader + 4, _timeDateStamp);
        writer.UInt16At(fileHeader + 16, OptionalHeaderSize);
        writer.UInt16At(fileHeader + 18, _fileCharacteristics);
        return fileHeader + PeConstants.FileHeaderSize;
    }

    private void WriteOptionalHeader(ByteWriter writer, int optionalHeader)
    {
        writer.UInt16At(optionalHeader, _optionalHeaderMagic);
        writer.UInt32At(optionalHeader + EntryPointField, _entryPoint);
        writer.UInt32At(optionalHeader + ImageBaseField, _imageBase);
        writer.UInt32At(optionalHeader + SectionAlignmentField, SectionAlignment);
        writer.UInt32At(optionalHeader + FileAlignmentField, FileAlignment);
        writer.UInt32At(optionalHeader + SizeOfImageField, NextSectionRva);
        writer.UInt32At(optionalHeader + SizeOfHeadersField, HeadersSize);
        writer.UInt32At(optionalHeader + DirectoryCountField, DirectoryCount);

        for (var index = 0; index < DirectoryCount; index++)
        {
            var directory = optionalHeader + DirectoriesField + index * PeConstants.DataDirectorySize;
            writer.UInt32At(directory, _directories[index].VirtualAddress);
            writer.UInt32At(directory + 4, _directories[index].Size);
        }
    }

    private void WriteSections(ByteWriter writer, int sectionTable)
    {
        var rawPointer = HeadersSize;

        for (var index = 0; index < _sections.Count; index++)
        {
            var section = _sections[index];
            var header = sectionTable + index * PeConstants.SectionHeaderSize;
            var rawSize = Align((uint)section.Data.Length, FileAlignment);

            writer.AsciiAt(header, section.Name);
            writer.UInt32At(header + SectionVirtualSizeField, section.VirtualSize);
            writer.UInt32At(header + SectionVirtualAddressField, section.Rva);
            writer.UInt32At(header + SectionRawSizeField, rawSize);
            writer.UInt32At(header + SectionRawPointerField, rawPointer);
            writer.UInt32At(header + SectionCharacteristicsField, section.Characteristics);
            writer.BytesAt((int)rawPointer, section.Data);

            if (rawSize > 0)
                writer.BytesAt((int)(rawPointer + rawSize - 1), [section.Data.Length == rawSize ? section.Data[^1] : (byte)0]);

            rawPointer += rawSize;
        }
    }

    private sealed record TestSection(string Name, uint Rva, byte[] Data, uint Characteristics, uint VirtualSize);
}
