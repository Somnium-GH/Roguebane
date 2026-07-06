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
| **Half-Giant** | 6 | 4 | 4 | 4 | STR |

Half-Giant locked 2026-07-05 (Doug via Cowork): the missing STR lane, straight from the specialist
formula — numbers flagged placeholder-blessed like the rest; art already shipped in the 07-05 drop.

## Demand & clearance
Effective stat in a core = race base + the core's stat bonus (`CORE_RUNES.md`). A core's **demand** is its
fully-active reserve per stat. ● = runs the whole kit at once; number = short by (deactivate or rune to fit).

Demands (verified against Doug's balance spreadsheet): Grunt STR 5 · CON 2 · Warden CON 10 · Adept INT 10 ·
Summoner INT 8 · Reaver DEX 9 · Ranger DEX 10 · CON 2 · Barbarian STR 10 · CON 2 (**Half-Giant is the
exact fit, zero headroom** — see CORE_RUNES.md's Barbarian note; corrected 2026-07-05, was miscalculated
as STR 15).

| Core | Human 5/5/5/5 | Elf 4/6/4/4 | Dwarf 4/4/4/6 | Halfling 4/4/6/4 | Half-Giant 6/4/4/4 |
|---|:--:|:--:|:--:|:--:|:--:|
| Grunt | ● +1 | ● +0 | ● +0 | ● +0 | ● +2 |
| Warden | ● +0 | −1 | ● +1 | −1 | −1 |
| Adept | ● +0 | ● +1 | −1 | −1 | −1 |
| Summoner | ● +0 | ● +1 | −1 | −1 | −1 |
| Reaver | ● +1 | ● +0 | ● +0 | ● +2 | ● +0 |
| Ranger | −1 | −2 | −2 | ● +0 | −2 |
| Barbarian | −1 | −2 | −2 | −2 | ● +0 |

**Read:** Human runs 5 of 6 classic cores full but tight (breadth, no headroom). Each specialist owns its
lane with room — Elf the INT cores, Dwarf the Warden, Halfling the Ranger. **Half-Giant is Barbarian's
home** — the one race that clears its full kit, and clears it EXACTLY (zero headroom, the tightest fit in
the game); every other race falls 1-2 STR short and triages one small item via the §6e cascade.
(Corrected 2026-07-05 against Doug's balance spreadsheet — this row previously read −6/−7/−7/−7/−5 from a hand-
math error that overstated Barbarian's STR demand as 15 instead of 10; see CORE_RUNES.md's Barbarian
note for the full reconciliation.)

## Open / TBD
- Current in-code values (Human 3/3/3/3-era placeholders) need updating; **Dwarf, Halfling + Half-Giant are
  the new races** to add (Half-Giant locked 2026-07-05, flagged placeholder-blessed).
- **HP per race — Dwarf/Half-Giant SWAPPED (Doug, 2026-07-05):** the in-code values had Dwarf (CON
  affinity) reading LOWER HP than Half-Giant (STR affinity) — backwards, since CON is the stat that
  converts to HP (this doc's own "1 CON = 2 HP atop the race's natural base" rule). Doug: "swap dwarf
  and half giant for now but still placeholders" — Dwarf takes Half-Giant's old number, Half-Giant takes
  Dwarf's old number; Halfling unchanged. Still placeholder-blessed, not a final balance pass.
- The **mono-attribute scaling** concern (mains scale high, little pulls secondary stats) — playtest.
