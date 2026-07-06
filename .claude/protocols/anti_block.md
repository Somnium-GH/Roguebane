# Anti-block / regression protocol

One failing gate must never permanently stop all progress. A gate failure is either YOUR SLICE'S
FAULT (fix it, or it's not done) or PRE-EXISTING (prove it, park it, keep moving). Never assume —
isolate.

## Isolation test (how to tell which one it is)
`git stash` (removes your uncommitted slice) → rebuild → re-run the failing gate → compare numbers.
- Same/near-same failure at HEAD without your change ⇒ PRE-EXISTING. Not your slice's job to fix.
- Failure disappears or is materially different ⇒ YOURS. Fix before commit, never mask.
`git stash pop` after either way.

Real precedent (2026-07-06): `ui_gate.py` was RED on every screen while landing Task #2's paging
work. Isolation proved it pre-existing (near-identical failure at HEAD, in fact slightly worse than
the branch). Task #2's fixes shipped on schedule; the gate finding was parked and became Task #3's
lead investigation item (which resolved it as a `fidelity_diff.py` tie-break artifact, not a render
bug — see `metrics.csv` row `2026-07-06,#3,...` and STATUS.md's Task #3 banner).

## PARK, don't block
When a gate fails and isolation proves PRE-EXISTING:
1. Commit your own proven-clean slice anyway — don't let someone else's red block your green.
2. Log the parked failure in STATUS.md with ENUMERATED numbers (per-element scores, counts) — never
   "seems off." No numbers ⇒ you haven't proven it, go back and measure.
3. Tag it for the next actionable task (Debt, Needs Doug, or a new Task-N candidate). Say what would
   resolve it (a fix, an investigation, or a human decision) so the next agent doesn't re-diagnose.
4. Keep working the rest of the queue. A parked item is not a stop condition.

## STOP is different from PARK
Only stop the loop when EVERY remaining item is blocked/needs-human — and that must be PROVEN, not
felt: emit the enumerated per-element remaining-delta list (each tagged CD / system / human, with a
reason), per loop.md. "I don't feel like there's more to do" is not a stop condition. "Here is the
complete list of N items and each is blocked on X" is.
