using BBTAG_IM.Core.Hantei;
using BBTAG_IM.Core.Porting.Moves;

namespace BBTAG_IM.Core.Tests.Porting.Moves;

public class MoveStateWriterTests
{
    private const string Vectors = """
        [   1,  1500,     0,  -80,    0 ], //  note
        [  48,  2100, -2400,    0,  250 ], //  note
        vectmpl.add({ number = 1, name = "stand",
            tmpl_param = { type="yarare_stand", power=def_VL_AtkLv_B1, vector_num=1, hit_pattern = 1 },
        })
        vectmpl.add({ number = 48, name = "launch",
            tmpl_param = { type="uki", power=def_VL_AtkLv_C1, },
            tbl = [ { vector_num = 48, }, ]
        })
        """;

    private static readonly Dictionary<string, int[]> Hit = new()
    {
        ["ATAT"] = [480],
        ["ATC0"] = [92, 0, 0],
        ["ATGD"] = [36],
        ["ATSU"] = [22],
        ["ATV2"] = [3, 2, 0, 1, 0, 151, 0, 48, 0, 171, 0, 1, 0, 161],
    };

    private static Ha6Frame Frame(int duration, Dictionary<string, int[]>? attack = null, Dictionary<string, int[]>? state = null, Dictionary<string, int[]>? animation = null) => new()
    {
        Duration = duration,
        Attack = attack ?? [],
        State = state ?? [],
        Animation = animation ?? [],
    };

    private static MoveStateWriter Writer(params Ha6Pattern[] patterns) =>
        new(HitVectorTable.Parse(Vectors), "Port_", patterns.ToDictionary(pattern => pattern.Id));

    private static MoveDefinition Normal(params int[] patterns) => new("NmlAtk5B", "INPUT_5B", MoveKind.StandingNormal, patterns, [], [], []);

