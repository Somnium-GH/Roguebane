# Rules snapshot — CURRENT STATE (temporary tiebreaker for the build loop)

> **Purpose:** the core/race/effect/kit/number design churned heavily 2026-07-05. This is the clean,
> consolidated CURRENT state — consult it on any conflict or ambiguity. **It supersedes** DESIGN_SPEC §11
> (old Core-Effect roster) and §7 (old 2-race set), and any in-code placeholder stats. The per-system
> `design/systems/*.md` docs hold the detail but carry historical reconciliation notes; when they seem to
> disagree with this, this is the clean truth. **TEMPORARY** — fold into DESIGN_SPEC when the design locks,
> then delete. All numbers are prototype / placeholder-blessed (tune later). Items marked **OPEN** are NOT
> settled — do not hardcode them as final.

## Races (5) — attributes are RACE-owned; a core adds a stat bonus on top
Formula: baseline **4/4/4/4**; Human **+1 to all**; each specialist **+2 into its one affinity**.

| Race | STR | INT | DEX | CON | Affinity |
|---|:--:|:--:|:--:|:--:|---|
| Human | 5 | 5 | 5 | 5 | generalist |
| Elf | 4 | 6 | 4 | 4 | INT |
| Dwarf | 4 | 4 | 4 | 6 | CON |
| Halfling | 4 | 4 | 6 | 4 | DEX |
| Half-Giant | 6 | 4 | 4 | 4 | STR |

*(In-code Human 3/3/3/3 / Elf 2/3/4/2 are stale placeholders; Dwarf/Halfling/Half-Giant are new.)*

## Cores (7) — effects, stat bonus, layout numbers
Core Effect roster REPLACES the old §11 set (Hollow Vessel / Unbroken Aegis / Overchannel / Legion / Bloodrush / Called Shot).

| Core | Effect | Rules text | Stat bonus | Budget · Actions · Minion-cap |
|---|---|---|---|---|
| Grunt | Jack of All Trades | Every attribute cost you pay is reduced by 1. | +1 all | 20 · 4 · 2 |
| Warden | Fortified | Plate armor is paid in CON at 1 less per tier. | +5 CON | 18 · 4 · 1 |
| Adept | Resonance | Each targeted spell that hits reduces its next charge time by 2%, stacking up to 5 times. | +5 INT | 16 · 4 · 1 |
| Summoner | Conscription | Minions do not consume Summons when activated. | +3 INT · +2 CON | 17 · 3 · 3 |
| Reaver | Finesse | Techniques requiring two weapons cost 1 less to activate. | +5 DEX | 19 · 4 · 0 |
| Ranger | Fletcher's Luck | Bow techniques have a 20% chance to consume no charge when fired, and bows cost 1 less per tier to equip. | +4 DEX · +1 CON | 18 · 4 · 2 |
| Barbarian | Warlord's Might | Two-handed swords cost 3 less strength to equip; STR plate costs 1 less strength per piece to equip. | +4 STR · +1 CON | 14 · 3 · 1 |

Effective stat in a core = race base + this bonus. Shared rule: on-hit boons (Siphon lifesteal, Resonance stack) need a **landed part-hit** — never a shield-absorbed hit, never a broken part.

## Default kits + full-kit demand (reserve per stat, discounts applied)

| Core | Req | Weapons | Armor | Techniques | Minion |
|---|---|---|---|---|---|
| Grunt | STR 5 · CON 2 | Iron Longsword + Wooden Shield | Iron plate ×4 | Jab, Brace, Bandage | — |
| Warden | CON 10 · STR 3 | Iron Longsword + Iron Buckler | Iron plate ×4 (paid in CON) | Jab, Brace, Bandage | — |
| Adept | INT 10 | Wooden Staff | Cotton Robe + Cloth Cap | Ember, Siphon, Stoneskin | — |
| Summoner | INT 8 | Adept Wand + Wooden Charm | Cotton Robe + Cloth Cap | Ember, Sacrifice, Barkskin | Skeleton |
| Reaver | DEX 9 · CON 2 | 2× Iron Dagger | leather ×4 | Frenzy, Flurry, **Bandage** | — |
| Ranger | DEX 10 · CON 2 | Iron Dagger + Short Bow | leather ×4 | Aimed Shot, Lunge, Bandage | Hound |
| Barbarian | STR 10 · CON 2 | Iron Claymore (2H) | Iron plate ×4 | Cleave, Bind, Bandage | — |

**Healing map:** Grunt, Warden, Ranger, Barbarian, **Reaver** → Bandage (CON); Adept → Siphon (INT lifesteal); Summoner → Sacrifice (consume a minion). *(Reaver-gains-Bandage is confirmed 2026-07-05 — the "no-heal glass cannon" interim is retired.)*

**Race × core clearance:** see `RACES.md`. Halfling is Reaver's home; Half-Giant is Barbarian's (exact fit). Human runs the six classic cores tight; no Barbarian exemption needed in the "activates full kit" test — assert the home race activates full, other combos activate the sustainable subset.

## Technique & gear reserves (T1)
Base technique speed **8.0s**; melee/ranged verbs CONSULT the weapon for damage; spells are innate INT + Tome.

