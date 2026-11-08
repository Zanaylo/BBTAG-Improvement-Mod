using BBTAG_IM.Core.Hantei;

namespace BBTAG_IM.Core.Porting.Moves;

public sealed class MoveStateWriter(HitVectorTable vectors, string spritePrefix, IReadOnlyDictionary<int, Ha6Pattern> patterns, double damageScale = 1)
{
    private const int CancelOnHitOrBlock = 1;
    private const int CancelAlways = 2;
    private const int CancelOnSuccessfulHit = 3;
    private const string WeakSwing = "004_swing_grap_1_1";
    private const string MiddleSwing = "006_swing_blade_1";
    private const string HeavySwing = "006_swing_blade_2";
    private const int MiddleLevel = 3;
    private const int HeavyLevel = 4;
    private const int AbleToCancelId = 40;
    private const int HoldCheckId = 50;
    private const int HoldEndId = 60;
    private const int HitBranchLabel = 1;
    private const int HoldBranchLabel = 1;
    private const int SuperCost = -10000;
    private const int ClearedCounterState = 3;
    private const int SetX = 0x10;
    private const int AddSpeedX = 0x20;
    private const int SetY = 0x01;
    private const int AddSpeedY = 0x02;
    private const int FullInvincibility = 3;
    private const string Body = "    ";
    private const string Handler = "        ";
    private const string Nested = "            ";

    public string WriteRegistration(MoveDefinition move)
    {
        List<string> lines = ["@StateRegister", $"def {move.State}({move.Input}):"];
        var requirements = move.Requirements.Count == 0 && move.Kind != MoveKind.Throw ? ["MoveMaxChainRepeat(1)", "SkillNotSame(1)"] : move.Requirements;
        lines.AddRange(requirements.Select(requirement => Body + requirement));

        if (move.Kind == MoveKind.Throw)
            lines.Add(Body + "MoveThrow()");
        return string.Join('\n', lines) + "\n";
    }

    public string Write(MoveDefinition move)
    {
        List<string> lines = ["@State", $"def {move.State}():", "", $"{Body}def upon_On_Initialize():"];
        lines.AddRange(Initialize(move).Select(line => Handler + line));

        var body = new FrameBody(this, lines, move);

        for (var index = 0; index < move.Patterns.Count; index++)
        {
            if (index > 0 && move.HoldButton is not null)
                body.OpenBranch(HoldBranchLabel);

            body.Add(patterns[move.Patterns[index]], index == 0);
        }

        if (move.HitPatterns.Count > 0)
        {
            body.OpenBranch(HitBranchLabel);
            lines.Add($"{Body}clearUponHandler(upon_Successful_Hit)");

            foreach (var pattern in move.HitPatterns)
                body.Add(patterns[pattern], false);
        }

        return string.Join('\n', lines) + "\n";
    }

    public string WriteObject(MoveObject moved, Ha6Pattern pattern)
    {
        List<string> lines =
        [
            "@State",
            $"def {moved.State}():",
            "",
            $"{Body}def upon_On_Initialize():",
            $"{Handler}AttackDefaults_SpecialProjectile()",
            $"{Handler}XPositionRelativeFacing({moved.X * NativeUnits.Position})",
            $"{Handler}AddY({-moved.Y * NativeUnits.Position})",
        ];

        if (LandingFrame(pattern) is { } landing)
            lines.Add($"{Handler}uponSendToLabel(upon_Landing, {landing})");

        new FrameBody(this, lines, null).AddObject(pattern, LandingFrame(pattern));
        return string.Join('\n', lines) + "\n";
    }

    private static int? LandingFrame(Ha6Pattern pattern) =>
        pattern.Frames.Select(frame => frame.Animation.GetValueOrDefault("AFJC")).FirstOrDefault(value => value is { Length: > 0 })?[0];

