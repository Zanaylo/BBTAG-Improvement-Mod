namespace BBTAG_IM.Core.Scripting.Live;

public sealed class ScriptReloadedException() : Exception("The game loaded this script again, so the edit applies to the next match from the DEV folder.");
