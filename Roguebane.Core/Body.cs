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
        _intact[part.Id] = Math.Max(0, Contribution(part) - amount);
        Cascade(part.Stat);
        _hands.RemoveAll(w => Capacity(w.Stat) < w.Reserve); // gear falls off below its threshold
        // Plate's shield RIDES its part-group (§6): break the group and the worn shield is gone with it.
        if (Capacity(part.Stat) == 0 && _armor.TryGetValue(part.Stat, out var a) && a.Kind == ArmorKind.Plate)
            DropShield(PlateShieldId(part.Stat));
    }

    public IReadOnlyList<Weapon> Hands => _hands;

    // Wield a weapon into a free hand if the body can lift it (stat capacity meets its threshold).
    public bool Wield(Weapon weapon)
    {
        if (_hands.Count >= 2) return false;
        if (Capacity(weapon.Stat) < weapon.Reserve) return false;
        _hands.Add(weapon);
        return true;
    }

    public void Unwield(Weapon weapon) => _hands.Remove(weapon);

    // The weapons a technique consults, by its stat (§7). Power/cost flow from these.
    public IReadOnlyList<Weapon> Consulted(Technique technique) => technique.Consults switch
    {
        WeaponUse.Primary => _hands.Where(w => w.Stat == technique.Stat).Take(1).ToList(),
        WeaponUse.Both => _hands.Where(w => w.Stat == technique.Stat).ToList(),
        _ => Array.Empty<Weapon>(),
    };

    private const int PlateRegenEvery = 40; // plate is a slow-recovering worn buffer, not free tanking
    private static string PlateShieldId(Stat group) => "plate-" + group;

    // Armor rides on a part-group (its Stat). One piece per group — equipping replaces. §8/§6: PLATE is
    // a worn SHIELD SOURCE (the flat-protection role retired) — equipping it raises a shield pool (Value
    // layers) while the group stands; leather stays evasion. Shields + full evade are the only mitigations.
    public void Equip(Armor piece)
    {
        Unequip(piece.Group); // drop a prior piece's plate shield before replacing
        _armor[piece.Group] = piece;
        if (piece.Kind == ArmorKind.Plate && piece.Value > 0 && Capacity(piece.Group) > 0)
            RaiseShield(PlateShieldId(piece.Group), piece.Value, PlateRegenEvery);
    }

    public void Unequip(Stat group)
    {
        if (_armor.TryGetValue(group, out var a) && a.Kind == ArmorKind.Plate) DropShield(PlateShieldId(group));
        _armor.Remove(group);
    }

    public Armor? ArmorOn(Stat group) => _armor.GetValueOrDefault(group);

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

    // Leather EVASION: a dodge chance (percent) on the struck part-group, riding its condition. A
    // whole-HP hit (no part) consults the legs (DEX) leather — body footing/dodge — while the legs
    // still stand. Break the part (or lose the group) and its evasion goes with it.
    public int EvasionPercent(BodyPart? part)
    {
        var group = part?.Stat ?? Stat.Dex;
        var standing = part is null ? Capacity(group) > 0 : Contribution(part) > 0;
        return standing && _armor.TryGetValue(group, out var a) && a.Kind == ArmorKind.Leather
            ? a.Value : 0;
    }

    // STR at full weight plus a quarter of DEX, kept in integer quarter-units.
    public int AttackPower => Capacity(Stat.Str) + Capacity(Stat.Dex) / 4;
}
