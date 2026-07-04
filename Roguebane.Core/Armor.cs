namespace Roguebane.Core;

// §6c armor lines: one LADDER per attribute type, four escalating tiers, one piece per body SLOT
// (the part-group it covers). Armor is a LIGHT effect layer — it grants and gates no attribute.
// A piece's LINE names its governing attribute: the line's per-tier bonus, and the attribute whose
// collapse DISABLES the piece (§6e: it stays ASSIGNED, sheds its effect, re-enables on recovery).
// The CON line's shield OBJECT is NOT body armor — it is a hand-config item and builds with the
// §6d wield model.
public enum ArmorLine
{
    Plate,   // STR: -2 part-damage to the covered part, per tier (never HP mitigation, §8)
    Leather, // DEX: +2% evade per tier, per worn piece (stacks across pieces)
    Robe,    // INT: +2 spell damage per worn piece (2-piece cap: robe + hat)
}

// A §6c piece: Slot = the part-group it covers (keyed by that group's stat, the §6 anatomy),
// Line = its ladder/governing attribute, Tier = 1..4 rung. Tier bonuses are §6c's blessed-initial
// numbers — data-derived, tuned later.
public sealed record Armor(string Id, string Name, Stat Slot, ArmorLine Line, int Tier)
{
    // The attribute that governs this piece's sustain (§6e DISABLED rides it, not the slot's stat).
    public Stat Governing => Line switch
    {
        ArmorLine.Plate => Stat.Str,
        ArmorLine.Leather => Stat.Dex,
        _ => Stat.Int,
    };

    // §6c per-tier bonus magnitudes (blessed initial): the LINE decides what the number means.
    public int PartMitigation => Line == ArmorLine.Plate ? 2 * Tier : 0; // part-damage soaked
    public int EvadePct => Line == ArmorLine.Leather ? 2 * Tier : 0;     // per worn piece
    public int SpellDamage => Line == ArmorLine.Robe ? 2 : 0;            // per piece, 2-piece cap

    // §6c per-tier equip gate (blessed initial, 2026-07-03) on the GOVERNING attribute:
    // STR armor 2/t · DEX armor 1/t · Robe (chest) 2 INT/t · Cap/Circlet (head) 1 INT/t.
    public int Requirement => Line switch
    {
        ArmorLine.Plate => 2 * Tier,
        ArmorLine.Leather => Tier,
        _ => (Slot == Stat.Con ? 2 : 1) * Tier,
    };
}
