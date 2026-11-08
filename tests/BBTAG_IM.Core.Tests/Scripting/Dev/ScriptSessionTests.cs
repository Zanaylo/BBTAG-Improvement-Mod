using BBTAG_IM.Core.IO;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Core.Scripting.Live;
using BBTAG_IM.Core.Tests.Support;
using NSubstitute;

namespace BBTAG_IM.Core.Tests.Scripting.Dev;

public class ScriptSessionTests
{
    private const string Source = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.py";
    private const string Binary = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.bin";
    private const string CSharpSource = @"D:\BBTAG\mods\DEV\data\char\char_brg_scr\scr_brg.cs";

    private static readonly DateTime FirstSave = new(2026, 10, 13, 22, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SecondSave = FirstSave.AddSeconds(5);

    private readonly IFileSystem _fileSystemMock = Substitute.For<IFileSystem>();
    private readonly IScriptBuffers _scriptBuffersMock = Substitute.For<IScriptBuffers>();
    private readonly IGameHeap _gameHeapMock = Substitute.For<IGameHeap>();
    private readonly IMoveRefresh _moveRefreshMock = Substitute.For<IMoveRefresh>();
    private readonly ScriptMemory _scriptMemory = new();
    private byte[] _loaded = new ScriptBytes().State("A").Command(ScriptTestDatabase.Damage, 12).Command(1).Build();

    [Fact]
    public void Open_NoSourceYet_WritesTheScriptTheGameRuns()
    {
        // Arrange
        _fileSystemMock.FileExists(Source).Returns(false);
        ScriptSession session = CreateScriptSession();

        // Act
        session.Open();

        // Assert
        _fileSystemMock.Received(1).WriteAllText(Source, Arg.Is<string>(text => text.Contains("Damage(12)")));
    }

    [Fact]
    public void Poll_SourceSaved_AppliesItAndWritesTheBinary()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.Applied, session.Status.State);
        Assert.Equal(15, _scriptMemory.Memory.Int32From(ScriptMemory.File + 4 + 36 + 36 + 4));
        _fileSystemMock.Received(1).WriteAllBytes(Binary, Arg.Any<byte[]>());
    }

    [Fact]
    public void Poll_MovesRegisteredAgain_SaysSo()
    {
        // Arrange
        _moveRefreshMock.Refresh(default, default!, default!).ReturnsForAnyArgs(MoveRefreshResult.Refreshed);
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");

        // Act
        session.Poll();

        // Assert
        Assert.Contains("Moves registered again", session.Status.Message);
    }

    [Fact]
    public void Poll_MovesNotRegisteredAgain_SaysTheyWaitForTheNextMatch()
    {
        // Arrange
        _moveRefreshMock.Refresh(default, default!, default!).ReturnsForAnyArgs(MoveRefreshResult.NotRefreshed);
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");

        // Act
        session.Poll();

        // Assert
        Assert.Contains("Move_Register changes work after the next match loads", session.Status.Message);
    }

    [Fact]
    public void Poll_SourceWithAMistake_MarksTheLineAndLeavesTheGameScript()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damag(15)\n");

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.CompileFailed, session.Status.State);
        Assert.Equal(12, _scriptMemory.Memory.Int32From(ScriptMemory.File + 4 + 36 + 36 + 4));
        _fileSystemMock.Received(1).WriteAllText(Source, "@State\ndef A():\n    # !!! Damag is not a known command.\n    Damag(15)\n");
    }

    [Fact]
    public void Poll_SourceWithTwoMistakes_MarksBothLines()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damag(15)\n    Damage(1, 2)\n");

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.CompileFailed, session.Status.State);
        Assert.StartsWith("Line 3: Damag is not a known command. (and 1 more mistake)", session.Status.Message);
        _fileSystemMock.Received(1).WriteAllText(
            Source,
            "@State\ndef A():\n    # !!! Damag is not a known command.\n    Damag(15)\n    # !!! Damage takes 1 values but was given 2.\n    Damage(1, 2)\n");
    }

    [Fact]
    public void Rollback_AfterAnEdit_PutsThePreviousScriptBackAndMarksTheLine()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");
        session.Poll();
        uint damage = ScriptMemory.File + 4 + 36 + 36;

        // Act
        bool rolledBack = session.Rollback(damage, "Script Function Call Error");

        // Assert
        Assert.True(rolledBack);
        Assert.Equal(12, _scriptMemory.Memory.Int32From(damage + 4));
        _fileSystemMock.Received(1).WriteAllText(Source, "@State\ndef A():\n    # !!! The game stopped here: Script Function Call Error\n    Damage(15)\n");
    }

    [Fact]
    public void Rollback_AfterAnEdit_WaitsForAFix()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");
        session.Poll();

        // Act
        session.Rollback(null, "Broke.");

        // Assert
        Assert.True(session.WaitsForFix);
    }

    [Fact]
    public void Poll_FixSavedAfterARollback_StopsWaiting()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");
        session.Poll();
        session.Rollback(null, "Broke.");
        Saved(SecondSave.AddSeconds(5), "@State\ndef A():\n    Damage(20)\n");

        // Act
        session.Poll();

        // Assert
        Assert.False(session.WaitsForFix);
        Assert.Equal(ScriptSessionState.Applied, session.Status.State);
    }

    [Fact]
    public void Poll_SavedBackToTheWorkingVersionAfterARollback_StopsWaiting()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");
        session.Poll();
        session.Rollback(null, "Broke.");
        Saved(SecondSave.AddSeconds(5), "@State\ndef A():\n    Damage(12)\n");

        // Act
        session.Poll();

        // Assert
        Assert.False(session.WaitsForFix);
        Assert.Equal(ScriptSessionState.Watching, session.Status.State);
    }

    [Fact]
    public void Poll_MistakeSavedAfterARollback_KeepsWaiting()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");
        session.Poll();
        session.Rollback(null, "Broke.");
        Saved(SecondSave.AddSeconds(5), "@State\ndef A():\n    Damag(20)\n");

        // Act
        session.Poll();

        // Assert
        Assert.True(session.WaitsForFix);
    }

    [Fact]
    public void Poll_FirstPollOfAKeptSourceThatDiffersFromTheGame_AppliesIt()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        _fileSystemMock.ReadAllText(Source).Returns("@State\ndef A():\n    Damage(15)\n");

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.Applied, session.Status.State);
        Assert.Equal(15, _scriptMemory.Memory.Int32From(ScriptMemory.File + 4 + 36 + 36 + 4));
    }

    [Fact]
    public void Poll_FirstPollOfAKeptSourceThatMatchesTheGame_LeavesTheGameScript()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        _fileSystemMock.ReadAllText(Source).Returns("@State\ndef A():\n    Damage(12)\n");

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.Watching, session.Status.State);
        _fileSystemMock.DidNotReceive().WriteAllBytes(Binary, Arg.Any<byte[]>());
    }

    [Fact]
    public void Poll_SecondPollWithoutASave_DoesNotReadTheSourceAgain()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        _fileSystemMock.ReadAllText(Source).Returns("@State\ndef A():\n    Damage(12)\n");
        session.Poll();
        _fileSystemMock.ClearReceivedCalls();

        // Act
        session.Poll();

        // Assert
        _fileSystemMock.DidNotReceive().ReadAllText(Source);
    }

    [Fact]
    public void Poll_NewSourceJustWritten_DoesNotReadItBack()
    {
        // Arrange
        _fileSystemMock.FileExists(Source).Returns(false);
        _fileSystemMock.LastWriteTimeUtc(Source).Returns(FirstSave);
        ScriptSession session = CreateScriptSession();
        session.Open();

        // Act
        session.Poll();

        // Assert
        _fileSystemMock.DidNotReceive().ReadAllText(Source);
    }

    [Fact]
    public void Rollback_ObjectStoppedBeforeTheEdit_MarksTheEditedLine()
    {
        // Arrange
        ScriptSession session = OpenedSession(new ScriptBytes().State("A").Command(2, "rg000_00", 3).Command(ScriptTestDatabase.Damage, 12).Command(1).Build());
        Saved(SecondSave, "@State\ndef A():\n    sprite('rg000_00', 3)\n    Damage(15)\n");
        session.Poll();
        uint sprite = ScriptMemory.File + 4 + 36 + 36;

        // Act
        session.Rollback(sprite, "Broke.");

        // Assert
        _fileSystemMock.Received(1).WriteAllText(Source, "@State\ndef A():\n    sprite('rg000_00', 3)\n    # !!! The game stopped here: Broke.\n    Damage(15)\n");
    }

    [Fact]
    public void Rollback_NothingEditedSinceOpening_ReturnsFalse()
    {
        // Arrange
        ScriptSession session = OpenedSession();

        // Act
        bool rolledBack = session.Rollback(null, "Script Function Call Error");

        // Assert
        Assert.False(rolledBack);
    }

    [Fact]
    public void Rollback_SecondTimeInARow_ReturnsFalse()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");
        session.Poll();
        session.Rollback(null, "Script Function Call Error");

        // Act
        bool rolledBack = session.Rollback(null, "Script Function Call Error");

        // Assert
        Assert.False(rolledBack);
    }

    [Fact]
    public void Hold_NothingToRollBackTo_WaitsForAFixAndMarksTheLine()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        Saved(SecondSave, "@State\ndef A():\n    Damage(15)\n");
        session.Poll();
        session.Rollback(null, "Broke.");
        _fileSystemMock.ReadAllText(Source).Returns("@State\ndef A():\n    Damage(15)\n");
        _fileSystemMock.ClearReceivedCalls();
        uint damage = ScriptMemory.File + 4 + 36 + 36;

        // Act
        bool held = session.Hold(damage, "Broke again.");

        // Assert
        Assert.True(held);
        Assert.True(session.WaitsForFix);
        Assert.Equal(ScriptSessionState.RolledBack, session.Status.State);
        _fileSystemMock.Received(1).WriteAllText(Source, Arg.Is<string>(text => text.Contains("# !!! The game stopped here: Broke again.")));
    }

    [Fact]
    public void Hold_InstructionOutsideTheScript_MarksTheFirstLine()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        _fileSystemMock.ReadAllText(Source).Returns("@State\ndef A():\n    Damage(12)\n");

        // Act
        session.Hold(null, "Broke.");

        // Assert
        _fileSystemMock.Received(1).WriteAllText(Source, "# !!! The game stopped here: Broke.\n@State\ndef A():\n    Damage(12)\n");
    }

    [Fact]
    public void Hold_GameLoadedTheScriptAgain_ReturnsFalse()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        _scriptMemory.Memory.UInt32At(ScriptMemory.Record, 0x07000000);
        session.Poll();

        // Act
        bool held = session.Hold(null, "Broke.");

        // Assert
        Assert.False(held);
        Assert.False(session.WaitsForFix);
    }

    [Fact]
    public void Poll_GameLoadedTheScriptAgain_ReportsThatTheSessionEnded()
    {
        // Arrange
        ScriptSession session = OpenedSession();
        _scriptMemory.Memory.UInt32At(ScriptMemory.Record, 0x07000000);

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.Reloaded, session.Status.State);
    }

    [Fact]
    public void Open_CSharpSourceMissing_WritesTheScriptAsACSharpClass()
    {
        // Arrange
        _fileSystemMock.FileExists(CSharpSource).Returns(false);
        ScriptSession session = CreateScriptSession(CSharpSource);

        // Act
        session.Open();

        // Assert
        _fileSystemMock.Received(1).WriteAllText(CSharpSource, Arg.Is<string>(text => text.Contains("public partial class scr_brg : Script") && text.Contains("        Damage(12);")));
    }

    [Fact]
    public void Poll_CSharpSourceWithAMistake_MarksTheLineWithACSharpComment()
    {
        // Arrange
        _fileSystemMock.FileExists(CSharpSource).Returns(true);
        _fileSystemMock.LastWriteTimeUtc(CSharpSource).Returns(FirstSave);
        ScriptSession session = CreateScriptSession(CSharpSource);
        session.Open();
        _fileSystemMock.LastWriteTimeUtc(CSharpSource).Returns(SecondSave);
        _fileSystemMock.ReadAllText(CSharpSource).Returns("class scr_brg : Script\n{\n    [State]\n    public void A()\n    {\n        Damag(15);\n    }\n}\n");

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.CompileFailed, session.Status.State);
        _fileSystemMock.Received(1).WriteAllText(CSharpSource, "class scr_brg : Script\n{\n    [State]\n    public void A()\n    {\n        // !!! Damag is not a known command.\n        Damag(15);\n    }\n}\n");
    }

    [Fact]
    public void Poll_ValidCSharpSource_AppliesIt()
    {
        // Arrange
        _fileSystemMock.FileExists(CSharpSource).Returns(true);
        _fileSystemMock.LastWriteTimeUtc(CSharpSource).Returns(FirstSave);
        ScriptSession session = CreateScriptSession(CSharpSource);
        session.Open();
        _fileSystemMock.LastWriteTimeUtc(CSharpSource).Returns(SecondSave);
        _fileSystemMock.ReadAllText(CSharpSource).Returns("class scr_brg : Script\n{\n    [State]\n    public void A()\n    {\n        Damage(15);\n    }\n}\n");

        // Act
        session.Poll();

        // Assert
        Assert.Equal(ScriptSessionState.Applied, session.Status.State);
        Assert.Equal(15, _scriptMemory.Memory.Int32From(ScriptMemory.File + 4 + 36 + 36 + 4));
    }

    [Fact]
    public void Open_OtherLanguageSavedLater_WritesTheScriptTheGameRunsAgain()
    {
        // Arrange
        _fileSystemMock.FileExists(Source).Returns(true);
        _fileSystemMock.FileExists(CSharpSource).Returns(true);
        _fileSystemMock.LastWriteTimeUtc(Source).Returns(FirstSave);
        _fileSystemMock.LastWriteTimeUtc(CSharpSource).Returns(SecondSave);
        ScriptSession session = CreateScriptSession();

        // Act
        session.Open();

        // Assert
        _fileSystemMock.Received(1).WriteAllText(Source, Arg.Is<string>(text => text.Contains("Damage(12)")));
    }

    [Fact]
    public void Open_OtherLanguageSavedEarlier_KeepsTheSource()
    {
        // Arrange
        _fileSystemMock.FileExists(Source).Returns(true);
        _fileSystemMock.FileExists(CSharpSource).Returns(true);
        _fileSystemMock.LastWriteTimeUtc(Source).Returns(SecondSave);
        _fileSystemMock.LastWriteTimeUtc(CSharpSource).Returns(FirstSave);
        ScriptSession session = CreateScriptSession();

        // Act
        session.Open();

        // Assert
        _fileSystemMock.DidNotReceive().WriteAllText(Source, Arg.Any<string>());
    }

    [Fact]
    public void Open_NoOtherLanguageFile_KeepsTheSource()
    {
        // Arrange
        _fileSystemMock.FileExists(Source).Returns(true);
        _fileSystemMock.FileExists(CSharpSource).Returns(false);
        _fileSystemMock.LastWriteTimeUtc(Source).Returns(FirstSave);
        ScriptSession session = CreateScriptSession();

        // Act
        session.Open();

        // Assert
        _fileSystemMock.DidNotReceive().WriteAllText(Source, Arg.Any<string>());
    }

    public ScriptSession CreateScriptSession(string source = Source)
    {
        _scriptMemory.Load(_loaded);
        ScriptDatabase database = ScriptTestDatabase.Create();
        LiveScript live = new(_scriptMemory.Memory, _scriptBuffersMock, _gameHeapMock, ScriptMemory.Record, ScriptBinary.Read(_loaded, database), database, _moveRefreshMock);
        return new ScriptSession(_fileSystemMock, new ScriptPaths(source, Binary), live, database);
    }

    private ScriptSession OpenedSession(byte[]? loaded = null)
    {
        _loaded = loaded ?? _loaded;
        _fileSystemMock.FileExists(Source).Returns(true);
        _fileSystemMock.LastWriteTimeUtc(Source).Returns(FirstSave);
        ScriptSession session = CreateScriptSession();
        session.Open();
        return session;
    }

    private void Saved(DateTime time, string text)
    {
        _fileSystemMock.LastWriteTimeUtc(Source).Returns(time);
        _fileSystemMock.ReadAllText(Source).Returns(text);
    }
}
