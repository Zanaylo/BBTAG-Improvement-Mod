using System.Text.RegularExpressions;
using BBTAG_IM.Core.Hantei;

namespace BBTAG_IM.Core.Porting.Moves;

public static partial class MoveCatalog
{
    private const string SpecialInput = "INPUT_SPECIALMOVE";
    private const string SuperInput = "INPUT_DISTORTION";
    private const string AstralInput = "INPUT_ASTRAL";
    private const string AnyAttackButton = "INPUT_0xed";
    private const string EndSuffix = "_End";
    private const string HitSuffix = "_Hit";
    private const string Ground = "Move_AirGround_(MOVE_GROUND)";
    private const string Air = "Move_AirGround_(MOVE_AIR)";
    private const string ExCost = "Move_AirGround_(MOVE_EX)";
    private const string SkillCost = "Move_AirGround_(MOVE_SKILL_GAUGE_2500)";
    private const string SuperCost = "Move_AirGround_(MOVE_DISTORTION)";
    private const string AstralCost = "Move_AirGround_(MOVE_ASTRAL)";
    private const string Followup = "FollowupOnly(1)";
    private const string Once = "MoveMaxChainRepeat(1)";
    private const string NotTheSameMove = "SkillNotSame(1)";
    private const string ButtonEitherBOrC = "BorC";
    private const string ButtonEx = "EX";
    private const string ButtonBoth = "BC";
    private const string ButtonSuper = "SP";
    private const string BothButtonsInput = "B+C";
    private const string DoubleDown = "0202";
    private const string NativeDoubleDown = "22";
    private const string RapidRelayCommand = "Atk_RapidRelayStdA";
    private const string RapidState = "NmlAtkRapidRelay";
    private const string FinishState = "NmlAtkRapidFinish";
    private const string ThirdSuffix = "_3";
    private const string SecondSuffix = "_2";
    private const string NeutralA = "NmlAtk5A";
    private const string BackA = "NmlAtk4A";
    private const string CrouchA = "NmlAtk2A";
    private const string CrouchC = "NmlAtk2C";
    private const string AirA = "NmlAtkAIR5A";
    private const string AirThrowState = "NmlAtkAirThrow";
    private const string AirThrowHitCode = "AirThrow";
    private const string ReversalState = "CmnActInvincibleAttack";
    private const string CpuInvincibleEntry = "Skill_Muteki";
    private const string LastArcCode = "LastArc_Hit";
    private const string LastArcEndCode = "LastArc_End";
    private const int DefaultObjectFrame = 100;
    private const int ThrowPattern = 55;
    private const int ThrowHitPattern = 57;
    private const int AirThrowPattern = 140;
    private const int AirThrowHitPattern = 142;
    private const int FinishPattern = 131;
    private const int RapidPattern = 31;
    private const int LastArcFirstFollowUp = 402;
    private const int LastArcLastFollowUp = 405;
    private const int RapidRepeats = 3;
    private const int ExMeterCost = 5000;
    private const int SkillMeterCost = 2500;
    private const int SuperGauge = 20000;

    private static readonly HashSet<string> HandledByNormals = ["Atk_Std6C", "Atk_Std3C", "Atk_2C_2C", "Throw_A", "Skill_LastArcBlast"];

    private static readonly (string State, string Input, MoveKind Kind, int Pattern, string MoveName)[] StandardNormals =
    [
        (NeutralA, "INPUT_5A", MoveKind.StandingNormal, 1, "Mv_Atk_StdA"),
        ("NmlAtk5B", "INPUT_5B", MoveKind.StandingNormal, 2, "Mv_Atk_StdB"),
        ("NmlAtk5C", "INPUT_5C", MoveKind.StandingNormal, 3, "Mv_Atk_StdC"),
        (CrouchA, "INPUT_2A", MoveKind.CrouchingNormal, 4, "Mv_Atk_CroA"),
        ("NmlAtk2B", "INPUT_2B", MoveKind.CrouchingNormal, 5, "Mv_Atk_CroB"),
        (CrouchC, "INPUT_2C", MoveKind.CrouchingNormal, 6, "Mv_Atk_CroC"),
        (AirA, "INPUT_J5A", MoveKind.AirNormal, 7, "Mv_Atk_AirA"),
        ("NmlAtkAIR5B", "INPUT_J5B", MoveKind.AirNormal, 8, "Mv_Atk_AirB"),
        ("NmlAtkAIR5C", "INPUT_J5C", MoveKind.AirNormal, 9, "Mv_Atk_AirC"),
    ];

