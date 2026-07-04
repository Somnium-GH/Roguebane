namespace Roguebane.Core;

// How a technique consults equipped weapons (§7: verbs are not bound to weapons — a weapon is a
// stat-stick the technique reads). None = self-contained (spells, brace); Primary = one weapon of
// the technique's stat; Both = every weapon of that stat (e.g. a dual-wield flurry).
public enum WeaponUse
{
    None,
    Primary,
    Both,
}

// What kind of implement a hand item is (§6d roster): melee ladders, the two RANGED-slot families
// (bow = full shield bypass + Charge; sling = its 1H shield-compatible cousin), the INT
// implements (wand = shield-SUBTRACTION hand item; staff = 2H blockable melee; charm/tome =
// pure-bonus offhands), and the CON shield OBJECT (§6c, a 1H hand item, equip-gate resolved
// 2026-07-04: 1 CON/tier). Kind decides which combat resolution and equip layer the piece uses.
public enum WeaponKind
{
    Melee,
    Bow,
    Sling,
    Wand,
    Staff,
    Charm,
    Tome,
    Shield,
}

// A weapon is a hand-held stat-stick with properties, granting ZERO abilities of its own. Reserve
// is the stat you need to wield it (and the cost a consulting technique pays to swing it); Power is
// the damage it lends that technique. §6d roster identity: Name/Tier ride the material ladder;
// Timer multiplies the consulting technique's CHARGE timer (<1.0 = faster; dual-wield averages —
// consumer is its own slice); Hands = 1 or 2 (§6: a 2H needs BOTH arms; a broken arm silences the
// hand either way).
public sealed record Weapon(string Id, Stat Stat, int Reserve, int Power,
    string Name = "", int Tier = 0, double Timer = 1.0, int Hands = 1,
    WeaponKind Kind = WeaponKind.Melee);
