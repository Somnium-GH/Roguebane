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
        public ICombatTarget? Aimed; // per-technique target; null => follow the caster's default front
        public BodyPart? Part;       // per-technique PART aim within Aimed; null => whole-target HP
    }

    private readonly Body _self;
    private ICombatTarget? _default;
    private readonly SortedDictionary<string, Run> _active = new(StringComparer.Ordinal);

    public int Tick { get; private set; }

    public Caster(Body self, ICombatTarget? target = null)
    {
        _self = self;
        _default = target;
    }

    public void Retarget(ICombatTarget target) => _default = target;

    // Per-technique aim: point one technique at its own target, independent of the default front.
    public void Aim(Technique technique, ICombatTarget target)
    {
        if (_active.TryGetValue(technique.Id, out var run)) { run.Aimed = target; run.Part = null; }
    }

    // Per-technique PART aim: point one technique at a specific part of a structured target. Damage
    // erodes that part's stat first and only spills into HP once the part bottoms out.
    public void Aim(Technique technique, ICombatTarget target, BodyPart part)
    {
        if (_active.TryGetValue(technique.Id, out var run)) { run.Aimed = target; run.Part = part; }
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
            var onAim = run.Aimed is { Down: false };
            var target = onAim ? run.Aimed : _default;
            if (target is null || target.Down) continue;

            // The PART aim only rides its own foe; a fallback to the front hits the HP pool.
            var part = onAim ? run.Part : null;

            if (run.Tech.Kind == TechniqueKind.Sustained)
            {
                Hit(target, part, run.Tech.Power);
                continue;
            }

            if (--run.Countdown <= 0)
            {
                Hit(target, part, run.Tech.Power);
                run.Countdown = run.Tech.Cooldown;
            }
        }
    }

    private static void Hit(ICombatTarget target, BodyPart? part, int power)
    {
        if (part is null) target.Damage(power);
        else target.DamagePart(part, power);
    }

    private void PruneSilenced()
    {
        foreach (var run in _active.Values.ToList())
            if (!_self.IsActive(Reservation(run.Tech)))
                _active.Remove(run.Tech.Id);
    }
}
