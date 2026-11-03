using System.Runtime.InteropServices;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Dev;

internal static unsafe class VisualStudioAutomation
{
    private const int QueryInterfaceSlot = 0;
    private const int ReleaseSlot = 2;
    private const int NextSlot = 3;
    private const int GetObjectSlot = 6;
    private const int EnumRunningSlot = 9;
    private const int GetDisplayNameSlot = 20;
    private const int GetIdsOfNamesSlot = 5;
    private const int InvokeSlot = 6;
    private const int Succeeded = 0;
    private const int CallRejected = unchecked((int)0x80010001);
    private const int RetryLater = unchecked((int)0x8001010A);
    private const int Attempts = 20;
    private const uint UserLocale = 0x400;

    private static readonly Guid DispatchInterface = new("00020400-0000-0000-C000-000000000046");
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(250);

    public static bool OpenFile(string solution, string file)
    {
        var instance = FindSolution(solution);

        if (instance == 0)
            return false;

        try
        {
            var operations = Object(instance, "ItemOperations");

            if (operations == 0)
                return false;

            try
            {
                var opened = Invoke(operations, "OpenFile", OleAut32.MethodCall, [file], out var window) >= 0;
                OleAut32.VariantClear(ref window);
                BringToFront(instance);
                return opened;
            }
            finally
            {
                Release(operations);
            }
        }
        finally
        {
            Release(instance);
        }
    }

    private static nint FindSolution(string solution)
    {
        if (Ole32.GetRunningObjectTable(0, out var table) < 0)
            return 0;

        nint monikers = 0;
        nint context = 0;

        try
        {
            if (Call(table, EnumRunningSlot, &monikers) < 0 || Ole32.CreateBindCtx(0, out context) < 0)
                return 0;

            while (Next(monikers) is { } moniker)
            {
                try
                {
                    if (Automation(table, context, moniker) is not { } instance)
                        continue;

                    if (HasSolution(instance, solution))
                        return instance;

                    Release(instance);
                }
                finally
                {
                    Release(moniker);
                }
            }

            return 0;
        }
        finally
        {
            Release(context);
            Release(monikers);
            Release(table);
        }
    }

    private static nint? Automation(nint table, nint context, nint moniker)
    {
        if (!ScriptEditorProgram.IsAutomationName(DisplayName(context, moniker)))
            return null;

        nint unknown;
        var getObject = (delegate* unmanaged[Stdcall]<nint, nint, nint*, int>)Method(table, GetObjectSlot);

        if (getObject(table, moniker, &unknown) < 0 || unknown == 0)
            return null;

        try
        {
            nint dispatch;
            var iid = DispatchInterface;
            var query = (delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)Method(unknown, QueryInterfaceSlot);
            return query(unknown, &iid, &dispatch) == Succeeded ? dispatch : null;
        }
        finally
        {
            Release(unknown);
        }
    }

    private static bool HasSolution(nint instance, string solution)
    {
        var loaded = Object(instance, "Solution");

        try
        {
            return loaded != 0 && ScriptEditorProgram.IsSolution(Text(loaded, "FullName"), solution);
        }
        finally
        {
            Release(loaded);
        }
    }

    private static void BringToFront(nint instance)
    {
        var window = Object(instance, "MainWindow");

        if (window == 0)
            return;

        try
        {
            Invoke(window, "Activate", OleAut32.MethodCall, [], out var result);
            OleAut32.VariantClear(ref result);

            if (Invoke(window, "HWnd", OleAut32.PropertyGet, [], out var handle) >= 0)
                User32.SetForegroundWindow(handle.Value);
        }
        finally
        {
            Release(window);
        }
    }

    private static nint Object(nint dispatch, string property)
    {
        if (Invoke(dispatch, property, OleAut32.PropertyGet, [], out var result) < 0)
            return 0;

        if (result.Type == OleAut32.ObjectType)
            return result.Value;

        OleAut32.VariantClear(ref result);
        return 0;
    }

    private static string? Text(nint dispatch, string property)
    {
        if (Invoke(dispatch, property, OleAut32.PropertyGet, [], out var result) < 0)
            return null;

        try
        {
            return result.Type == OleAut32.TextType && result.Value != 0 ? new string((char*)result.Value) : null;
        }
        finally
        {
            OleAut32.VariantClear(ref result);
        }
    }

    private static int Invoke(nint dispatch, string name, ushort kind, ReadOnlySpan<string> arguments, out Variant result)
    {
        result = default;
        var noInterface = Guid.Empty;
        int id;

        fixed (char* text = name)
        {
            var names = text;
            var getIds = (delegate* unmanaged[Stdcall]<nint, Guid*, char**, uint, uint, int*, int>)Method(dispatch, GetIdsOfNamesSlot);
            var found = getIds(dispatch, &noInterface, &names, 1, UserLocale, &id);

            if (found < 0)
                return found;
        }

        var values = stackalloc Variant[Math.Max(arguments.Length, 1)];

        for (var index = 0; index < arguments.Length; index++)
            values[arguments.Length - 1 - index] = new Variant { Type = OleAut32.TextType, Value = OleAut32.AllocateText(arguments[index]) };

        var parameters = new DispatchParameters { Arguments = (nint)values, Count = (uint)arguments.Length };

        try
        {
            fixed (Variant* output = &result)
                return InvokeWithRetry(dispatch, id, kind, &parameters, output);
        }
        finally
        {
            for (var index = 0; index < arguments.Length; index++)
                OleAut32.VariantClear(ref values[index]);
        }
    }

    private static int InvokeWithRetry(nint dispatch, int id, ushort kind, DispatchParameters* parameters, Variant* result)
    {
        var noInterface = Guid.Empty;
        var invoke = (delegate* unmanaged[Stdcall]<nint, int, Guid*, uint, ushort, DispatchParameters*, Variant*, nint, nint, int>)Method(dispatch, InvokeSlot);
        var outcome = invoke(dispatch, id, &noInterface, UserLocale, kind, parameters, result, 0, 0);

        for (var attempt = 1; attempt < Attempts && outcome is CallRejected or RetryLater; attempt++)
        {
            Thread.Sleep(RetryDelay);
            outcome = invoke(dispatch, id, &noInterface, UserLocale, kind, parameters, result, 0, 0);
        }

        return outcome;
    }

    private static string DisplayName(nint context, nint moniker)
    {
        char* name = null;
        var get = (delegate* unmanaged[Stdcall]<nint, nint, nint, char**, int>)Method(moniker, GetDisplayNameSlot);

        if (get(moniker, context, 0, &name) < 0 || name == null)
            return string.Empty;

        try
        {
            return new string(name);
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)name);
        }
    }

    private static nint? Next(nint monikers)
    {
        nint moniker;
        uint fetched;
        var next = (delegate* unmanaged[Stdcall]<nint, uint, nint*, uint*, int>)Method(monikers, NextSlot);

        return next(monikers, 1, &moniker, &fetched) == Succeeded && fetched == 1 ? moniker : null;
    }

    private static int Call(nint instance, int slot, nint* output) => ((delegate* unmanaged[Stdcall]<nint, nint*, int>)Method(instance, slot))(instance, output);

    private static void Release(nint instance)
    {
        if (instance != 0)
            ((delegate* unmanaged[Stdcall]<nint, uint>)Method(instance, ReleaseSlot))(instance);
    }

    private static nint Method(nint instance, int slot) => (*(nint**)instance)[slot];
}
