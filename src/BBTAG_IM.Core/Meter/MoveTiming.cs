namespace BBTAG_IM.Core.Meter;

public sealed record MoveTiming(string State, int Startup, int Active, int Recovery, int Total);
