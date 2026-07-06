namespace Roguebane.Core;

// The socketed body on the new model. The live stat pool is DERIVED from intact parts, not fixed:
// damage a part and the stat it feeds shrinks, which can no longer cover what equipment/abilities
// have reserved — so they fall off. This single rule unifies graded degradation, equip/ability
// fall-off, and the allocation economy.
public sealed class Body
{
    private readonly List<BodyPart> _parts = new();
    private readonly Dictionary<string, int> _intact = new();
    private readonly List<Active> _actives = new(); // engagement order; cascade sheds newest first
    private readonly Dictionary<Stat, Armor> _armor = new(); // one piece per part-group (keyed by Stat)
    private readonly List<Weapon> _hands = new(); // up to two; anatomical hand count
    private readonly Dictionary<string, ShieldPool> _shields = new(); // §6b shield sources, keyed by source id

    // Equip order, for the DISABLE CASCADE's last-equipped-first tiebreak (SUSTAIN MODEL, §17 #16).
    // Index-parallel to _hands rather than keyed by item identity: Weapon/Armor are records
    // (structural equality), so a value-keyed map would collide two identically-tiered pieces
    // (e.g. dual-wielding twin daggers).
    private int _equipSeq;
    private readonly List<int> _handSeq = new();
    private int? _rangedSeq;
    private readonly Dictionary<Stat, int> _armorSeq = new();
    private CoreEffectKind _effect = CoreEffectKind.None;

    public IReadOnlyList<BodyPart> Parts => _parts;
    public IReadOnlyList<Active> Actives => _actives;

    // Set once at assembly (CoreRune.NewBody) so every equip-time/sustain-time gate below can apply
    // its core's discount without threading the enum through every call site.
    public void SetCoreEffect(CoreEffectKind effect) => _effect = effect;

    // WarlordMight/FletcherLuck/JackOfAllTrades: an equip-time discount on what a weapon costs to
    // wield/ready. Shared by Wield/EquipRanged AND DisabledGear's ongoing sustain math — both must
    // agree on the same discounted number or the equip gate and the post-damage cascade desync.
    private int EffectiveWeaponReserve(Weapon w)
    {
        var r = w.Reserve;
        if (_effect == CoreEffectKind.WarlordMight && w.Hands == 2 && w.Stat == Stat.Str) r -= 3;
        if (_effect == CoreEffectKind.FletcherLuck && w.Kind == WeaponKind.Bow) r -= w.Tier;
        if (_effect == CoreEffectKind.JackOfAllTrades) r -= 1;
        return Math.Max(0, r);
    }

    // Fortified/WarlordMight/JackOfAllTrades: an equip-time discount on armor's governing attribute
    // and requirement. Fortified reassigns Plate's governing STR to CON (paid in CON instead) at a
    // per-tier rate; WarlordMight's STR-plate discount is a flat per-piece amount — two distinct
    // formula shapes, not one shared abstraction (see plan).
    private (Stat Governing, int Requirement) EffectiveArmor(Armor piece)
    {
        var governing = piece.Governing;
        var req = piece.Requirement;
        if (_effect == CoreEffectKind.Fortified && piece.Line == ArmorLine.Plate)
        {
            governing = Stat.Con;
            req -= piece.Tier;
        }
        if (_effect == CoreEffectKind.WarlordMight && piece.Line == ArmorLine.Plate) req -= 1;
        if (_effect == CoreEffectKind.JackOfAllTrades) req -= 1;
        return (governing, Math.Max(0, req));
    }

    public void Add(BodyPart part)
    {
        _parts.Add(part);
        _intact[part.Id] = part.Capacity;
    }

    public int Contribution(BodyPart part) => _intact.GetValueOrDefault(part.Id);

    public int Capacity(Stat stat) => _parts.Where(p => p.Stat == stat).Sum(Contribution);

