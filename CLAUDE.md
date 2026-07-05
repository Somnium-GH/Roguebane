# Roguebane — agent guide

Small roguelike where the player IS the socketed thing. MonoGame (C#). The POC exists to test
one thing: can a player exploit a Core rune's structure to build something it wasn't built for,
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
- Tests must NOT pin Claude-Design-authored manifest content (`layout.json` keys/elements) — that file is
  regenerated externally. Assert the CONTRACT/schema (parse, required types/screens/templates) or a
  test-owned fixture, so a CD re-drop never reddens the build.
- Tests green before commit. One task = one small, semantically-named commit.
- Prefer real partial work over stubs. Any compromise is logged as Debt with a reconciliation
  path. Anything needing a human goes to "Needs human" — never silently dropped.

## Hygiene (keep it lean)
- Comments explain WHY, never WHAT. If a comment restates code, delete it.
- No speculative abstraction — build for the task in front of you.
- No UNDESIGNED mechanics: code + sample/test content must not invent resources, effects, conditions, or
  content mechanics absent from `DESIGN_SPEC` and agreed. Need one? Surface it (Needs human), don't add it.
- PLACEHOLDERS ok, but ALWAYS FLAGGED: an improvised stopgap (an un-designed screen / UI / asset built to
  unblock) is fine — but flag it as a placeholder needing design (Needs human / Needs Claude Design);
  NEVER ship it silently as if it were the design. An unflagged placeholder is indistinguishable from
  finished work — that's how drift hides. (The merchant popover was the miss: built, not flagged.)
- CLEAN RENAMES: when a name changes, update ALL usages — no aliases, mapping layers, or back-compat
  shims — UNLESS feature-flagging is explicitly requested. Old names must not linger.
- No redundant docs. `STATUS.md` is the single source of state. This file stays under ~60 lines.
- No third-party IP in the product. Avoid design-reference names but if they should be present they would only live in
  design docs only — never in code, identifiers, assets, content data, or user-facing text.

## Working
- Start each task by reading `STATUS.md`. Finish by updating it. Run work via `/loop`.
- CD drops ship the design SOURCES (`design/dchtml/`, READ-ONLY — never edit CD source). Drops are
  processed by the Cowork session (stop → apply → guards → re-arm); the re-arm block in STATUS names
  what each drop RESOLVED and reprioritizes unblocked work — trust it, don't rediscover.
- Design canon = `design/DESIGN_SPEC.md`; keep it CURRENT — fold a decision in WHEN IT LOCKS. `STATUS.md`
  is build-STATE (target / debt / needs-human / progress) and POINTS to the spec, it doesn't re-specify
  design. A change that alters locked design reconciles DESIGN_SPEC in the same pass.
- Content/economy canon = `design/systems/*.md` (RACES/CORE_RUNES/TECHNIQUES/WEAPONS/ARMOR/FOES) — these
  ARE the source of truth for code; don't re-derive their numbers by hand. Doug maintains a balance
  spreadsheet outside the repo (cost/demand model, not tracked here) that these tables must stay
  reconciled against whenever he shares an update — that's how the 2026-07-05 Barbarian STR mismatch was
  caught: hand arithmetic written into CORE_RUNES.md had drifted from Doug's actual model. When he drops
  a new spreadsheet, reconcile design/systems/*.md against it in the same pass, then it's canon again.

### Claude Cowork Agents **ONLY**
Please read the orientation memory in the Roguebane project to ensure you follow all processes properly.