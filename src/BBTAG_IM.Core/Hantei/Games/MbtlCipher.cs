namespace BBTAG_IM.Core.Hantei.Games;

public static class MbtlCipher
{
    public const int KeyLength = 1024;

    private const byte FirstMask = 0xA5;
    private const byte SecondMask = 0x18;
    private const byte KeyRowMask = 0xAC;
    private const int StreamSeed = 0x76381;
    private const int HeadLength = 2;

    public static byte[] Decode(byte[] coded, ReadOnlySpan<byte> key)
    {
        var plain = coded.ToArray();

        if (plain.Length < HeadLength)
            return plain;

        plain[0] ^= FirstMask;
        plain[1] ^= SecondMask;

        var row = plain[0] ^ KeyRowMask;
        var stream = row ^ plain[1] ^ StreamSeed;

        for (var index = plain.Length - 1; index >= HeadLength; index--, stream++)
            plain[index] ^= key[row ^ (stream & (KeyLength - 1))];

        return plain;
    }
}
