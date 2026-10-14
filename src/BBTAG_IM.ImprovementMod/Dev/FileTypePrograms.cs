using System.Runtime.InteropServices;
using BBTAG_IM.Core.Scripting.Dev;
using BBTAG_IM.Native;

namespace BBTAG_IM.ImprovementMod.Dev;

internal static unsafe class FileTypePrograms
{
    private const int ReleaseSlot = 2;
    private const int NextSlot = 3;
    private const int GetNameSlot = 3;
    private const int GetDisplayNameSlot = 4;
    private const int Succeeded = 0;

    public static IReadOnlyList<ScriptEditorChoice> For(string extension)
    {
        if (Shell32.EnumerateHandlers(extension, Shell32.EveryHandler, out var handlers) < 0 || handlers == 0)
            return [];

        try
        {
            var programs = new List<ScriptEditorChoice>();

            while (Next(handlers) is { } handler)
            {
                try
                {
                    programs.Add(new ScriptEditorChoice(Text(handler, GetDisplayNameSlot), Text(handler, GetNameSlot)));
                }
                finally
                {
                    Release(handler);
                }
            }

            return programs;
        }
        finally
        {
            Release(handlers);
        }
    }

    private static nint? Next(nint handlers)
    {
        nint handler;
        uint fetched;
        var next = (delegate* unmanaged[Stdcall]<nint, uint, nint*, uint*, int>)Method(handlers, NextSlot);

        return next(handlers, 1, &handler, &fetched) == Succeeded && fetched == 1 ? handler : null;
    }

    private static string Text(nint handler, int slot)
    {
        char* text = null;
        var get = (delegate* unmanaged[Stdcall]<nint, char**, int>)Method(handler, slot);

        if (get(handler, &text) < 0 || text == null)
            return string.Empty;

        try
        {
            return new string(text);
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)text);
        }
    }

    private static void Release(nint instance) => ((delegate* unmanaged[Stdcall]<nint, uint>)Method(instance, ReleaseSlot))(instance);

    private static nint Method(nint instance, int slot) => (*(nint**)instance)[slot];
}
