namespace Roguebane.Core;

// Armor is a LIGHT survivability layer, NOT attribute gear (it grants and gates no stat). One piece
// per part-group, keyed to the group's Stat (INT=head, CON=chest, STR=arms, DEX=legs). Its effect
// is keyed to TYPE and RIDES the part's condition — break the part and the effect goes with it.
public enum ArmorKind
{
    Plate,     // flat PROTECTION (1-4) subtracted from stat-damage to that part — functional now
    Leather,   // EVASION instead of flat protection — deferred (needs a seeded RNG; see Debt)
    SpellWard, // head spell/blind protection — deferred (spells/blind not yet modelled; see Debt)
}

// A piece of armor over a part-group. Modest by design: at the 1-3 damage band, flat protection
// should blunt, not negate (a balance note for human tuning).
public sealed record Armor(string Id, Stat Group, ArmorKind Kind, int Value);
