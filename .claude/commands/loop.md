Run the development loop AUTONOMOUSLY. Keep shipping work until everything left needs a human,
then stop and surface the queue. Do NOT stop after one iteration.

Each iteration:
1. Read `STATUS.md`. Humans edit it between runs; new entries may critique or supersede
   ALREADY-SHIPPED work (a checked-off item is NOT immutable). Address these FIRST — a human
   revision of finished work OUTRANKS new roadmap work: re-open it as Debt and reconcile it before
   starting anything new. Otherwise, pick the most valuable roadmap item NOT parked in "Needs human".
2. Build the REAL thing — a working slice, not a stub — honoring the invariants in `CLAUDE.md`.
   - If it depends on something not yet built, build the real PARTIAL version that compiles and
     runs as far as it honestly can, and record what's missing + how to finish it under "Debt".
   - Stub only if no real partial can compile and run. If you must, log it under "Debt" with a
     reconciliation trigger. Always prefer something real over a hollow stub.
3. Write/extend headless tests for any Core change. Run all tests. Never commit red.
4. If you hit something that GENUINELY needs a human (an unmade design decision, ambiguous
   intent, a feel/judgment call, an external secret): add it to "Needs human" with enough
   context to resolve it cold, then ROUTE AROUND IT — pick the next unblocked work. Do not halt
   the loop. If a task can't be made green after a real attempt, park it the same way and move
   on rather than thrashing.
5. Commit the slice (small, semantic message). Update `STATUS.md`: check off done, update "Debt"
   and "Needs human", set the next target.
6. Before starting NEW work, reconcile any "Debt" whose blocker has cleared — prefer finishing
   provisional work over starting fresh, all else equal.
7. Repeat from step 1.

Stop ONLY when every roadmap item is done or parked in "Needs human". Then post one summary:
what shipped, the "Needs human" queue (the only thing that needs you), and any remaining "Debt".
