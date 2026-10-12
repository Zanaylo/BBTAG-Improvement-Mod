using System.Buffers.Binary;
using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Patching;

public static class ExePatchPlanner
{
    private const int BranchFieldSize = sizeof(uint);

    public static ExePatchPlan Plan(FileImage stock, FileImage modded)
    {
        EnsureCompatible(stock.Headers, modded.Headers);

        var context = new PlanContext(
            RelocationTable.ReadAddressSites(stock, stock.Headers),
            new AddressRange(modded.Headers.ImageBase, modded.Headers.SizeOfImage),
            new AddressRange(stock.Headers.SizeOfImage, modded.Headers.SizeOfImage - stock.Headers.SizeOfImage));
        var stockSections = stock.Headers.Sections;

        var writes = stockSections
            .SelectMany((section, index) => new SectionWritePlanner(
                stock.SectionData(section).ToArray(),
                modded.SectionData(modded.Headers.Sections[index]).ToArray(),
                section,
                context).Plan())
            .ToArray();
        var entries = writes.SelectMany(BranchTargets).ToArray();
        var sections = modded.Headers.Sections
            .Skip(stockSections.Count)
            .Select(section => NewSection(modded, section, entries, context.Image))
            .ToArray();

        return new ExePatchPlan(stock.Headers.ImageBase, context.NewSectionRvas, writes, sections);
    }

    private static void EnsureCompatible(PeHeaders stock, PeHeaders modded)
    {
        if (stock.TimeDateStamp != modded.TimeDateStamp)
            throw new IncompatibleExecutableException(
                $"The modded BBTAG.exe was made from a different game build (0x{modded.TimeDateStamp:X8}) than the installed one (0x{stock.TimeDateStamp:X8}).");

        if (stock.ImageBase != modded.ImageBase)
            throw new IncompatibleExecutableException("The modded BBTAG.exe uses a different image base than the installed one.");

        if (modded.Sections.Count < stock.Sections.Count)
            throw new IncompatibleExecutableException("The modded BBTAG.exe is missing sections of the installed one.");

        if (modded.SizeOfImage < stock.SizeOfImage)
            throw new IncompatibleExecutableException("The modded BBTAG.exe is smaller in memory than the installed one.");

        var moved = stock.Sections.Where((section, index) => !IsSameSection(section, modded.Sections[index])).FirstOrDefault();

        if (moved is not null)
            throw new IncompatibleExecutableException($"Section {moved.Name} is laid out differently in the modded BBTAG.exe.");
    }

    private static bool IsSameSection(PeSection stock, PeSection modded) =>
        stock.Name == modded.Name && stock.HasSameLayoutAs(modded) && stock.RawDataSize == modded.RawDataSize;

    private static IEnumerable<uint> BranchTargets(PatchWrite write) =>
        write.BranchFields.Select(field =>
            write.Rva + (uint)(field + BranchFieldSize) + BinaryPrimitives.ReadUInt32LittleEndian(write.Bytes.AsSpan(field)));

    private static PatchSection NewSection(FileImage modded, PeSection section, uint[] entries, AddressRange image)
    {
        var rawData = modded.SectionData(section);
        var virtualSize = section.VirtualSize == 0 ? (uint)rawData.Length : section.VirtualSize;
        var bytes = rawData[..Math.Min(rawData.Length, (int)virtualSize)].ToArray();
        var fields = section.IsExecutable
            ? CaveCodeScanner.FindFields(bytes, section.VirtualAddress, entries, image)
            : new CaveFields([], []);

        return new PatchSection(section.Name, section.VirtualAddress, virtualSize, bytes, section.IsExecutable, fields.AddressFields, fields.BranchFields);
    }
}
