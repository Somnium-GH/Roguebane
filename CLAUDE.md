# Roguebane — agent guide

Small roguelike where the player IS the socketed thing. MonoGame (C#). The POC exists to test
one thing: can a player exploit a chassis's structure to build something it wasn't built for,
and does it feel good.

## Architecture invariants (non-negotiable)
- `Core` has ZERO MonoGame references. Pure C# simulation, engine-agnostic, headless-testable.
- `Game` is a thin render/input shell over `Core`. Reads state, draws it, sends input back as
  commands. No game rules.
- Loop split: `Update` mutates, `Draw` only reads. Never draw from Update, never mutate in Draw.
- Simulation is FIXED-TIMESTEP and DETERMINISTIC: same seed + inputs => same outcome. Rates use
  the fixed tick, not frame time.
- Content is DATA, not code: runes, techniques, enemies, parts are data added without new
  classes. One code path interprets data.
- SOLID, plain OO. NO ECS — small entity count; model entities as whole objects.

## Definition of done (every task)
- Core changes have headless tests in `Core.Tests`. Economy/thesis math is asserted, not assumed.
- Tests green before commit. One task = one small, semantically-named commit.
- Prefer real partial work over stubs. Any compromise is logged as Debt with a reconciliation
  path. Anything needing a human goes to "Needs human" — never silently dropped.

## Hygiene (keep it lean)
- Comments explain WHY, never WHAT. If a comment restates code, delete it.
- No speculative abstraction — build for the task in front of you.
- No redundant docs. `STATUS.md` is the single source of state. This file stays under ~60 lines.
- No third-party IP in the product. Design-reference names (FTL, Shadowbane, PoE, etc.) live in
  d