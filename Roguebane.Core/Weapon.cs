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

// A weapon is a hand-held stat-stick with properties, granting ZERO abilities of its own. Reserve is
// the stat you need to wield it (and the cost a consulting technique pays to swing it); Power is the
// damage it lends that technique. Lose the arm that carries the stat and the weapon falls off.
public sealed record Weapon(string Id, Stat Stat, int Reserve, int Power);
