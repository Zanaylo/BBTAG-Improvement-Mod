using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal static class MbtlExeBytes
{
    private const int KeyLength = 1024;
    private const int KeyAlignment = 16;
    private const int TableGapLength = 12;
    private const int StringPadding = 16;

    public static byte[] Build(byte[] key, params (string Name, uint Offset, int Size)[] files)
    {
        var builder = new TestPeBuilder();
        builder.AddSection(".rdata", rva => Section(rva, key, files));
        return builder.Build();
    }

    private static byte[] Section(uint rva, byte[] key, (string Name, uint Offset, int Size)[] files)
    {
        var sizesLength = files.Length * sizeof(int);
        var keyAt = (sizesLength + KeyAlignment - 1) / KeyAlignment * KeyAlignment;
        var namesAt = keyAt + KeyLength;
        var offsetsAt = namesAt + sizesLength + TableGapLength;
        var stringsAt = offsetsAt + sizesLength + StringPadding;
        var writer = new ByteWriter(stringsAt + files.Sum(file => file.Name.Length + 1));
        var text = stringsAt;

        writer.BytesAt(keyAt, key);

        for (var index = 0; index < files.Length; index++)
        {
            writer.UInt32At(index * sizeof(int), (uint)files[index].Size);
            writer.UInt32At(namesAt + index * sizeof(int), TestPeBuilder.DefaultImageBase + rva + (uint)text);
            writer.UInt32At(offsetsAt + index * sizeof(int), files[index].Offset);
            writer.BytesAt(text, Encoding.ASCII.GetBytes(files[index].Name + "\0"));
            text += files[index].Name.Length + 1;
        }

        return writer.ToArray();
    }
}
