namespace BBTAG_IM.Core.Roster;

public sealed class InvalidCharacterManifestException(string message, Exception? inner = null) : Exception(message, inner);
