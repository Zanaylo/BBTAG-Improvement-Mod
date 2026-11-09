using BBTAG_IM.Core.Meter;

namespace BBTAG_IM.Core.Tests.Meter;

public class CharacterTrackTests
{
    private const string Stand = "CmnActStand";
    private const string Attack = "NmlAtk5A";
    private const string Chain = "NmlAtk5B";

    private int _frame;

    public CharacterTrack CreateCharacterTrack() => new(CharacterTrack.DefaultCapacity);

    [Fact]
    public void Add_StandingFrame_RecordsIdle()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        track.Add(Free(), projectileActive: false);

        // Assert
        Assert.Equal([FrameKind.Idle], Kinds(track));
    }

    [Fact]
    public void Add_WholeAttack_RecordsStartupActiveAndRecovery()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        PlayAttack(track, startup: 6, active: 3, recovery: 4);
        track.Add(Free(), projectileActive: false);

        // Assert
        Assert.Equal([.. Repeat(FrameKind.Startup, 6), .. Repeat(FrameKind.Active, 3), .. Repeat(FrameKind.Recovery, 4), FrameKind.Idle], Kinds(track));
    }

    [Fact]
    public void Add_AttackEnds_ReportsItsTiming()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        PlayAttack(track, startup: 12, active: 5, recovery: 44);
        track.Add(Free(), projectileActive: false);

        // Assert
        Assert.Equal(new MoveTiming(Attack, 13, 5, 44, 61), track.LastMove);
    }

    [Fact]
    public void Add_AttackStillRunning_ReportsTheTimingSoFar()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        PlayAttack(track, startup: 6, active: 2, recovery: 3);

        // Assert
        Assert.Equal(new MoveTiming(Attack, 7, 2, 3, 11), track.LastMove);
    }

    [Fact]
    public void Add_CancelIntoAnotherAttack_StartsANewMoveAndRelabelsItsStartup()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();
        PlayAttack(track, startup: 6, active: 3, recovery: 2);

        // Act
        PlayAttack(track, startup: 8, active: 2, recovery: 5, state: Chain);
        track.Add(Free(), projectileActive: false);

        // Assert
        Assert.Equal(
            [.. Repeat(FrameKind.Startup, 6), .. Repeat(FrameKind.Active, 3), .. Repeat(FrameKind.Recovery, 2), .. Repeat(FrameKind.Startup, 8), .. Repeat(FrameKind.Active, 2), .. Repeat(FrameKind.Recovery, 5), FrameKind.Idle],
            Kinds(track));
        Assert.Equal(new MoveTiming(Chain, 9, 2, 5, 15), track.LastMove);
    }

    [Fact]
    public void Add_FollowUpStateWithoutHitboxes_StaysRecovery()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();
        PlayAttack(track, startup: 4, active: 2, recovery: 2);

        // Act
        track.Add(Acting("NmlAtkJ2C_Landing"), projectileActive: false);
        track.Add(Acting("NmlAtkJ2C_Landing"), projectileActive: false);
        track.Add(Free(), projectileActive: false);

        // Assert
        Assert.Equal(new MoveTiming(Attack, 5, 2, 4, 10), track.LastMove);
    }

    [Fact]
    public void Add_ProjectileComesOut_EndsTheStartupAndMarksTheFrames()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        for (var startup = 0; startup < 10; startup++)
            track.Add(Acting(Attack), projectileActive: false);

        for (var recovery = 0; recovery < 6; recovery++)
            track.Add(Acting(Attack), projectileActive: true);

        track.Add(Free(), projectileActive: true);

        // Assert
        Assert.Equal(new MoveTiming(Attack, 11, 1, 5, 16), track.LastMove);
        Assert.Equal(FrameKind.Recovery, track.Cells[10].Kind);
        Assert.True(track.Cells[^1].Marks.HasFlag(CellMarks.Projectile));
    }

    [Fact]
    public void Add_ActionWithoutHitboxesEnds_KeepsTheTimingOfTheMoveBefore()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();
        PlayAttack(track, startup: 6, active: 3, recovery: 4);
        track.Add(Free(), projectileActive: false);

        // Act
        track.Add(Acting("CmnActEntry"), projectileActive: false);
        track.Add(Free(), projectileActive: false);

        // Assert
        Assert.Equal(new MoveTiming(Attack, 7, 3, 4, 13), track.LastMove);
    }

    [Fact]
    public void Add_CancelIntoAProjectileMove_StartsANewMoveWhenTheProjectileComesOut()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();
        PlayAttack(track, startup: 6, active: 2, recovery: 3);

        // Act
        for (var startup = 0; startup < 8; startup++)
            track.Add(Acting(Chain), projectileActive: false);

        for (var recovery = 0; recovery < 4; recovery++)
            track.Add(Acting(Chain), projectileActive: true);

        track.Add(Free(), projectileActive: true);

        // Assert
        Assert.Equal(new MoveTiming(Chain, 9, 1, 3, 12), track.LastMove);
        Assert.Equal(FrameKind.Startup, track.Cells[11].Kind);
    }

    [Fact]
    public void Add_HitstunAndBlockstun_RecordTheirKinds()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        track.Add(Frame("CmnActHitStandLv4", hitstun: 17), projectileActive: false);
        track.Add(Frame("CmnActFDownLoop"), projectileActive: false);
        track.Add(Frame("CmnActHighGuardLoop", blockstun: 16), projectileActive: false);
        track.Add(Frame("CmnActUkemiLandN"), projectileActive: false);

        // Assert
        Assert.Equal([FrameKind.Hitstun, FrameKind.Hitstun, FrameKind.Blockstun, FrameKind.Tech], Kinds(track));
    }

    [Fact]
    public void Add_GuardEndThatCanAct_RecordsIdle()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        track.Add(Frame("CmnActHighGuardEnd", canAct: true), projectileActive: false);

        // Assert
        Assert.Equal([FrameKind.Idle], Kinds(track));
    }

    [Fact]
    public void Add_DashAndBackdash_RecordMovementWithoutAMove()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        track.Add(Frame("CmnActFDash", canAct: true), projectileActive: false);
        track.Add(Frame("CmnActBDash"), projectileActive: false);

        // Assert
        Assert.Equal([FrameKind.Movement, FrameKind.Movement], Kinds(track));
        Assert.Null(track.LastMove);
    }

    [Fact]
    public void Add_PartnerAwayThenLeaving_RecordsEmptyAndLeaving()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        track.Add(Frame("CmnActTagBattleWait"), projectileActive: false);
        track.Add(Frame("CmnActChangeReturnAppeal"), projectileActive: false);

        // Assert
        Assert.Equal([FrameKind.Empty, FrameKind.Leaving], Kinds(track));
    }

    [Fact]
    public void Add_AssistAttackThenLeaving_ReportsTheAssistTiming()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        PlayAttack(track, startup: 28, active: 25, recovery: 22, state: "CmnActChangePartnerAssistAtk_A");
        track.Add(Frame("CmnActChangeReturnAppeal"), projectileActive: false);

        // Assert
        Assert.Equal(new MoveTiming("CmnActChangePartnerAssistAtk_A", 29, 25, 22, 75), track.LastMove);
    }

    [Fact]
    public void Add_InvulnerableArmoredFrameThatConnects_MarksIt()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();

        // Act
        track.Add(Acting(Attack) with { Invulnerability = Invulnerability.Full, Armor = true }, projectileActive: false);
        track.Add(Acting(Attack) with { Hitboxes = 2, Contact = Contact.Hit }, projectileActive: false);

        // Assert
        Assert.Equal(Invulnerability.Full, track.Cells[0].Invulnerability);
        Assert.True(track.Cells[0].Marks.HasFlag(CellMarks.Armor));
        Assert.True(track.Cells[1].Marks.HasFlag(CellMarks.HitStart));
    }

    [Fact]
    public void Add_MoreFramesThanItKeeps_DropsTheOldestAndKeepsTheTiming()
    {
        // Arrange
        CharacterTrack track = new(capacity: 20);

        // Act
        PlayAttack(track, startup: 12, active: 5, recovery: 10);
        track.Add(Free(), projectileActive: false);

        // Assert
        Assert.True(track.Cells.Count <= 20);
        Assert.Equal(FrameKind.Idle, track.Cells[^1].Kind);
        Assert.Equal(new MoveTiming(Attack, 13, 5, 10, 27), track.LastMove);
    }

    [Fact]
    public void Clear_RecordedFrames_EmptiesTheTrackButKeepsTheLastMove()
    {
        // Arrange
        CharacterTrack track = CreateCharacterTrack();
        PlayAttack(track, startup: 2, active: 1, recovery: 1);
        track.Add(Free(), projectileActive: false);

        // Act
        track.Clear();

        // Assert
        Assert.Empty(track.Cells);
        Assert.NotNull(track.LastMove);
    }

    private void PlayAttack(CharacterTrack track, int startup, int active, int recovery, string state = Attack)
    {
        for (var frame = 0; frame < startup; frame++)
            track.Add(Acting(state), projectileActive: false);

        for (var frame = 0; frame < active; frame++)
            track.Add(Acting(state) with { Hitboxes = 2 }, projectileActive: false);

        for (var frame = 0; frame < recovery; frame++)
            track.Add(Acting(state), projectileActive: false);
    }

    private CharacterFrame Free() => Frame(Stand, canAct: true);

    private CharacterFrame Acting(string state) => Frame(state);

    private CharacterFrame Frame(string state, bool canAct = false, int hitstun = 0, int blockstun = 0) =>
        new(++_frame, state, canAct, _frame, 0, hitstun, blockstun, 0, Invulnerability.None, false, Contact.None, false, 17000);

    private static FrameKind[] Kinds(CharacterTrack track) => track.Cells.Select(cell => cell.Kind).ToArray();

    private static IEnumerable<FrameKind> Repeat(FrameKind kind, int count) => Enumerable.Repeat(kind, count);
}
