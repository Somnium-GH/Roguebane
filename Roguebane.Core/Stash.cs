namespace Roguebane.Core;

// The persistent run economy — gold and the gear pack (unequipped weapons + armor). Lives above a
// single Expedition so it carries across the legs of a campaign. (Potions are gone: part-heals are
// in-combat techniques now, not buyable items — 2026-06-30 directive.)
public sealed class Stash
{
    public int Gold { get; private set; }

    private readonly List<Weapon> _weapons = new(); // carried but not wielded
    private readonly List<Armor> _armor = new();    // carried but not worn
    private readonly List<Technique> _techniques = new(); // bought, awaiting a palette slot (§12)
    private readonly List<Minion> _minions = new();       // bought, awaiting a minion slot (§12)
    private readonly List<Mark> _marks = new();           // bought runes in the bag (§12)

    // The GEAR tab's stable identity roster: every weapon/armor piece ever owned (wielded/worn OR
    // packed), in first-seen order, tracked SEPARATELY from _weapons/_armor above (those two only
    // hold what's currently packed -- a piece moves out of them the instant it's equipped). Without
    // this, the GEAR tab had no fixed order to hand out click indices against: it was built by
    // concatenating the equipped set + the pack each frame, so equipping/unequipping physically moved
    // a piece between buckets and reshuffled every OTHER piece's position too, mis-routing clicks onto
    // whatever the reshuffle put at that screen slot this frame (root cause, HIGH PRIORITY bug #1).
    // Reference-identity keyed (records compare by VALUE, so two otherwise-identical pieces -- the
    // seeded duplicate-armor case -- must not collapse into one roster slot).
    private readonly List<Weapon> _weaponRoster = new();
    private readonly List<Armor> _armorRoster = new();
    private readonly HashSet<object> _rosterSeen = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<Weapon> WeaponRoster => _weaponRoster;
    public IReadOnlyList<Armor> ArmorRoster => _armorRoster;

    // Idempotent (by reference identity): call for a piece the moment it enters play -- kit mint,
    // purchase, or loot -- from wherever that happens, regardless of whether it lands in the pack or
    // straight onto the Body.
    public void TrackOwned(Weapon weapon) { if (_rosterSeen.Add(weapon)) _weaponRoster.Add(weapon); }
    public void TrackOwned(Armor piece) { if (_rosterSeen.Add(piece)) _armorRoster.Add(piece); }

    public Stash(int gold = 0) => Gold = gold;

    // The gear pack: gear acquired (found/bought) sits here until equipped onto the body, and returns
    // here when unequipped or displaced. §12 receiving (LOCKED 2026-07-03): technique/minion/rune
    // purchases land in the run inventory too — SLOTTING stays the Equipment screen's job.
    public IReadOnlyList<Weapon> Weapons => _weapons;
    public IReadOnlyList<Armor> Armor => _armor;
    public IReadOnlyList<Technique> Techniques => _techniques;
    public IReadOnlyList<Minion> Minions => _minions;
    public IReadOnlyList<Mark> Marks => _marks;

    public void AddWeapon(Weapon weapon) { TrackOwned(weapon); _weapons.Add(weapon); }
    public void AddArmor(Armor piece) { TrackOwned(piece); _armor.Add(piece); }
    public void AddTechnique(Technique technique) => _techniques.Add(technique);
    public void AddMinion(Minion minion) => _minions.Add(minion);
    public void AddMark(Mark mark) => _marks.Add(mark);
    public bool HasWeapon(Weapon weapon) => _weapons.Contains(weapon);
    public bool HasArmor(Armor piece) => _armor.Contains(piece);
    public bool RemoveWeapon(Weapon weapon) => _weapons.Remove(weapon);
    public bool RemoveArmor(Armor piece) => _armor.Remove(piece);

    public void AddGold(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Gold += amount;
    }

    public bool TrySpend(int cost)
    {
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        if (Gold < cost) return false;
        Gold -= cost;
        return true;
    }
}
