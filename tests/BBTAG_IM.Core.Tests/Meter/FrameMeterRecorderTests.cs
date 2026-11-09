using BBTAG_IM.Core.Meter;

namespace BBTAG_IM.Core.Tests.Meter;

public class FrameMeterRecorderTests
{
    private const string Stand = "CmnActStand";
    private const string Wait = "CmnActTagBattleWait";
    private const string Attack = "NmlAtk5A";
    private const string Hit = "CmnActHitStandLv4";
    private const string Guard = "CmnActHighGuardLoop";
    private const int FullHealth = 17000;

    private int _frame;
    private readonly int[] _stateTime = new int[4];
    private readonly List<MeterEvent> _events = [];

    public FrameMeterRecorder CreateFrameMeterRecorder()
    {
        var recorder = new FrameMeterRecorder(CharacterTrack.DefaultCapacity);
        recorder.Happened += _events.Add;
        return recorder;
    }

    [Fact]
    public void Record_SameGameFrameTwice_AddsOneColumn()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        MatchFrame frame = Next(Free(), Acting(Attack));

        // Act
        recorder.Record(frame);
        recorder.Record(frame);

        // Assert
        Assert.Equal(1, recorder.Columns);
    }

    [Fact]
    public void Record_HitstopWhereNothingMoves_SkipsTheFrameAndCountsTheFreeze()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        recorder.Record(Next(Acting(Attack), Frame(Hit, hitstun: 17)));

        // Act
        recorder.Record(Next(Frozen(Acting(Attack)), Frozen(Frame(Hit, hitstun: 17))));

        // Assert
        Assert.Equal((1, 1), (recorder.Columns, recorder.Freeze));
    }

    [Fact]
    public void Record_DefenderChangesStateDuringHitstop_StillSkipsTheFrame()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        recorder.Record(Next(Acting(Attack) with { Hitboxes = 2, Hitstop = 10 }, Free()));

        // Act
        recorder.Record(Next(Frozen(Acting(Attack) with { Hitboxes = 2 }), Frame(Hit, hitstun: 17, hitstop: 10)));

        // Assert
        Assert.Equal((1, 1), (recorder.Columns, recorder.Freeze));
    }

    [Fact]
    public void Combo_HealthDropsTheFrameBeforeTheHitIsCounted_StillCountsTheDamage()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        Play(recorder, 1, Acting(Attack), Free());
        Play(recorder, 1, Acting(Attack), Frame(Hit, hitstun: 17, health: 15800));
        Play(recorder, 1, Acting(Attack), Frame(Hit, hitstun: 17, hitstop: 10, health: 15800));

        // Assert
        Assert.Equal(new ComboReadout(1, FullHealth - 15800, 0, 0), recorder.Combo(Side.Player2));
    }

    [Fact]
    public void Record_ActivityAfterAQuietStretch_StartsAFreshMeter()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        recorder.Record(Next(Acting(Attack), Free()));

        for (var quiet = 0; quiet < FrameMeterRecorder.QuietFramesBeforeRestart + 5; quiet++)
            recorder.Record(Next(Free(), Free()));

        // Act
        recorder.Record(Next(Acting(Attack), Free()));

        // Assert
        Assert.Equal(1, recorder.Columns);
        Assert.Equal(FrameKind.Startup, recorder.Lane(Side.Player1, Role.Point)[0].Kind);
    }

    [Fact]
    public void Record_LongQuietStretch_StopsAddingColumnsAndCountsIt()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        recorder.Record(Next(Acting(Attack), Free()));

        // Act
        for (var quiet = 0; quiet < 40; quiet++)
            recorder.Record(Next(Free(), Free()));

        // Assert
        Assert.Equal(1 + FrameMeterRecorder.QuietFramesBeforeRestart, recorder.Columns);
        Assert.Equal(40, recorder.QuietRun);
    }

    [Fact]
    public void Record_PartnerFlyingOffScreen_CountsAsQuiet()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        recorder.Record(Next(Free(), Free(), Acting("CmnActChangePartnerAssistAtk_A")));

        // Act
        for (var leaving = 0; leaving < 40; leaving++)
            recorder.Record(Next(Free(), Free(), Acting("CmnActChangeReturnAppeal")));

        // Assert
        Assert.Equal(1 + FrameMeterRecorder.QuietFramesBeforeRestart, recorder.Columns);
    }

    [Fact]
    public void Lane_AssistOnScreen_ShowsThePointAndThePartnerSeparately()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        recorder.Record(Next(Free(), Free(), Acting("CmnActChangePartnerAssistAtk_A")));

        // Assert
        Assert.Equal(FrameKind.Idle, recorder.Lane(Side.Player1, Role.Point)[0].Kind);
        Assert.Equal(FrameKind.Startup, recorder.Lane(Side.Player1, Role.Partner)[0].Kind);
        Assert.Equal(FrameKind.Empty, recorder.Lane(Side.Player2, Role.Partner)[0].Kind);
    }

    [Fact]
    public void Lane_PartnerTakesOverAsPoint_FollowsTheNewPointCharacter()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        recorder.Record(Next(Acting(Attack), Free()));

        // Act
        recorder.Record(Next(Frame(Wait), Free(), Acting("NmlAtk5B")));

        // Assert
        Assert.Equal(FrameKind.Startup, recorder.Lane(Side.Player1, Role.Point)[1].Kind);
        Assert.Equal(FrameKind.Empty, recorder.Lane(Side.Player1, Role.Partner)[1].Kind);
        Assert.Equal(1, recorder.PointOf(Side.Player1));
    }

    [Fact]
    public void Advantage_AttackerRecoversFirst_IsPositiveForTheAttacker()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        Play(recorder, 6, Acting(Attack), Free());
        Play(recorder, 3, Acting(Attack) with { Hitboxes = 2 }, Frame(Guard, blockstun: 16));
        Play(recorder, 10, Acting(Attack), Frame(Guard, blockstun: 9));
        Play(recorder, 2, Free(), Frame(Guard, blockstun: 1));
        Play(recorder, 1, Free(), Free());

        // Assert
        Assert.Equal(2, recorder.Advantage(Side.Player1));
        Assert.Equal(-2, recorder.Advantage(Side.Player2));
    }

    [Fact]
    public void Advantage_AttackerFreeTheWholeTime_CountsFromWhenTheExchangeStarted()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        Play(recorder, 30, Free(), Free());

        // Act
        Play(recorder, 10, Free(), Frame(Hit, hitstun: 5));
        Play(recorder, 1, Free(), Free());

        // Assert
        Assert.Equal(10, recorder.Advantage(Side.Player1));
    }

    [Fact]
    public void Combo_HitLandsWhileBothAreInHitstop_StillCountsTheHitAndDamage()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        Play(recorder, 1, Acting(Attack), Free());
        Play(recorder, 1, Acting(Attack) with { Hitstop = 10 }, Free() with { Health = 15800 });

        // Act
        Play(recorder, 3, Acting(Attack) with { Hitstop = 9 }, Frame(Hit, hitstun: 17, hitstop: 10, health: 15800));
        Play(recorder, 1, Acting(Attack), Frame(Hit, hitstun: 16, health: 15800));

        // Assert
        Assert.Equal(new ComboReadout(1, FullHealth - 15800, 0, 0), recorder.Combo(Side.Player2));
        Assert.Contains(new MeterEvent(MeterEventKind.HitLanded, Side.Player1, 1), _events);
    }

    [Fact]
    public void Combo_WaitingPartnerHealthJumps_IsNotDamage()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        Play(recorder, 1, Acting(Attack), Free());

        // Act
        recorder.Record(Next(Acting(Attack), Frame(Hit, hitstun: 17, hitstop: 10, health: 16000), null, Frame(Wait, health: 170)));
        recorder.Record(Next(Acting(Attack), Frame(Hit, hitstun: 16, health: 16000), null, Frame(Wait, health: 17000)));
        recorder.Record(Next(Acting(Attack), Frame(Hit, hitstun: 15, health: 16000), null, Frame(Wait, health: 170)));

        // Assert
        Assert.Equal(new ComboReadout(1, FullHealth - 16000, 0, 0), recorder.Combo(Side.Player2));
    }

    [Fact]
    public void Advantage_NoHitOrBlock_StaysUnknown()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        Play(recorder, 8, Acting(Attack), Free());
        Play(recorder, 1, Free(), Free());

        // Assert
        Assert.Null(recorder.Advantage(Side.Player1));
    }

    [Fact]
    public void Gap_DefenderFreeBetweenTwoBlockedAttacks_IsTheFreeFrames()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        Play(recorder, 2, Acting(Attack), Frame(Guard, blockstun: 5));
        Play(recorder, 3, Acting("NmlAtk5B"), Free());
        Play(recorder, 1, Acting("NmlAtk5B"), Frame(Guard, blockstun: 5));

        // Assert
        Assert.Equal(3, recorder.Gap(Side.Player2));
    }

    [Fact]
    public void Combo_TwoHitsLand_CountsHitsAndDamageForTheDefender()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        Play(recorder, 1, Acting(Attack), Free());
        Play(recorder, 1, Acting(Attack), Frame(Hit, hitstun: 17, hitstop: 10, health: 16200));
        Play(recorder, 2, Acting(Attack), Frame(Hit, hitstun: 15, health: 16200));
        Play(recorder, 1, Acting(Attack), Frame(Hit, hitstun: 17, hitstop: 10, health: 15000));

        // Assert
        Assert.Equal(new ComboReadout(2, FullHealth - 15000, 0, 0), recorder.Combo(Side.Player2));
    }

    [Fact]
    public void Combo_BlockedHitsWithChip_CountsBlocksAndChip()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        Play(recorder, 1, Acting(Attack), Free());
        Play(recorder, 1, Acting(Attack), Frame(Guard, blockstun: 16, hitstop: 10, health: 16900));

        // Assert
        Assert.Equal(new ComboReadout(0, 0, 1, 100), recorder.Combo(Side.Player2));
    }

    [Fact]
    public void Record_MoveStartsAndHitLands_RaisesEventsForTheAttacker()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();

        // Act
        Play(recorder, 1, Free(), Free());
        Play(recorder, 1, Acting(Attack), Free());
        Play(recorder, 1, Acting(Attack), Frame(Hit, hitstun: 17, hitstop: 10, health: 16200));
        Play(recorder, 1, Acting(Attack), Frame(Hit, hitstun: 3, health: 16200));
        Play(recorder, 1, Acting(Attack), Frame(Guard, blockstun: 16, hitstop: 10));

        // Assert
        Assert.Equal(
            [new MeterEvent(MeterEventKind.MoveStarted, Side.Player1, 0), new MeterEvent(MeterEventKind.HitLanded, Side.Player1, 1), new MeterEvent(MeterEventKind.HitBlocked, Side.Player1, 1)],
            _events);
    }

    [Fact]
    public void View_PartnerOnPoint_NamesFollowTheRoles()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        recorder.Record(Next(Frame(Wait), Free(), Acting(Attack)));

        // Act
        MeterView view = recorder.View(["YUMI", "RAGNA", "RACHEL", "JIN"]);

        // Assert
        Assert.Equal(("RACHEL", "YUMI"), (view.Player1.PointName, view.Player1.PartnerName));
        Assert.Equal(("RAGNA", "JIN"), (view.Player2.PointName, view.Player2.PartnerName));
        Assert.Equal(FrameKind.Startup, view.Player1.Point[0].Kind);
        Assert.Equal(new MoveTiming(Attack, 0, 0, 0, 1), view.Player1.PointMove);
    }

    [Fact]
    public void Reset_AfterRecording_EmptiesEverything()
    {
        // Arrange
        FrameMeterRecorder recorder = CreateFrameMeterRecorder();
        Play(recorder, 3, Acting(Attack), Free());

        // Act
        recorder.Reset();

        // Assert
        Assert.Equal((0, 0, 0), (recorder.Columns, recorder.Freeze, recorder.QuietRun));
        Assert.Null(recorder.Advantage(Side.Player1));
    }

    private void Play(FrameMeterRecorder recorder, int frames, CharacterFrame player1, CharacterFrame player2)
    {
        for (var frame = 0; frame < frames; frame++)
            recorder.Record(Next(player1, player2));
    }

    private MatchFrame Next(CharacterFrame player1, CharacterFrame player2, CharacterFrame? player1Partner = null, CharacterFrame? player2Partner = null)
    {
        _frame++;
        CharacterFrame?[] characters = [player1, player2, player1Partner ?? Frame(Wait), player2Partner ?? Frame(Wait)];

        for (var slot = 0; slot < characters.Length; slot++)
            characters[slot] = Advance(characters[slot]!, slot);

        return new MatchFrame(characters, [false, false, false, false]);
    }

    private CharacterFrame Advance(CharacterFrame frame, int slot)
    {
        if (frame.Frame == FrozenMark)
            return frame with { Frame = _frame, StateTime = _stateTime[slot] };

        _stateTime[slot]++;
        return frame with { Frame = _frame, StateTime = _stateTime[slot] };
    }

    private const int FrozenMark = -1;

    private static CharacterFrame Frozen(CharacterFrame frame) => frame with { Frame = FrozenMark, Hitstop = 5 };

    private static CharacterFrame Free() => Frame(Stand, canAct: true);

    private static CharacterFrame Acting(string state) => Frame(state);

    private static CharacterFrame Frame(string state, bool canAct = false, int hitstun = 0, int blockstun = 0, int hitstop = 0, int health = FullHealth) =>
        new(0, state, canAct, 0, hitstop, hitstun, blockstun, 0, Invulnerability.None, false, Contact.None, false, health);
}
