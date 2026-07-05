# Techniques — canon (prototyped)

> **Authoritative design for techniques.** T1 numbers this session; tiers parked. Rules/structure are canon.
> Shared engine mechanics: `../DESIGN_SPEC.md` §5 / §6b / §9 / §11.

## Purpose
Action-bar verbs. A technique consults equipped gear and RESERVES its attribute(s) while active — a **Timered**
one returns its reserve when it fires; a **Sustained / passive** holds it while on (toggle off to free the
stat). Parallelism is limited only by the pool: run as many at once as you can reserve.

## Entry pattern (one technique)
- **Name** · **kind** (weapon-verb / spell / shield-source / heal / minion).
- **Stat + reserve** (attributes held while active).
- **Charge** — base **8.0s** × (weapon timer, for weapon-verbs) × the verb's speed-mult × DEX haste; passives are "held."
- **Damage / effect** — weapon-verb = weapon damage × mult; spell = innate INT base + Tome; else fixed.
- **Special** — pierce + Charge, lifesteal, shield pool, heal, etc.
- **Tier** — parked (T1 only).

## Number model (T1)
- **Base speed 8.0s** anchors a ×1.0 verb; fixed 10-tick/s clock, deterministic.
- **Melee / ranged verbs consult the weapon:** DPS = weapon-DPS × (damage-mult ÷ speed-mult).
- **Spells are weapon-independent:** innate INT base × mult, + Tome (+0.1×/tier). Scale with INT (head).
- **Reserve is per-technique** (no global DPS law). §5a caps spell/innate hits at 1–3; weapons run 1–8 (§6d).
- **Tier scaling:** effect up + charge time up with it (≈constant DPS) + reserve up — **EXCEPT DEX attack
  skills, which scale charge DOWN (faster) with tier**. Other exceptions noted as found. Parked (T1 only).
- **On-hit boons require a landed PART-hit** — never a shield-absorbed hit, never a broken part (governs
  Siphon's lifesteal AND Adept's Resonance stack). RNG (e.g. Ranger 20%) rolls off the seeded sim RNG.

## Content

### Weapon-verbs (damage from the equipped weapon)
Shape = (damage × weapon, speed × 8s·timer). Example numbers on Iron Longsword (4 dmg, 1.0×) and 2× Iron Dagger.

| Verb | Dmg × wpn | Speed × | Reserve | Special | Iron Longsword | 2× Iron Dagger |
|---|:--:|:--:|:--:|---|:--:|:--:|
| Swing (anchor) | 1.0 | 1.0 | 0 | basic main-hand strike | 4 / 8.0s = 0.50 | — |
| Jab | 0.5 | 0.5 | 1 | quick light strike | 2 / 4.0s = 0.50 | — |
| Cleave | 1.5 | 1.5 | 2 | heavy arc; big single hit (break-through) | 6 / 12.0s = 0.50 | — |
| Lunge | 0.75 | 0.6 | 1 | darting thrust; charge-free DEX damage | — | 1 / 2.9s = 0.35 (1 blade) |
| Frenzy | both blades | 1.0 (avg) | 2 → 1 Finesse | dual-wield; needs two weapons | — | 2 / 4.8s = 0.42 |
| Flurry | both ×0.5 | 0.5 | 1 → 0 Finesse | fast dual-wield flurry | — | 1 / 2.4s = 0.42 |
| Shot | bow ×1.0 | 1.0 | 0 · +1 Charge | bypasses shields; dry → holds | bow-gated | — |
| Aimed Shot | bow ×2.0 | 2.0 | 1 · +1 Charge | slow heavy shot; bypasses shields | bow-gated | — |

### Spells (innate INT base + Tome; no weapon)

| Spell | Base | Speed × 8s | Reserve | Rules text |
|---|:--:|:--:|:--:|---|
| Ember | 1 | 0.375 (3.0s) | 1 INT | A fast fire bolt for 1 damage. Targeted → feeds Resonance. |
| Siphon | 2 | 0.75 (6.0s) | 2 INT | A draining bolt for 2 that heals you by replenishing an equal amount of your own attribute damage. No lifesteal on a shield-absorbed hit or from an already-broken part. Targeted → feeds Resonance. |

### Shield sources (passive, toggled; sets the pool it holds; CON scales regen for all sources)

| Source | Stat | Reserve | Rules text |
|---|---|:--:|---|
| Brace (T1 CON) | CON | 2 | Hold a pool of 4 CON shield points, each absorbing one hit, +1 pip / 2.0s. Requires a shield OBJECT equipped. |
| T2 CON Shield | CON | 3 | Stronger held guard: pool 8, +1 pip / 1.5s. (Warden signature.) |
| barkskin (INT) | INT | 1 | A held spell keeping 3 shield points, +1 pip / 3.0s. Ladder: barkskin → stoneskin → steelskin → diamondskin. |

### Heals (self)

| Heal | Stat | Reserve | Rules text |
|---|---|:--:|---|
| Bandage (T1) | CON | 1 | Mends your most-damaged part 1 / 8.0s. The flat baseline. |
| T2 CON Heal | CON | 2 | Mends your most-damaged part 2 / 8.0s. (Warden signature.) |
| Sacrifice | — | consumes 1 minion | Consume one of your own minions to mend your body. **New mechanic — needs-design.** |

### Minions (field cost = Summons + reserve gate stat)

| Minion | Stat | Reserve | Power / cadence | Rules text |
|---|---|:--:|:--:|---|
| Skeleton (T1)| INT | 1 | 1 / 3s | A raised thrall; fast / weak. |
| Iron Golem (T2) | INT | 2 | 3 / 5s | A bound iron golem; slow / strong. |
| Hound | DEX | 1 | 1 / 4.0s | A DEX pet; Hound will provide an accuracy bonus when active, 5% as a Tier 1 (+5% per Tier for descendants) |

## Open / TBD
- **Sacrifice** (consume-a-minion heal) = new mechanic. **Bow damage** OPEN (§17 #9). Technique / minion tier
  ladders parked (T1 only). Frenzy / Flurry should gate on DEX for the dagger Reaver (STR in current code).
