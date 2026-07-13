# Core Runes — canon (prototyped)

> **Authoritative design for the seven Core Runes.** Effect names + rules text are canon (this prototype set
> REPLACES the older §11 roster — Hollow Vessel / Unbroken Aegis / etc.). Kits + numbers are the v6 balance pass;
> **Barbarian added 2026-07-05** (Doug's numbers + the 01/02-`<core>` design refs).
> Shared mechanics: `../DESIGN_SPEC.md` §7 / §11. See also `TECHNIQUES.md`, `WEAPONS.md`, `ARMOR.md`, `RACES.md`.

## Purpose
A Core Rune is the LAYOUT a Race's body sockets into. It grants an additive **stat bonus** (prototypal rule:
additive-only for now — pro/con revisited later), a minion capacity, a fixed starting kit (techniques / minions /
weapons / armor), and a signature **Core Effect** (stronger than a keystone). Design principles: **~4 techniques
is the ceiling, not the fill** (leave free slots); **minion capacity can start empty** (minions acquired); **each
core heals differently**.

## Core Effects

| Core | Effect | Rules text |
|------|--------|------------|
| Grunt | **Jack of All Trades** | Every attribute cost you pay is reduced by 1. |
| Warden | **Fortified** | Plate armor is paid in CON at 1 less per tier. |
| Adept | **Resonance** | Each targeted spell that hits reduces its next charge time by 2%, stacking up to 5 times. |
| Summoner | **Conscription** | Minions do not consume Summons when activated. |
| Reaver | **Finesse** | Techniques requiring two weapons cost 1 less to activate. |
| Ranger | **Fletcher's Luck** | Bow techniques have a 20% chance to consume no charge when fired, and bows cost 1 less per tier to equip. |
| Barbarian | **Warlord's Might** | Two-handed swords cost 3 less strength to equip; STR plate costs 1 less strength per piece to equip. |

Shared rule: **on-hit boons require a landed PART-hit** — never a shield-absorbed hit, never a broken part
(governs Siphon's lifesteal + Resonance stacks). Ranger's 20% rolls off the seeded sim RNG.

## Stat bonuses (additive, on top of race base)

| Core | STR | INT | DEX | CON |
|------|:--:|:--:|:--:|:--:|
| Grunt | +1 | +1 | +1 | +1 |
| Warden | – | – | – | +5 |
| Adept | – | +5 | – | – |
| Summoner | – | +3 | – | +2 |
| Reaver | – | – | +5 | – |
| Ranger | – | – | +4 | +1 |
| Barbarian | +4 | – | – | +1 |

## Layout numbers (budget · actions · minion capacity — from the 02-equipment refs; Barbarian set by Doug 2026-07-05)

| Core | Rune budget | Actions (technique slots) | Minion capacity |
|------|:--:|:--:|:--:|
| Grunt | 20 | 4 | 2 |
| Warden | 18 | 4 | 1 |
| Adept | 16 | 4 | 1 |
| Summoner | 17 | 4 | 3 |
| Reaver | 19 | 4 | 0 |
| Ranger | 18 | 4 | 2 |
| Barbarian | 14 | 4 | 1 |

Base HP stays race-owned (all refs show Human hp 20). The old per-core `RuneDiscount` (rune-PRICE
discount) appears nowhere in v6 — affordability perks now live in the Core Effects, which discount
ATTRIBUTE costs, a different thing. **RuneDiscount retires to 0 for all cores — CONFIRMED 2026-07-12
(Doug).** Matches current code already (`CoreRune.cs`'s default, never overridden by any roster core) —
no code change was owed, this was purely a documentation question.

## Default loadouts
Requirement = fully-active reserve demand per stat (armor + weapons + skills + minions), effect discounts applied.

### Grunt — *THE GENERALIST* · badge STARTER — req **STR 5 · CON 2**
Bonus +1 all. Weapons: Iron Longsword + Wooden Shield + Shepherd's Sling (ranged, DEX backup — **added
2026-07-12**, `Roguebane_Balance (14).xlsx`) · Armor: Iron plate ×4 · Techniques: Jab, **Shot** (new —
fires the Sling, legacy technique now in real use), Brace, Bandage · Minions: none (capacity 2).

### Warden — *THE WALL* · badge BULWARK — req **CON 9 · STR 3**
Bonus +5 CON. Weapons: Iron Longsword + **Wooden Shield** (downgraded from Iron Buckler — **(14)**,
drops CON req 10→9) + Shepherd's Sling (ranged backup, same as Grunt) · Armor: Iron plate ×4 (paid in
CON, −1/tier via Fortified) · Techniques: Jab, **Shot**, Brace, Bandage · Minions: none (capacity 1).

