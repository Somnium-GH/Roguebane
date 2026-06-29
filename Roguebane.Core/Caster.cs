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

    private const int BlockCap = 3; // a held CON block absorbs at most this much off an HP hit (low scale)

    private readonly Body _self;
    private Rng? _rng; // chance effects (evasion); set by Battle so a fight is reproducible
    private ICombatTarget? _default;
    private readonly SortedDictionary<string, Run> _active = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, Minion> _bays = new(StringComparer.Ordinal);

    public int Tick { get; private set; }

    private int _charge;
    public int MaxCharge { get; }
    public int Charge => _charge;

    public Caster(Body self, ICombatTarget? target = null, int maxCharge = 0)
    {
        _self = self;
        _default = target;
        MaxCharge = maxCharge;
        _charge = maxCharge;
    }

    // Battle hands every caster the fight's shared PRNG so chance rolls are deterministic.
    public void UseRng(Rng rng) => _rng = rng;

    // The finite magic resource is refilled out of combat (loot / rest), not regenerated mid-fight.
    public void Recharge() => _charge = MaxCharge;

    public void Recharge(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        _charge = Math.Min(MaxCharge, _charge + amount);
    }

    private bool TrySpendCharge(int cost)
    {
        if (_charge < cost) return false;
        _charge -= cost;
        return true;
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

    // A self-contained technique reserves its own stat; a weapon-consulting one reserves the sum of
    // its consulted weapons' reserves (you can't swing what you aren't holding — reserve 0 → can't).
    private Active Reservation(Technique t) => t.Consults == WeaponUse.None
        ? new(t.Id, t.Stat, t.Reserve)
        : new(t.Id, t.Stat, _self.Consulted(t).Sum(w => w.Reserve));

    private int EffectivePower(Technique t) => t.Consults == WeaponUse.None
        ? t.Power
        : t.Power + _self.Consulted(t).Sum(w => w.Power);

    public bool Activate(Technique technique)
    {
        if (_active.ContainsKey(technique.Id)) return true;
        var reservation = Reservation(technique);
        if (reservation.Reserve <= 0) return false; // nothing to swing (no weapon to consult)
        if (!_self.Activate(reservation)) return false;
        _active[technique.Id] = new Run { Tech = technique, Countdown = technique.Cooldown };
        return true;
    }

    public void Deactivate(Technique technique)
    {
        if (!_active.Remove(technique.Id)) return;
        _self.Deactivate(Reservation(technique));
    }

    public int MinionCount => _bays.Count;
    public bool HasMinion(Minion minion) => _bays.ContainsKey(minion.Id);

    private static Active Reservation(Minion m) => new(m.Id, m.Stat, m.Reserve);

    // Summon a minion into a free bay: capped by the chassis's bay count and gated by free stat.
    public bool Summon(Minion minion, int bayCap)
    {
        if (_bays.ContainsKey(minion.Id)) return true;
        if (_bays.Count >= bayCap) return false;
        if (!_self.Activate(Reservation(minion))) return false;
        _bays[minion.Id] = minion;
        return true;
    }

    public void Dismiss(Minion minion)
    {
        if (!_bays.Remove(minion.Id)) return;
        _self.Deactivate(Reservation(minion));
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

            // Sustained fires every tick; timered fires when its countdown elapses.
            var fires = run.Tech.Kind == TechniqueKind.Sustained || --run.Countdown <= 0;
            if (!fires) continue;

            // A magic technique with no charge holds fire — a timered one keeps retrying (its
            // countdown stays elapsed) until the resource refills out of combat.
            if (run.Tech.ChargeCost > 0 && !TrySpendCharge(run.Tech.ChargeCost)) continue;

            Hit(target, part, EffectivePower(run.Tech));
            if (run.Tech.Kind == TechniqueKind.Timered) run.Countdown = run.Tech.Cooldown;
        }

        // Minions auto-fire on whatever the caster is pressing (the default front).
        if (_default is { Down: false })
            foreach (var minion in _bays.Values)
                Hit(_default, null, minion.Power);
    }

    // Apply a hit through the defender's mitigation layer: leather EVASION (a dodge roll on the
    // struck part-group) can negate it; a whole-HP hit is then blunted by the CON block. Part hits
    // run plate protection inside DamagePart. (Block on part hits is deferred — foes hit HP today.)
    private void Hit(ICombatTarget target, BodyPart? part, int power)
    {
        var frame = target.Frame;
        if (frame is not null && _rng is not null && _rng.Chance(frame.EvasionPercent(part)))
            return; // dodged

        if (part is null)
        {
            var blocked = frame?.BlockMitigation(BlockCap) ?? 0;
            var dealt = Math.Max(0, power - blocked);
            if (dealt > 0) target.Damage(dealt);
        }
        else target.DamagePart(part, power);
    }

    private void PruneSilenced()
    {
        foreach (var run in _active.Values.ToList())
            if (!_self.IsActive(Reservation(run.Tech)))
                _active.Remove(run.Tech.Id);

        // A drained stat dismisses a minion the same way it silences a technique.
        foreach (var minion in _bays.Values.ToList())
            if (!_self.IsActive(Reservation(minion)))
                _bays.Remove(minion.Id);
    }
}
