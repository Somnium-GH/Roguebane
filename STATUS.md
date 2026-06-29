# Status

## Current target
All shippable headless work done (items 1-5, 8 + item-6 combat screen). Remaining work (item 7
end-to-end + 6b loadout/map screens) is parked on human decisions — see "Needs human".

## Needs human (route around these; resolve when you can)
Each entry has enough context to resolve cold. The loop never blocks on these — it skips them.
- part-targeting interaction (per-technique aim vs. single focus-part vs. queue) — undecided.
  Currently single focus-part (structural=front, else weakest-first). Pick before 6b combat UX.
- item 7 end-to-end "play to feel it": the POC's whole point is a human judging whether the
  chassis exploit feels good. Needs (a) the chassis->rune->body flow built (see Debt), (b) a
  balance pass, then (c) a person to play it. Decide the parts-on-chassis + base-pool widening
  so a built body is real, then play. Headless math is already green (items 1-5).
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
- Shell ships only the combat/damage screen. Build/loadout + run-map screens (6b) are unbuilt.
  Blocked on: (a) the item-7 flow that turns a Chassis + RuneLoadout into a playable body
  (Sessions.Demo currently hand-builds a body, bypassing chassis/rune allocation), and (b) a
  balance pass. Reconcile after item 7's chassis->rune->body wiring + the parts-on-chassis call.
- Chassis has no parts (head/limbs) of its own; Sessions.Demo bolts on a head so the player can
  cast. Reconcile by moving body parts onto Chassis as data and widening chassis base pools to
  cover them (the current Grunt/Adept pools are toy thesis values). Needed by item 7.

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
- [~] 6. MonoGame shell: combat/damage screen DONE (placeholder shapes, input->commands, pause,
        win/flee/pause overlays) over Core.Session. Build/loadout + run-map screens parked (6b)
        — they need the item-7 chassis->rune->body flow + a balance pass. See Debt.
- [ ] 7. End-to-end playable: pick chassis -> allocate runes -> run -> siege. Play to feel it.
        <- NEEDS HUMAN (feel/judgment + a real play session). See Needs human.
- [x] 8. (optional) headless balance-sim: run N builds, surface dominant strategies.
        BalanceSim ranks BuildSpecs by deterministic ticks-to-clear; sweep over 4 loadouts.
