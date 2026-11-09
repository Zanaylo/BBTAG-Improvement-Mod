namespace BBTAG_IM.Core.Meter;

public sealed record CharacterFrame(
    int Frame,
    string State,
    bool CanAct,
    int StateTime,
    int Hitstop,
    int Hitstun,
    int Blockstun,
    int Hitboxes,
    Invulnerability Invulnerability,
    bool Armor,
    Contact Contact,
    bool Airborne,
    int Health);
