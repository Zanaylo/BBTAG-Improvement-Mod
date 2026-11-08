using System.Text;

namespace BBTAG_IM.Core.Tests.Support;

internal sealed class Ha6Bytes
{
    private const int FileNameLength = 32;
    private const int NameFieldLength = 32;

    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;

    public Ha6Bytes()
    {
        _writer = new BinaryWriter(_stream);
        _writer.Write(Encoding.ASCII.GetBytes("Hantei6DataFile".PadRight(FileNameLength, '\0')));
        _writer.Write("_STR"u8);
        _writer.Write(0);
    }

    public Ha6Bytes Tag(string tag, params int[] values)
    {
        _writer.Write(Encoding.ASCII.GetBytes(tag));

        foreach (var value in values)
            _writer.Write(value);

        return this;
    }

    public Ha6Bytes Name(string tag, string name)
    {
        var bytes = new byte[NameFieldLength];
        Encoding.ASCII.GetBytes(name, bytes);
        _writer.Write(Encoding.ASCII.GetBytes(tag));
        _writer.Write(bytes.Length);
        _writer.Write(bytes);
        return this;
    }

    public Ha6Bytes Pattern(int id, string name, Action<Ha6Bytes> body)
    {
        Tag("PSTR", id);
        Name("PTT2", name);
        body(this);
        return Tag("PEND");
    }

    public Ha6Bytes Frame(Action<Ha6Bytes> body)
    {
        Tag("FSTR");
        body(this);
        return Tag("FEND");
    }

    public Ha6Bytes Anim(Action<Ha6Bytes> body)
    {
        Tag("AFST");
        body(this);
        return Tag("AFED");
    }

    public Ha6Bytes State(Action<Ha6Bytes> body)
    {
        Tag("ASST");
        body(this);
        return Tag("ASED");
    }

    public Ha6Bytes Attack(Action<Ha6Bytes> body)
    {
        Tag("ATST");
        body(this);
        return Tag("ATED");
    }

    public byte[] Build()
    {
        Tag("_END");
        _writer.Flush();
        return _stream.ToArray();
    }
}
