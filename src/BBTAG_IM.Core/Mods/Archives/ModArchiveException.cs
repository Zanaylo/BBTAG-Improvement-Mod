namespace BBTAG_IM.Core.Mods.Archives;

public sealed class ModArchiveException(string message, Exception? innerException = null) : Exception(message, innerException);
