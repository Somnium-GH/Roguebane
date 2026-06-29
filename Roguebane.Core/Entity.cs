namespace Roguebane.Core;

public sealed class Entity
{
    public AttributePool Pool { get; }

    private readonly List<Part> _parts = new();
    private readonly HashSet<string> _enabled = new();
    private readonly Dictionary<string, int> _health = new();

    public Entity(AttributePool pool) => Pool = pool;

    public IReadOnlyList<Part> Parts => _parts;

    public bool IsEnabled(Part part) => _enabled.Contains(part.Id);

    public int Health(Part part) => _health.GetValueOrDefault(part.Id);

    public bool IsDestroyed(Part part) => part.MaxHealth > 0 && Health(part) <= 0;

    public void Add(Part part)
    {
        _parts.Add(part);
        _health[part.Id] = part.MaxHealth;
    }

    // Enabling claims the part's full demand or nothing; a destroyed part cannot power on.
    public bool Enable(Part part)
    {
        if (_enabled.Contains(part.Id)) return true;
        if (IsDestroyed(part)) return false;
        if (!Pool.TryAllocateAll(part.Demand)) return false;
        _enabled.Add(part.Id);
        return true;
    }

    // Disabling returns the part's attributes to the pool for something else to claim.
    public void Disable(Part part)
    {
        if (!_enabled.Remove(part.Id)) return;
        Pool.ReleaseAll(part.Demand);
    }

    // Damage degrades capability: at zero health a part is destroyed and powers off, freeing
    // its allocation but barred from re-enabling until repaired.
    public void Damage(Part part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (part.MaxHealth <= 0) return;
        var next = Math.Max(0, Health(part) - amount);
        _health[part.Id] = next;
        if (next == 0) Disable(part);
    }

    // Rallied support reinforces a standing part; cannot raise the dead or exceed its max.
    public void Repair(Part part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (part.MaxHealth <= 0 || IsDestroyed(part)) return;
        _health[part.Id] = Math.Min(part.MaxHealth, Health(part) + amount);
    }

    // Casting flows through the head. No live head => silenced.
    public bool CanCast =>
        _parts.Any(p => p.Role == PartRole.Head && IsEnabled(p) && !IsDestroyed(p));
}
