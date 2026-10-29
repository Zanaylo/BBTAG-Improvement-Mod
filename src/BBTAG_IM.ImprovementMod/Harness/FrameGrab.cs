using BBTAG_IM.Core.Imaging;
using BBTAG_IM.ImprovementMod.Graphics;

namespace BBTAG_IM.ImprovementMod.Harness;

internal sealed unsafe class FrameGrab
{
    private const int BytesPerPixel = 4;

    private readonly Lock _lock = new();
    private Request? _pending;

    public string Capture(string path, TimeSpan timeout)
    {
        var request = new Request(path);

        lock (_lock)
            _pending = request;

        if (request.Done.Wait(timeout))
            return request.Reply;

        lock (_lock)
            _pending = null;

        return "error no frame was drawn in time";
    }

    public void OnPresent(nint devicePointer)
    {
        Request? request;

        lock (_lock)
        {
            request = _pending;
            _pending = null;
        }

        if (request is null)
            return;

        var image = Read(new Direct3DDevice(devicePointer), out var failure);

        if (image is null)
        {
            request.Finish($"error {failure}");
            return;
        }

        Task.Run(() => Save(request, image.Value));
    }

    private static (int Width, int Height, byte[] Pixels)? Read(Direct3DDevice device, out string failure)
    {
        var backBuffer = device.BackBuffer();
        nint plain = 0;
        nint readable = 0;

        try
        {
            if (backBuffer == 0)
            {
                failure = "the back buffer could not be taken";
                return null;
            }

            var description = Direct3DDevice.Describe(backBuffer);
            plain = device.CreatePlainRenderTarget(description.Width, description.Height, description.Format);

            if (plain == 0 || !device.Stretch(backBuffer, plain))
            {
                failure = $"the back buffer ({description.Width}x{description.Height}, format {description.Format}, multisample {description.MultiSampleType}) could not be copied";
                return null;
            }

            readable = device.CreateReadableSurface(description.Width, description.Height, description.Format);

            if (readable == 0 || !device.CopyRenderTarget(plain, readable) || Direct3DDevice.LockForReading(readable) is not { } locked)
            {
                failure = "the copy could not be read back";
                return null;
            }

            var width = (int)description.Width;
            var height = (int)description.Height;
            var pixels = new byte[width * height * BytesPerPixel];

            for (var y = 0; y < height; y++)
                new ReadOnlySpan<byte>((byte*)locked.Bits + y * locked.Pitch, width * BytesPerPixel).CopyTo(pixels.AsSpan(y * width * BytesPerPixel));

            Direct3DDevice.Unlock(readable);
            failure = string.Empty;
            return (width, height, pixels);
        }
        finally
        {
            Direct3DDevice.Release(readable);
            Direct3DDevice.Release(plain);
            Direct3DDevice.Release(backBuffer);
        }
    }

    private static void Save(Request request, (int Width, int Height, byte[] Pixels) image)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(request.Path))!);
            File.WriteAllBytes(request.Path, PngWriter.Encode(image.Width, image.Height, image.Pixels, image.Width * BytesPerPixel));
            request.Finish($"ok {image.Width}x{image.Height}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            request.Finish($"error {exception.Message}");
        }
    }

    private sealed class Request(string path)
    {
        public string Path => path;

        public ManualResetEventSlim Done { get; } = new();

        public string Reply { get; private set; } = string.Empty;

        public void Finish(string reply)
        {
            Reply = reply;
            Done.Set();
        }
    }
}
