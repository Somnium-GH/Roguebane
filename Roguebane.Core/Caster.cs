namespace Roguebane.Core;

// The combat tick: fixed-step and deterministic. Active techniques reserve their stat on the
// body (parallel-by-allocation — how many run at once is gated by free stats) and output on a
// fixed cadence. If the body can no longer power a technique's stat (a smashed head drains INT),
// the reservation cascades off the body and the technique is silenced here.
public sealed class Caster
{
    private sealed class Run
    {
        public required Technique Tech;
        public int Countdown;
        public Foe? Aimed; // per-technique target; null => follow the caster's default front
    }

    private readonly Body _self;
    private Foe? _default;
    private readonly SortedDictionary<string, Run> _active = new(StringComparer.Ordinal);

    public int Tick { get; private set; }

    public Caster(Body self, Foe? target = null)
    {
        _self = self;
        _default = target;
    }

    public void Retarget(Foe target) => _default = target;

    // Per-technique aim: point one technique at its own foe, independent of the default front.
    public void Aim(Technique technique, Foe foe)
    {
        if (_active.TryGetValue(technique.Id, out var run)) run.Aimed = foe;
    }

    public bool IsActive(Technique technique) => _active.ContainsKey(technique.Id);

    public int ActiveCount => _active.Count;

    private static Active Reservation(Technique t) => new(t.Id, t.Stat, t.Reserve);

    public bool Activate(Technique technique)
    {
        if (_active.ContainsKey(technique.Id)) return true;
        if (!_self.Activate(Reservation(technique))) return false;
        _active[technique.Id] = new Run { Tech = technique, Countdown = technique.Cooldown };
        return true;
    }

    public void Deactivate(Technique technique)
    {
        if (!_active.Remove(technique.Id)) return;
        _self.Deactivate(Reservation(technique));
    }

    public void Step()
    {
        Tick++;
        PruneSilenced();

        foreach (var run in _active.Values)
        {
            // A technique hits its own aim while that foe stands, else falls back to the front.
            var target = run.Aimed is { Down: false } ? run.Aimed : _default;
            if (target is null || target.Down) continue;

            if (run.Tech.Kind == TechniqueKind.Sustained)
            {
                target.Damage(run.Tech.Power);
                continue;
            }

            if (--run.Countdown <= 0)
            {
                target.Damage(run.Tech.Power);
                run.Countdown = run.Tech.Cooldown;
            }
        }
    }

    private void PruneSilenced()
    {
        foreach (var run in _active.Values.ToList())
            if (!_self.IsActive(Reservation(run.Tech)))
                _active.Remove(run.Tech.Id);
    }
}