- **Weapon-verbs:** Swing 0 · Jab 1 · Cleave 2 · Lunge 1 · Frenzy **3 (STR OR DEX)** · Flurry **2 (STR OR DEX)** · Shot 0 (+1 Charge) · Aimed Shot 2 (+1 Charge).
  **Dual-wield = single stat-flexible technique** (Frenzy/Flurry): one reserve, paid in STR *or* DEX resolved from the wielded weapon (heavy → STR, fast → DEX). No `frenzy_dex`/`flurry_dex` clones. Finesse (Reaver) is −1 → 2/1.
- **Spells:** Ember 1 INT · Siphon 2 INT (lifesteal).
- **Shields (passive):** Brace (CON T1, 2) · Steel (CON T2, 3) · Barkskin (INT T1, 1) · Stoneskin (INT T2, 2 — pool 6, +2 pips/3s) · Bind (STR T1, 2) · Parry (DEX T1, 2).
- **Heals:** Bandage (CON T1, 2) · Suture (CON T2, 3) · Sacrifice (consume 1 fielded minion; heal scales with its tier; minion destroyed, no refund).
- **Minions:** Skeleton (INT T1, res 1, 1/3s) · Iron Golem (INT T2, res 2, 3/5s) · Hound (DEX T1, res 1, 1/4s + accuracy aura).
- **Weapon families / numbers:** see `WEAPONS.md`. Staff is 2 INT + magic dmg = 2× a tome. Bows/sling damage OPEN.

## Reservation / combat model
- One shared per-stat pool. **Two different triggers feed it — do not conflate them:**
  **(1) EQUIPMENT reserves the moment it's equipped**, unconditionally, for as long as it stays
  equipped — worn armor and wielded/ranged weapons are NOT separately "activated"; being equipped
  IS the reservation event (Equipment screen / build time), and it's cumulative with everything else
  equipped. **(2) TECHNIQUES AND MINIONS reserve ONLY on their own in-combat activation** (charging,
  passively active, or fielded) — never merely for being slotted/owned — and free the instant they're
  deactivated/dismissed. A technique or minion cannot become active/passive/fielded at all while the
  pool lacks room for its reserve (the activation attempt itself is refused, not just later disabled).
  (DESIGN_SPEC §7 "Reservation timing" is the fuller lock this compresses — read it if this summary
  and the code ever seem to disagree.)
- A part's damage drops its stat → active things that need that stat **deactivate** until it heals (§5/§6).
- **Weapon/technique stat mismatch is legal, intentionally unaddressed [NOTED 2026-07-07, Doug;
  CORRECTED 2026-07-07].** This note originally claimed a mismatched technique (STR technique, DEX-only
  weapon) "activates and reserves fine, it just has nothing to Consult for weapon-scaled damage" — that's
  wrong. Every real caller goes through `Caster.Activate`, which gates ANY `Consults != None` technique
  on `Consulted(technique).Count == 0 => return false` BEFORE `Body.Activate`'s pool check ever runs — a
  mismatched technique **never activates at all**, zero offense, not "activates for zero weapon damage."
  Proven generically by `WeaponTests.WithoutAWeaponAConsultingTechniqueCannotActivate`, pinned through
  real content by `FoeSkeletonTests` (`Foes.Skeleton`'s Iron Dagger + Jab deals zero damage across a full
  battle). Doug's underlying ruling is unchanged: this is legal and intentional, do NOT add a
  stat-matching restriction — only the technical description of what "as written" does was wrong. A
  flexible technique (`AltStat` set, e.g. Frenzy/Flurry) is unaffected — it activates off whichever
  stat's wielded weapon it actually consults. See `FOES.md`'s matching corrected note.
- **Demand** = the full-kit reserve per stat with effect discounts applied (the "req" above).
- **Tier scaling (PARKED — T1 only):** tier ↑ = bigger effect + longer charge (≈constant DPS) + more reserve — EXCEPT DEX attack skills, which tier into SPEED (charge down). Dual-wield flavor: DEX → speed, STR → damage.

## OPEN / not settled — do NOT treat as final
- **Sacrifice** per-tier heal numbers **APPROVED as the standing placeholder (Doug, 2026-07-05 — "placeholder for now"): T1 = 4 part-points, T2 = 8.** Not final-locked, but no longer blocking — build/keep as-is until a real balance pass revisits it.
- **Bow / sling damage** (§17 #9). **Minion + technique tier ladders** parked (T1 only).
- **RuneDiscount** (old per-core rune-price discount) assumed retired to 0 — needs Doug confirm.
- **HP per race**; **mono-attribute scaling** (mains scale high, little pulls secondary stats) — playtest concern.
- Reserve stat "picked from the wielded weapon" for the split dual-wield technique needs a small engine touch (vs today's fixed-`Stat` field).
- **Crossover skills** (per-pair offense/defense — Confusion, Stun, Ward, etc.) are EXPERIMENTAL concepts only, parked in TECHNIQUES.md's *EXPERIMENTAL — IN DESIGN* section. Do NOT build. A future INT+CON Ward would rename Stoneskin → Mana Shield.
