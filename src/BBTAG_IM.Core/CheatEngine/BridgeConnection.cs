using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace BBTAG_IM.Core.CheatEngine;

public sealed class BridgeConnection(Stream pipe) : IDisposable
{
    private const int HeaderSize = sizeof(uint);
    private const int MaximumReplySize = 1 << 20;
    private const int StatusCountCount = 3;
    private const int FailureFieldCount = 2;

    private readonly Lock _lock = new();

    public void Enable(int scriptId) => Toggle(BridgeScript.EnableCommand, scriptId);

    public void Disable(int scriptId) => Toggle(BridgeScript.DisableCommand, scriptId);

    public void ShowWindow(bool visible) => Request(visible ? BridgeScript.ShowCommand : BridgeScript.HideCommand);

    public IReadOnlySet<int> ActiveScripts() =>
        Request(BridgeScript.StateCommand)
            .Split(BridgeScript.IdSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(id => int.Parse(id, CultureInfo.InvariantCulture))
            .ToHashSet();

    public BridgeStatus Status()
    {
        var reply = Request(BridgeScript.StatusCommand);
        var counts = reply.Split(BridgeScript.IdSeparator);

        if (counts.Length != StatusCountCount || !counts.All(IsCount))
            throw new BridgeException($"Cheat Engine answered \"{reply}\" when asked how many scripts are still turning on.");

        return new BridgeStatus(ParseCount(counts[0]), ParseCount(counts[1]), ParseCount(counts[2]));
    }

    public IReadOnlyList<ScriptFailure> Failures() =>
        Request(BridgeScript.FailuresCommand)
            .Split(BridgeScript.FailureSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseFailure)
            .ToArray();

    public void CancelWaits() => Request(BridgeScript.CancelCommand);

    public void Close()
    {
        lock (_lock)
            Send(BridgeScript.QuitCommand);
    }

    public void Dispose() => pipe.Dispose();

    private void Toggle(string command, int scriptId)
    {
        var reply = Request(string.Create(CultureInfo.InvariantCulture, $"{command} {scriptId}"));

        if (reply == BridgeScript.MissingReply)
            throw new BridgeException($"Cheat Engine has no script with id {scriptId} in the loaded table.");
    }

    private static bool IsCount(string text) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out _);

    private static int ParseCount(string text) => int.Parse(text, NumberStyles.None, CultureInfo.InvariantCulture);

    private static ScriptFailure ParseFailure(string line)
    {
        var fields = line.Split(BridgeScript.ReasonSeparator, FailureFieldCount);

        if (fields.Length != FailureFieldCount || !IsCount(fields[0]))
            throw new BridgeException($"Cheat Engine answered \"{line}\" when asked which scripts failed.");

        return new ScriptFailure(ParseCount(fields[0]), fields[1]);
    }

    private string Request(string command)
    {
        lock (_lock)
        {
            Send(command);
            return Receive();
        }
    }

    private void Send(string command)
    {
        var body = Encoding.ASCII.GetBytes(command);
        var message = new byte[HeaderSize + body.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(message, (uint)body.Length);
        body.CopyTo(message, HeaderSize);

        try
        {
            pipe.Write(message);
            pipe.Flush();
        }
        catch (Exception exception) when (IsClosedPipe(exception))
        {
            throw new BridgeException("Cheat Engine closed the connection.", exception);
        }
    }

    private string Receive()
    {
        var header = ReadExactly(HeaderSize);
        var size = BinaryPrimitives.ReadUInt32LittleEndian(header);

        if (size > MaximumReplySize)
            throw new BridgeException($"Cheat Engine sent a reply of {size} bytes, which is not a bridge message.");

        return Encoding.ASCII.GetString(ReadExactly((int)size));
    }

    private byte[] ReadExactly(int size)
    {
        var buffer = new byte[size];

        try
        {
            pipe.ReadExactly(buffer);
            return buffer;
        }
        catch (Exception exception) when (IsClosedPipe(exception))
        {
            throw new BridgeException("Cheat Engine closed the connection.", exception);
        }
    }

    private static bool IsClosedPipe(Exception exception) =>
        exception is IOException or ObjectDisposedException or InvalidOperationException;
}
