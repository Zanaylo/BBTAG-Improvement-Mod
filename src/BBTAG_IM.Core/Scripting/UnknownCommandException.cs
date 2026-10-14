namespace BBTAG_IM.Core.Scripting;

public sealed class UnknownCommandException(int id) : Exception($"Command {id} is not in the script database.")
{
    public int Id { get; } = id;
}
