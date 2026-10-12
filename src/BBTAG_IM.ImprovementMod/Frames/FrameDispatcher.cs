using BBTAG_IM.Core.Logging;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Frames;

internal sealed class FrameDispatcher(ProcessMemory memory, ILog log)
{
    private readonly List<FrameHandler> _handlers = [];
    private readonly List<Action> _resetHandlers = [];

    private bool _firstFrameSeen;

    public void Add(string name, Action<nint> handler) => _handlers.Add(new FrameHandler(name, handler));

    public void AddReset(Action handler) => _resetHandlers.Add(handler);

    public void OnReset()
    {
        foreach (var handler in _resetHandlers)
        {
            try
            {
                handler();
            }
            catch (Exception exception)
            {
                SafeLog.Error(log, $"Releasing graphics before the game resets the screen failed: {exception}");
            }
        }
    }

    public void OnPresent(nint device)
    {
        memory.ForgetRegions();

        if (!_firstFrameSeen)
        {
            _firstFrameSeen = true;
            SafeLog.Info(log, "The game drew its first frame through the Improvement Mod.");
        }

        foreach (var handler in _handlers.ToArray())
            Run(handler, device);
    }

    private void Run(FrameHandler handler, nint device)
    {
        try
        {
            handler.OnPresent(device);
        }
        catch (Exception exception)
        {
            _handlers.Remove(handler);
            SafeLog.Error(log, $"{handler.Name} stopped after an error and stays off until the game restarts: {exception}");
        }
    }

    private sealed record FrameHandler(string Name, Action<nint> OnPresent);
}
