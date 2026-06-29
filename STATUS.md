# Status

## Current target
Item 6: thin MonoGame shell — combat/damage screen (placeholder shapes), input->commands, pause.

## Needs human (route around these; resolve when you can)
Each entry has enough context to resolve cold. The loop never blocks on these — it skips them.
- part-targeting interaction (per-technique aim vs. single focus-part vs. queue) — undecided.
- silence-on-head: binary vs. graded — leaning graded, with a disable as the hard off.
  Currently BINARY in code (head destroyed/disabled => fully silenced). Same call applies to
  capability degradation generally: a damaged part is currently all-or-nothing (full output
  until 0 health, then destroyed). Graded would scale technique/part output with remaining
  health. To resolve: decide the curve (linear? threshold bands?) and which capabilities scale.
- rallied-support flavor: repair the standing front (built) vs. spawn fresh defenders vs. both.
  Decides whether a siege is a DPS race or an attrition/adds fight. See matching Debt entry.

## Debt (provisional work + how to reconcile it)
Real-but-incomplete work and (rare) stubs, each with the trigger that lets it be finished.
- Enemies modeled as single-part encounter defenders (a Part with health on a shared Entity),
  not multi-part foes that cast back. Reconcile when an enemy needs its own parts/techniques:
  give each defender its own Entity + Caster aimed at the player and step both sides in Battle.
- Rallied support implemented as a repair-stream on the front. Alternative (spawn fresh
  defenders into the encounter) is unbuilt — a feel call; see Needs human. Reconcile by adding
  a spawn mode to Encounter.RallyTick once the support flavor is decided.

## POC roadmap (thesis-first; items 1-5 are headless Core+tests, no rendering)
- [x] 1. Core skeleton: attribute pool (live allocation), Entity + Parts, base types. Tests.
- [x] 2. Rune economy: budget; Marks (prereq ladder, overwrite, partial-refund climb); Paths;
        one keystone (Hollow Vessel). Tests assert budget/prereq/climb math.
- [x] 3. Two chassis: Grunt (low base, fat budget, cheap runes) + specialist (high base, tight
        budget). Test: Grunt can climb to the specialist's keystone at a real cost.
        <- thesis math validated headless.
- [x] 4. Techniques + combat tick: 6 techniques, timered + sustained, parallel-by-allocation,
        deterministic fixed-step. Parts as subsystems; disable = temp part-off that returns its
        attribute to the pool; damage degrades capability; head-disable silences casting. Tests.
- [x] 5. Enemies-with-parts + one structured castle; rallied-support stream; flee; run = 2
        control points -> castle. Tests.
- [ ] 6. MonoGame shell: combat/damage screen (placeholder shapes), input->commands, pause.
        Then build/loadout screen. Then run map.
- [ ] 7. End-to-end playable: pick chassis -> allocate runes -> run -> siege. Play to feel it.
- [ ] 8. (optional) headless balance-sim: run N builds, surface dominant strategies.
