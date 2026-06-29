namespace Roguebane.Core;

// Live allocation pool: capacity is fixed per attribute; allocations are claimed and
// released at runtime by whatever powers off it (parts now, techniques/minions later).
// Integer-only so the simulation stays deterministic.
public sealed class AttributePool
{
    private readonly Dictionary<Attribute, int> _capacity;
    private readonly Dictionary<Attribute, int> _allocated;

    public AttributePool(IReadOnlyDictionary<Attribute, int> capacity)
    {
        _capacity = new Dictionary<Attribute, int>(capacity);
        _allocated = new Dictionary<Attribute, int>();
        foreach (var key in _capacity.Keys)
            _allocated[key] = 0;
    }

    public int Capacity(Attribute a) => _capacity.GetValueOrDefault(a);

    public int Allocated(Attribute a) => _allocated.GetValueOrDefault(a);

    public int Available(Attribute a) => Capacity(a) - Allocated(a);

    public bool TryAllocate(Attribute a, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (Available(a) < amount) return false;
        _allocated[a] = Allocated(a) + amount;
        return true;
    }

    public void Release(Attribute a, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        var next = Allocated(a) - amount;
        if (next < 0) throw new InvalidOperationException("release exceeds current allocation");
        _allocated[a] = next;
    }

    // Claim a whole demand or nothing — a subsystem that can only be partly powered does not run.
    public bool TryAllocateAll(IReadOnlyDictionary<Attribute, int> demand)
    {
        var claimed = new List<KeyValuePair<Attribute, int>>();
        foreach (var d in demand)
        {
            if (!TryAllocate(d.Key, d.Value))
            {
                foreach (var c in claimed) Release(c.Key, c.Value);
                return false;
            }
            claimed.Add(d);
        }
        return true;
    }

    public void ReleaseAll(IReadOnlyDictionary<Attribute, int> demand)
    {
        foreach (var d in demand) Release(d.Key, d.Value);
    }
}
