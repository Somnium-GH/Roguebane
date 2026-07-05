# Races — canon (prototyped)

> **Authoritative design for races.** Spread is the v6 balance pass. Numbers tune later. Shared mechanics:
> `../DESIGN_SPEC.md` §5 / §6 / §7. See `CORE_RUNES.md`, `TECHNIQUES.md`.

## Purpose
A Race supplies the body: base attributes (STR / INT / DEX / CON) + HP. On top, the socketed Core Rune adds an
additive stat bonus (see `CORE_RUNES.md`). Attributes otherwise come only from race base + the parts Marks grant.
Chest damage drops CON → MAX HP shrinks (1 CON = 2 HP atop the race's natural base).

## The race formula
Baseline is **4/4/4/4**. **Human** gets **+1 straight across** — breadth, a little of everything. Each
**specialist** gets **+2 into its one affinity** — depth: a spike (plus headroom in its lane) traded for flexibility.

| Race | STR | INT | DEX | CON | Affinity |
|---|:--:|:--:|:--:|:--:|---|
| **Human** | 5 | 5 | 5 | 5 | none — generalist |
| **Elf** | 4 | 6 | 4 | 4 | INT |
| **Dwarf** | 4 | 4 | 4 | 6 | CON |
| **Halfling** | 4 | 4 | 6 | 4 | DEX |

## Demand & clearance
Effective stat in a core = race base + the core's stat bonus (`CORE_RUNES.md`). A core's **demand** is its
fully-active reserve per stat. ● = runs the whole kit at once; number = short by (deactivate or rune to fit).

Demands: Grunt STR 5 · CON 2 · Warden CON 10 · Adept INT 10 · Summoner INT 8 · Reaver DEX 9 · Ranger DEX 10 · CON 2.

| Core | Human 5/5/5/5 | Elf 4/6/4/4 | Dwarf 4/4/4/6 | Halfling 4/4/6/4 |
|---|:--:|:--:|:--:|:--:|
| Grunt | ● +1 | ● +0 | ● +0 | ● +0 |
| Warden | ● +0 | −1 | ● +1 | −1 |
| Adept | ● +0 | ● +1 | −1 | −1 |
| Summoner | ● +0 | ● +1 | −1 | −1 |
| Reaver | ● +1 | ● +0 | ● +0 | ● +2 |
| Ranger | −1 | −2 | −2 | ● +0 |

**Read:** Human runs 5 of 6 full but tight (breadth, no headroom to grow). Each specialist owns its lane with
room — Elf the INT cores, Dwarf the Warden, Halfling the Ranger. Every core has at least one full home.

## Open / TBD
- Current in-code values (Human 3/3/3/3, Elf 2/3/4/2) need updating; **Dwarf + Halfling are new races** to add.
- HP per race; the **mono-attribute scaling** concern (mains scale high, little pulls secondary stats) — playtest.
