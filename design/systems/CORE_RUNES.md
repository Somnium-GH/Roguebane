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
| Summoner | 17 | 3 | 3 |
| Reaver | 19 | 4 | 0 |
| Ranger | 18 | 4 | 2 |
| Barbarian | 14 | 3 | 1 |

Base HP stays race-owned (all refs show Human hp 20). The old per-core `RuneDiscount` (rune-PRICE
discount) appears nowhere in v6 — affordability perks now live in the Core Effects, which discount
ATTRIBUTE costs, a different thing. Working assumption: RuneDiscount retires to 0 for all cores
(**flagged, needs Doug's confirm** — Jack of All Trades reads as attribute costs, not rune prices).

## Default loadouts
Requirement = fully-active reserve demand per stat (armor + weapons + skills + minions), effect discounts applied.

### Grunt — *THE GENERALIST* · badge STARTER — req **STR 5 · CON 2**
Bonus +1 all. Weapons: Iron Longsword + Wooden Shield · Armor: Iron plate ×4 · Techniques: Jab, Brace, Bandage ·
Minions: none (capacity 2).

### Warden — *THE WALL* · badge BULWARK — req **CON 10 · STR 3**
Bonus +5 CON. Weapons: Iron Longsword + Iron Buckler · Armor: Iron plate ×4 (paid in CON, −1/tier via Fortified) ·
Techniques: Jab, Brace, Bandage · Minions: none (capacity 1).

### Adept — *THE SCHOLAR* · badge CASTER — req **INT 10**
Bonus +5 INT. Weapons: Wooden Staff (+magic damage = 2× a tome) · Armor: Cotton Robe + Cloth Cap · Techniques:
Ember, Siphon, Stoneskin · Minions: none (capacity 1). **Stoneskin numbers (Doug, 2026-07-05): T2 INT ward —
pool 6, +2 pips / 3.0s, reserve 2 INT** (rung 2 of the barkskin ladder, TECHNIQUES.md).

### Summoner — *THE BINDER* · badge SPECIALIST — req **INT 8**
Bonus +3 INT · +2 CON. Weapons: Adept Wand + Wooden Charm · Armor: Cotton Robe + Cloth Cap · Techniques: Ember,
Sacrifice, Barkskin · Minions: Skeleton (capacity 3, 2 free). **Sacrifice locked 2026-07-05 (Doug):** heal
scales with the sacrificed minion's tier, minion destroyed permanently (no refund) — see TECHNIQUES.md for
the (still-flagged) exact per-tier numbers. Barkskin here is T1 — Adept gets the stronger Stoneskin T2
(below); the two are intentionally different tiers, not a mix-up.

### Reaver — *THE DUELIST* · badge SPECIALIST — req **DEX 9**
Bonus +5 DEX. Weapons: 2× Iron Dagger · Armor: leather ×4 · Techniques: Frenzy, Flurry · Minions: none (capacity 0) ·
**no heal** (glass cannon). **Locked 2026-07-05 (Doug):** Frenzy/Flurry are single **stat-flexible** techniques (STR OR DEX, same
reserve — see TECHNIQUES.md); Reaver pays them in DEX, and Finesse (−1) brings them to 2/1 — leather 4 +
daggers 2 + Frenzy 2 + Flurry 1 = the DEX 9 above. (The earlier `frenzy_dex`/`flurry_dex` clone framing is retired.)

### Ranger — *THE MARKSMAN* · badge SPECIALIST — req **DEX 10 · CON 2**
Bonus +4 DEX · +1 CON. Weapons: Iron Dagger + Short Bow · Armor: leather ×4 · Techniques: Aimed Shot, Lunge,
Bandage · Minions: Hound (capacity 2, 1 free).

### Barbarian — *THE WARLORD* · badge SPECIALIST — req **STR 10 · CON 2 (Half-Giant is the exact fit)**
Bonus +4 STR · +1 CON. Weapons: Iron Claymore (2H) · Armor: Iron plate ×4 · Techniques: Cleave, Bind,
Bandage · Minions: none (capacity 1). Added 2026-07-05 — numbers from Doug (budget 14 · actions 3 ·
minions 1) + the `01/02-*-barbarian` refs.

**Corrected 2026-07-05 against Doug's balance spreadsheet (kept outside the repo; this superseded an
earlier hand-math error — see below):** fully-active STR = claymore 2 (5−3 Warlord's Might) + plate 4
(4 pieces × (2−1), Warlord's Might's plate discount) + Cleave 2 + Bind 2 = **10**. Half-Giant's effective
STR is 6+4=**10** — an EXACT fit, zero headroom, not an over-demand. Every other race falls short by 1-2
STR (Human −1, Elf/Dwarf/Halfling −2 — see RACES.md's clearance table) and must deactivate/trade one
small item to run the kit, same triage pattern as any tight core. Engine consequence: the "every core
activates its whole default kit" test does NOT need a Barbarian exemption — assert Half-Giant+Barbarian
activates the FULL kit; other race+Barbarian pairs activate the sustainable subset (same as any other
short combo).

**What was wrong (logged so it doesn't happen again):** the previous text here computed Warlord's Might
as −2 STR on the claymore only, and priced plate at its raw 2/piece with no discount at all, giving a
hand-math total of 15 vs. Half-Giant's 10 — a real 5-point gap that was then written up as an
intentional "over-demand identity" needing a test exemption. Doug's actual balance model (reconciled
2026-07-05) shows the true numbers above: the Warlord's Might discount is −3 on the claymore AND −1/piece
on plate, which is what makes Half-Giant land on exactly 10. The "over-demand" framing and the planned
test exemption were both artifacts of that hand-math error, not real design intent — do not resurrect
them.

## Shared rules
- **Healing map:** Grunt, Warden, Ranger, Barbarian → Bandage (T1 CON heal); Adept → Siphon (lifesteal spell);
  Summoner → Sacrifice (consume a minion); Reaver → none. Flat baselines pull players toward acquiring better heals.
- **Discount perks live in the effects** — Grunt (−1 all), Warden (plate-in-CON −1/tier), Reaver (dual-wield −1),
  Ranger (bow −1/tier) all fold their affordability discount into their Core Effect text above.
- A Core Effect outranks a keystone rune.

## Open / TBD
- Minion capacities are working numbers; **Sacrifice** (consume-a-minion heal) + the minion tier ladder need building.
- This set diverges from the locked §11 roster — reconcile DESIGN_SPEC when it locks.
- **Mono-attribute scaling** — each core leans on ~one stat, so mains scale high and little pulls secondary
  stats yet; feel it out in playtest.
