namespace BBTAG_IM.Core.Scripting.Live;

public static class ScriptMapHash
{
    public static uint Of(string name)
    {
        if (name.Length == 0)
            return 0;

        return unchecked((uint)(Letter(name[0]) + Letter(name[name.Length / 2]) + Letter(name[^1])));
    }

    private static int Letter(char letter) => unchecked((sbyte)(byte)letter);
}
