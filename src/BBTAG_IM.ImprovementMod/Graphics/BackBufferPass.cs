namespace BBTAG_IM.ImprovementMod.Graphics;

internal static class BackBufferPass
{
    private const int CaptureAllStates = 1;

    public static void Run(nint devicePointer, Action<Direct3DDevice, DeviceViewport> draw)
    {
        var device = new Direct3DDevice(devicePointer);
        var stateBlock = device.CreateStateBlock(CaptureAllStates);

        if (stateBlock == 0)
            return;

        try
        {
            DrawOnBackBuffer(device, draw);
        }
        finally
        {
            Direct3DDevice.ApplyStateBlock(stateBlock);
            Direct3DDevice.Release(stateBlock);
        }
    }

    private static void DrawOnBackBuffer(Direct3DDevice device, Action<Direct3DDevice, DeviceViewport> draw)
    {
        var backBuffer = device.BackBuffer();

        if (backBuffer == 0)
            return;

        var previousTarget = device.RenderTarget();

        try
        {
            device.SetRenderTarget(backBuffer);
            DrawInScene(device, draw);
        }
        finally
        {
            if (previousTarget != 0)
                device.SetRenderTarget(previousTarget);

            Direct3DDevice.Release(previousTarget);
            Direct3DDevice.Release(backBuffer);
        }
    }

    private static void DrawInScene(Direct3DDevice device, Action<Direct3DDevice, DeviceViewport> draw)
    {
        if (!device.BeginScene())
            return;

        try
        {
            draw(device, device.Viewport());
        }
        finally
        {
            device.EndScene();
        }
    }
}
