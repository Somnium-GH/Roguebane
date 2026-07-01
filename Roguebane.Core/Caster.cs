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
        public ICombatTarget? Aimed; // per-technique target; null => no target (player) or default front (engine)
        public BodyPart? Part;       // per-technique PART aim within Aimed; null => whole-target HP
        public bool Auto = true;     // AUTO: discharge on cadence when ready. Off => hold for a Fire() command.
    }

    private const int BlockCap = 3;  // a held CON block absorbs at most this much off an HP hit (low scale)
    private const int HasteRate = 2; // % cooldown reduction per point of DEX (action speed)
    private const int HasteCap = 28; // ...capped so haste stays non-OP near 20 DEX

    private readonly Body _self;
    private Rng? _rng; // chance effects (evasion); set by Battle so a fight is reproducible
    private readonly bool _requireAim; // player doctrine: techniques fire ONLY at an explicit aim — no front fallback
    private bool _keepTargets;         // global player AUTO: ON => no module clears its target after firing
    private ICombatTarget? _default;
    private readonly SortedDictionary<string, Run> _active = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, Minion> _bays = new(StringComparer.Ordinal);

    public int Tick { get; private set; }

    private int _charge;
    public int MaxCharge { get; }
    public int Charge => _charge;

    // requireAim drives the PLAYER firing doctrine: a technique fires only at its own explicit aim
    // (untargeted holds, never falling back to a default front). Engine casters (foe offense, sims)
    // leave it false and keep the default-front auto-fire. Minions and Sustained reserves track the
    // front in BOTH modes — only the per-technique offensive FSM is gated.
    public Caster(Body self, ICombatTarget? target = null, int maxCharge = 0, bool requireAim = false, int bayCap = 0)
    {
        _self = self;
        _default = target;
        MaxCharge = maxCharge;
        _charge = maxCharge;
        _requireAim = requireAim;
        _keepTargets = !requireAim; // engine casters never clear (front auto-fire); player default = OFF (one-shot)
        BayCap = bayCap;
    }

    public int BayCap { get; } // how many minion bays this caster's chassis has (for the render lane)

    // Battle hands every caster the fight's shared PRNG so chance rolls are deterministic.
    public void UseRng(Rng rng) => _rng = rng;

    // Charge (the finite shield-pierce resource, §6b) is refilled out of combat (loot / rest), not
    // regenerated mid-fight.
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

    // Dismiss a technique's target: it drops its own aim. A player technique (requireAim) goes untargeted
    // and HOLDS; an engine technique falls back to the default front. (FSM: right-click clears the target.)
    // Leaves the technique active and its flags untouched.
    public void ClearAim(Technique technique)
    {
        if (_active.TryGetValue(technique.Id, out var run)) { run.Aimed = null; run.Part = null; }
    }

    public bool IsActive(Technique technique) => _active.ContainsKey(technique.Id);

    // FTL firing model: a Timered technique charges down to ready, then HOLDS (Ready) until fired —
    // on command (Fire) or, if Auto, automatically every cadence. Toggle Auto per technique.
    public void SetAuto(Technique technique, bool auto)
    {
        if (_active.TryGetValue(technique.Id, out var run)) run.Auto = auto;
    }

    public bool IsAuto(Technique technique) =>
        _active.TryGetValue(technique.Id, out var run) && run.Auto;

    // The GLOBAL player AUTO toggle: ON => no module clears its target after firing (every powered +
    // targeted module keeps charging and firing at the SAME target); OFF (default) => one-shot, each
    // module clears its target after the shot. One switch governs the whole bar. Distinct from the
    // per-technique engine Auto flag above (discharge-on-cadence).
    public void SetAutoAll(bool keepTargets) => _keepTargets = keepTargets;
    public bool AutoAll => _keepTargets;

    // A charged technique is HOLDING when its countdown has elapsed (ready to discharge).
    public bool IsReady(Technique technique) =>
        _active.TryGetValue(technique.Id, out var run) && run.Countdown <= 0;

    // The technique's effective target for rendering: its own aim, else the default front — but a
    // player technique (requireAim) reports NO target when unaimed (it holds and won't fire).
    public ICombatTarget? AimOf(Technique technique) =>
        _active.TryGetValue(technique.Id, out var run) ? run.Aimed ?? (_requireAim ? null : _default) : null;

    // The technique's per-target PART aim (which foe part it strikes), or null for whole-target HP.
    public BodyPart? PartOf(Technique technique) =>
        _active.TryGetValue(technique.Id, out var run) ? run.Part : null;

    // Fire a charged technique NOW at its aim. No-op (false) if not active, not yet ready, sustained,
    // or the discharge can't land (no target / dry charge). Resets the cooldown on a hit.
    public bool Fire(Technique technique)
    {
        if (!_active.TryGetValue(technique.Id, out var run)) return false;
        if (run.Tech.Kind == TechniqueKind.Sustained || run.Countdown > 0) return false;
        return Discharge(run);
    }

    // A render-facing snapshot of one technique's live state for the action bar. Countdown/Cooldown
    // drive the cooldown fill; the flags pick the card state (held / charging-dry / ready / auto).
    public readonly record struct TechStatus(
        bool Active, int Countdown, int Cooldown, bool Sustained, bool ChargeDry, bool Ready, bool Auto);

    public TechStatus StatusOf(Technique t)
    {
        var cooldown = EffectiveCooldown(t);
        if (!_active.TryGetValue(t.Id, out var run))
            return new TechStatus(false, cooldown, cooldown, t.Kind == TechniqueKind.Sustained, false, false, false);
        var dry = t.ShieldPiercing && _charge < Math.Max(1, t.ChargeCost); // only pierce draws charge
        var ready = run.Tech.Kind == TechniqueKind.Timered && run.Countdown <= 0;
        // Card's "auto" reflects the GLOBAL player AUTO (keep-targets), not the engine cadence flag.
        return new TechStatus(true, run.Countdown, cooldown, t.Kind == TechniqueKind.Sustained, dry, ready, _keepTargets);
    }

    public int ActiveCount => _active.Count;

    // A self-contained technique reserves its own stat; a weapon-consulting one reserves the sum of
    // its consulted weapons' reserves (you can't swing what you aren't holding — reserve 0 → can't).
    private Active Reservation(Technique t) => t.Consults == WeaponUse.None
        ? new(t.Id, t.Stat, t.Reserve)
        : new(t.Id, t.Stat, _self.Consulted(t).Sum(w => w.Reserve));

    private int EffectivePower(Technique t) => t.Consults == WeaponUse.None
        ? t.Power
        : t.Power + _self.Consulted(t).Sum(w => w.Power);

    // DEX haste shortens a technique's cooldown a modest % per point, capped (non-OP). Quoted in
    // ticks at the 10/sec combat clock. Read live so a smashed leg (DEX drop) slows you back down.
    public int EffectiveCooldown(Technique t)
    {
        if (t.Cooldown <= 0) return t.Cooldown;
        var haste = Math.Min(HasteCap, _self.Capacity(Stat.Dex) * HasteRate);
        return Math.Max(1, t.Cooldown * (100 - haste) / 100);
    }

    // Engine primitive: auto defaults ON (an unattended caster — foe offense, balance sim — fires on
    // cadence). The PLAYER path activates with auto:false so a technique charges and HOLDS until fired.
    public bool Activate(Technique technique, bool auto = true)
    {
        if (_active.ContainsKey(technique.Id)) return true;
        var reservation = Reservation(technique);
        if (reservation.Reserve <= 0) return false; // nothing to swing (no weapon to consult)
        if (!_self.Activate(reservation)) return false;
        _active[technique.Id] = new Run { Tech = technique, Countdown = EffectiveCooldown(technique), Auto = auto };
        if (technique.ShieldLayers > 0) _self.RaiseShield(technique.Id, technique.ShieldLayers, ShieldRegenTicks(technique));
        return true;
    }

    // A shield source regenerates a layer faster the more CON the body carries (§6b: CON scales regen
    // for ALL sources). Placeholder scaling — the actual numbers are a Needs-human balance pass.
    private int ShieldRegenTicks(Technique t) =>
        Math.Max(1, t.ShieldRegen * 10 / (10 + _self.Capacity(Stat.Con)));

    public void Deactivate(Technique technique)
    {
        if (!_active.Remove(technique.Id)) return;
        _self.Deactivate(Reservation(technique));
        if (technique.ShieldLayers > 0) _self.DropShield(technique.Id);
    }

    public int MinionCount => _bays.Count;
    public bool HasMinion(Minion minion) => _bays.ContainsKey(minion.Id);
    public IReadOnlyList<Minion> Minions => _bays.Values.ToList(); // bay occupants, for the render lane

    private static Active Reservation(Minion m) => new(m.Id, m.Stat, m.Reserve);

    // Summon a minion into a free bay: capped by the chassis's bay count, paid per its GATE — a stat
    // reservation (default), nothing (chassis-granted), or a one-off charge cost (alt-cost caster).
    public bool Summon(Minion minion, int bayCap)
    {
        if (_bays.ContainsKey(minion.Id)) return true;
        if (_bays.Count >= bayCap) return false;
        switch (minion.Gate)
        {
            case MinionGate.None: break; // ungated loyal ally — no cost
            // Alt-cost is a DESIGNED cost (HP or a stat, §9) — NOT Charge; Charge is the shield-pierce
            // resource now. No alt-cost minion is authored yet + HP isn't reachable here, so it is an
            // un-costed placeholder until one ships (then wire the real HP/stat spend).
            case MinionGate.AltCost: break;
            default: if (!_self.Activate(Reservation(minion))) return false; break;
        }
        _bays[minion.Id] = minion;
        return true;
    }

    public void Dismiss(Minion minion)
    {
        if (!_bays.Remove(minion.Id)) return;
        if (minion.Gate == MinionGate.Stat) _self.Deactivate(Reservation(minion)); // free the stat
    }

    public void Step()
    {
        Tick++;
        PruneSilenced();
        _self.TickShields(); // regenerate held shield layers on the combat tick (§6b)

        foreach (var run in _active.Values)
        {
            if (run.Tech.Kind == TechniqueKind.Sustained)
            {
                Discharge(run); // a held output: fires every tick (e.g. the CON block reserve)
                continue;
            }

            // Timered: charge down to ready, then HOLD. Auto discharges on cadence (only if a target
            // resolves — an untargeted player technique just holds); a non-auto technique waits for a
            // Fire() command. A one-shot (non-persist) technique drops its target after a landed shot.
            if (run.Countdown > 0) run.Countdown--;
            if (run.Countdown <= 0 && run.Auto && Discharge(run) && !_keepTargets)
            {
                run.Aimed = null;
                run.Part = null;
            }
        }

        // Minions auto-fire on whatever the caster is pressing (the default front).
        if (_default is { Down: false })
            foreach (var minion in _bays.Values)
                Hit(_default, null, minion.Power);
    }

    // Resolve a technique's aim and land one discharge: hits its own foe (and PART) while that foe
    // stands, else falls back to the caster's front (whole-HP). Holds fire if there's no target or
    // (for a shield-piercing technique) its Charge is dry; resets a Timered cooldown on a landed hit.
    private bool Discharge(Run run)
    {
        // A part-heal ignores targets: it mends the caster's own most-damaged part. Holds fire (and
        // keeps its cooldown ready) when nothing is hurt, so it fires the instant a part takes damage.
        if (run.Tech.Heals)
        {
            var wound = _self.MostDamagedPart();
            if (wound is null) return false;
            _self.Repair(wound, EffectivePower(run.Tech));
            if (run.Tech.Kind == TechniqueKind.Timered) run.Countdown = EffectiveCooldown(run.Tech);
            return true;
        }

        var onAim = run.Aimed is { Down: false };
        // Player doctrine (requireAim): no live aim => HOLD, never falling back to the default front.
        var target = onAim ? run.Aimed : (_requireAim ? null : _default);
        if (target is null || target.Down) return false;

        var part = onAim ? run.Part : null;
        // CHARGE = shield-pierce fuel: only a shield-piercing technique spends it; dry => HOLD the pierce.
        if (run.Tech.ShieldPiercing && !TrySpendCharge(Math.Max(1, run.Tech.ChargeCost))) return false;

        Hit(target, part, EffectivePower(run.Tech), run.Tech.ShieldPiercing);
        if (run.Tech.Kind == TechniqueKind.Timered) run.Countdown = EffectiveCooldown(run.Tech);
        return true;
    }

    // Apply a hit through the defender's mitigation layer: leather EVASION (a dodge roll on the
    // struck part-group) can negate it; a whole-HP hit is then blunted by the CON block. Part hits
    // run plate protection inside DamagePart. (Block on part hits is deferred — foes hit HP today.)
    private void Hit(ICombatTarget target, BodyPart? part, int power, bool pierce = false)
    {
        var frame = target.Frame;
        if (frame is not null && _rng is not null && _rng.Chance(frame.EvasionPercent(part)))
            return; // dodged

        // §6b shields are the OUTERMOST layer: they eat damage before armor/block/parts. Body-wide,
        // so they apply to both whole-HP and part-aimed hits; a fully-absorbed hit lands nothing.
        // A SHIELD-PIERCING hit (Charge-fuelled) skips the pool entirely.
        if (frame is not null && !pierce)
        {
            power = frame.AbsorbShields(power);
            if (power <= 0) return;
        }

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
            {
                _active.Remove(run.Tech.Id);
                if (run.Tech.ShieldLayers > 0) _self.DropShield(run.Tech.Id); // a smashed source sheds its shield
            }

        // A drained stat dismisses a STAT-gated minion the same way it silences a technique. Ungated
        // and alt-cost minions hold no reservation, so the cascade leaves them standing.
        foreach (var minion in _bays.Values.ToList())
            if (minion.Gate == MinionGate.Stat && !_self.IsActive(Reservation(minion)))
                _bays.Remove(minion.Id);
    }
}
