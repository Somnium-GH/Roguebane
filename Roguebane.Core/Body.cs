namespace Roguebane.Core;

// The socketed body on the new model. The live stat pool is DERIVED from intact parts, not fixed:
// damage a part and the stat it feeds shrinks, which can no longer cover what equipment/abilities
// have reserved — so they fall off. This single rule unifies graded degradation, equip/ability
// fall-off, and the allocation economy.
public sealed class Body
{
    private readonly List<BodyPart> _parts = new();
    private readonly Dictionary<string, int> _intact = new();
    private readonly List<Active> _actives = new(); // engagement order; cascade sheds newest first
    private readonly Dictionary<Stat, Armor> _armor = new(); // one piece per part-group (keyed by Stat)
    private readonly List<Weapon> _hands = new(); // up to two; anatomical hand count
    private readonly Dictionary<string, ShieldPool> _shields = new(); // §6b shield sources, keyed by source id

    public IReadOnlyList<BodyPart> Parts => _parts;
    public IReadOnlyList<Active> Actives => _actives;

    public void Add(BodyPart part)
    {
        _parts.Add(part);
        _intact[part.Id] = part.Capacity;
    }

    public int Contribution(BodyPart part) => _intact.GetValueOrDefault(part.Id);

    public int Capacity(Stat stat) => _parts.Where(p => p.Stat == stat).Sum(Contribution);

    public int Reserved(Stat stat) => _actives.Where(a => a.Stat == stat).Sum(a => a.Reserve);

    public int Available(Stat stat) => Capacity(stat) - Reserved(stat);

    public bool IsActive(Active active) => _actives.Contains(active);

    public bool Activate(Active active)
    {
        if (_actives.Contains(active)) return true;
        if (Available(active.Stat) < active.Reserve) return false;
        _actives.Add(active);
        return true;
    }

    public void Deactivate(Active active) => _actives.Remove(active);

    public void Damage(BodyPart part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        // §6c STR plate: a sustained piece soaks the covered part's OWN damage per tier —
        // never HP damage (§8: shields + full evade stay the only HP mitigations).
        if (_armor.TryGetValue(part.Stat, out var worn) && worn.PartMitigation > 0 && ArmorSustained(worn))
            amount = Math.Max(0, amount - worn.PartMitigation);
        _intact[part.Id] = Math.Max(0, Contribution(part) - amount);
        Cascade(part.Stat);
        _hands.RemoveAll(w => Capacity(w.Stat) < w.Reserve); // gear falls off below its threshold
    }

    public IReadOnlyList<Weapon> Hands => _hands;

    // Wield a weapon into a free hand if the body can lift it (stat capacity meets its threshold).
    // §6d: bows/slings are RANGED-slot items, never hand items; a wand or staff cannot share a
    // build with an occupied ranged slot (mutual exclusion; the staff blocks ranged like a shield).
    public bool Wield(Weapon weapon)
    {
        if (weapon.Kind is WeaponKind.Bow or WeaponKind.Sling) return false;
        if (weapon.Kind is WeaponKind.Wand or WeaponKind.Staff && _ranged is not null) return false;
        if (_hands.Count >= 2) return false;
        if (Capacity(weapon.Stat) < weapon.Reserve) return false;
        _hands.Add(weapon);
        return true;
    }

    public void Unwield(Weapon weapon) => _hands.Remove(weapon);

    // §6d: ONE ranged slot, independent of the melee hands — a sword+shield AND a bow coexist.
    private Weapon? _ranged;
    public Weapon? Ranged => _ranged;

    public bool EquipRanged(Weapon w)
    {
        if (w.Kind is not (WeaponKind.Bow or WeaponKind.Sling)) return false;
        if (_ranged is not null) return false;
        if (Capacity(w.Stat) < w.Reserve) return false;
        // Mutual exclusion (§6d): a held wand excludes the ranged slot; a staff blocks it too.
        if (_hands.Any(h => h.Kind is WeaponKind.Wand or WeaponKind.Staff)) return false;
        _ranged = w;
        return true;
    }

    public Weapon? UnequipRanged()
    {
        var r = _ranged;
        _ranged = null;
        return r;
    }

    // §6d arm gates at USE time: a BOW needs both arms unbroken (same as any 2H); the 1H SLING
    // needs one usable throwing arm. The item stays ASSIGNED either way (§6e).
    public bool RangedUsable => _ranged is { } r
        && Capacity(r.Stat) >= r.Reserve
        && (r.Kind == WeaponKind.Bow ? HandUsable(0) && HandUsable(1)
                                     : HandUsable(0) || HandUsable(1));

    // §6d/§6 hard override: a hand's weapon works only while its ARM stands — a broken arm's
    // hand slot is physically gone regardless of which stat the weapon gates on, for player and
    // foe alike (§8 symmetry). Hand 0 rides the SECOND Str part (armR, dominant), hand 1 the
    // first (armL), mirroring the figure's socket order. Bodies without arm parts don't gate.
    public bool HandUsable(int handIndex)
    {
        var arms = _parts.Where(p => p.Stat == Stat.Str).ToList();
        if (arms.Count == 0) return true;
        var ix = handIndex == 0 ? Math.Min(1, arms.Count - 1) : 0;
        return Contribution(arms[ix]) > 0;
    }

    private IEnumerable<Weapon> UsableHands() => _hands.Where((_, i) => HandUsable(i));

    // §6d magic offhands (+0.1x per tier): ONE off-hand slot exists, so the best USABLE held
    // piece counts — a broken arm silences its bonus like any other hand item.
    public double CharmMinionMult => 1.0 + 0.1 * UsableHands()
        .Where(w => w.Kind == WeaponKind.Charm).Select(w => w.Tier).DefaultIfEmpty(0).Max();
    public double TomeSpellMult => 1.0 + 0.1 * UsableHands()
        .Where(w => w.Kind == WeaponKind.Tome).Select(w => w.Tier).DefaultIfEmpty(0).Max();

