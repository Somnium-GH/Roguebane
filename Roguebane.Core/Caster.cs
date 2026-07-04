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
    public Caster(Body self, ICombatTarget? target = null, int maxCharge = 0, bool requireAim = false, int bayCap = 0, int maxSummons = -1)
    {
        _self = self;
        _default = target;
        MaxCharge = maxCharge;
        _charge = maxCharge;
        _requireAim = requireAim;
        _keepTargets = !requireAim; // engine casters never clear (front auto-fire); player default = OFF (one-shot)
        BayCap = bayCap;
        // §9/§14 deploy budget: unlimited unless the assembler passes one (Forge does for runs) —
        // bare unit-test casters stay resource-free.
        MaxSummons = maxSummons < 0 ? int.MaxValue : maxSummons;
        SummonsLeft = MaxSummons;
    }

    public int BayCap { get; } // how many minion bays this caster's chassis has (for the render lane)

    // §9 SUMMONS [LOCKED]: the finite deploy resource. Paid ONCE per FRESH summon; an idle minion is
    // still summoned (free reactivation); merchant/loot refills top it up.
    public int MaxSummons { get; }
    public int SummonsLeft { get; private set; }

    public void AddSummons(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        SummonsLeft = Math.Min(MaxSummons, SummonsLeft + amount);
    }

    // The Summoner's Core Effect hook (§11): refund one Summons per surviving minion on Redeploy.
    public void RefundSummons(int amount) => AddSummons(amount);

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

    // The ONE AUTO toggle (global, player-facing): ON => no module clears its target after firing (every
    // powered + targeted module keeps charging and firing at the SAME target); OFF (default) => one-shot,
    // each module clears its target after the shot and then holds. One switch governs the whole bar —
    // there is no per-weapon AUTO. (Run.Auto below is a separate engine-only primitive: whether an
    // UNATTENDED caster — foe offense, minions, balance sim — discharges on cadence; always on for the
    // player, whose holding comes from requireAim, not from a per-technique flag.)
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
    // §6d: the consulting weapon's TIMER multiplies the charge timer on top (<1.0x = faster;
    // dual-wield = the AVERAGE of both weapons') — self-contained techniques are untouched; the
    // haste x timer interaction is a balance-pass knob, both simply scale the same counter.
    public int EffectiveCooldown(Technique t)
    {
        if (t.Cooldown <= 0) return t.Cooldown;
        var haste = Math.Min(HasteCap, _self.Capacity(Stat.Dex) * HasteRate);
        var ticks = t.Cooldown * (100 - haste) / 100.0;
        var consulted = _self.Consulted(t);
        if (consulted.Count > 0) ticks *= consulted.Average(w => w.Timer);
        return Math.Max(1, (int)Math.Round(ticks, MidpointRounding.AwayFromZero));
    }

    // Engine primitive (NOT the player AUTO toggle): auto defaults ON so any caster discharges on
    // cadence. The player also activates auto:on — a player technique holds only because requireAim
    // gives it no target until aimed, not because of a per-technique flag. auto:false is an engine/test
    // convenience for an unattended caster that should charge but not fire.
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
    // §9: a FRESH summon costs BOTH — 1 SUMMONS (the finite deploy resource, uniform across gates) and
    // its GATE — a stat reservation (default), nothing (chassis-granted), or the alt-cost placeholder.
    public bool Summon(Minion minion, int bayCap)
    {
        if (_bays.ContainsKey(minion.Id)) return true;
        if (_bays.Count >= bayCap) return false;
        if (SummonsLeft <= 0) return false;
        switch (minion.Gate)
        {
            case MinionGate.None: break; // ungated loyal ally — no reservation (Summons still spends)
            // Alt-cost is a DESIGNED cost (HP or a stat, §9) — NOT Charge; Charge is the shield-pierce
            // resource now. No alt-cost minion is authored yet + HP isn't reachable here, so it is an
            // un-costed placeholder until one ships (then wire the real HP/stat spend).
            case MinionGate.AltCost: break;
            default: if (!_self.Activate(Reservation(minion))) return false; break;
        }
        SummonsLeft--;
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
                Discharge(run); // a held output: fires every tick (e.g. a shield source keeping its pool up)
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

        // Minions auto-fire on whatever the caster is pressing (the default front); an IDLE minion
        // (gate stat drained, §9) holds its bay but stays silent until the stat recovers.
        if (_default is { Down: false })
            foreach (var minion in _bays.Values)
                if (minion.Gate != MinionGate.Stat || _self.IsActive(Reservation(minion)))
                    // §6d charm offhand: +0.1x MINION attack damage per tier of the held charm.
                    Hit(_default, null, (int)Math.Round(
                        minion.Power * _self.CharmMinionMult, MidpointRounding.AwayFromZero));
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

        // §6c INT robe: +2 SPELL DAMAGE per worn sustained piece (2-piece cap) — damage only,
        // never heals (the Repair branch above stays unbuffed) and only INT-stat casts.
        var robe = run.Tech.Stat == Stat.Int ? _self.SpellDamageBonus : 0;
        // §10 wand redefinition: a cast consulting WANDS resolves by shield-SUBTRACTION — the
        // standing pool blunts the damage without being consumed. (Dual wands are legal; a staff
        // is 2H/no-dual so a mixed wand+staff consult can't arise.)
        var consulted = _self.Consulted(run.Tech);
        var wandCast = consulted.Count > 0 && consulted.All(w => w.Kind == WeaponKind.Wand);
        var power = EffectivePower(run.Tech) + robe;
        // §6d tome offhand: +0.1x SPELL damage per tier — INT casts only, applied over the whole
        // spell damage (base + robe; composition order is a balance-pass knob).
        if (run.Tech.Stat == Stat.Int)
            power = (int)Math.Round(power * _self.TomeSpellMult, MidpointRounding.AwayFromZero);
        Hit(target, part, power, run.Tech.ShieldPiercing, wandCast);
        if (run.Tech.Kind == TechniqueKind.Timered) run.Countdown = EffectiveCooldown(run.Tech);
        return true;
    }

    // Apply a hit through the defender's §8 mitigation: a full EVASION dodge (§6c leather, global,
    // leg-gated) negates it, else a shield pool absorbs; whatever lands hits the part AND HP together.
    private void Hit(ICombatTarget target, BodyPart? part, int power, bool pierce = false,
        bool subtract = false)
    {
        var frame = target.Frame;
        if (frame is not null && _rng is not null && _rng.Chance(frame.EvasionPercent()))
            return; // dodged

        // §6b shields are the ONLY damage mitigation now (alongside a full evade above): points absorb
        // the hit before it lands. A SHIELD-PIERCING hit (Charge-fuelled) skips the pool entirely.
        // A WAND cast (§10) SUBTRACTS the standing pool from its damage WITHOUT consuming it —
        // big stacks blunt wands; wands chip through what stands.
        if (frame is not null && !pierce)
        {
            power = subtract ? Math.Max(0, power - frame.ShieldPoints) : frame.AbsorbShields(power);
            if (power <= 0) return;
        }

        // §8 [LOCKED]: every hit deals BOTH — the targeted PART's stat AND HP — simultaneously, the same
        // power, from the one hit. No part-vs-HP split, no HP-only-on-overkill path, no flat CON block or
        // plate blunt (shields + full evade are the ONLY mitigations).
        if (part is not null) frame?.Damage(part, power);
        target.Damage(power);
    }

    private void PruneSilenced()
    {
        foreach (var run in _active.Values.ToList())
            if (!_self.IsActive(Reservation(run.Tech)))
            {
                _active.Remove(run.Tech.Id);
                if (run.Tech.ShieldLayers > 0) _self.DropShield(run.Tech.Id); // a smashed source sheds its shield
            }

        // §9 [LOCKED 2026-07-02]: a drained stat only IDLES a stat-gated minion — it stays SUMMONED
        // (paid once) and re-raises FREE the moment its stat recovers. Idle minions hold their bay but
        // do not fire (the auto-fire loop skips them).
        foreach (var minion in _bays.Values)
            if (minion.Gate == MinionGate.Stat && !_self.IsActive(Reservation(minion)))
                _self.Activate(Reservation(minion)); // free re-raise; fails harmlessly while short
    }
}
