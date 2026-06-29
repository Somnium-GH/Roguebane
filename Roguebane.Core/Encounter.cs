namespace Roguebane.Core;

// One encounter, interpreted from data. A control point is non-structural with no support
// (focus the weakest defender). A castle is structural (break the front before the next layer)
// with a rallied-support stream that periodically repairs the standing front — a DPS race.
public sealed class Encounter
{
    public string Name { get; }
    public Entity Defenders { get; }

    private readonly List<Part> _parts;
    private readonly int _repairAmount;
    private readonly int _repairEvery;
    private int _tick;

    public Encounter(
        string name,
        Entity defenders,
        IReadOnlyList<Part> parts,
        bool structural,
        int repairAmount = 0,
        int repairEvery = 0)
    {
        Name = name;
        Defenders = defenders;
        _parts = parts.ToList();
        Structural = structural;
        _repairAmount = repairAmount;
        _repairEvery = repairEvery;
    }

    public bool Structural { get; }

    public IReadOnlyList<Part> Parts => _parts;

    public bool Cleared => _parts.All(p => Defenders.IsDestroyed(p));

    public Part? CurrentTarget
    {
        get
        {
            var alive = _parts.Where(p => !Defenders.IsDestroyed(p)).ToList();
            if (alive.Count == 0) return null;
            if (Structural) return alive[0]; // strict front: layers fall in order
            return alive.OrderBy(Defenders.Health).ThenBy(_parts.IndexOf).First(); // focus weakest
        }
    }

    public void RallyTick()
    {
        _tick++;
        if (_repairEvery <= 0 || _repairAmount <= 0) return;
        if (_tick % _repairEvery != 0) return;
        if (CurrentTarget is { } front) Defenders.Repair(front, _repairAmount);
    }
}
