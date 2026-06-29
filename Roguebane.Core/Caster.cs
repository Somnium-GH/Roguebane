namespace Roguebane.Core;

// The combat tick: fixed-step and deterministic. Active techniques reserve allocation from the
// caster's pool (parallel-by-allocation — how many run at once is gated by free attributes) and
// output on a fixed cadence. Same activations + same steps => same damage.
public sealed class Caster
{
    private sealed class Run
    {
        public required Technique Tech;
        public int Countdown;
    }

    private readonly Entity _self;
    private readonly Entity _target;
    private readonly Part _targetPart;
    private readonly SortedDictionary<string, Run> _active = new(StringComparer.Ordinal);

    public int Tick { get; private set; }

    public Caster(Entity self, Entity target, Part targetPart)
    {
        _self = self;
        _target = target;
        _targetPart = targetPart;
    }

    public bool IsActive(Technique t) => _active.ContainsKey(t.Id);

    public int ActiveCount => _active.Count;

    public bool Activate(Technique t)
    {
        if (_active.ContainsKey(t.Id)) return true;
        if (!_self.CanCast) return false;
        if (!_self.Pool.TryAllocateAll(t.Cost)) return false;
        _active[t.Id] = new Run { Tech = t, Countdown = t.Cooldown };
        return true;
    }

    public void Deactivate(Technique t)
    {
        if (!_active.Remove(t.Id)) return;
        _self.Pool.ReleaseAll(t.Cost);
    }

    public void Step()
    {
        Tick++;

        // Head down mid-combat silences everything and returns its allocation.
        if (!_self.CanCast)
        {
            foreach (var run in _active.Values.ToList()) Deactivate(run.Tech);
            return;
        }

        foreach (var run in _active.Values)
        {
            if (run.Tech.Kind == TechniqueKind.Sustained)
            {
                _target.Damage(_targetPart, run.Tech.Power);
                continue;
            }

            if (--run.Countdown <= 0)
            {
                _target.Damage(_targetPart, run.Tech.Power);
                run.Countdown = run.Tech.Cooldown;
            }
        }
    }
}