    private static readonly string[][] GroundChain =
    [
        [NeutralA, CrouchA],
        ["NmlAtk5B", "NmlAtk2B"],
        ["NmlAtk5C", CrouchC],
        ["NmlAtk6C", "NmlAtk3C"],
    ];

    private static readonly string[][] AirChain = [[AirA], ["NmlAtkAIR5B"], ["NmlAtkAIR5C"]];

    private static readonly (string Name, string Input)[] RapidInputs =
    [
        ("Rapid_B", "INPUT_5B"), ("Rapid_C", "INPUT_5C"), ("Rapid_2A", "INPUT_2A"), ("Rapid_2B", "INPUT_2B"), ("Rapid_2C", "INPUT_2C"),
    ];

    private static readonly (string Name, string Input)[] FinishInputs = [("Finish_B", "INPUT_5B"), ("Finish_C", "INPUT_5C")];

    private static readonly (string State, string Code, int Pattern)[] Assists =
    [
        ("CmnActChangePartnerAssistAtk_A", "P_AtkA", 3),
        ("CmnActChangePartnerAssistAtk_B", "P_AtkB", 6),
        ("CmnActChangePartnerAssistAtk_D", "P_AtkC", 2),
    ];

    public static IReadOnlyList<MoveDefinition> Build(string commands, string moves, IReadOnlyDictionary<int, Ha6Pattern> patterns, NativeCommandTable? table = null, string cpu = "")
    {
        var known = table ?? NativeCommandTable.Empty;
        var codes = patterns.Values.Where(pattern => pattern.Code.Length > 0).GroupBy(pattern => pattern.Code).ToDictionary(group => group.Key, group => group.First().Id);
        var names = CommandName().Matches(commands).Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value).Distinct().ToArray();
        var rapid = known.Find(RapidRelayCommand) is not null && patterns.ContainsKey(RapidPattern);
        var normals = Normals(names, moves, codes, patterns);
        List<MoveDefinition> built = [];

        if (rapid)
            built.AddRange(RapidMoves(patterns));

        built.AddRange(normals);

        if (rapid)
            built.AddRange(Repeats(normals));

        AddThrows(built, patterns, codes, names);

        var specials = names.SelectMany(name => Special(name, moves, codes, known)).ToList();
        built.AddRange(specials);
        built.AddRange(ArcFinish(names, patterns, codes));
        built.AddRange(AssistMoves(codes, patterns, moves));

        if (Reversal(cpu, specials) is { } reversal)
            built.Add(reversal);

