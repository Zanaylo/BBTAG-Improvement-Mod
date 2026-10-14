using BBTAG_IM.Launcher.Native;

namespace BBTAG_IM.Launcher.Debugging;

internal sealed unsafe class DispatchObject(nint instance) : ComObject(instance)
{
    public static readonly Guid InterfaceId = new("00020400-0000-0000-C000-000000000046");

    private const int GetIdsOfNamesSlot = 5;
    private const int InvokeSlot = 6;
    private const ushort CallMethod = 1;
    private const ushort GetProperty = 2;
    private const int CallRejected = unchecked((int)0x80010001);
    private const int RetryLater = unchecked((int)0x8001010A);
    private const int MostRetries = 100;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    public DispatchObject Object(string name) => ToObject(Invoke(name, GetProperty, null));

    public DispatchObject Item(int index)
    {
        var argument = new Variant { Type = Variant.Integer, IntegerValue = index };
        return ToObject(Invoke("Item", CallMethod | GetProperty, &argument));
    }

    public int Integer(string name)
    {
        var result = Invoke(name, GetProperty, null);

        if (result.Type == Variant.Integer)
            return result.IntegerValue;

        OleMethods.VariantClear(&result);
        throw new AutomationException($"{name} is not a number.");
    }

    public void Call(string name)
    {
        var result = Invoke(name, CallMethod, null);
        OleMethods.VariantClear(&result);
    }

    private static DispatchObject ToObject(Variant result)
    {
        if (result.Type == Variant.Dispatch && result.PointerValue != 0)
            return new DispatchObject(result.PointerValue);

        OleMethods.VariantClear(&result);
        throw new AutomationException("Visual Studio did not return an object.");
    }

    private Variant Invoke(string name, ushort flags, Variant* argument)
    {
        var id = IdOf(name);
        var noInterface = Guid.Empty;
        var parameters = new DispatchParameters { Arguments = argument, ArgumentCount = argument == null ? 0u : 1u };
        var invoke = (delegate* unmanaged[Stdcall]<nint, int, Guid*, uint, ushort, DispatchParameters*, Variant*, nint, uint*, int>)Method(InvokeSlot);
        Variant result = default;
        uint failedArgument;

        for (var attempt = 1; ShouldRetry(invoke(Instance, id, &noInterface, 0, flags, &parameters, &result, 0, &failedArgument), attempt); attempt++)
            Thread.Sleep(RetryDelay);

        return result;
    }

    private int IdOf(string name)
    {
        var noInterface = Guid.Empty;
        var getIds = (delegate* unmanaged[Stdcall]<nint, Guid*, char**, uint, uint, int*, int>)Method(GetIdsOfNamesSlot);
        int id;

        fixed (char* text = name)
        {
            var names = text;

            for (var attempt = 1; ShouldRetry(getIds(Instance, &noInterface, &names, 1, 0, &id), attempt); attempt++)
                Thread.Sleep(RetryDelay);
        }

        return id;
    }

    private static bool ShouldRetry(int result, int attempt)
    {
        if (result is (CallRejected or RetryLater) && attempt < MostRetries)
            return true;

        AutomationException.ThrowIfFailed(result);
        return false;
    }
}
