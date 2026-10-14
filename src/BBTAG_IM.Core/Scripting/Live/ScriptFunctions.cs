namespace BBTAG_IM.Core.Scripting.Live;

public readonly record struct FunctionKey(int Start, string Name, int Occurrence);

public readonly record struct ScriptFunction(FunctionKey Key, int Offset);

public static class ScriptFunctions
{
    public static IReadOnlyList<ScriptFunction> Of(ScriptBinary script)
    {
        var functions = new List<ScriptFunction>();
        var occurrences = new Dictionary<(int, string), int>();

        foreach (var instruction in script.Instructions)
        {
            if (!ScriptCommands.StartsFunction(instruction.Id))
                continue;

            var name = instruction.TextAt(0);
            var occurrence = occurrences.GetValueOrDefault((instruction.Id, name));
            occurrences[(instruction.Id, name)] = occurrence + 1;
            functions.Add(new ScriptFunction(new FunctionKey(instruction.Id, name, occurrence), instruction.Offset - script.DataStart));
        }

        return functions;
    }
}
