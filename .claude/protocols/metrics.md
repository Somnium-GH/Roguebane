# Loop metrics — schema + how to log a run

**(2026-07-07, Doug) This is a byproduct, not a deliverable — a cheap breadcrumb, not a ritual.**
Never spend a separate commit or a separate turn on it. If logging a row would cost more than typing
one CSV line into the commit you're already making, skip it that cycle — a run whose only output is
a metrics row is worthless and must not happen. `estimate_minutes` is RETIRED (see below); nothing
needs to be estimated before starting.

Data lives in `metrics.csv`, one row per commit that closes a loop run/slice — appended INSIDE that
same commit, never a follow-up one. Append, never rewrite history. Purpose: tune slice size against
the ≤60min target with a query, when convenient — a nice-to-have, not a gate, not a reason to run.

## Columns
`date,task,minutes,lines_changed,estimate_minutes,method`

- `date` — commit date, `YYYY-MM-DD`.
- `task` — short task tag (`#1`, `#2`, `#3`, or a one-word slice name). Matches STATUS.md/plan
  naming so a row is traceable back to what shipped.
- `minutes` — wall-clock for the run. See `method` — this is a PROXY, not a stopwatch.
- `lines_changed` — `git show --shortstat <sha>` insertions+deletions, this commit only.
- `estimate_minutes` — **RETIRED 2026-07-07.** Always `-`. It never got used in practice and isn't
  worth the overhead of maintaining — don't estimate before starting, don't chase this column.
- `method` — how `minutes` was derived (see below), if you're bothering to log at all.

## `method` values (and their honesty level)
- `commit-delta` — this commit's timestamp minus the previous commit's. CHEAPEST but includes any
  idle/break time between sessions — inflates badly across a session boundary. Flag suspiciously
  large gaps in a comment rather than trusting them.
- `tool-duration` — a background agent/fork reported its own `duration_ms` for the whole slice
  (Agent-tool result `usage.duration_ms`). GROUND TRUTH for that slice — prefer this when a task ran
  as one continuous fork/agent call.
- `manual` — hand-timed or estimated after the fact. Least reliable, use only when the other two
  aren't available.

## Reading the data so far (seeded 2026-07-06)
Task #2 and #3 both ran 2-3x over the ≤60min target (179min, 112-117min). Task #1 (v6 overhaul)
shows 339min by commit-delta but that slice was explicitly scoped as ONE coupled unit (races+cores+
kits+effects can't split — `CoreCampaignTests` proves the coupling) — its number isn't a slicing
failure, it's an intentionally large atomic chunk. The real signal: multi-step VERIFICATION work
(screenshot review across every tab/page/state) is what pushed #2 long, not the code change itself.
**Recommendation carried into RETRO.md:** when a slice's verification phase is open-ended (drive N
states, compare N screenshots), fork it — bounds the main loop's own turn length even when total
wall-clock for the feature stays long. Task #3 did this from the start (whole slice as one fork
call) and landed clean in one shot.