    private IEnumerable<string> Initialize(MoveDefinition move)
    {
        yield return Defaults(move);

        if (JumpCancel(move) is { } jumpCancel)
            yield return jumpCancel;

        if (move.Kind == MoveKind.Throw)
            yield return "ThrowRange(120000)";

        if (move.HitPatterns.Count > 0)
            yield return $"uponSendToLabel(upon_Successful_Hit, {HitBranchLabel})";

        foreach (var cancel in move.Cancels)
        {
            yield return $"{(OnlyCancelsOnHit(move) ? "HitCancel" : "HitOrBlockCancel")}({cancel})";

            if (OpensOnWhiff(move))
                yield return $"WhiffCancel({cancel})";
        }

        foreach (var followup in move.Followups)
        {
            yield return $"HitOrBlockCancel({followup})";

            if (FollowupsAllowWhiff(move))
                yield return $"WhiffCancel({followup})";
        }
    }

    private static string Defaults(MoveDefinition move) => move.Kind switch
    {
        MoveKind.StandingNormal => "AttackDefaults_StandingNormal()",
        MoveKind.CrouchingNormal => "AttackDefaults_CrouchingNormal()",
        MoveKind.AirNormal => "AttackDefaults_AirNormal()",
        MoveKind.AirSpecial => "AttackDefaults_AirSpecial()",
        MoveKind.Super => "AttackDefaults_StandingDD()",
        MoveKind.Reversal => "AttackDefaults_ReversalAction()",
        MoveKind.Throw => $"AttackDefaults_Throw({move.ThrowResult}, 1, 0, 0)",
        _ => "AttackDefaults_StandingSpecial()",
    };

    private string? JumpCancel(MoveDefinition move)
    {
        if (!move.IsNormal || !CancelFrames(move).Any())
            return null;

        return move.IsAirborne ? "HitOrBlockJumpCancel(1)" : "HitJumpCancel(1)";
    }

    private IEnumerable<int> CancelFrames(MoveDefinition move) =>
        move.Patterns.SelectMany(Frames).Select(frame => StateValue(frame, "ASCN")).Where(value => value >= CancelOnHitOrBlock);

    private bool OpensOnWhiff(MoveDefinition move) => CancelFrames(move).Any(value => value == CancelAlways);

    private bool FollowupsAllowWhiff(MoveDefinition move)
    {
        var frames = move.Patterns.SelectMany(Frames).ToArray();
        return frames.Any(frame => FrameId(frame) == AbleToCancelId || StateValue(frame, "ASCS") == CancelAlways) || frames.All(frame => StateValue(frame, "ASCS") == 0);
    }

    private bool OnlyCancelsOnHit(MoveDefinition move) => CancelFrames(move).Any() && CancelFrames(move).All(value => value == CancelOnSuccessfulHit);

    private static int StateValue(Ha6Frame frame, string chunk) => frame.State.TryGetValue(chunk, out var values) && values.Length > 0 ? values[0] : 0;

    private static string SwingOf(int level) => level >= HeavyLevel ? HeavySwing : level >= MiddleLevel ? MiddleSwing : WeakSwing;

    private int Scaled(int damage) => (int)Math.Round(damage * damageScale);

    private IEnumerable<string> AttackLines(Ha6Attack attack, bool airMove)
    {
        var ground = vectors.Find(attack.GroundHitVector);
        var air = vectors.Find(attack.AirHitVector);

        if (attack.Damage == 0 && ground is null)
            yield break;

        yield return $"AttackLevel({ground?.Level ?? air?.Level ?? 3})";

        if (attack.Damage > 0)
            yield return $"Damage({Scaled(attack.Damage)})";

        if (attack.Proration > 0)
            yield return $"AttackP2({attack.Proration})";

        if (attack.IsLow)
            yield return "HitLow(2)";

        if (attack.IsOverhead && !airMove)
            yield return "HitOverhead(2)";

        if (ground is { Launches: false })
        {
            yield return $"PushbackX({NativeUnits.Speed(ground.SpeedX)})";

            if (ground.Stun is { } hitstun)
                yield return $"Hitstun({hitstun})";
        }

        if (vectors.Find(attack.GroundGuardVector)?.Stun is { } blockstun)
            yield return $"Blockstun({blockstun})";

        if (ground is { Launches: true })
        {
            yield return "GroundedHitstunAnimation(AIR_FACE_UP_SKIP)";
            yield return "AirHitstunAnimation(AIR_FACE_UP_SKIP)";
        }

        var launch = ground is { Launches: true } ? ground : air;

        if (launch is not null)
        {
            yield return $"AirPushbackX({NativeUnits.Speed(launch.SpeedX)})";
            yield return $"AirPushbackY({NativeUnits.Speed(-launch.SpeedY)})";
        }

        if (attack.UntechTime > 0)
            yield return $"AirUntechableTime({attack.UntechTime})";

        if (attack.IsHardKnockdown)
            yield return "HardKnockdown(1)";
    }

