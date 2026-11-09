namespace BBTAG_IM.Core.Updates;

public sealed class GitHubException(string message, Exception? innerException = null) : Exception(message, innerException);