    // The weapons a technique consults, by its stat (§7) — broken-arm hands never answer (§6d).
    // A CHARGE/pierce verb (Shot family) looses the RANGED slot, never a hand item — that's the
    // §6d two-layer split: melee techniques read the hand-config, ranged techniques the slot.
    public IReadOnlyList<Weapon> Consulted(Technique technique)
    {
        if (technique.ShieldPiercing)
            return _ranged is { } r && r.Stat == technique.Stat && RangedUsable
                ? new[] { r } : Array.Empty<Weapon>();
        return technique.Consults switch
        {
            WeaponUse.Primary => UsableHands().Where(w => w.Stat == technique.Stat).Take(1).ToList(),
            WeaponUse.Both => UsableHands().Where(w => w.Stat == technique.Stat).ToList(),
            _ => Array.Empty<Weapon>(),
        };
    }

    // Armor covers a part-group SLOT (keyed by the group's stat, §6 anatomy) — one piece per slot,
    // equipping replaces. §6c: armor is a light EFFECT layer (per-line tier bonuses on the record),
    // GATED at equip time on its governing attribute (per-tier requirement); the
    // worn-plate-as-shield-source role is RETIRED — §6b shield sources are techniques (+ the §6d
    // shield OBJECT when that builds). Shields + full evade stay the only HP mitigations.
    public bool Equip(Armor piece)
    {
        if (Capacity(piece.Governing) < piece.Requirement) return false;
        _armor[piece.Slot] = piece;
        return true;
    }

    public void Unequip(Stat group) => _armor.Remove(group);

    public Armor? ArmorOn(Stat group) => _armor.GetValueOrDefault(group);

    // §6e sustain: a worn piece works while its LINE's governing attribute stands. The
    // blessed-initial threshold is total collapse (capacity 0) — §6c's "both arms break and the
    // STR armor goes RED across the board"; finer per-tier thresholds ride the tuning session.
    public bool ArmorSustained(Armor piece) => Capacity(piece.Governing) > 0;

    // §6c INT robe: +2 spell damage per worn sustained robe piece, capped at TWO pieces
    // (robe + hat — the line only authors those slots, the cap keeps that true under tuning).
    public int SpellDamageBonus =>
        Math.Min(2, _armor.Values.Count(a => a.SpellDamage > 0 && ArmorSustained(a))) * 2;

    // The part with the most stat missing (Capacity - live contribution), or null if every part is
    // whole. Drives a part-heal's target — mend where it hurts most; ties resolve by part order.
    public BodyPart? MostDamagedPart()
    {
        BodyPart? worst = null;
        var most = 0;
        foreach (var p in _parts)
        {
            var missing = p.Capacity - Contribution(p);
            if (missing > most) { most = missing; worst = p; }
        }
        return worst;
    }

    // Healing restores PARTS, never HP: a repaired part feeds its stat back into the pool.
    public void Repair(BodyPart part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        _intact[part.Id] = Math.Min(part.Capacity, Contribution(part) + amount);
    }

    private void Cascade(Stat stat)
    {
        for (var i = _actives.Count - 1; i >= 0 && Reserved(stat) > Capacity(stat); i--)
            if (_actives[i].Stat == stat) _actives.RemoveAt(i);
    }

    // §6b SHIELDS: a shield source maintains a regenerating pool of 1-damage layers on the body — the
    // OUTERMOST mitigation, absorbing incoming damage before armor/parts/HP. The owning caster raises
    // one per active shield technique, ticks them each combat step, and drops them when the source ends.
    public void RaiseShield(string id, int layers, int regenEvery)
    {
        if (!_shields.ContainsKey(id)) _shields[id] = new ShieldPool(layers, regenEvery);
    }

    public void DropShield(string id) => _shields.Remove(id);

    public void TickShields() { foreach (var pool in _shields.Values) pool.Tick(); }

    public int ShieldPoints => _shields.Values.Sum(p => p.Points);

    public int ShieldLayers => _shields.Values.Sum(p => p.Layers);

    // The furthest-along regen among still-filling pools — what the single regen bar under the pips
    // shows (multiple sources regen independently; the bar reads the next pip to land).
    public float ShieldRegenProgress
        => _shields.Values.Select(p => p.RegenProgress).DefaultIfEmpty(0f).Max();

    // Absorb incoming damage across the standing shield layers; returns the unabsorbed remainder.
    public int AbsorbShields(int damage)
    {
        foreach (var pool in _shields.Values)
        {
            if (damage <= 0) break;
            damage = pool.Absorb(damage);
        }
        return damage;
    }

    // §6c DEX/leather EVASION: +2% per tier, PER worn sustained piece (stacks across pieces) —
    // a global dodge, not per-struck-part. §6 HARD OVERRIDE: a broken LEG zeroes evasion outright,
    // overriding any residual DEX — the footing is gone. (A DEX-attribute-derived base evade is
    // in the §6 table but unnumbered — not invented; leather is the only source until it lands.)
    public int EvasionPercent()
    {
        var legs = _parts.Where(p => p.Stat == Stat.Dex).ToList();
        if (legs.Count > 0 && legs.Any(l => Contribution(l) == 0)) return 0; // broken leg
        return _armor.Values.Where(a => a.EvadePct > 0 && ArmorSustained(a)).Sum(a => a.EvadePct);
    }

    // STR at full weight plus a quarter of DEX, kept in integer quarter-units.
    public int AttackPower => Capacity(Stat.Str) + Capacity(Stat.Dex) / 4;
}