    // SUSTAIN MODEL [DESIGN_SPEC §17 #16, resolved 2026-07-04]: gear and active techniques draw on
    // the SAME shared pool per stat. Reserved = techniques (always-on once activated) + whatever
    // gear currently fits what's left (see DisabledGear) — so Available reflects the true headroom
    // for a new technique activation, gear equip disable is separate (raw Capacity gate at equip
    // time, unchanged) from this ongoing sustain accounting. Exposed as two public halves (not just
    // the combined Reserved total) so the UI can draw the DESIGN_SPEC 4-zone pip bar (gear zone vs.
    // technique zone are visually distinct — §7 "ATTRIBUTE PIP BAR — 4-ZONE ENCODING").
    public int TechReserved(Stat stat) => _actives.Where(a => a.Stat == stat).Sum(a => a.Reserve);

    public int GearReserved(Stat stat) => DisabledGear(stat).EnabledTotal;

    public int Reserved(Stat stat) => TechReserved(stat) + GearReserved(stat);

    public int Available(Stat stat) => Capacity(stat) - Reserved(stat);

    // Capacity lost to damage, per stat -- the gap between a part's undamaged Capacity and its
    // current Contribution. Distinct from Reserved(): reserved pool is still THERE, just spoken
    // for; damaged pool is gone until healed. The pip bar's 4th zone (§7 4-ZONE ENCODING).
    public int Damaged(Stat stat) => _parts.Where(p => p.Stat == stat).Sum(p => p.Capacity - Contribution(p));

    private readonly record struct GearCandidate(string Kind, int Reserve, int Seq, int HandIndex, Stat ArmorSlot);

    private sealed record GearDisable(HashSet<int> Hands, bool Ranged, HashSet<Stat> Armor, int EnabledTotal);

    // DISABLE CASCADE (§17 #16): when gear's combined reserve exceeds what's left of the pool after
    // techniques take their share, items disable highest-requirement-first, ties last-equipped-first
    // — a pure ranking over current attr level, so healing re-enables cheapest-first automatically.
    private GearDisable DisabledGear(Stat stat, int? techReservedOverride = null)
    {
        var candidates = new List<GearCandidate>();
        for (var i = 0; i < _hands.Count; i++)
            if (_hands[i].Stat == stat) candidates.Add(new GearCandidate("hand", EffectiveWeaponReserve(_hands[i]), _handSeq[i], i, default));
        if (_ranged is { } r && r.Stat == stat)
            candidates.Add(new GearCandidate("ranged", EffectiveWeaponReserve(r), _rangedSeq ?? 0, -1, default));
        foreach (var (slot, piece) in _armor)
        {
            var eff = EffectiveArmor(piece);
            if (eff.Governing == stat)
                candidates.Add(new GearCandidate("armor", eff.Requirement, _armorSeq[slot], -1, slot));
        }

        var pool = Math.Max(0, Capacity(stat) - (techReservedOverride ?? TechReserved(stat)));
        var remaining = candidates.Sum(c => c.Reserve);
        var hands = new HashSet<int>();
        var armor = new HashSet<Stat>();
        var ranged = false;

        foreach (var c in candidates.OrderByDescending(c => c.Reserve).ThenByDescending(c => c.Seq))
        {
            if (remaining <= pool) break;
            remaining -= c.Reserve;
            switch (c.Kind)
            {
                case "hand": hands.Add(c.HandIndex); break;
                case "ranged": ranged = true; break;
                case "armor": armor.Add(c.ArmorSlot); break;
            }
        }
        return new GearDisable(hands, ranged, armor, remaining);
    }

    public bool IsActive(Active active) => _actives.Contains(active);

    public bool Activate(Active active)
    {
        if (_actives.Contains(active)) return true;
        // Gate on raw Capacity-minus-TechReserved, NOT Available(): Available() nets out gear at
        // its CURRENT (pre-activation) size, so a technique landing on an already-full pool would
        // see zero room even though SUSTAIN MODEL gear is meant to yield to it (DisabledGear already
        // recomputes gear's fit from TechReserved once this active is counted -- see Reserved()).
        if (Capacity(active.Stat) - TechReserved(active.Stat) < active.Reserve) return false;
        _actives.Add(active);
        return true;
    }

