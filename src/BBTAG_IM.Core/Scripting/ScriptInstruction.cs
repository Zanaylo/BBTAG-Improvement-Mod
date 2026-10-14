namespace BBTAG_IM.Core.Scripting;

public sealed record ScriptInstruction(int Offset, CommandDefinition Command, IReadOnlyList<ScriptValue> Values)
{
    public int Id => Command.Id;

    public int Size => Command.Size;

    public int End => Offset + Size;

    public string TextAt(int index) => ScriptText.Decode(Values[index].Text ?? []);
}
