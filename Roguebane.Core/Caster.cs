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
        public int ResonanceStacks;  // Adept's Resonance: landed hits stack a −2%/stack charge discount (cap 5)
    }

    private const int HasteRate = 2; // % cooldown reduction per point of DEX (action speed)
    private const int HasteCap = 28; // ...capped so haste stays non-OP near 20 DEX

    private readonly Body _self;
    private Rng? _rng; // chance effects (evasion); set by Battle so a fight is reproducible
    private readonly bool _requireAim; // player doctrine: techniques fire ONLY at an explicit aim — no front fallback
    private bool _keepTargets;         // global player AUTO: ON => no module clears its target after firing
    private ICombatTarget? _default;
    private readonly SortedDictionary<string, Run> _active = new(StringComparer.Ordinal);
    // Ordered by MINION SLOT (§6e: "slot index IS the hotkey"), not by id — a fresh Summon appends to
    // the first free slot, a Dismiss compacts left, and ReorderMinion below repositions in place.
    private readonly List<Minion> _minions = new();
    private readonly Dictionary<string, int> _minionCountdown = new();

    public int Tick { get; private set; }

    private int _charge;
    public int MaxCharge { get; }
    public int Charge => _charge;

    // requireAim drives the PLAYER firing doctrine: a technique fires only at its own explicit aim
    // (untargeted holds, never falling back to a default front). Engine casters (foe offense, sims)
    // leave it false and keep the default-front auto-fire. Minions and Sustained reserves track the
    // front in BOTH modes — only the per-technique offensive FSM is gated.
    public Caster(Body self, ICombatTarget? target = null, int maxCharge = 0, bool requireAim = false, int minionCap = 0, int maxSummons = -1, bool freeSummons = false, CoreEffectKind effect = CoreEffectKind.None)
    {
        _self = self;
        _default = target;
        MaxCharge = maxCharge;
        _charge = maxCharge;
        _requireAim = requireAim;
        _keepTargets = !requireAim; // engine casters never clear (front auto-fire); player default = OFF (one-shot)
        MinionCap = minionCap;
        // §9/§14 deploy budget: unlimited unless the assembler passes one (Forge does for runs) —
        // bare unit-test casters stay resource-free.
        MaxSummons = maxSummons < 0 ? int.MaxValue : maxSummons;
        SummonsLeft = MaxSummons;
        _freeSummons = freeSummons;
        _effect = effect;
    }

    // The Summoner's Core Effect (Conscription, CORE_RUNES.md): fielding a minion never spends the
    // Summons resource at all — a different mechanic from the old refund-on-Redeploy Legion effect.
    private readonly bool _freeSummons;
    private readonly CoreEffectKind _effect;

    public int MinionCap { get; } // how many minion slots this caster's chassis has (for the render lane)

    // §9 SUMMONS [LOCKED]: the finite deploy resource. Paid ONCE per FRESH summon; an idle minion is
    // still summoned (free reactivation); merchant/loot refills top it up.
    public int MaxSummons { get; }
    public int SummonsLeft { get; private set; }

    public void AddSummons(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        SummonsLeft = Math.Min(MaxSummons, SummonsLeft + amount);
    }

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

    // Default-activation-state LOCK (DESIGN_SPEC "nothing starts charging... at the beginning of an
    // encounter") + RE-ARM SCOPE (DESIGN_SPEC §7, LOCKED 2026-07-05): techniques PERSIST across
    // back-to-back encounters (on/off state untouched, only the charge clock rewinds so leftover
    // charge can't discharge instantly); minions do NOT persist — every fielded minion is dismissed at
    // encounter end, full stop, so re-fielding one next encounter re-pays Summons like any fresh
    // summon (no carry-over, no "it was already out" discount).
    public void RearmForEncounter()
    {
        foreach (var run in _active.Values)
            if (run.Tech.Kind == TechniqueKind.Timered)
            {
                run.ResonanceStacks = 0; // Resonance decays fresh each encounter — no cross-fight snowball
                run.Countdown = EffectiveCooldown(run.Tech);
            }
        foreach (var minion in _minions.ToList()) // snapshot: Dismiss mutates _minions mid-loop
            Dismiss(minion);
    }

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
        _active.TryGetValue(t.Id, out var run);
        var cooldown = EffectiveCooldown(t, run?.ResonanceStacks ?? 0);
        if (run is null)
            return new TechStatus(false, cooldown, cooldown, t.Kind == TechniqueKind.Sustained, false, false, false);
        var dry = t.ShieldPiercing && _charge < Math.Max(1, t.ChargeCost); // only pierce draws charge
        var ready = run.Tech.Kind == TechniqueKind.Timered && run.Countdown <= 0;
        // Card's "auto" reflects the GLOBAL player AUTO (keep-targets), not the engine cadence flag.
        return new TechStatus(true, run.Countdown, cooldown, t.Kind == TechniqueKind.Sustained, dry, ready, _keepTargets);
    }

    public int ActiveCount => _active.Count;

    // A self-contained technique reserves its own stat; so does a Both-consulting dual-wield technique
    // (Frenzy/Flurry, RULES_SNAPSHOT) — its Reserve is the TECHNIQUE's own cost, distinct from the
    // reservation its consulted weapons already stand as equipped gear (SUSTAIN MODEL, §17 #16). A
    // Primary-consulting technique reserves NOTHING of its own: the one weapon it swings already
    // reserves as gear, and baking that into the technique's Active too would double-count the same
    // stat. Activate() below gates weapon-consulting techniques on having something to swing instead.
    private Active Reservation(Technique t)
    {
        if (t.Consults == WeaponUse.Primary) return new Active(t.Id, t.Stat, 0);
        var reserve = t.Reserve;
        if (t.Consults == WeaponUse.Both && _effect == CoreEffectKind.Finesse) reserve -= 1;
        if (_effect == CoreEffectKind.JackOfAllTrades) reserve -= 1;
        return new Active(t.Id, t.Stat, Math.Max(0, reserve));
    }

    private int EffectivePower(Technique t) => t.Consults == WeaponUse.None
        ? t.Power
        : t.Power + (int)Math.Round(
            _self.Consulted(t).Sum(w => w.Power) * t.DamageMult, MidpointRounding.AwayFromZero);

    // DEX haste shortens a technique's cooldown a modest % per point, capped (non-OP). Quoted in
    // ticks at the 10/sec combat clock. Read live so a smashed leg (DEX drop) slows you back down.
    // §6d: the consulting weapon's TIMER multiplies the charge timer on top (<1.0x = faster;
    // dual-wield = the AVERAGE of both weapons') — self-contained techniques are untouched; the
    // haste x timer interaction is a balance-pass knob, both simply scale the same counter.
    public int EffectiveCooldown(Technique t, int resonanceStacks = 0)
    {
        if (t.Cooldown <= 0) return t.Cooldown;
        var haste = Math.Min(HasteCap, _self.Capacity(Stat.Dex) * HasteRate);
        var ticks = t.Cooldown * (100 - haste) / 100.0;
        var consulted = _self.Consulted(t);
        if (consulted.Count > 0) ticks *= consulted.Average(w => w.Timer);
        if (resonanceStacks > 0) ticks *= 1.0 - 0.02 * resonanceStacks; // Adept's Resonance, cap 5 stacks
        return Math.Max(1, (int)Math.Round(ticks, MidpointRounding.AwayFromZero));
    }

    // Engine primitive (NOT the player AUTO toggle): auto defaults ON so any caster discharges on
    // cadence. The player also activates auto:on — a player technique holds only because requireAim
    // gives it no target until aimed, not because of a per-technique flag. auto:false is an engine/test
    // convenience for an unattended caster that should charge but not fire.
    public bool Activate(Technique technique, bool auto = true)
    {
        if (_active.ContainsKey(technique.Id)) return true;
        if (technique.Consults == WeaponUse.None)
        {
            // Sacrifice (ConsumesMinion) is a legitimate standing Reserve-0 toggle -- it costs a
            // MINION per discharge, not a stat -- so it skips the "Reserve<=0 is misconfigured" guard.
            if (technique.Reserve <= 0 && !technique.ConsumesMinion) return false;
        }
        else if (_self.Consulted(technique).Count == 0) return false; // nothing to swing
        var reservation = Reservation(technique);
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

    public int MinionCount => _minions.Count;
    public bool HasMinion(Minion minion) => _minions.Any(m => m.Id == minion.Id);
    public IReadOnlyList<Minion> Minions => _minions.ToList(); // summoned minions in slot order, for the render lane

    private static Active Reservation(Minion m) => new(m.Id, m.Stat, m.Reserve);

    // A fielded minion's flat evasion-reduction bonus (Hound: +5%, TECHNIQUES.md) — summed across
    // every minion that is actually firing (idle stat-gated minions contribute nothing), applied
    // against the DEFENDER'S evasion roll in Hit() below.
    private int AccuracyBonus => _minions
        .Where(m => m.Gate != MinionGate.Stat || _self.IsActive(Reservation(m)))
        .Sum(m => m.AccuracyBonus);

    // Summon a minion into a free slot: capped by the chassis's minion capacity, paid per its GATE — a stat
    // reservation (default), nothing (chassis-granted), or a one-off charge cost (alt-cost caster).
    // §9: a FRESH summon costs BOTH — 1 SUMMONS (the finite deploy resource, uniform across gates) and
    // its GATE — a stat reservation (default), nothing (chassis-granted), or the alt-cost placeholder.
    public bool Summon(Minion minion, int minionCap)
    {
        if (HasMinion(minion)) return true;
        if (_minions.Count >= minionCap) return false;
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
        if (!_freeSummons) SummonsLeft--; // Conscription: Summoner's minions never spend Summons
        _minions.Add(minion); // §6e ORDERING: click slots into the first free slot -> append
        _minionCountdown[minion.Id] = minion.Timer; // fresh summon starts charging, same as a technique
        return true;
    }

    public bool Dismiss(Minion minion)
    {
        var i = _minions.FindIndex(m => m.Id == minion.Id);
        if (i < 0) return false;
        _minions.RemoveAt(i); // §6e ORDERING: unslot compacts left (no holes)
        _minionCountdown.Remove(minion.Id);
        if (minion.Gate == MinionGate.Stat) _self.Deactivate(Reservation(minion)); // free the stat
        return true;
    }

    // §6e reorder: drag-and-drop insertion in the minion strip — same model as ReorderTechnique
    // (Expedition.cs), just against the minion list instead of the equipped-technique list. Pure
    // position mutation: no Summon/Dismiss, no reservation change.
    public bool ReorderMinion(Minion minion, int newIndex)
    {
        var i = _minions.FindIndex(m => m.Id == minion.Id);
        if (i < 0) return false;
        newIndex = Math.Clamp(newIndex, 0, _minions.Count - 1);
        if (i == newIndex) return true;
        _minions.RemoveAt(i);
        _minions.Insert(newIndex, minion);
        return true;
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

        // §9 [RESOLVED 2026-07-04]: a minion fires on its OWN Timer, not every combat tick / piggybacked
        // on the caster's front-target check. The countdown ticks unconditionally each Step (same as a
        // Timered technique's Run.Countdown) so an idle minion (gate stat drained) keeps charging in the
        // background; only the actual discharge is gated on being active + a live front.
        foreach (var minion in _minions)
        {
            var countdown = _minionCountdown.GetValueOrDefault(minion.Id, minion.Timer);
            if (countdown > 0) countdown--;
            var canFire = minion.Gate != MinionGate.Stat || _self.IsActive(Reservation(minion));
            if (countdown <= 0 && canFire && _default is { Down: false })
            {
                // §6d charm offhand: +0.1x MINION attack damage per tier of the held charm.
                Hit(_default, null, (int)Math.Round(
                    minion.Power * _self.CharmMinionMult, MidpointRounding.AwayFromZero));
                countdown = minion.Timer;
            }
            _minionCountdown[minion.Id] = countdown;
        }
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
            if (run.Tech.ConsumesMinion)
            {
                // Sacrifice: also holds fire with no minion fielded (same "wait for the condition"
                // shape as holding fire with no wound). Dismiss frees the minion's own reservation --
                // "no refund" (TECHNIQUES.md) means no Summons/re-summon grace, not a stuck stat.
                var minion = _minions.OrderByDescending(m => m.Reserve).FirstOrDefault();
                if (minion is null) return false;
                Dismiss(minion);
                _self.Repair(wound, 4 * minion.Reserve);
            }
            else
            {
                _self.Repair(wound, EffectivePower(run.Tech));
            }
            if (run.Tech.Kind == TechniqueKind.Timered) run.Countdown = EffectiveCooldown(run.Tech);
            return true;
        }

        var onAim = run.Aimed is { Down: false };
        // Player doctrine (requireAim): no live aim => HOLD, never falling back to the default front.
        var target = onAim ? run.Aimed : (_requireAim ? null : _default);
        if (target is null || target.Down) return false;

        var part = onAim ? run.Part : null;
        // CHARGE = shield-pierce fuel: only a shield-piercing technique spends it; dry => HOLD the pierce.
        // Ranger's Fletcher's Luck: a bow-consulting pierce has a 20% chance to cost no charge at all.
        if (run.Tech.ShieldPiercing)
        {
            var luckyFree = _effect == CoreEffectKind.FletcherLuck
                && _self.Consulted(run.Tech).Any(w => w.Kind == WeaponKind.Bow)
                && _rng is not null && _rng.Chance(20);
            if (!luckyFree && !TrySpendCharge(Math.Max(1, run.Tech.ChargeCost))) return false;
        }

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
        var landed = Hit(target, part, power, run.Tech.ShieldPiercing, wandCast);
        // Siphon lifesteal (TECHNIQUES.md, shared on-hit-boon gate): only on a CLEAN landed part-hit —
        // never a shield-absorbed hit, never an already-broken part. Heals by the damage just dealt.
        if (run.Tech.Lifesteal && landed)
        {
            var wound = _self.MostDamagedPart();
            if (wound is not null) _self.Repair(wound, power);
        }
        // Adept's Resonance (shared on-hit-boon gate): a landed hit stacks −2%/stack off this
        // technique's OWN next charge time, capped at 5 stacks.
        if (_effect == CoreEffectKind.Resonance && landed) run.ResonanceStacks = Math.Min(5, run.ResonanceStacks + 1);
        if (run.Tech.Kind == TechniqueKind.Timered) run.Countdown = EffectiveCooldown(run.Tech, run.ResonanceStacks);
        return true;
    }

    // Apply a hit through the defender's §8 mitigation: a full EVASION dodge (§6c leather, global,
    // leg-gated) negates it, else a shield pool absorbs; whatever lands hits the part AND HP together.
    // Returns whether it was a CLEAN part-hit (not dodged/absorbed/on-an-already-broken-part) — the
    // shared on-hit-boon gate (Siphon lifesteal today; any future on-hit boon reuses this signal).
    private bool Hit(ICombatTarget target, BodyPart? part, int power, bool pierce = false,
        bool subtract = false)
    {
        var frame = target.Frame;
        if (frame is not null && _rng is not null &&
            _rng.Chance(Math.Max(0, frame.EvasionPercent() - AccuracyBonus)))
            return false; // dodged

        var wasBroken = part is not null && frame is not null && frame.Contribution(part) == 0;
        var shielded = frame is not null && frame.ShieldPoints > 0;

        // §6b shields are the ONLY damage mitigation now (alongside a full evade above): points absorb
        // the hit before it lands. A SHIELD-PIERCING hit (Charge-fuelled) skips the pool entirely.
        // A WAND cast (§10) SUBTRACTS the standing pool from its damage WITHOUT consuming it —
        // big stacks blunt wands; wands chip through what stands.
        if (frame is not null && !pierce)
        {
            power = subtract ? Math.Max(0, power - frame.ShieldPoints) : frame.AbsorbShields(power);
            if (power <= 0) return false;
        }

        // §8 [LOCKED]: every hit deals BOTH — the targeted PART's stat AND HP — simultaneously, the same
        // power, from the one hit. No part-vs-HP split, no HP-only-on-overkill path, no flat CON block or
        // plate blunt (shields + full evade are the ONLY mitigations).
        if (part is not null) frame?.Damage(part, power);
        target.Damage(power);
        return part is not null && !wasBroken && !shielded;
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
        // (paid once) and re-raises FREE the moment its stat recovers. Idle minions hold their slot but
        // do not fire (the auto-fire loop skips them).
        foreach (var minion in _minions)
            if (minion.Gate == MinionGate.Stat && !_self.IsActive(Reservation(minion)))
                _self.Activate(Reservation(minion)); // free re-raise; fails harmlessly while short
    }
}
