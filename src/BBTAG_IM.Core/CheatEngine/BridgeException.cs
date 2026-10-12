namespace BBTAG_IM.Core.CheatEngine;

public sealed class BridgeException(string message, Exception? innerException = null) : Exception(message, innerException);
