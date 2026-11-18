using System.IO.Compression;

namespace BBTAG_IM.Core.Tests.Support;

internal static class TestArchives
{
    public static string Zip(string path, params (string Name, string Text)[] entries)
    {
        File.Delete(path);

        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            foreach (var (name, text) in entries)
            {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(text);
            }
        }

        return path;
    }

    public static string ZipBytes(string path, string name, byte[] bytes)
    {
        File.Delete(path);

        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        using (var entry = zip.CreateEntry(name).Open())
            entry.Write(bytes);

        return path;
    }

    public static string Fixture(string fixtureName, string path)
    {
        var base64 = (string)typeof(ArchiveFixtures).GetField(fixtureName)!.GetRawConstantValue()!;
        File.WriteAllBytes(path, Convert.FromBase64String(base64));
        return path;
    }
}
