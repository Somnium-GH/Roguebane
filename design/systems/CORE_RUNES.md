# Core Runes — canon (prototyped)

> **Authoritative design for the six Core Runes.** Effect names + rules text are canon (this prototype set
> REPLACES the older §11 roster — Hollow Vessel / Unbroken Aegis / etc.). Kits + numbers are the v6 balance pass.
> Shared mechanics: `../DESIGN_SPEC.md` §7 / §11. See also `TECHNIQUES.md`, `WEAPONS.md`, `ARMOR.md`, `RACES.md`.

## Purpose
A Core Rune is the LAYOUT a Race's body sockets into. It grants an additive **stat bonus** (prototypal rule:
additive-only for now — pro/con revisited later), a bay count, a fixed starting kit (techniques / minions /
weapons / armor), and a signature **Core Effect** (stronger than a keystone). Design principles: **~4 techniques
is the ceiling, not the fill** (leave free slots); **bays can start empty** (minions acquired); **each core
heals differently**.

## Core Effects

| Core | Effect | Rules text |
|------|--------|------------|
| Grunt | **Jack of All Trades** | Every attribute cost you pay is reduced by 1. |
| Warden | **Fortified** | Plate armor is paid in CON at 1 less per tier. |
| Adept | **Resonance** | Each targeted spell that hits reduces its next charge time by 2%, stacking up to 5 times. |
| Summoner | **Conscription** | Minions do not consume Summons when activated. |
| Reaver | **Finesse** | Techniques requiring two weapons cost 1 less to activate. |
| Ranger | **Fletcher's Luck** | Bow techniques have a 20% chance to consume no charge when fired, and bows cost 1 less per tier to equip. |

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

## Default loadouts
Requirement = fully-active reserve demand per stat (armor + weapons + skills + minions), effect discounts applied.

### Grunt — *THE GENERALIST* · badge STARTER — req **STR 5 · CON 2**
Bonus +1 all. Weapons: Iron Longsword + Wooden Shield · Armor: Iron plate ×4 · Techniques: Jab, Brace, Bandage ·
Minions: none (2 bays).

### Warden — *THE WALL* · badge BULWARK — req **CON 10 · STR 3**
Bonus +5 CON. Weapons: Iron Longsword + Iron Buckler · Armor: Iron plate ×4 (paid in CON, −1/tier via Fortified) ·
Techniques: Jab, Brace, Bandage · Minions: none (1 bay).

### Adept — *THE SCHOLAR* · badge CASTER — req **INT 10**
Bonus +5 INT. Weapons: Wooden Staff (+magic damage = 2× a tome) · Armor: Cotton Robe + Cloth Cap · Techniques:
Ember, Siphon, Stoneskin · Minions: none (1 bay).

### Summoner — *THE BINDER* · badge SPECIALIST — req **INT 8**
Bonus +3 INT · +2 CON. Weapons: Adept Wand + Wooden Charm · Armor: Cotton Robe + Cloth Cap · Techniques: Ember,
Sacrifice, Barkskin · Minions: Skeleton (3 bays, 2 free).

### Reaver — *THE DUELIST* · badge SPECIALIST — req **DEX 9**
Bonus +5 DEX. Weapons: 2× Iron Dagger · Armor: leather ×4 · Techniques: Frenzy, Flurry · Minions: none (0 bays) ·
**no heal** (glass cannon).

### Ranger — *THE MARKSMAN* · badge SPECIALIST — req **DEX 10 · CON 2**
Bonus +4 DEX · +1 CON. Weapons: Iron Dagger + Short Bow · Armor: leather ×4 · Techniques: Aimed Shot, Lunge,
Bandage · Minions: Hound (2 bays, 1 free).

## Shared rules
- **Healing map:** Grunt, Warden, Ranger → Bandage (T1 CON heal); Adept → Siphon (lifesteal spell); Summoner →
  Sacrifice (consume a minion); Reaver → none. Flat baselines pull players toward acquiring better heals.
- **Discount perks live in the effects** — Grunt (−1 all), Warden (plate-in-CON −1/tier), Reaver (dual-wield −1),
  Ranger (bow −1/tier) all fold their affordability discount into their Core Effect text above.
- A Core Effect outranks a keystone rune.

## Open / TBD
- Bay capacities are working numbers; **Sacrifice** (consume-a-minion heal) + the minion tier ladder need building.
- This set diverges from the locked §11 roster — reconcile DESIGN_SPEC when it locks.
- **Mono-attribute scaling** — each core leans on ~one stat, so mains scale high and little pulls secondary
  stats yet; feel it out in playtest.
