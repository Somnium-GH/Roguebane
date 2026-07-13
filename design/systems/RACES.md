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

Demands (**recomputed 2026-07-12 against `Roguebane_Balance (14).xlsx`** — supersedes the numbers below;
several kits changed, not just numbers, see CORE_RUNES.md's per-core notes): Grunt STR 5 · CON 2 ·
Warden CON 9 · STR 3 · Adept INT 8 · STR 3 · Summoner INT 7 · CON 3 · Reaver DEX 9 · STR 2 · CON 2 ·
Ranger DEX 7 · STR 2 · CON 5 · Barbarian STR 8 · DEX 2 · CON 5.

| Core | Human 5/5/5/5 | Elf 4/6/4/4 | Dwarf 4/4/4/6 | Halfling 4/4/6/4 | Half-Giant 6/4/4/4 |
|---|:--:|:--:|:--:|:--:|:--:|
| Grunt | ● +1 | ● +0 | ● +0 | ● +0 | ● +2 |
| Warden | ● +1 | ● +0 | ● +1 | ● +0 | ● +0 |
| Adept | ● +2 | ● +1 | ● +1 | ● +1 | ● +1 |
| Summoner | ● +1 | ● +2 | ● +0 | ● +0 | ● +0 |
| Reaver | ● +1 | ● +0 | ● +0 | ● +2 | ● +0 |
| Ranger | ● +1 | ● +0 | ● +1 | ● +0 | ● +0 |
| Barbarian | ● +1 | ● +0 | ● +0 | ● +0 | ● +0 |

**Read (2026-07-12 — every core's kit got a bit cheaper or gained a spare item that redistributed its
demand): every race now clears every core's full default kit**, several exactly at +0 headroom (tight,
no slack) but NONE short. This retires the earlier "only Half-Giant clears Barbarian, only Halfling
clears Ranger" framing entirely — it wasn't a rounding difference, the demand genuinely dropped (e.g.
Barbarian's Bind→Brace swap alone cuts 2 STR). The §6e disable cascade (armor sheds before weapons)
still exists as a general rule for when damage/debuffs shrink a pool mid-run — it just doesn't trigger
on any race's healthy default kit anymore.

**2026-07-05 numbers (superseded above, kept for history):** Grunt STR 5 · CON 2 · Warden CON 10 ·
Adept INT 10 · Summoner INT 8 · Reaver DEX 9 · Ranger DEX 10 · CON 2 · Barbarian STR 10 · CON 2, with
Barbarian showing Half-Giant as the sole exact fit and every other race short 1-2 STR. That table was
internally correct for the kits as they stood then — the kits themselves changed 2026-07-12, not the
arithmetic.

## Open / TBD
- Current in-code values (Human 3/3/3/3-era placeholders) need updating; **Dwarf, Halfling + Half-Giant are
  the new races** to add (Half-Giant locked 2026-07-05, flagged placeholder-blessed).
- **HP per race — Dwarf/Half-Giant SWAPPED (Doug, 2026-07-05):** the in-code values had Dwarf (CON
  affinity) reading LOWER HP than Half-Giant (STR affinity) — backwards, since CON is the stat that
  converts to HP (this doc's own "1 CON = 2 HP atop the race's natural base" rule). Doug: "swap dwarf
  and half giant for now but still placeholders" — Dwarf takes Half-Giant's old number, Half-Giant takes
  Dwarf's old number; Halfling unchanged. Still placeholder-blessed, not a final balance pass.
- The **mono-attribute scaling** concern (mains scale high, little pulls secondary stats) — playtest.
