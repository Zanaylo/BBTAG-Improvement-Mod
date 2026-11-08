using System.IO.Pipes;
using System.Numerics;
using System.Text;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Harness;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Overlay;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.ImprovementMod.Menu;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Harness;

internal sealed class HarnessServer : IHarnessTarget
{
    private const char LineEnd = '\n';
    private const int MostLetters = 4096;
    private const int LeftButton = 0;

    private static readonly TimeSpan KeysBaseWait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FrameWait = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan GrabWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan IdleCheck = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan QuitDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MenuInputGap = TimeSpan.FromMilliseconds(120);

    private readonly InjectedKeyQueue _keys = new();
    private readonly FrameGrab _grab = new();
    private readonly ILog _log;
    private GameView? _game;
    private MatchExit? _exit;

    private HarnessServer(ILog log) => _log = log;

    public int ProcessId => Environment.ProcessId;

    public static bool IsWanted => Environment.GetEnvironmentVariable(HarnessCommands.EnvironmentVariable) == HarnessCommands.EnabledValue;

    public static HarnessServer InstallEarly(LoadedModule game, FrameDispatcher frames, ILog log)
    {
        var server = new HarnessServer(log);
        QuietWindow.Install(game);
        InjectedKeyboard.Install(game, server._keys);
        frames.Add("Harness keys", _ => server._keys.OnFrame());
        new Thread(server.Serve) { IsBackground = true, Name = "Harness" }.Start();
        log.Info($"Harness on: the window stays behind the others, keys come from the pipe {HarnessCommands.PipeName} and nothing takes the focus.");
        return server;
    }

    public void GrabAfterTheMenu(FrameDispatcher frames) => frames.Add("Harness frame grab", _grab.OnPresent);

    public void Attach(GameView game, MatchExit exit)
    {
        _game = game;
        _exit = exit;
    }

    public bool PlayKeys(KeyScript script)
    {
        _keys.Enqueue(script.Steps);
        var deadline = DateTime.UtcNow + KeysBaseWait + FrameWait * script.Frames;

        while (!_keys.IsIdle)
        {
            if (DateTime.UtcNow > deadline)
            {
                _keys.Clear();
                return false;
            }

            Thread.Sleep(IdleCheck);
        }

        return true;
    }

    public string Grab(string path) => _grab.Capture(path, GrabWait);

    public string State() =>
        _game is { } game ? $"ok mode {(int)game.Mode} state {(int)game.State}" : "error the game objects were not found";

    public string ToggleMenu()
    {
        MenuInput.Toggle();
        return MenuInput.IsOpen ? "ok open" : "ok closed";
    }

    public string MenuKey(int key, bool down)
    {
        MenuInput.Receive(down ? new KeyPressed(key) : new KeyReleased(key));
        Thread.Sleep(MenuInputGap);
        return "ok";
    }

    public string MenuClick(int x, int y)
    {
        WindowInput[] click = [MenuInput.AtDisplayPoint(new Vector2(x, y)), new MouseButton(LeftButton, true), new MouseButton(LeftButton, false)];

        foreach (var input in click)
        {
            MenuInput.Receive(input);
            Thread.Sleep(MenuInputGap);
        }

        return "ok";
    }

    public string BackToCharacterSelect() =>
        _exit?.ToCharacterSelect() == true ? "ok" : "error no match is running";

    public string Quit()
    {
        Task.Run(async () =>
        {
            await Task.Delay(QuitDelay);
            Environment.Exit(0);
        });
        return "ok";
    }

    private void Serve()
    {
        while (true)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(HarnessCommands.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte);
                pipe.WaitForConnection();
                var line = ReadLine(pipe);
                SafeLog.Info(_log, $"Harness: {line}");
                var reply = HarnessCommands.Execute(line, this);
                SafeLog.Info(_log, $"Harness reply: {reply}");
                pipe.Write(Encoding.UTF8.GetBytes(reply + LineEnd));

                if (OperatingSystem.IsWindows())
                    pipe.WaitForPipeDrain();
            }
            catch (IOException exception)
            {
                SafeLog.Error(_log, $"Harness pipe: {exception.Message}");
            }
        }
    }

    private static string ReadLine(Stream pipe)
    {
        var line = new StringBuilder();

        while (line.Length < MostLetters)
        {
            var value = pipe.ReadByte();

            if (value < 0 || value == LineEnd)
                break;

            line.Append((char)value);
        }

        return line.ToString().TrimEnd('\r');
    }
}