    private static IEnumerable<string> SpeedLines(Ha6Frame frame, bool airMove)
    {
        if (frame.State.ContainsKey("ASVX"))
        {
            yield return "physicsXImpulse(0)";

            if (airMove)
                yield return "physicsYImpulse(0)";

            yield break;
        }

        if (!frame.State.TryGetValue("ASV0", out var speed))
            yield break;

        var (flags, speedX, speedY, accelerationX, accelerationY) = (speed[0], speed[1], speed[2], speed[3], speed[4]);

        if ((flags & (SetX | AddSpeedX)) != 0)
            yield return $"physicsXImpulse({NativeUnits.Speed(speedX)})";

        if ((flags & (SetY | AddSpeedY)) != 0 && (speedY != 0 || airMove))
            yield return $"physicsYImpulse({NativeUnits.Speed(-speedY)})";

        if (accelerationY != 0)
            yield return $"setGravity({NativeUnits.Speed(accelerationY)})";

        if (accelerationX != 0)
            yield return $"SetAcceleration({NativeUnits.Speed(accelerationX)})";
    }

    private static int? FrameId(Ha6Frame frame) => frame.Animation.TryGetValue("AFID", out var id) && id.Length > 0 ? id[0] : null;

    private static bool HasChunk(Ha6Frame frame, string chunk, int value) => frame.State.TryGetValue(chunk, out var values) && values.Length > 0 && values[0] == value;

    private sealed class FrameBody(MoveStateWriter writer, List<string> lines, MoveDefinition? move)
    {
        private readonly bool _airMove = move?.IsAirborne ?? false;
        private readonly bool _opensFollowupsAtStart = move is { Followups.Count: > 0 } && move.Patterns.SelectMany(pattern => writer.Frames(pattern)).All(frame => FrameId(frame) != AbleToCancelId && StateValue(frame, "ASCS") == 0);
        private Ha6Attack? _attack;
        private int _hits;
        private bool _invincible;
        private bool _holding;
        private bool _clearHold;
        private bool _cancelOpen;

        private bool OpensCancels(Ha6Frame frame)
        {
            if (move is { Followups.Count: > 0 } && (FrameId(frame) == AbleToCancelId || StateValue(frame, "ASCS") == CancelAlways))
                return true;

            return move is { Cancels.Count: > 0 } && StateValue(frame, "ASCN") == CancelAlways;
        }

        public void OpenBranch(int label)
        {
            lines.Add($"{Body}ExitState()");
            lines.Add($"{Body}label({label})");
        }

        public void Add(Ha6Pattern pattern, bool startsTheMove)
        {
            for (var index = 0; index < pattern.Frames.Count; index++)
            {
                var frame = pattern.Frames[index];

                if (HasChunk(frame, "ASMV", 1))
                {
                    if (move?.HoldButton is null && move?.HitPatterns.Count is null or 0)
                        lines.Add($"{Body}ExitState()");

                    return;
                }

                AddSprite(pattern, index);

                if (startsTheMove && index == 0)
                    AddMoveStart();

                AddHold(frame);
                AddShared(frame);
                AddMoveFrame(frame);
            }
        }

        public void AddObject(Ha6Pattern pattern, int? landingFrame)
        {
            for (var index = 0; index < pattern.Frames.Count; index++)
            {
                if (index == landingFrame)
                    lines.Add($"{Body}label({index})");

                AddSprite(pattern, index);
                AddShared(pattern.Frames[index]);
            }
        }

