# Status

## Current target
Item 3: two chassis (Grunt vs specialist); test Grunt climbs to specialist keystone at real cost.

## Needs human (route around these; resolve when you can)
Each entry has enough context to resolve cold. The loop never blocks on these — it skips them.
- part-targeting interaction (per-technique aim vs. single focus-part vs. queue) — undecided.
- silence-on-head: binary vs. graded — leaning graded, with a disable as the hard off.

## Debt (provisional work + how to reconcile it)
Real-but-incomplete work and (rare) stubs, each with the trigger that lets it be finished.
- (none yet)

## POC roadmap (thesis-first; items 1-5 are headless Core+tests, no rendering)
- [x] 1. Core skeleton: attribute pool (live allocation), Entity + Parts, base types. Tests.
- [x] 2. Rune economy: budget; Marks (prereq ladder, overwrite, partial-refund climb); Paths;
        one keystone (Hollow Vessel). Tests assert budget/prereq/climb math.
- [ ] 3. Two chassis: Grunt (low base, fat budget, cheap runes) + specialist (high base, tight
        budget). Test: Grunt can climb to the specialist's keystone at a real cost.
        <- thesis math validated headless.
- [ ] 4. Techniques + combat tick: 6 techniques, timered + sustained, parallel-by-allocation,
        deterministic fixed-step. Parts as subsystems; disable = temp part-off that returns its
        attribute to the pool; damage degrades capability; head-disable silences casting. Tests.
- [ ] 5. Enemies-with-parts + one structured castle; rallied-support stream; flee; run = 2
        control points -> castle. Tests.
- [ ] 6. MonoGame shell: combat/damage screen (placeholder shapes), input->commands, pause.
        Then build/loadout screen. Then run map.
- [ ] 7. End-to-end playable: pick chassis -> allocate runes -> run -> siege. Play to feel it.
- [ ] 8. (optional) headless balance-sim: run N builds, surface dominant strategies.
