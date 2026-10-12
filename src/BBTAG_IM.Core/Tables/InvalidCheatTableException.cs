namespace BBTAG_IM.Core.Tables;

public sealed class InvalidCheatTableException(string message, Exception? innerException = null) : Exception(message, innerException);