    public void Deactivate(Active active) => _actives.Remove(active);

    public void Damage(BodyPart part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        // §6c STR plate: a sustained piece soaks the covered part's OWN damage per tier —
        // never HP damage (§8: shields + full evade stay the only HP mitigations).
        if (_armor.TryGetValue(part.Stat, out var worn) && worn.PartMitigation > 0 && ArmorSustained(worn))
            amount = Math.Max(0, amount - worn.PartMitigation);
        _intact[part.Id] = Math.Max(0, Contribution(part) - amount);
        Cascade(part.Stat);
        // §6 [LOCKED]: gear does NOT fall off below its threshold — it stays ASSIGNED, reads
        // DISABLED (red), stops answering (UsableHands), and re-activates when the stat heals.
    }

    public IReadOnlyList<Weapon> Hands => _hands;

    // Wield a weapon into a free hand if the body can lift it (stat capacity meets its threshold).
    // §6d: bows/slings are RANGED-slot items, never hand items; a wand or staff cannot share a
    // build with an occupied ranged slot (mutual exclusion; the staff blocks ranged like a shield).
    public bool Wield(Weapon weapon)
    {
        if (weapon.Kind is WeaponKind.Bow or WeaponKind.Sling) return false;
        if (weapon.Kind is WeaponKind.Wand or WeaponKind.Staff && _ranged is not null) return false;
        if (_hands.Count >= 2) return false;
        if (Capacity(weapon.Stat) < EffectiveWeaponReserve(weapon)) return false;
        _hands.Add(weapon);
        _handSeq.Add(++_equipSeq);
        return true;
    }

    public void Unwield(Weapon weapon)
    {
        var i = _hands.IndexOf(weapon);
        if (i < 0) return;
        _hands.RemoveAt(i);
        _handSeq.RemoveAt(i);
    }

    // §6d: ONE ranged slot, independent of the melee hands — a sword+shield AND a bow coexist.
    private Weapon? _ranged;
    public Weapon? Ranged => _ranged;

    public bool EquipRanged(Weapon w)
    {
        if (w.Kind is not (WeaponKind.Bow or WeaponKind.Sling)) return false;
        if (_ranged is not null) return false;
        if (Capacity(w.Stat) < EffectiveWeaponReserve(w)) return false;
        // Mutual exclusion (§6d): a held wand excludes the ranged slot; a staff blocks it too.
        if (_hands.Any(h => h.Kind is WeaponKind.Wand or WeaponKind.Staff)) return false;
        _ranged = w;
        _rangedSeq = ++_equipSeq;
        return true;
    }

    public Weapon? UnequipRanged()
    {
        var r = _ranged;
        _ranged = null;
        _rangedSeq = null;
        return r;
    }

    // §6d arm gates at USE time: a BOW needs both arms unbroken (same as any 2H); the 1H SLING
    // needs one usable throwing arm. The item stays ASSIGNED either way (§6e).
    public bool RangedUsable => _ranged is { } r
        && !DisabledGear(r.Stat).Ranged
        && (r.Kind == WeaponKind.Bow ? HandUsable(0) && HandUsable(1)
                                     : HandUsable(0) || HandUsable(1));

    // §6d/§6 hard override: a hand's weapon works only while its ARM stands — a broken arm's
    // hand slot is physically gone regardless of which stat the weapon gates on, for player and
    // foe alike (§8 symmetry). Hand 0 rides the SECOND Str part (armR, dominant), hand 1 the
    // first (armL), mirroring the figure's socket order. Bodies without arm parts don't gate.
    public bool HandUsable(int handIndex)
    {
        var arms = _parts.Where(p => p.Stat == Stat.Str).ToList();
        if (arms.Count == 0) return true;
        var ix = handIndex == 0 ? Math.Min(1, arms.Count - 1) : 0;
        return Contribution(arms[ix]) > 0;
    }

