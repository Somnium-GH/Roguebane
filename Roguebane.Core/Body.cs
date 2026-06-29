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

    // Armor rides on a part-group (its Stat). One piece per group — equipping replaces.
    public void Equip(Armor piece) => _armor[piece.Group] = piece;

    public Armor? ArmorOn(Stat group) => _armor.GetValueOrDefault(group);

    // Flat plate protection on a part, but only while the part still stands — the effect rides the
    // part's condition. Other armor kinds (leather evasion, spell-ward) are not flat protection.
    public int Protection(BodyPart part) =>
        Contribution(part) > 0 && _armor.TryGetValue(part.Stat, out var a) && a.Kind == ArmorKind.Plate
            ? a.Value : 0;

    // A localized incoming hit: plate blunts it, the part's stat erodes (running the cascade), and
    // the unabsorbed remainder (overkill) is returned for the caller's HP pool to take.
    public int AbsorbPartHit(BodyPart part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var effective = Math.Max(0, amount - Protection(part));
        var absorbed = Math.Min(Contribution(part), effective);
        if (absorbed > 0) Damage(part, absorbed);
        return effective - absorbed;
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

    // A held block (a defensive active) reserves CON and absorbs up to the CON it reserves, capped.
    // Raise it = power it; drop it = the CON returns to the pool.
    public int BlockMitigation(int cap) => Math.Min(Reserved(Stat.Con), cap);

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
