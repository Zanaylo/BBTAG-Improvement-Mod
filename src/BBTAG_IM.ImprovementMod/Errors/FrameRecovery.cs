using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BBTAG_IM.Core.Game;
using BBTAG_IM.Core.Logging;
using BBTAG_IM.Core.Memory;
using BBTAG_IM.ImprovementMod.Frames;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Errors;

internal static unsafe class FrameRecovery
{
    private const uint CallFirst = 1;
    private const int ContinueSearch = 0;
    private const int ContinueExecution = -1;
    private const int ExceptionCodeField = 0;
    private const int ExceptionAddressField = 12;
    private const int ContextRecordField = 4;
    private const int InstructionPointerField = 0xB8;
    private const int Armed = 1;
    private const int RecoveriesPerMatch = 120;

    private static readonly RecoveryBudget Budget = new(RecoveriesPerMatch);
    private static readonly RecoveryReports Reports = new();

    private static CodeCave[] s_innermostFirst = [];
    private static MatchExit s_matchExit = null!;
    private static GameView s_game = null!;
    private static ILog s_log = null!;
    private static uint s_moduleStart;
    private static uint s_moduleEnd;
    private static long s_matchNumber;
    private static bool s_wasInMatch;

    public static void Install(CodeImage code, LoadedModule module, GameView game, MatchExit matchExit, ILog log)
    {
        s_game = game;
        s_matchExit = matchExit;
        s_log = log;
        s_moduleStart = (uint)module.BaseAddress;
        s_moduleEnd = s_moduleStart + module.Headers.SizeOfImage;

        var scene = Prepare(code, RecoveryPoints.SceneStep);
        var script = Prepare(code, RecoveryPoints.ObjectScript);

        if (Kernel32.AddVectoredExceptionHandler(CallFirst, (nint)(delegate* unmanaged[Stdcall]<nint, int>)&Handler) == 0)
            throw new PatchFailedException("The error recovery handler could not be registered.");

        s_innermostFirst = [script.Cave, scene.Cave];
        CodeCave.Redirect(scene.Site, RecoveryPointCode.Jump(RecoveryPoints.SceneStep, scene.Site, scene.Cave.Address));
        CodeCave.Redirect(script.Site, RecoveryPointCode.Jump(RecoveryPoints.ObjectScript, script.Site, script.Cave.Address));
    }

    public static uint? AddMessageBoxThunk(nint managedHandler)
    {
        if (s_innermostFirst.Length == 0)
            return null;

        var cave = s_innermostFirst[^1];
        cave.Put(RecoveryPointCode.ThunkOffset, RecoveryPointCode.MessageBoxThunk((uint)managedHandler));
        return cave.Address + RecoveryPointCode.ThunkOffset;
    }

    public static void OnPresent()
    {
        var inMatch = s_game.State == GameState.Match;

        if (inMatch && !s_wasInMatch)
            s_matchNumber++;

        s_wasInMatch = inMatch;
    }

    public static uint? ResumeIfAllowed()
    {
        if (InnermostArmed() is not { } cave || !Budget.TrySpend(s_matchNumber))
            return null;

        return cave.Address + RecoveryPointCode.ResumeOffset;
    }

    private static (uint Site, CodeCave Cave) Prepare(CodeImage code, RecoveryPoint point)
    {
        var site = RecoveryPointCode.Locate(code, point);
        var target = point.Call == RecoveryCall.Direct ? RecoveryPointCode.CallTargetOf(code, point, site) : 0;
        var cave = CodeCave.Reserve(RecoveryPointCode.BlockLength, point.Name);
        cave.Put(RecoveryPointCode.ResumeOffset, RecoveryPointCode.Code(point, site, cave.Address, target));
        return (site, cave);
    }

    private static CodeCave? InnermostArmed()
    {
        var thread = Kernel32.GetCurrentThreadId();

        foreach (var cave in s_innermostFirst)
        {
            var block = (byte*)cave.Address;

            if (*(int*)(block + RecoveryPointCode.ArmedOffset) == Armed && *(uint*)(block + RecoveryPointCode.ThreadOffset) == thread)
                return cave;
        }

        return null;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Handler(nint pointers)
    {
        var record = *(nint*)pointers;
        var context = *(nint*)(pointers + ContextRecordField);
        var code = *(uint*)(record + ExceptionCodeField);
        var address = *(uint*)(record + ExceptionAddressField);

        if (!FatalErrorPolicy.CanRecover(code, address, s_moduleStart, s_moduleEnd, InnermostArmed() is not null) || !LeftMatch())
            return ContinueSearch;

        if (ResumeIfAllowed() is not { } resume)
        {
            SafeLog.Error(s_log, $"The game hit an {FatalErrorPolicy.Describe(code, address, s_moduleStart)} again. The match did not end after {RecoveriesPerMatch} skipped steps, so the game closes.");
            return ContinueSearch;
        }

        if (Reports.IsFirst(s_matchNumber, address))
            SafeLog.Error(s_log, $"The game hit an {FatalErrorPolicy.Describe(code, address, s_moduleStart)} during a match. The game was not closed: that step was skipped and the match goes back to character select.");

        *(uint*)(context + InstructionPointerField) = resume;
        return ContinueExecution;
    }

    private static bool LeftMatch()
    {
        try
        {
            return s_matchExit.ToCharacterSelect();
        }
        catch (Exception)
        {
            return false;
        }
    }
}
