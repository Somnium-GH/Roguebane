# Races — canon (prototyped)

> **Authoritative design for races.** Attribute spread is locked-this-session; HP + copy are current. Numbers
> tune later. Shared mechanics: `../DESIGN_SPEC.md` §5 / §6 / §7. See `CORE_RUNES.md`, `TECHNIQUES.md`.

## Purpose
A Race supplies the body: base attributes (STR / INT / DEX / CON) + HP. A Core Rune adds none — attributes come
only from race base + the parts Marks (runes) grant. Chest damage drops CON → MAX HP shrinks (1 CON = 2 HP atop
the race's natural base). The design leans on **pressure**: no race needs to fully clear a core's simultaneous
demand — falling short just means you sequence (attack **or** heal, not both), which is intended tension. At
these tight margins a single stat point is the difference between doing two things at once and choosing one.

## Entry pattern (one race)
- **Attributes** — STR / INT / DEX / CON base.
- **HP** — natural base (CON adds +2 each on top).
- **Title / tag / blurb** — the card copy.
- **Shape** — where it clears cores comfortably vs. plays under pressure.

## Content

| Race | STR | INT | DEX | CON | HP | Title · tag |
|---|:--:|:--:|:--:|:--:|:--:|---|
| **Human** | 5 | 5 | 5 | 5 | 20 | Human · THE FOUNDER LINE |
| **Elf** | 4 | 6 | 6 | 4 | 14 | Elf · THE KEEN & FLEET |

- **Human** — "No innate edge or lack — fits any core it can afford." Sits exactly on every demand: runs each
  core's full rig, but tight (no freewheeling on Warden CON 5 or Adept INT 5).
- **Elf** — "Keen and fleet, but frail — punishes a dropped block." +1 INT / DEX (a felt edge at these margins:
  attack + heal at once where a Human can't), −1 STR / CON frailty (lower HP; pressured as a Wall — shield or
  heal, not both). The small spread sidesteps INT-core over-ramp.

## Per-core full-rig demand & clearance
Demand = sum of a core's default reserves in a stat (effect discounts applied). ✓ = holds the whole kit at
once; ⚠ = sequences under pressure.

| Core | STR | INT | DEX | CON | Human 5/5/5/5 | Elf 4/6/4/6 |
|---|:--:|:--:|:--:|:--:|:--:|:--:|
| Grunt | 2 | – | – | 1 | ✓ | ✓ |
| Warden | 2 | – | – | 5 | ✓ (tight) | ⚠ CON 4<5 |
| Adept | – | 5 | – | – | ✓ (tight) | ✓ (+1 edge) |
| Summoner | – | 4 | – | – | ✓ | ✓ |
| Reaver | – | – | 2 | – | ✓ | ✓ |
| Ranger | – | – | 3 | 1 | ✓ | ✓ |

*(Doug lists spreads as STR/INT/CON/DEX: Human 5/5/5/5, Elf 4/6/4/6 — same numbers. Reservation model: a
technique reserves only while active and returns on fire; armor/weapons need remaining stat ≥ their gate.)*

## Open / TBD
- Current in-code values (Human 3/3/3/3, Elf 2/3/4/2) need updating to the locked spread. More races later.
- HP tuning; whether racial identity re-diverges past this provisional-playable baseline.