        return WithChains(built.Select(move => WithFollowups(move, built)).ToList(), known.HasReverseBeat, rapid);
    }

    [GeneratedRegex(@"(?:\bcmd\.(\w+)\s*<-|^\s*((?:Skill|Atk|Throw)_\w+)\s*=)", RegexOptions.Multiline)]
    private static partial Regex CommandName();

    [GeneratedRegex(@"(A|BorC|B|C|EX|BC|SP)(?:_\w+)?$")]
    private static partial Regex LastButton();

    [GeneratedRegex(@"^(\d)\+?([A-D])$")]
    private static partial Regex DirectionButton();

    [GeneratedRegex(@"^Skill_(J?)(\d+)(A|BorC|B|C|EX|BC|SP)$")]
    private static partial Regex SpecialName();

    [GeneratedRegex(@"^Skill_(\d+(?:A|B))_(JAdd(?:4A|4BorC)?|Add)$")]
    private static partial Regex FollowupName();

    [GeneratedRegex(@"usepat\s*(?:<-|=)\s*""(\w+)""")]
    private static partial Regex UsePattern();

    [GeneratedRegex(@"(?:\[(?<frame>\d+)\]\s*=\s*|itemParam_(?<frame>\d+)\s*=\s*)?\{\s*x=(?<x>-?\d+),\s*y=(?<y>-?\d+),\s*(?:mv|mvname)=""(?:\w*_)?(?<name>[^_""]+)""(?:,\s*pat=""(?<pat>\w+)"")?")]
    private static partial Regex ObjectEntry();

    [GeneratedRegex(@"case\s+(?<frame>\d+):\s*BMvEff\.CreateObject\(\s*\{\s*mvname=""Mv_Obj_(?<name>\w+)"",\s*x=(?<x>-?\d+),\s*y=(?<y>-?\d+)")]
    private static partial Regex FrameObject();

    [GeneratedRegex(@"SetPattern_NotHoldButton\(\s*\{\s*ButtonMask=\(1<<(\d)\),[^}]*SetPattern=""(\w+)""")]
    private static partial Regex HeldPattern();

    private static List<MoveDefinition> Normals(string[] names, string moves, Dictionary<string, int> codes, IReadOnlyDictionary<int, Ha6Pattern> patterns)
    {
        var standard = StandardNormals.Where(normal => patterns.ContainsKey(normal.Pattern)).Select(normal => Normal(normal, moves, codes)).ToList();
        AddCommandNormals(standard, names, moves, codes, patterns);
        return standard;
    }

    private static MoveDefinition Normal((string State, string Input, MoveKind Kind, int Pattern, string MoveName) normal, string moves, Dictionary<string, int> codes)
    {
        var block = MoveBlock(normal.MoveName, moves);
        var held = HeldPattern().Match(block);
        var hasEnd = held.Success && codes.ContainsKey(held.Groups[2].Value);
        var requirements = normal.State is NeutralA or CrouchA or AirA ? new[] { Once } : new[] { Once, NotTheSameMove };

        return new MoveDefinition(normal.State, normal.Input, normal.Kind, hasEnd ? [normal.Pattern, codes[held.Groups[2].Value]] : [normal.Pattern], requirements, [], [])
        {
            MoveName = normal.MoveName,
            HoldButton = hasEnd ? "ABCD"[int.Parse(held.Groups[1].Value)].ToString() : null,
            Objects = FrameObjects(block, codes),
        };
    }

    private static string MoveBlock(string moveName, string moves)
    {
        var start = moves.IndexOf($"t.{moveName} <-", StringComparison.Ordinal);

        if (start < 0)
            return string.Empty;

        var end = moves.IndexOf("\nt.", start + 1, StringComparison.Ordinal);
        return end < 0 ? moves[start..] : moves[start..end];
    }

    private static List<MoveObject> FrameObjects(string block, Dictionary<string, int> codes)
    {
        List<MoveObject> objects = [];

        foreach (Match entry in FrameObject().Matches(block))
        {
            if (codes.ContainsKey(entry.Groups["name"].Value))
                objects.Add(new MoveObject(int.Parse(entry.Groups["frame"].Value), entry.Groups["name"].Value, int.Parse(entry.Groups["x"].Value), int.Parse(entry.Groups["y"].Value)));
        }

        return objects;
    }

    private static void AddCommandNormals(List<MoveDefinition> built, string[] names, string moves, Dictionary<string, int> codes, IReadOnlyDictionary<int, Ha6Pattern> patterns)
    {
        if (names.Contains("Atk_Std6C") && codes.TryGetValue("6C", out var sixC))
            built.Add(new MoveDefinition("NmlAtk6C", "INPUT_6C", MoveKind.StandingNormal, WithEnd("6C", sixC, codes), [Once, NotTheSameMove], [], []) { MoveName = "Mv_Atk_Std6C", Objects = FrameObjects(MoveBlock("Mv_Atk_Std6C", moves), codes) });

        if (names.Contains("Atk_Std3C") && patterns.ContainsKey(FinishPattern))
            built.Add(new MoveDefinition("NmlAtk3C", "INPUT_3C", MoveKind.CrouchingNormal, [FinishPattern], [Once, NotTheSameMove], [], []));

        if (names.Contains("Atk_2C_2C") && codes.TryGetValue("2C_2C", out var repeat))
            built.Add(new MoveDefinition("NmlAtk2C2C", "INPUT_2C", MoveKind.CrouchingNormal, [repeat], [Followup, Once], [], []) { MoveName = "Mv_Atk_2C_2C" });
    }

    private static IEnumerable<MoveDefinition> RapidMoves(IReadOnlyDictionary<int, Ha6Pattern> patterns)
    {
        foreach (var (name, input) in RapidInputs)
            yield return new MoveDefinition(name, input, MoveKind.StandingNormal, [], [Followup, Once, $"SharedGatling({RapidState})", $"StateCall({RapidState})"], [], []) { AliasOf = RapidState };

        yield return new MoveDefinition("Rapid_4A", "INPUT_4A", MoveKind.StandingNormal, [], [Followup, Once, $"SharedGatling({RapidState})", $"StateCall({RapidState})"], [], []) { AliasOf = RapidState };
        yield return new MoveDefinition(RapidState, "INPUT_5A", MoveKind.StandingNormal, [RapidPattern], [Followup, Once], [], []);

        if (!patterns.ContainsKey(FinishPattern))
            yield break;

        foreach (var (name, input) in FinishInputs)
            yield return new MoveDefinition(name, input, MoveKind.StandingNormal, [], [Followup, Once, $"SharedGatling({FinishState})", $"StateCall({FinishState})"], [], []) { AliasOf = FinishState };

        yield return new MoveDefinition(FinishState, "INPUT_5A", MoveKind.StandingNormal, [FinishPattern], [Followup, Once], [], []);
    }

    private static IEnumerable<MoveDefinition> Repeats(List<MoveDefinition> normals)
    {
        if (normals.FirstOrDefault(move => move.State == NeutralA) is { } neutral)
        {
            yield return neutral with { State = BackA, Input = "INPUT_4A", Requirements = [Once, $"SharedGatling({NeutralA})", $"StateCall({NeutralA})"], AliasOf = NeutralA };

            foreach (var repeat in Repeated(neutral, "INPUT_4A"))
                yield return repeat;
        }

        if (normals.FirstOrDefault(move => move.State == CrouchA) is { } crouching)
        {
            foreach (var repeat in Repeated(crouching, crouching.Input))
                yield return repeat;
        }

        if (normals.FirstOrDefault(move => move.State == AirA) is { } air)
        {
            foreach (var repeat in Repeated(air, air.Input))
                yield return repeat;
        }
    }

    private static IEnumerable<MoveDefinition> Repeated(MoveDefinition first, string input)
    {
        for (var repeat = 2; repeat <= RapidRepeats; repeat++)
            yield return first with { State = first.State + (repeat == RapidRepeats ? ThirdSuffix : SecondSuffix), Input = input, Requirements = [Followup, Once] };
    }

    private static void AddThrows(List<MoveDefinition> built, IReadOnlyDictionary<int, Ha6Pattern> patterns, Dictionary<string, int> codes, string[] names)
    {
        if (patterns.ContainsKey(ThrowPattern))
        {
            built.Add(new MoveDefinition("NmlAtkThrow", "INPUT_THROW", MoveKind.Throw, [ThrowPattern], [], [], []) { ThrowResult = "ThrowExe", HitPatterns = ExistingPattern(patterns, ThrowHitPattern) });
            built.Add(new MoveDefinition("NmlAtkBackThrow", "INPUT_BACKTHROW", MoveKind.Throw, [ThrowPattern], [], [], []) { ThrowResult = "BackThrowExe", HitPatterns = ExistingPattern(patterns, ThrowHitPattern) });
        }

        if (!names.Contains("Throw_A") || !patterns.ContainsKey(AirThrowPattern))
            return;

        built.Add(new MoveDefinition(AirThrowState, SpecialInput, MoveKind.AirNormal, [AirThrowPattern], [Air, "Move_Input_(INPUT_PRESS_D)", "Move_Input_(INPUT_HOLD_A)", Once], [], [])
        {
            HitPatterns = ExistingPattern(patterns, AirThrowHitPattern),
        });
    }

    private static List<int> ExistingPattern(IReadOnlyDictionary<int, Ha6Pattern> patterns, int pattern) => patterns.ContainsKey(pattern) ? [pattern] : [];

    private static List<int> WithEnd(string code, int pattern, Dictionary<string, int> codes) =>
        codes.TryGetValue(code + EndSuffix, out var end) ? [pattern, end] : [pattern];

    private static IEnumerable<MoveDefinition> FromCommand(string name, NativeCommand command, string moves, Dictionary<string, int> codes)
    {
        var moveName = command.MoveName ?? MoveNameOf(name);
        var button = ButtonOf(name, command);
        var inputs = InputsOf(name, command, button);

        if (inputs.Length == 0 || name.EndsWith("_Hit", StringComparison.Ordinal) || CodeOf(moveName, moves, codes) is not { } code)
            yield break;

        var isFollowup = command.OnlyFrom.Count > 0;
        var inAir = command.InAir || name.StartsWith("Skill_J", StringComparison.Ordinal);
        var isSuper = button == ButtonSuper;
        var isNormal = name.StartsWith("Atk_", StringComparison.Ordinal) && !command.Inputs.Any(input => input.EndsWith(BothButtonsInput, StringComparison.Ordinal));
        var holds = !isSuper && !isFollowup && button is "A" or ButtonEitherBOrC && codes.ContainsKey(code + EndSuffix);
        var conditions = isFollowup || isNormal ? [] : Conditions(inAir, button).ToArray();
        string[] marks = isFollowup ? [Followup] : [];

        var move = new MoveDefinition(
            name,
            isNormal ? NormalInput(command, inAir) : isSuper ? SuperInput : SpecialInput,
            isNormal ? (inAir ? MoveKind.AirNormal : MoveKind.StandingNormal) : isSuper ? MoveKind.Super : inAir ? MoveKind.AirSpecial : MoveKind.StandingSpecial,
            holds ? WithEnd(code, codes[code], codes) : [codes[code]],
            isNormal ? [.. marks, Once] : [.. conditions, .. inputs[0], .. marks],
            [],
            [])
        {
            MoveName = moveName,
            ParentMoves = command.OnlyFrom,
            Objects = ObjectsOfMove(moveName, moves, codes),
            HoldButton = holds ? ReadableButton(button) : null,
            HitPatterns = isSuper ? HitBranch(code, codes) : OwnHit(code, codes),
            MeterCost = button switch { ButtonEx => ExMeterCost, ButtonBoth => SkillMeterCost, _ => 0 },
            Flashes = button is ButtonEx or ButtonBoth,
        };

        if (!isNormal)
        {
            foreach (var (extra, index) in inputs.Skip(1).Select((input, index) => (input, index)))
                yield return move with { State = $"{name}_Alt{index}", Requirements = [.. conditions, .. extra, .. marks, $"StateCall({name})"], AliasOf = name };
        }

        yield return move;
    }

    private static IReadOnlyList<string>[] InputsOf(string name, NativeCommand command, string button)
    {
        if (button == ButtonSuper)
            return [[.. DefaultInput("236", ButtonBoth)]];

        if (IsAnyButton(command.Inputs))
            return [[$"Move_Input_({AnyAttackButton})"]];

        var inputs = NativeInputs(command);

        if (inputs.Length > 0)
            return inputs;

        return SpecialName().Match(name) is { Success: true } special ? [[.. DefaultInput(special.Groups[2].Value, special.Groups[3].Value)]] : [];
    }

    private static string? CodeOf(string moveName, string moves, Dictionary<string, int> codes)
    {
        var block = TemplateCase(moveName, moves) ?? MoveBlock(Regex.Match(moves, $@"t\.{moveName}\s*<-\s*t\.(\w+)\s*;") is { Success: true } alias ? alias.Groups[1].Value : moveName, moves);

        if (UsePattern().Match(block) is { Success: true } used && codes.ContainsKey(used.Groups[1].Value))
            return used.Groups[1].Value;

        var plain = moveName.Replace("Mv_Skill_", string.Empty, StringComparison.Ordinal).Replace("Mv_Atk_Std", string.Empty, StringComparison.Ordinal).Replace("Mv_Atk_", string.Empty, StringComparison.Ordinal);
        string[] candidates = [plain, plain.Replace("Cro", string.Empty, StringComparison.Ordinal), plain.Replace("Air", "J", StringComparison.Ordinal), plain.Replace("BandC", "B+C", StringComparison.Ordinal), plain.Replace("CroBandC", "2+B+C", StringComparison.Ordinal)];
        return Array.Find(candidates, codes.ContainsKey);
    }

    private static string ButtonOf(string name, NativeCommand command)
    {
        if (command.Cost >= SuperGauge)
            return ButtonSuper;

        if (command.Cost > 0)
            return ButtonEx;

        return LastButton().Match(name) is { Success: true } button ? button.Groups[1].Value : string.Empty;
    }

    private static string NormalInput(NativeCommand command, bool inAir)
    {
        var input = command.Inputs.FirstOrDefault(item => item.StartsWith('2')) ?? command.Inputs[0];
        var direction = DirectionButton().Match(input) is { Success: true } held ? held.Groups[1].Value : "5";
        var button = input[^1];
        return $"INPUT_{(inAir ? "J" : string.Empty)}{direction}{button}";
    }

    private static IEnumerable<MoveDefinition> Special(string name, string moves, Dictionary<string, int> codes, NativeCommandTable table)
    {
        if (HandledByNormals.Contains(name))
            return [];

        if (table.Find(name) is { } command)
            return FromCommand(name, command, moves, codes);

        if (FollowupName().Match(name) is { Success: true } followup)
            return FollowupMove(name, followup, moves, codes, table);

        return SpecialName().Match(name) is { Success: true } special ? Motion(name, special, moves, codes, table) : [];
    }

    private static IEnumerable<MoveDefinition> Motion(string name, Match special, string moves, Dictionary<string, int> codes, NativeCommandTable table)
    {
        var inAir = special.Groups[1].Length > 0;
        var motion = special.Groups[2].Value;
        var button = special.Groups[3].Value;
        var code = PatternCode(name, moves, $"{special.Groups[1].Value}{motion}{ReadableButton(button)}");

        if (!codes.TryGetValue(code, out var pattern))
            yield break;

        var isSuper = button == ButtonSuper;
        var holds = !isSuper && button is "A" or ButtonEitherBOrC && codes.ContainsKey(code + EndSuffix);
        var inputs = table.Find(name) is { } command ? NativeInputs(command) : [];
        var first = inputs.Length > 0 ? inputs[0] : DefaultInput(motion, button);

        var move = new MoveDefinition(
            name,
            isSuper ? SuperInput : SpecialInput,
            isSuper ? MoveKind.Super : inAir ? MoveKind.AirSpecial : MoveKind.StandingSpecial,
            isSuper ? [pattern] : holds ? WithEnd(code, pattern, codes) : [pattern],
            Conditions(inAir, button).Concat(first).ToArray(),
            [],
            [])
        {
            Objects = ObjectsOf(name, moves, codes),
            HoldButton = holds ? ReadableButton(button) : null,
            HitPatterns = isSuper ? HitBranch(code, codes) : OwnHit(code, codes),
            MeterCost = button switch { ButtonEx => ExMeterCost, ButtonBoth => SkillMeterCost, _ => 0 },
            Flashes = button is ButtonEx or ButtonBoth,
        };

        foreach (var (extra, index) in inputs.Skip(1).Select((input, index) => (input, index)))
            yield return move with { State = $"{name}_Alt{index}", Requirements = Conditions(inAir, button).Concat(extra).Append($"StateCall({name})").ToArray(), AliasOf = name };

        yield return move;
    }

    private static IEnumerable<string> DefaultInput(string motion, string button)
    {
        yield return $"Move_Input_(INPUT_{(motion == DoubleDown ? NativeDoubleDown : motion)})";
        yield return $"Move_Input_({ButtonInput(button)})";
    }

    private static IEnumerable<string> Conditions(bool inAir, string button)
    {
        yield return inAir ? Air : Ground;

        if (button == ButtonEx)
            yield return ExCost;

        if (button == ButtonBoth)
            yield return SkillCost;

        if (button == ButtonSuper)
            yield return SuperCost;
    }

    private static List<int> OwnHit(string code, Dictionary<string, int> codes) => codes.TryGetValue(code + HitSuffix, out var hit) ? [hit] : [];

    private static List<int> HitBranch(string code, Dictionary<string, int> codes)
    {
        List<int> branch = [];

        if (codes.TryGetValue(code + HitSuffix, out var hit))
            branch.Add(hit);

        if (codes.TryGetValue(code + EndSuffix, out var end))
            branch.Add(end);

        return branch;
    }

    private static IEnumerable<MoveDefinition> FollowupMove(string name, Match followup, string moves, Dictionary<string, int> codes, NativeCommandTable table)
    {
        var code = PatternCode(name, moves, name["Skill_".Length..].Replace(ButtonEitherBOrC, "B", StringComparison.Ordinal));

        if (!codes.TryGetValue(code, out var pattern))
            yield break;

        var parent = "Skill_" + followup.Groups[1].Value;
        var command = table.Find(name);
        var inputs = command is { Inputs.Count: > 0 } && !IsAnyButton(command.Inputs) ? NativeInputs(command) : [];
        var kind = command is { InAir: false } ? MoveKind.StandingSpecial : MoveKind.AirSpecial;
        var first = inputs.Length > 0 ? inputs[0] : [$"Move_Input_({AnyAttackButton})"];

        var move = new MoveDefinition(name, SpecialInput, kind, [pattern], [.. first, Followup], [], [])
        {
            Objects = ObjectsOf(name, moves, codes),
            Parent = parent,
        };

        foreach (var (extra, index) in inputs.Skip(1).Select((input, index) => (input, index)))
            yield return move with { State = $"{name}_Alt{index}", Requirements = [.. extra, Followup, $"StateCall({name})"], AliasOf = name };

        yield return move;
    }

    private static bool IsAnyButton(IReadOnlyList<string> inputs) => inputs is ["A", "B", "C"];

    private static IReadOnlyList<string>[] NativeInputs(NativeCommand command) =>
        command.Inputs.Select(CommandInput.Requirements).OfType<IReadOnlyList<string>>().DistinctBy(input => string.Join('|', input)).ToArray();

    private static string ReadableButton(string button) => button == ButtonEitherBOrC ? "B" : button;

    private static string ButtonInput(string button) => button switch
    {
        "A" => "INPUT_PRESS_A",
        ButtonEitherBOrC or "B" => "INPUT_PRESS_B",
        "C" or ButtonEx => "INPUT_PRESS_C",
        _ => "INPUT_PRESS_BC",
    };

    private static string PatternCode(string name, string moves, string fallback)
    {
        var moveName = MoveNameOf(name);
        var block = TemplateCase(moveName, moves) ?? MoveBlock(Regex.Match(moves, $@"t\.{moveName}\s*<-\s*t\.(\w+)\s*;") is { Success: true } alias ? alias.Groups[1].Value : moveName, moves);
        return UsePattern().Match(block) is { Success: true } used ? used.Groups[1].Value : fallback;
    }

    private static string MoveNameOf(string name) => name.Replace("Skill_", "Mv_Skill_", StringComparison.Ordinal).Replace(ButtonEitherBOrC, "B", StringComparison.Ordinal);

    private static string? TemplateCase(string moveName, string moves)
    {
        if (Regex.Match(moves, $@"t\.{moveName}\s*<-\s*(\w+)\(\s*\{{\s*type=""(\w+)""") is not { Success: true } use)
            return null;

        var template = moves.IndexOf($"local {use.Groups[1].Value} =", StringComparison.Ordinal);
        var start = template < 0 ? -1 : moves.IndexOf($"case \"{use.Groups[2].Value}\":", template, StringComparison.Ordinal);

        if (start < 0)
            return null;

        var end = moves.IndexOf("break;", start, StringComparison.Ordinal);
        return end < 0 ? moves[start..] : moves[start..end];
    }

    private static List<MoveObject> ObjectsOf(string name, string moves, Dictionary<string, int> codes) => ObjectsOfMove(MoveNameOf(name), moves, codes);

    private static List<MoveObject> ObjectsOfMove(string moveName, string moves, Dictionary<string, int> codes)
    {
        if (TemplateCase(moveName, moves) is not { } block)
            return [];

        List<MoveObject> objects = [];

        foreach (Match entry in ObjectEntry().Matches(block))
        {
            var frame = entry.Groups["frame"].Success ? int.Parse(entry.Groups["frame"].Value) : DefaultObjectFrame;
            var code = entry.Groups["pat"].Success ? entry.Groups["pat"].Value : entry.Groups["name"].Value;

            if (codes.ContainsKey(code))
                objects.Add(new MoveObject(frame, code, int.Parse(entry.Groups["x"].Value), int.Parse(entry.Groups["y"].Value)));
        }

        return objects;
    }

    private static IEnumerable<MoveDefinition> ArcFinish(string[] names, IReadOnlyDictionary<int, Ha6Pattern> patterns, Dictionary<string, int> codes)
    {
        if (!names.Contains("Skill_LastArcBlast") || !codes.TryGetValue(LastArcCode, out var hit))
            yield break;

        List<int> frames = [hit];
        frames.AddRange(Enumerable.Range(LastArcFirstFollowUp, LastArcLastFollowUp - LastArcFirstFollowUp + 1).Where(patterns.ContainsKey));

        if (codes.TryGetValue(LastArcEndCode, out var end))
            frames.Add(end);

        yield return new MoveDefinition("AstralHeat", AstralInput, MoveKind.Super, frames, [Ground, AstralCost, "Move_Input_(INPUT_222)", "Move_Input_(INPUT_PRESS_BC)"], [], []);
    }

    private static IEnumerable<MoveDefinition> AssistMoves(Dictionary<string, int> codes, IReadOnlyDictionary<int, Ha6Pattern> patterns, string moves)
    {
        foreach (var (state, code, fallback) in Assists)
        {
            var pattern = codes.TryGetValue(code, out var own) ? own : fallback;

            if (!patterns.ContainsKey(pattern))
                continue;

            yield return new MoveDefinition(state, SpecialInput, MoveKind.StandingSpecial, [pattern], [], [], [])
            {
                ReplacesCommonState = true,
                Objects = ObjectsOfMove("PatMv_" + code["P_".Length..], moves, codes),
            };
        }
    }

    private static MoveDefinition? Reversal(string cpu, List<MoveDefinition> specials)
    {
        var entry = Regex.Match(cpu, $@"\b{CpuInvincibleEntry}\s*=\s*def_CN_(Skill_\w+)");

        if (!entry.Success)
            return null;

        var native = entry.Groups[1].Value;
        var move = specials.FirstOrDefault(special => special.State == native);

        return move is null ? null : new MoveDefinition(ReversalState, "INPUT_REVERSAL", MoveKind.Reversal, move.Patterns, [], [], [])
        {
            ReplacesCommonState = true,
            HitPatterns = move.HitPatterns,
            Objects = move.Objects,
        };
    }

    private static MoveDefinition WithFollowups(MoveDefinition move, List<MoveDefinition> moves)
    {
        var parent = move.State.Replace(ButtonEitherBOrC, "B", StringComparison.Ordinal);
        var followups = moves
            .Where(other => other.Parent == parent || (move.MoveName is not null && other.ParentMoves.Contains(move.MoveName)))
            .Select(other => other.AliasOf is null ? other.State : $"'{other.State}'")
            .ToArray();
        return followups.Length == 0 ? move : move with { Followups = followups };
    }

    private static List<MoveDefinition> WithChains(List<MoveDefinition> moves, bool reverseBeat, bool rapid)
    {
        var known = moves.Select(move => move.State).ToHashSet();
        var aliases = moves.Where(move => !move.HasBody).Select(move => move.State).ToHashSet();
        return moves.Select(move => move.HasBody ? move with { Cancels = CancelsOf(move.State, known, reverseBeat, rapid).Select(cancel => Reference(cancel, aliases)).ToArray() } : move).ToList();
    }

    private static string Reference(string state, HashSet<string> aliases) => aliases.Contains(state) ? $"'{state}'" : state;

    private static string[] CancelsOf(string state, HashSet<string> known, bool reverseBeat, bool rapid)
    {
        if (state == RapidState)
            return known.Contains(FinishState) ? [FinishState, .. FinishInputs.Select(finish => finish.Name)] : [];

        var family = FamilyOf(state);
        var chain = AirChain.Any(tier => tier.Contains(family)) ? AirChain : GroundChain;
        var tier = Array.FindIndex(chain, names => names.Contains(family));

        if (tier < 0)
            return [];

        var others = reverseBeat ? chain.SelectMany(names => names).Where(name => name != family) : Later(chain, tier);
        List<string> cancels = others.Where(known.Contains).ToList();

        if (state == CrouchC && known.Contains("NmlAtk2C2C"))
            cancels.Add("NmlAtk2C2C");

        if (reverseBeat && chain == GroundChain && known.Contains(BackA) && family != NeutralA)
            cancels.Add(BackA);

        if (known.Contains(NextRepeat(state, family)))
            cancels.Add(NextRepeat(state, family));

        if (rapid && chain == GroundChain && family is not ("NmlAtk6C" or "NmlAtk3C"))
            cancels.AddRange(RapidInputs.Select(input => input.Name).Append(RapidState).Where(known.Contains));

        if (rapid && state == NeutralA + ThirdSuffix)
            cancels.Add("Rapid_4A");

        return cancels.ToArray();
    }

    private static IEnumerable<string> Later(string[][] chain, int tier) => chain.Skip(tier + (tier == 0 ? 0 : 1)).SelectMany(names => names);

    private static string FamilyOf(string state) => state.EndsWith(SecondSuffix, StringComparison.Ordinal) || state.EndsWith(ThirdSuffix, StringComparison.Ordinal) ? state[..^SecondSuffix.Length] : state;

    private static string NextRepeat(string state, string family) => state == family ? family + SecondSuffix : state == family + SecondSuffix ? family + ThirdSuffix : string.Empty;
}
