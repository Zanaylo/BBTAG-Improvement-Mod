using System.Buffers.Binary;
using System.Text;
using BBTAG_IM.Core.CheatEngine;

namespace BBTAG_IM.Core.Tests.CheatEngine;

public class BridgeConnectionTests
{
    private readonly ScriptedPipe _pipeMock = new();

    public BridgeConnection CreateBridgeConnection() => new(_pipeMock);

    [Fact]
    public void Enable_CheatEngineAnswersOk_SendsTheScriptId()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("ok");

        // Act
        connection.Enable(21573);

        // Assert
        Assert.Equal(["enable 21573"], _pipeMock.Sent);
    }

    [Fact]
    public void Disable_CheatEngineAnswersOk_SendsTheScriptId()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("ok");

        // Act
        connection.Disable(12);

        // Assert
        Assert.Equal(["disable 12"], _pipeMock.Sent);
    }

    [Fact]
    public void Enable_ScriptCheatEngineDoesNotKnow_ThrowsBridgeExceptionNamingTheId()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("missing");

        // Act
        Exception? exception = Record.Exception(() => connection.Enable(5));

        // Assert
        Assert.Equal("Cheat Engine has no script with id 5 in the loaded table.", Assert.IsType<BridgeException>(exception).Message);
    }

    [Theory]
    [InlineData(true, "show")]
    [InlineData(false, "hide")]
    public void ShowWindow_Visibility_SendsTheMatchingCommand(bool visible, string expected)
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("ok");

        // Act
        connection.ShowWindow(visible);

        // Assert
        Assert.Equal([expected], _pipeMock.Sent);
    }

    [Fact]
    public void ActiveScripts_CheatEngineListsIds_ReturnsThem()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("1,21573,8");

        // Act
        IReadOnlySet<int> active = connection.ActiveScripts();

        // Assert
        Assert.Equal(new HashSet<int> { 1, 8, 21573 }, active);
    }

    [Fact]
    public void ActiveScripts_NothingRuns_ReturnsEmpty()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("");

        // Act
        IReadOnlySet<int> active = connection.ActiveScripts();

        // Assert
        Assert.Empty(active);
    }

    [Fact]
    public void Status_CheatEngineCountsPendingRunningAndWaitingScripts_ReturnsTheCounts()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("2,5,1");

        // Act
        BridgeStatus status = connection.Status();

        // Assert
        Assert.Equal(new BridgeStatus(2, 5, 1), status);
        Assert.Equal(["status"], _pipeMock.Sent);
    }

    [Theory]
    [InlineData("ok")]
    [InlineData("1,2")]
    [InlineData("1,x,3")]
    public void Status_ReplyThatIsNotThreeCounts_ThrowsBridgeException(string reply)
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply(reply);

        // Act
        Exception? exception = Record.Exception(() => connection.Status());

        // Assert
        Assert.IsType<BridgeException>(exception);
    }

    [Fact]
    public void Failures_CheatEngineListsFailedScripts_ReturnsTheirIdsAndReasons()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("21539\tError in line 4: Wait condition failed.\n3430\tNot all code is injectable.");

        // Act
        IReadOnlyList<ScriptFailure> failures = connection.Failures();

        // Assert
        Assert.Equal(
            [new ScriptFailure(21539, "Error in line 4: Wait condition failed."), new ScriptFailure(3430, "Not all code is injectable.")],
            failures);
        Assert.Equal(["failures"], _pipeMock.Sent);
    }

    [Fact]
    public void Failures_NothingFailed_ReturnsEmpty()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("");

        // Act
        IReadOnlyList<ScriptFailure> failures = connection.Failures();

        // Assert
        Assert.Empty(failures);
    }

    [Theory]
    [InlineData("21539")]
    [InlineData("abc\treason")]
    public void Failures_LineWithoutAnIdAndAReason_ThrowsBridgeException(string reply)
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply(reply);

        // Act
        Exception? exception = Record.Exception(() => connection.Failures());

        // Assert
        Assert.IsType<BridgeException>(exception);
    }

    [Fact]
    public void CancelWaits_CheatEngineAnswersOk_SendsTheCancelCommand()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("ok");

        // Act
        connection.CancelWaits();

        // Assert
        Assert.Equal(["cancel"], _pipeMock.Sent);
    }

    [Fact]
    public void ActiveScripts_CheatEngineHangsUpBeforeAnswering_ThrowsBridgeException()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();

        // Act
        Exception? exception = Record.Exception(() => connection.ActiveScripts());

        // Assert
        Assert.IsType<BridgeException>(exception);
    }

    [Fact]
    public void Enable_PipeAlreadyClosed_ThrowsBridgeException()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();
        _pipeMock.Reply("ok");
        connection.Dispose();

        // Act
        Exception? exception = Record.Exception(() => connection.Enable(1));

        // Assert
        Assert.IsType<BridgeException>(exception);
    }

    [Fact]
    public void Close_CheatEngineNeverAnswers_SendsQuitWithoutWaiting()
    {
        // Arrange
        BridgeConnection connection = CreateBridgeConnection();

        // Act
        connection.Close();

        // Assert
        Assert.Equal(["quit"], _pipeMock.Sent);
    }

    public sealed class ScriptedPipe : Stream
    {
        private const int MaximumBytesPerRead = 3;

        private readonly MemoryStream _replies = new();
        private readonly MemoryStream _requests = new();
        private bool _closed;

        public IReadOnlyList<string> Sent => ReadMessages(new MemoryStream(_requests.ToArray()));

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public void Reply(string text)
        {
            long readPosition = _replies.Position;
            _replies.Seek(0, SeekOrigin.End);
            byte[] body = Encoding.ASCII.GetBytes(text);
            byte[] header = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)body.Length);
            _replies.Write(header);
            _replies.Write(body);
            _replies.Position = readPosition;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            return _replies.Read(buffer, offset, Math.Min(count, MaximumBytesPerRead));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            _requests.Write(buffer, offset, count);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            _closed = true;
            base.Dispose(disposing);
        }

        private static List<string> ReadMessages(MemoryStream stream)
        {
            List<string> messages = [];

            while (stream.Position < stream.Length)
            {
                byte[] header = new byte[sizeof(uint)];
                stream.ReadExactly(header);
                byte[] body = new byte[BinaryPrimitives.ReadUInt32LittleEndian(header)];
                stream.ReadExactly(body);
                messages.Add(Encoding.ASCII.GetString(body));
            }

            return messages;
        }
    }
}
