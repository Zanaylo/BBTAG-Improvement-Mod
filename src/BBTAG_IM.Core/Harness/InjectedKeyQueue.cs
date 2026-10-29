namespace BBTAG_IM.Core.Harness;

public sealed class InjectedKeyQueue
{
    private const int KeyCount = 256;
    private const byte Pressed = 0x80;

    private readonly Lock _lock = new();
    private readonly Queue<KeyStep> _steps = new();
    private readonly bool[] _down = new bool[KeyCount];
    private KeyStep? _current;
    private bool _releasing;
    private int _framesLeft;

    public bool IsIdle
    {
        get
        {
            lock (_lock)
                return _current is null && _steps.Count == 0;
        }
    }

    public void Enqueue(IEnumerable<KeyStep> steps)
    {
        lock (_lock)
        {
            foreach (var step in steps)
                _steps.Enqueue(step);
        }
    }

    public void OnFrame()
    {
        lock (_lock)
        {
            if (_current is null)
            {
                StartNext();
                return;
            }

            if (--_framesLeft > 0)
                return;

            if (_releasing)
            {
                StartNext();
                return;
            }

            SetKeys(_current, false);
            _releasing = true;
            _framesLeft = Math.Max(_current.GapFrames, 1);
        }
    }

    public bool IsDown(int key)
    {
        lock (_lock)
            return key is >= 0 and < KeyCount && _down[key];
    }

    public void Merge(Span<byte> keyboardState)
    {
        lock (_lock)
        {
            for (var key = 0; key < Math.Min(KeyCount, keyboardState.Length); key++)
            {
                if (_down[key])
                    keyboardState[key] |= Pressed;
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _steps.Clear();
            _current = null;
            Array.Clear(_down);
        }
    }

    private void StartNext()
    {
        if (!_steps.TryDequeue(out var next))
        {
            _current = null;
            return;
        }

        _current = next;
        _releasing = false;
        _framesLeft = Math.Max(next.HoldFrames, 1);
        SetKeys(next, true);
    }

    private void SetKeys(KeyStep step, bool down)
    {
        foreach (var key in step.Keys.Where(key => key is > 0 and < KeyCount))
            _down[key] = down;
    }
}
