namespace Roguebane.Core;

public sealed class Entity
{
    public AttributePool Pool { get; }

    private readonly List<Part> _parts = new();
    private readonly HashSet<string> _enabled = new();

    public Entity(AttributePool pool) => Pool = pool;

    public IReadOnlyList<Part> Parts => _parts;

    public bool IsEnabled(Part part) => _enabled.Contains(part.Id);

    public void Add(Part part) => _parts.Add(part);

    // Enabling claims the part's full demand or nothing — a part that can only be
    // partially powered does not run.
    public bool Enable(Part part)
    {
        if (_enabled.Contains(part.Id)) return true;

        var claimed = new List<KeyValuePair<Attribute, int>>();
        foreach (var demand in part.Demand)
        {
            if (!Pool.TryAllocate(demand.Key, demand.Value))
            {
                foreach (var c in claimed) Pool.Release(c.Key, c.Value);
                return false;
            }
            claimed.Add(demand);
        }

        _enabled.Add(part.Id);
        return true;
    }

    // Disabling returns the part's attributes to the pool for something else to claim.
    public void Disable(Part part)
    {
        if (!_enabled.Remove(part.Id)) return;
        foreach (var demand in part.Demand)
            Pool.Release(demand.Key, demand.Value);
    }
}