### Adept — *THE SCHOLAR* · badge CASTER — req **INT 8 · STR 3**
Bonus +5 INT. Weapons: Wooden Staff (STR-gated, own +1/tier SPELL bonus — same flat formula as a Tome, no
longer "2× a tome"; **corrected 2026-07-12**, Doug's balance pass) · Armor: Cotton Robe + Cloth Cap ·
Techniques: Ember, Siphon, Stoneskin, **Jab** (added 2026-07-12 — the STR-gated Staff makes Jab a free
backup attack, giving Adept a real STR pressure alongside its INT spell suite) · Minions: none (capacity
1). **Stoneskin numbers (Doug, 2026-07-05): T2 INT ward — pool 6, +2 pips / 3.0s, reserve 2 INT** (rung 2
of the barkskin ladder, TECHNIQUES.md).

### Summoner — *THE BINDER* · badge SPECIALIST — req **INT 7 · CON 3**
Bonus +3 INT · +2 CON. Weapons: Adept Wand + **Wooden Shield** (CON shieldobj — **reworked 2026-07-12**,
Doug's balance pass; Wooden Charm is DROPPED from the starting kit) · Armor: Cotton Robe + Cloth Cap ·
Techniques: Ember, **Blast** (new — INT wand-attack, see TECHNIQUES.md), Sacrifice, **Brace** (replaces
Barkskin — a CON shield source pairing with the new Wooden Shield, instead of an INT ward) · Minions:
Skeleton only (capacity 3, 2 free). A Wand (1H) freely pairs with a shield in the other hand — only a
2H Bow excludes one; the old "bow/wand can't coexist with a shield" wording in WEAPONS.md was wrong and
is corrected there too. **Sacrifice locked 2026-07-05 (Doug):** heal scales with the sacrificed minion's
tier, minion destroyed permanently (no refund) — see TECHNIQUES.md for the (still-flagged) exact
per-tier numbers.

### Reaver — *THE DUELIST* · badge SPECIALIST — req **DEX 9 · STR 2 · CON 2**
Bonus +5 DEX. Weapons: **Iron Longsword + Iron Rapier** (STR+DEX mixed pair — **replaced the twin-dagger
kit, `(14)` 2026-07-12**; this is why Frenzy/Flurry were made stat-flexible in the first place, a mixed
pair makes `AltStat: Stat.Dex` matter far more than two same-stat daggers did) · Armor: leather ×4 ·
Techniques: Frenzy, Flurry, Bandage · Minions: none (capacity 0).
**Bandage added 2026-07-05 (Doug + balance spreadsheet Kits/Demand tabs, CON 2):** Reaver carries the flat CON part-heal like
every core bar Adept/Summoner — the earlier "no heal glass cannon" framing is retired. **Locked 2026-07-05 (Doug):** Frenzy/Flurry are single **stat-flexible** techniques (STR OR DEX, same
reserve — see TECHNIQUES.md); Reaver pays them in DEX, and Finesse (−1) brings them to 2/1 — leather 4 +
rapier 2 + Frenzy 2 + Flurry 1 = the DEX 9 above; the longsword's STR 2 is a separate, small STR demand
this core didn't carry before (14).

### Ranger — *THE MARKSMAN* · badge SPECIALIST — req **DEX 7 · STR 2 · CON 5**
Bonus +4 DEX · +1 CON. Weapons: **Iron Axe + Short Bow + Wooden Shield** (gains a shield, drops the
dagger for an axe — `(14)` 2026-07-12; axe/shield are hand items, bow is the separate ranged slot, all
three coexist) · Armor: leather ×4 · Techniques: **Jab**, Aimed Shot, Bandage, **Brace** (replaces
Lunge — Brace needs the new shield) · Minions: Hound (capacity 2, 1 free).

### Barbarian — *THE WARLORD* · badge SPECIALIST — req **STR 8 · DEX 2 · CON 5**
Bonus +4 STR · +1 CON. Weapons: **Iron Claymore (2H) + Shepherd's Sling (ranged) + Wooden Shield**
(gains a ranged backup and a shield — `(14)` 2026-07-12) · Armor: Iron plate ×4 · Techniques: Cleave,
**Shot**, Bandage, **Brace** (replaces Bind — Brace needs the new shield) · Minions: none (capacity 1).
Budget 14, actions **4** (was 3, raised to fit the now-4-technique kit).

**Superseded 2026-07-12 against `Roguebane_Balance (14).xlsx` (the 2026-07-05 "Half-Giant is the exact
fit" framing below no longer holds — kept for the arithmetic history, not as current design):** fully-
active STR = claymore 2 (5−3 Warlord's Might) + plate 4 (4 pieces × (2−1), Warlord's Might's plate
discount) + Cleave 2 = **8** (Bind's STR 2 dropped out along with Bind itself). Half-Giant's effective
STR is 6+4=10 — **2 points of headroom now, not an exact fit.** Recomputing the whole roster against
(14)'s `Analysis` sheet: **every race clears every core**, several at exactly +0 headroom but none
short — this retires the "only Half-Giant clears Barbarian" narrative entirely, not just the exact
number. See `RACES.md`'s clearance table (same pass). The disable-cascade fix (armor sheds before
weapons — `Body.cs`) is still correct as a general rule; it just won't trigger on a healthy Barbarian
kit anymore regardless of race.

**2026-07-05 arithmetic history (STR 10, exact-fit-only-Half-Giant) — superseded above, not current:**
fully-active STR was claymore 2 + plate 4 + Cleave 2 + **Bind 2** = 10, an exact fit for Half-Giant only.
That was correct for the KIT AS IT STOOD THEN (with Bind, no shield, no sling) — the kit itself changed
2026-07-12, not the arithmetic method.

## Shared rules
- **Healing map:** Grunt, Warden, Ranger, Barbarian, **Reaver** → Bandage (T1 CON heal); Adept → Siphon (lifesteal spell);
  Summoner → Sacrifice (consume a minion). Only Adept/Summoner heal off-Bandage; every other core carries the flat CON
  baseline (Reaver's Bandage added 2026-07-05 — it was briefly dropped to "no heal," now restored). Flat baselines pull
  players toward acquiring better heals.
- **Discount perks live in the effects** — Grunt (−1 all), Warden (plate-in-CON −1/tier), Reaver (dual-wield −1),
  Ranger (bow −1/tier) all fold their affordability discount into their Core Effect text above.
- A Core Effect outranks a keystone rune.

## Open / TBD
- Minion capacities are working numbers; **Sacrifice** (consume-a-minion heal) + the minion tier ladder need building.
- This set diverges from the locked §11 roster — reconcile DESIGN_SPEC when it locks.
- **Mono-attribute scaling** — each core leans on ~one stat, so mains scale high and little pulls secondary
  stats yet; feel it out in playtest.
