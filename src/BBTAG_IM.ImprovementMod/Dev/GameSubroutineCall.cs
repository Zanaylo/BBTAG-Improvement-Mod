using System.Text;
using BBTAG_IM.Core.Scripting;
using BBTAG_IM.Core.Scripting.Live;

namespace BBTAG_IM.ImprovementMod.Dev;

internal sealed unsafe class GameSubroutineCall(uint callSubroutine) : ISubroutineCall
{
    public void Call(uint entity, string subroutine)
    {
        var name = stackalloc byte[ScriptBinary.NameLength];
        new Span<byte>(name, ScriptBinary.NameLength).Clear();
        Encoding.Latin1.GetBytes(subroutine.AsSpan(0, Math.Min(subroutine.Length, ScriptBinary.NameLength - 1)), new Span<byte>(name, ScriptBinary.NameLength));
        ((delegate* unmanaged[Thiscall]<nint, byte*, void>)callSubroutine)((nint)entity, name);
    }
}
