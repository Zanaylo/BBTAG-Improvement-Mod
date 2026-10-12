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
        ReportFirstFrame();

        foreach (var handler in _handlers)
        {
            if (handler.IsRunning)
                Run(handler, device);
        }
    }

    private void ReportFirstFrame()
    {
        if (_firstFrameSeen)
            return;

        _firstFrameSeen = true;
        SafeLog.Info(log, "The game drew its first frame through the Improvement Mod.");
    }

    private void Run(FrameHandler handler, nint device)
    {
        try
        {
            handler.OnPresent(device);
        }
        catch (Exception exception)
        {
            handler.IsRunning = false;
            SafeLog.Error(log, $"{handler.Name} stopped after an error and stays off until the game restarts: {exception}");
        }
    }

    private sealed class FrameHandler(string name, Action<nint> onPresent)
    {
        public string Name => name;

        public Action<nint> OnPresent => onPresent;

        public bool IsRunning { get; set; } = true;
    }
}
