namespace BBTAG_IM.Core.IO;

public sealed class MemoryFile(byte[] bytes)
{
    private readonly Lock _gate = new();
    private long _position;

    public long Length => bytes.Length;

    public long Position
    {
        get
        {
            lock (_gate)
                return _position;
        }
    }

    public int Read(Span<byte> destination)
    {
        lock (_gate)
        {
            var read = CopyFrom(_position, destination);
            _position += read;
            return read;
        }
    }

    public int ReadAt(long offset, Span<byte> destination)
    {
        lock (_gate)
        {
            var read = CopyFrom(offset, destination);
            _position = offset + read;
            return read;
        }
    }

    public long? Seek(long distance, SeekOrigin origin)
    {
        lock (_gate)
        {
            var target = OffsetOf(origin) + distance;

            if (target < 0)
                return null;

            _position = target;
            return target;
        }
    }

    private long OffsetOf(SeekOrigin origin) => origin switch
    {
        SeekOrigin.Current => _position,
        SeekOrigin.End => bytes.Length,
        _ => 0,
    };

    private int CopyFrom(long offset, Span<byte> destination)
    {
        if (offset < 0 || offset >= bytes.Length)
            return 0;

        var count = (int)Math.Min(destination.Length, bytes.Length - offset);
        bytes.AsSpan((int)offset, count).CopyTo(destination);
        return count;
    }
}
