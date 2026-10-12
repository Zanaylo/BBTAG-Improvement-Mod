namespace BBTAG_IM.ImprovementMod.Graphics;

internal sealed class BackBufferPass
{
    private const int CaptureAllStates = 1;

    private nint _stateBlock;
    private nint _stateBlockDevice;

    public void Run(nint devicePointer, Action<Direct3DDevice, DeviceViewport> draw)
    {
        var device = new Direct3DDevice(devicePointer);
        var stateBlock = SavedStates(device, devicePointer);

        if (stateBlock == 0)
            return;

        try
        {
            DrawOnBackBuffer(device, draw);
        }
        finally
        {
            Direct3DDevice.ApplyStateBlock(stateBlock);
        }
    }

    public void Release()
    {
        Direct3DDevice.Release(_stateBlock);
        _stateBlock = 0;
        _stateBlockDevice = 0;
    }

    private nint SavedStates(Direct3DDevice device, nint devicePointer)
    {
        if (_stateBlock != 0 && _stateBlockDevice == devicePointer)
            return Direct3DDevice.CaptureStateBlock(_stateBlock) ? _stateBlock : 0;

        Release();
        _stateBlock = device.CreateStateBlock(CaptureAllStates);
        _stateBlockDevice = _stateBlock == 0 ? 0 : devicePointer;
        return _stateBlock;
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