        private void AddSprite(Ha6Pattern pattern, int index) =>
            lines.Add($"{Body}sprite('{SpriteRemap.FrameName(writer.Prefix, pattern.Id, index)}', {pattern.Frames[index].Duration})");

        private void AddMoveStart()
        {
            if (_opensFollowupsAtStart)
                lines.Add($"{Body}WhiffCancelEnable(1)");

            if (move?.Kind == MoveKind.Reversal)
            {
                _invincible = true;
                lines.Add($"{Body}setInvincible(1)");
            }

            if (move?.Flashes == true)
            {
                lines.Add($"{Body}ExSkillFlash()");
                lines.Add($"{Body}ConsumeSuperMeter({-move.MeterCost})");
            }

            if (move?.Kind != MoveKind.Super)
                return;

            lines.Add($"{Body}ConsumeSuperMeter({SuperCost})");
            lines.Add($"{Body}SuperPortrait()");
        }

        private void AddHold(Ha6Frame frame)
        {
            if (_clearHold)
            {
                lines.Add($"{Body}clearUponHandler(upon_Idling)");
                _clearHold = false;
            }

            if (move?.HoldButton is not { } button)
                return;

            if (FrameId(frame) == HoldCheckId && !_holding)
            {
                _holding = true;
                lines.Add($"{Body}def upon_Idling():");
                lines.Add($"{Handler}SLOT_Tmp = CheckInput(INPUT_NOT_HOLD_{button})");
                lines.Add($"{Handler}if SLOT_Tmp:");
                lines.Add($"{Nested}clearUponHandler(upon_Idling)");
                lines.Add($"{Nested}sendToLabel({HoldBranchLabel})");
            }

            if (FrameId(frame) == HoldEndId)
                _clearHold = true;
        }

        private void AddShared(Ha6Frame frame)
        {
            if (HasChunk(frame, "ASCT", ClearedCounterState))
                lines.Add($"{Body}Recovery()");

            lines.AddRange(SpeedLines(frame, _airMove).Select(line => Body + line));
            AddInvincibility(frame);
            AddAttack(frame);
        }

        private void AddInvincibility(Ha6Frame frame)
        {
            var invincible = HasChunk(frame, "ASYS", FullInvincibility);

            if (move?.Kind == MoveKind.Reversal)
                invincible = _invincible && !HasChunk(frame, "ASCT", ClearedCounterState);

            if (invincible == _invincible)
                return;

            _invincible = invincible;
            lines.Add($"{Body}setInvincible({(invincible ? 1 : 0)})");
        }

        private void AddAttack(Ha6Frame frame)
        {
            if (Ha6Attack.Of(frame) is not { } attack)
                return;

            if (frame.State.ContainsKey("ASAA") && _hits++ > 0)
                lines.Add($"{Body}RefreshMultihit()");

            if (attack == _attack)
                return;

            if (_attack is null && writer.LevelOf(attack) is { } level)
                lines.Add($"{Body}CommonSE('{SwingOf(level)}')");

            _attack = attack;
            lines.AddRange(writer.AttackLines(attack, _airMove).Select(line => Body + line));
        }

        private void AddMoveFrame(Ha6Frame frame)
        {
            if (move is null)
                return;

            if (!_cancelOpen && OpensCancels(frame))
            {
                _cancelOpen = true;
                lines.Add($"{Body}WhiffCancelEnable(1)");
            }

            foreach (var moved in move.Objects.Where(moved => moved.FrameId == FrameId(frame)))
                lines.Add($"{Body}GFX_0('{moved.State}', 0)");
        }
    }

    private int? LevelOf(Ha6Attack attack) => vectors.Find(attack.GroundHitVector)?.Level ?? vectors.Find(attack.AirHitVector)?.Level;

    private string Prefix => spritePrefix;

    private IReadOnlyList<Ha6Frame> Frames(int pattern) => patterns[pattern].Frames;
}
