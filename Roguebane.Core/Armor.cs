namespace Roguebane.Core;

// Armor is a LIGHT survivability layer, NOT attribute gear (it grants and gates no stat). One piece
// per part-group, keyed to the group's Stat (INT=head, CON=chest, STR=arms, DEX=legs). Its effect
// is keyed to TYPE and RIDES the part's condition — break the part and the effect goes with it.
public enum ArmorKind
{
    Plate,     // a worn SHIELD SOURCE (§6b): raises `Value` shield layers while the part-group stands
    Leather,   // EVASION (a % dodge on the struck part-group, via the seeded RNG) — functional
    SpellWard, // head spell/blind protection — deferred (spells/blind not yet modelled; see Debt)
}

// A piece of armor over a part-group. Under §8, shields + full evade are the ONLY mitigations, so plate
// gives a modest worn shield pool (Value layers, slow regen, shed when its part-group breaks) instead
// of the retired flat protection. Value is the layer count — modest by design (human-tunable).
public sealed record Armor(string Id, Stat Group, ArmorKind Kind, int Value);