    [Fact]
    public void Write_Frames_KeepTheNativeDurations()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(3), Frame(2)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Contains("@State\ndef NmlAtk5B():", script);
        Assert.Contains("AttackDefaults_StandingNormal()", script);
        Assert.Contains("sprite('Port_002_00', 3)\n    sprite('Port_002_01', 2)", script);
    }

    [Fact]
    public void Write_AttackFrame_SetsItsValuesOnItsSprite()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(2), Frame(4, Hit)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Contains("sprite('Port_002_01', 4)\n    CommonSE('006_swing_blade_1')\n    AttackLevel(3)\n    Damage(480)\n    AttackP2(92)\n    HitLow(2)", script);
    }

    [Fact]
    public void Write_GroundHit_UsesTheGroundPushback()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(4, Hit)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Contains("PushbackX(11719)", script);
    }

    [Fact]
    public void Write_AirHit_UsesTheAirVectorScaledToNativeUnits()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(4, Hit)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Contains("AirPushbackX(16406)\n    AirPushbackY(18750)\n    AirUntechableTime(22)", script);
    }

    [Fact]
    public void Write_Damage_IsScaledToTheHealthOfTheRoster()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(4, Hit)]);
        MoveStateWriter writer = new(HitVectorTable.Parse(Vectors), "Port_", new Dictionary<int, Ha6Pattern> { [2] = pattern }, 1.5);

        // Act
        string script = writer.Write(Normal(2));

        // Assert
        Assert.Contains("Damage(720)", script);
    }

    [Fact]
    public void Write_StunOfTheGame_OverridesTheAttackLevel()
    {
        // Arrange
        const string timed = "const def_VL_VT_Hit_Std_B1 = 16;\nconst def_VL_VT_Guard_Std_B1 = 14;\n" + Vectors + "\nvectmpl.add({ number = 151, name = \"guard\",\n tmpl_param = { type=\"guard_stand\", power=def_VL_AtkLv_B1, },\n})";
        Ha6Pattern pattern = new(2, "", [Frame(4, Hit)]);
        MoveStateWriter writer = new(HitVectorTable.Parse(timed), "Port_", new Dictionary<int, Ha6Pattern> { [2] = pattern });

        // Act
        string script = writer.Write(Normal(2));

        // Assert
        Assert.Contains("Hitstun(16)", script);
        Assert.Contains("Blockstun(14)", script);
    }

    [Fact]
    public void Write_CancelWindowOfTheFrames_OpensTheWhiffCancels()
    {
        // Arrange
        Dictionary<string, int[]> always = new() { ["ASCN"] = [2] };
        Dictionary<string, int[]> onHit = new() { ["ASCN"] = [1] };
        Ha6Pattern pattern = new(2, "", [Frame(2, state: onHit), Frame(2, state: always)]);

        // Act
        string script = Writer(pattern).Write(Normal(2) with { Cancels = ["NmlAtk5C"] });

        // Assert
        Assert.Contains("HitOrBlockCancel(NmlAtk5C)", script);
        Assert.Contains("WhiffCancel(NmlAtk5C)", script);
        Assert.Contains("sprite('Port_002_01', 2)\n    WhiffCancelEnable(1)", script);
    }

    [Fact]
    public void Write_CancelOnlyOnSuccessfulHit_DoesNotCancelOnBlock()
    {
        // Arrange
        Dictionary<string, int[]> onDamage = new() { ["ASCN"] = [3] };
        Ha6Pattern pattern = new(2, "", [Frame(2, state: onDamage)]);

        // Act
        string script = Writer(pattern).Write(Normal(2) with { Cancels = ["NmlAtk5C"] });

        // Assert
        Assert.Contains("HitCancel(NmlAtk5C)", script);
        Assert.DoesNotContain("HitOrBlockCancel", script);
    }

    [Theory]
    [InlineData(MoveKind.StandingNormal, "HitJumpCancel(1)")]
    [InlineData(MoveKind.AirNormal, "HitOrBlockJumpCancel(1)")]
    public void Write_NormalWithACancelWindow_CanJumpCancel(MoveKind kind, string expected)
    {
        // Arrange
        Dictionary<string, int[]> onHit = new() { ["ASCN"] = [1] };
        Ha6Pattern pattern = new(2, "", [Frame(2, state: onHit)]);

        // Act
        string script = Writer(pattern).Write(Normal(2) with { Kind = kind });

        // Assert
        Assert.Contains(expected, script);
    }

    [Fact]
    public void Write_MoveWithAMeterCost_FlashesAndPays()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(2)]);

        // Act
        string script = Writer(pattern).Write(Normal(2) with { Kind = MoveKind.StandingSpecial, MeterCost = 5000, Flashes = true });

        // Assert
        Assert.Contains("ExSkillFlash()\n    ConsumeSuperMeter(-5000)", script);
    }

    [Fact]
    public void Write_Reversal_IsInvincibleUntilItsRecovery()
    {
        // Arrange
        Dictionary<string, int[]> recovery = new() { ["ASCT"] = [3] };
        Ha6Pattern pattern = new(2, "", [Frame(2), Frame(2, state: recovery)]);

        // Act
        string script = Writer(pattern).Write(Normal(2) with { Kind = MoveKind.Reversal });

        // Assert
        Assert.Contains("AttackDefaults_ReversalAction()", script);
        Assert.Contains("setInvincible(1)", script);
        Assert.Contains("setInvincible(0)", script);
    }

    [Fact]
    public void Write_FollowupWindowOnlyOnHit_IsNotWhiffCancellable()
    {
        // Arrange
        Dictionary<string, int[]> onHit = new() { ["ASCS"] = [1] };
        Ha6Pattern pattern = new(2, "", [Frame(2), Frame(2, state: onHit)]);

        // Act
        string script = Writer(pattern).Write(Normal(2) with { Kind = MoveKind.StandingSpecial, Followups = ["Skill_236A_Add"] });

        // Assert
        Assert.Contains("HitOrBlockCancel(Skill_236A_Add)", script);
        Assert.DoesNotContain("WhiffCancel(Skill_236A_Add)", script);
        Assert.DoesNotContain("WhiffCancelEnable(1)", script);
    }

    [Fact]
    public void Write_HitBranch_DoesNotRestartOnItsOwnHits()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(2)]);
        Ha6Pattern hit = new(3, "", [Frame(2)]);

        // Act
        string script = Writer(pattern, hit).Write(Normal(2) with { HitPatterns = [3] });

        // Assert
        Assert.Contains("label(1)\n    clearUponHandler(upon_Successful_Hit)\n    sprite('Port_003_00', 2)", script);
    }

    [Fact]
    public void WriteRegistration_Throw_IsMarkedAsAThrow()
    {
        // Arrange
        MoveDefinition move = new("NmlAtkThrow", "INPUT_THROW", MoveKind.Throw, [55], [], [], []);

        // Act
        string registration = Writer().WriteRegistration(move);

        // Assert
        Assert.Contains("MoveThrow()", registration);
    }

    [Fact]
    public void Write_RepeatedAttackValues_AreOnlySetOnce()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(1, Hit), Frame(1, Hit)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Single(script.Split("Damage(480)")[1..]);
    }

    [Fact]
    public void Write_NewHitInsideTheMove_RefreshesTheMultihit()
    {
        // Arrange
        Dictionary<string, int[]> newHit = new() { ["ASAA"] = [1] };
        Ha6Pattern pattern = new(2, "", [Frame(1, Hit, newHit), Frame(1), Frame(1, Hit, newHit)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Single(script.Split("RefreshMultihit()")[1..]);
    }

    [Fact]
    public void Write_SpeedOfTheFrame_BecomesNativeImpulses()
    {
        // Arrange
        Dictionary<string, int[]> moving = new() { ["ASV0"] = [17, 2080, -3100, 0, 200] };
        Ha6Pattern pattern = new(2, "", [Frame(2, state: moving)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Contains("physicsXImpulse(16250)\n    physicsYImpulse(24219)\n    setGravity(1562)", script);
    }

    [Fact]
    public void Write_FrameStoppingTheCharacter_ClearsTheSpeed()
    {
        // Arrange
        Dictionary<string, int[]> stop = new() { ["ASVX"] = [] };
        Ha6Pattern pattern = new(2, "", [Frame(2, state: stop)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Contains("physicsXImpulse(0)", script);
    }

    [Fact]
    public void Write_AbleToMoveFrame_EndsTheState()
    {
        // Arrange
        Dictionary<string, int[]> free = new() { ["ASMV"] = [1] };
        Ha6Pattern pattern = new(2, "", [Frame(2), Frame(3, state: free), Frame(4)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.DoesNotContain("Port_002_02", script);
        Assert.Contains("sprite('Port_002_00', 2)\n    ExitState()", script);
    }

    [Fact]
    public void Write_ClearedCounterState_MarksTheRecovery()
    {
        // Arrange
        Dictionary<string, int[]> recovery = new() { ["ASCT"] = [3] };
        Ha6Pattern pattern = new(2, "", [Frame(2, state: recovery)]);

        // Act
        string script = Writer(pattern).Write(Normal(2));

        // Assert
        Assert.Contains("Recovery()", script);
    }

    [Fact]
    public void Write_Cancels_ListTheLaterNormals()
    {
        // Arrange
        Ha6Pattern pattern = new(2, "", [Frame(2)]);
        MoveDefinition move = Normal(2) with { Cancels = ["NmlAtk5C", "NmlAtk2C"] };

        // Act
        string script = Writer(pattern).Write(move);

        // Assert
        Assert.Contains("HitOrBlockCancel(NmlAtk5C)\n        HitOrBlockCancel(NmlAtk2C)", script);
    }

    [Fact]
    public void Write_MoveWithObject_SpawnsItOnItsFrameId()
    {
        // Arrange
        Dictionary<string, int[]> identified = new() { ["AFID"] = [100] };
        Ha6Pattern pattern = new(2, "", [Frame(2), Frame(2, animation: identified)]);
        MoveDefinition move = Normal(2) with { Objects = [new MoveObject(100, "DropBomb", 0, -250)] };

        // Act
        string script = Writer(pattern).Write(move);

        // Assert
        Assert.Contains("sprite('Port_002_01', 2)\n    GFX_0('Obj_DropBomb', 0)", script);
    }

    [Fact]
    public void Write_HeldPattern_BranchesToItsEndWhenTheButtonIsReleased()
    {
        // Arrange
        Ha6Pattern stance = new(195, "", [Frame(2, animation: new() { ["AFID"] = [50] }), Frame(2, animation: new() { ["AFID"] = [60] }), Frame(2)], "236A");
        Ha6Pattern end = new(192, "", [Frame(1)], "236A_End");
        MoveDefinition move = new("Skill_236A", "INPUT_SPECIALMOVE", MoveKind.StandingSpecial, [195, 192], [], [], []) { HoldButton = "A" };

        // Act
        string script = Writer(stance, end).Write(move);

        // Assert
        Assert.Contains("def upon_Idling():\n        SLOT_Tmp = CheckInput(INPUT_NOT_HOLD_A)\n        if SLOT_Tmp:\n            clearUponHandler(upon_Idling)\n            sendToLabel(1)", script);
        Assert.Contains("sprite('Port_195_02', 2)\n    clearUponHandler(upon_Idling)", script);
        Assert.Contains("ExitState()\n    label(1)\n    sprite('Port_192_00', 1)", script);
    }

    [Fact]
    public void Write_Super_SpendsMeterAndStartsWithTheSuperFlash()
    {
        // Arrange
        Ha6Pattern pattern = new(350, "", [Frame(3)], "41236SP");
        MoveDefinition move = new("Skill_41236SP", "INPUT_DISTORTION", MoveKind.Super, [350], [], [], []);

        // Act
        string script = Writer(pattern).Write(move);

        // Assert
        Assert.Contains("AttackDefaults_StandingDD()", script);
        Assert.Contains("sprite('Port_350_00', 3)\n    ConsumeSuperMeter(-10000)\n    SuperPortrait()", script);
    }

    [Fact]
    public void Write_Followups_OpenTheirCancelWindow()
    {
        // Arrange
        Ha6Pattern pattern = new(230, "", [Frame(2), Frame(2, animation: new() { ["AFID"] = [40] })], "214A");
        MoveDefinition move = new("Skill_214A", "INPUT_SPECIALMOVE", MoveKind.StandingSpecial, [230], [], [], ["Skill_214A_JAdd"]);

        // Act
        string script = Writer(pattern).Write(move);

        // Assert
        Assert.Contains("WhiffCancel(Skill_214A_JAdd)", script);
        Assert.Contains("sprite('Port_230_01', 2)\n    WhiffCancelEnable(1)", script);
    }

    [Fact]
    public void WriteObject_PlacesItRelativeToTheCharacter()
    {
        // Arrange
        Ha6Pattern pattern = new(241, "", [Frame(3, Hit)], "DropBombGro");

        // Act
        string script = Writer(pattern).WriteObject(new MoveObject(100, "DropBombGro", 50, -250), pattern);

        // Assert
        Assert.Contains("@State\ndef Obj_DropBombGro():", script);
        Assert.Contains("AttackDefaults_SpecialProjectile()", script);
        Assert.Contains("XPositionRelativeFacing(50000)", script);
        Assert.Contains("AddY(250000)", script);
    }
}
