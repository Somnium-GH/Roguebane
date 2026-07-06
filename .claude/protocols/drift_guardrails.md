# Drift guardrails — the approval channel for gate-relaxing changes

loop.md already states the rule ("measurement is sacred"). This doc names the CHANNEL so it's
never ambiguous which changes need it or how to request it.

## What needs approval before it lands
Any change whose effect is to make a gate score BETTER without the underlying render getting
better:
- Editing `tools/ui_baseline.json` (`--update`).
- Loosening a threshold, adding/widening a mask, or changing what `ui_gate.py` measures.
- Changing a smoke drive's seeded state to avoid a divergent case instead of fixing it.

Fixing the actual render (layout, binds, content) so a TRUE score rises needs no approval — that's
just doing the work. The line: did the WORLD change, or did the RULER change? Ruler changes need
sign-off.

## The channel
1. Write the proposed change + the numbers it would move (before/after, per-element) into STATUS.md
   under "Needs Doug", tagged **BASELINE-UPDATE-REQUEST**.
2. Do not apply the change in the same commit as the request. Wait for Doug to respond in STATUS.md
   (he edits it directly) or in chat — either counts as logged approval once it's reflected in
   STATUS.md.
3. Land the approved change as its own commit, citing the STATUS.md line that approved it.

## Why this exists
Silent ruler-bending is invisible in a diff and indistinguishable from real progress until someone
eyeballs the actual screen much later. STATUS.md is the one place both Doug and every future loop
run reads first — routing through it means a bent ruler can't hide.