    // §6/§6e: a hand item WORKS only while its arm stands AND its stat sustains its reserve —
    // otherwise it stays ASSIGNED (the red card), stops answering, and leaves the render.
    public bool HandItemUsable(int i) => i < _hands.Count
        && HandUsable(i) && !DisabledGear(_hands[i].Stat).Hands.Contains(i);

    private IEnumerable<Weapon> UsableHands() => _hands.Where((_, i) => HandItemUsable(i));

    // §6d magic offhands (+0.1x per tier): ONE off-hand slot exists, so the best USABLE held
    // piece counts — a broken arm silences its bonus like any other hand item.
    public double CharmMinionMult => 1.0 + 0.1 * UsableHands()
        .Where(w => w.Kind == WeaponKind.Charm).Select(w => w.Tier).DefaultIfEmpty(0).Max();
    public double TomeSpellMult => 1.0 + 0.1 * UsableHands()
        .Where(w => w.Kind == WeaponKind.Tome).Select(w => w.Tier).DefaultIfEmpty(0).Max();

    // The weapons a technique consults, by its stat (§7) — broken-arm hands never answer (§6d).
    // A CHARGE/pierce verb (Shot family) looses the RANGED slot, never a hand item — that's the
    // §6d two-layer split: melee techniques read the hand-config, ranged techniques the slot.
    public IReadOnlyList<Weapon> Consulted(Technique technique)
    {
        if (technique.ShieldPiercing)
            return _ranged is { } r && r.Stat == technique.Stat && RangedUsable
                ? new[] { r } : Array.Empty<Weapon>();
        return technique.Consults switch
        {
            WeaponUse.Primary => UsableHands()
                .Where(w => w.Stat == technique.Stat || w.Stat == technique.AltStat).Take(1).ToList(),
            WeaponUse.Both => UsableHands()
                .Where(w => w.Stat == technique.Stat || w.Stat == technique.AltStat).ToList(),
            _ => Array.Empty<Weapon>(),
        };
    }

    // Armor covers a part-group SLOT (keyed by the group's stat, §6 anatomy) — one piece per slot,
    // equipping replaces. §6c: armor is a light EFFECT layer (per-line tier bonuses on the record),
    // GATED at equip time on its governing attribute (per-tier requirement); the
    // worn-plate-as-shield-source role is RETIRED — §6b shield sources are techniques (+ the §6d
    // shield OBJECT when that builds). Shields + full evade stay the only HP mitigations.
    public bool Equip(Armor piece)
    {
        var eff = EffectiveArmor(piece);
        if (Capacity(eff.Governing) < eff.Requirement) return false;
        _armor[piece.Slot] = piece;
        _armorSeq[piece.Slot] = ++_equipSeq;
        return true;
    }

    public void Unequip(Stat group)
    {
        _armor.Remove(group);
        _armorSeq.Remove(group);
    }

    public Armor? ArmorOn(Stat group) => _armor.GetValueOrDefault(group);

    // §6e sustain: a worn piece works while the shared pool on its LINE's governing attribute can
    // still cover its Requirement, after techniques + higher-ranked gear take their share (SUSTAIN
    // MODEL, DISABLE CASCADE — see DisabledGear).
    public bool ArmorSustained(Armor piece) => !DisabledGear(EffectiveArmor(piece).Governing).Armor.Contains(piece.Slot);

    // Reservation timing [DESIGN_SPEC lock 2026-07-04]: techniques reserve attributes only on
    // in-combat ACTIVATION, never for sitting equipped — so the Equipment screen must read gear
    // sustain against the GEAR-ONLY pool, ignoring whatever's still active from the last fight.
    // The real (TechReserved-inclusive) checks above stay untouched for actual combat resolution.
    public bool RangedGearOnlyUsable => _ranged is { } r
        && !DisabledGear(r.Stat, techReservedOverride: 0).Ranged
        && (r.Kind == WeaponKind.Bow ? HandUsable(0) && HandUsable(1)
                                     : HandUsable(0) || HandUsable(1));

