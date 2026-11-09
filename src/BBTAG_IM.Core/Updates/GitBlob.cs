using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BBTAG_IM.Core.Updates;

public static class GitBlob
{
    private const int BufferLength = 1 << 20;

    public static string Sha(Stream content)
    {
        content.Position = 0;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        hash.AppendData(Encoding.ASCII.GetBytes(string.Create(CultureInfo.InvariantCulture, $"blob {content.Length}\0")));

        var buffer = new byte[BufferLength];
        int read;

        while ((read = content.Read(buffer)) > 0)
            hash.AppendData(buffer, 0, read);

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
