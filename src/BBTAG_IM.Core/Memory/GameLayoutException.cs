namespace BBTAG_IM.Core.Memory;

public sealed class GameLayoutException(string message) : Exception(message)
{
    public const string UnsupportedVersion = "This game version is not supported.";
}
