using BBTAG_IM.Core.Pe;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed record TestImportLayout(byte[] Data, uint DirectoryRva, uint DirectorySize, IReadOnlyDictionary<string, uint> SlotRvas);

internal sealed record TestExportLayout(byte[] Data, uint DirectoryRva, uint DirectorySize);

internal static class TestPeTables
{
    public const string OrdinalPrefix = "#";

    private const int SectionCapacity = 0x4000;
    private const int ExportDirectorySize = 40;
    private const ushort RelocationPadding = 0;

    public static string SlotKey(string dll, string function) => $"{dll}!{function}";

    public static TestImportLayout Imports(uint sectionRva, IReadOnlyList<(string Dll, string[] Functions)> dlls)
    {
        ByteWriter writer = new(SectionCapacity);
        Dictionary<string, uint> slots = [];
        int cursor = (dlls.Count + 1) * PeConstants.ImportDescriptorSize;
        List<(int Names, int Slots)> thunks = [];

        foreach ((string _, string[] functions) in dlls)
        {
            int tableSize = (functions.Length + 1) * PeConstants.AddressSize;
            thunks.Add((cursor, cursor + tableSize));
            cursor += tableSize * 2;
        }

        for (int dllIndex = 0; dllIndex < dlls.Count; dllIndex++)
        {
            (string dll, string[] functions) = dlls[dllIndex];
            int descriptor = dllIndex * PeConstants.ImportDescriptorSize;
            (int names, int slotTable) = thunks[dllIndex];

            writer.UInt32At(descriptor, sectionRva + (uint)names);
            writer.UInt32At(descriptor + 12, sectionRva + (uint)cursor);
            writer.UInt32At(descriptor + 16, sectionRva + (uint)slotTable);
            cursor = writer.AsciiAt(cursor, dll);

            for (int index = 0; index < functions.Length; index++)
            {
                uint entry = EntryFor(functions[index], sectionRva, ref cursor, writer);
                writer.UInt32At(names + index * PeConstants.AddressSize, entry);
                writer.UInt32At(slotTable + index * PeConstants.AddressSize, entry);
                slots[SlotKey(dll, functions[index])] = sectionRva + (uint)(slotTable + index * PeConstants.AddressSize);
            }
        }

        writer.BytesAt(cursor, [0]);
        return new TestImportLayout(writer.ToArray(), sectionRva, (uint)((dlls.Count + 1) * PeConstants.ImportDescriptorSize), slots);
    }

    public static TestExportLayout Exports(uint sectionRva, string dllName, IReadOnlyList<(string Name, uint Rva)> functions)
    {
        ByteWriter writer = new(SectionCapacity);
        (string Name, uint Rva)[] sorted = functions.OrderBy(function => function.Name, StringComparer.Ordinal).ToArray();
        int addresses = ExportDirectorySize;
        int names = addresses + sorted.Length * PeConstants.AddressSize;
        int ordinals = names + sorted.Length * PeConstants.AddressSize;
        int cursor = ordinals + sorted.Length * sizeof(ushort);

        writer.UInt32At(12, sectionRva + (uint)cursor);
        cursor = writer.AsciiAt(cursor, dllName);
        writer.UInt32At(16, 1);
        writer.UInt32At(20, (uint)sorted.Length);
        writer.UInt32At(24, (uint)sorted.Length);
        writer.UInt32At(28, sectionRva + (uint)addresses);
        writer.UInt32At(32, sectionRva + (uint)names);
        writer.UInt32At(36, sectionRva + (uint)ordinals);

        for (int index = 0; index < sorted.Length; index++)
        {
            writer.UInt32At(addresses + index * PeConstants.AddressSize, sorted[index].Rva);
            writer.UInt32At(names + index * PeConstants.AddressSize, sectionRva + (uint)cursor);
            writer.UInt16At(ordinals + index * sizeof(ushort), (ushort)index);
            cursor = writer.AsciiAt(cursor, sorted[index].Name);
        }

        return new TestExportLayout(writer.ToArray(), sectionRva, (uint)cursor);
    }

    public static byte[] Relocations(IEnumerable<uint> addressSites)
    {
        ByteWriter writer = new(SectionCapacity);
        int cursor = 0;

        foreach (IGrouping<uint, uint> page in addressSites.GroupBy(site => site & ~(uint)PeConstants.RelocationOffsetMask).OrderBy(group => group.Key))
        {
            List<ushort> entries = page.Order().Select(site => (ushort)(PeConstants.RelocationTypeHighLow << PeConstants.RelocationTypeShift | (site & PeConstants.RelocationOffsetMask))).ToList();

            if (entries.Count % 2 != 0)
                entries.Add(RelocationPadding);

            writer.UInt32At(cursor, page.Key);
            writer.UInt32At(cursor + 4, (uint)(PeConstants.RelocationBlockHeaderSize + entries.Count * sizeof(ushort)));

            for (int index = 0; index < entries.Count; index++)
                writer.UInt16At(cursor + PeConstants.RelocationBlockHeaderSize + index * sizeof(ushort), entries[index]);

            cursor += PeConstants.RelocationBlockHeaderSize + entries.Count * sizeof(ushort);
        }

        return writer.ToArray();
    }

    private static uint EntryFor(string function, uint sectionRva, ref int cursor, ByteWriter writer)
    {
        if (function.StartsWith(OrdinalPrefix, StringComparison.Ordinal))
            return PeConstants.ImportByOrdinalFlag | uint.Parse(function[OrdinalPrefix.Length..]);

        int hintName = cursor + cursor % sizeof(ushort);
        writer.UInt16At(hintName, 0);
        cursor = writer.AsciiAt(hintName + sizeof(ushort), function);
        return sectionRva + (uint)hintName;
    }
}
