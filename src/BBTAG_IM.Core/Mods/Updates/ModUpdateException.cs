namespace BBTAG_IM.Core.Mods.Updates;

public sealed class ModUpdateException(string message, Exception? innerException = null) : Exception(message, innerException);