    public bool HandItemGearOnlyUsable(int i) => i < _hands.Count
        && HandUsable(i) && !DisabledGear(_hands[i].Stat, techReservedOverride: 0).Hands.Contains(i);

    public bool ArmorGearOnlySustained(Armor piece) =>
        !DisabledGear(EffectiveArmor(piece).Governing, techReservedOverride: 0).Armor.Contains(piece.Slot);

    // §6c INT robe: +2 spell damage per worn sustained robe piece, capped at TWO pieces
    // (robe + hat — the line only authors those slots, the cap keeps that true under tuning).
    public int SpellDamageBonus =>
        Math.Min(2, _armor.Values.Count(a => a.SpellDamage > 0 && ArmorSustained(a))) * 2;

    // The part with the most stat missing (Capacity - live contribution), or null if every part is
    // whole. Drives a part-heal's target — mend where it hurts most; ties resolve by part order.
    public BodyPart? MostDamagedPart()
    {
        BodyPart? worst = null;
        var most = 0;
        foreach (var p in _parts)
        {
            var missing = p.Capacity - Contribution(p);
            if (missing > most) { most = missing; worst = p; }
        }
        return worst;
    }

    // Healing restores PARTS, never HP: a repaired part feeds its stat back into the pool.
    public void Repair(BodyPart part, int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        _intact[part.Id] = Math.Min(part.Capacity, Contribution(part) + amount);
    }

    private void Cascade(Stat stat)
    {
        for (var i = _actives.Count - 1; i >= 0 && TechReserved(stat) > Capacity(stat); i--)
            if (_actives[i].Stat == stat) _actives.RemoveAt(i);
    }

    // §6b SHIELDS: a shield source maintains a regenerating pool of 1-damage layers on the body — the
    // OUTERMOST mitigation, absorbing incoming damage before armor/parts/HP. The owning caster raises
    // one per active shield technique, ticks them each combat step, and drops them when the source ends.
    public void RaiseShield(string id, int layers, int regenEvery)
    {
        if (!_shields.ContainsKey(id)) _shields[id] = new ShieldPool(layers, regenEvery);
    }

    public void DropShield(string id) => _shields.Remove(id);

    public void TickShields() { foreach (var pool in _shields.Values) pool.Tick(); }

    public int ShieldPoints => _shields.Values.Sum(p => p.Points);

    public int ShieldLayers => _shields.Values.Sum(p => p.Layers);

    // The furthest-along regen among still-filling pools — what the single regen bar under the pips
    // shows (multiple sources regen independently; the bar reads the next pip to land).
    public float ShieldRegenProgress
        => _shields.Values.Select(p => p.RegenProgress).DefaultIfEmpty(0f).Max();

    // Absorb incoming damage across the standing shield layers; returns the unabsorbed remainder.
    public int AbsorbShields(int damage)
    {
        foreach (var pool in _shields.Values)
        {
            if (damage <= 0) break;
            damage = pool.Absorb(damage);
        }
        return damage;
    }

    // §6c DEX/leather EVASION: +2% per tier, PER worn sustained piece (stacks across pieces) —
    // a global dodge, not per-struck-part. §6 HARD OVERRIDE: a broken LEG zeroes evasion outright,
    // overriding any residual DEX — the footing is gone. (A DEX-attribute-derived base evade is
    // in the §6 table but unnumbered — not invented; leather is the only source until it lands.)
    public int EvasionPercent()
    {
        var legs = _parts.Where(p => p.Stat == Stat.Dex).ToList();
        if (legs.Count > 0 && legs.Any(l => Contribution(l) == 0)) return 0; // broken leg
        return _armor.Values.Where(a => a.EvadePct > 0 && ArmorSustained(a)).Sum(a => a.EvadePct);
    }

    // STR at full weight plus a quarter of DEX, kept in integer quarter-units.
    public int AttackPower => Capacity(Stat.Str) + Capacity(Stat.Dex) / 4;
}
