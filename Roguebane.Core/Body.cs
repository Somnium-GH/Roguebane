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

    // STR at full weight plus a quarter of DEX, kept in integer quarter-units.
    public int AttackPower => Capacity(Stat.Str) + Capacity(Stat.Dex) / 4;
}
