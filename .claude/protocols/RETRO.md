# Retrospective — /loop development, through 2026-07-06 (Tasks #1-3)

Point-in-time entries, newest first. Append a dated section on future retros — don't rewrite prior
ones, prior context stays true to when it was written.

## 2026-07-06 — v6 overhaul + paging + pixel-perfection (Tasks #1-3)

**What worked**
- **Fork-for-verification.** Task #3 ran as one continuous background fork instead of inline main-
  loop turns. Landed clean in ~112min of fork time with zero main-loop context spent on screenshot-
  by-screenshot review. Compare Task #2, done inline: same class of verification work (drive N
  states, compare N shots) cost 179min AND all of it sat in the main loop's context. Forking
  open-ended verification is the single highest-leverage change available right now — see
  `metrics.md`'s recommendation.
- **Isolation before blame.** When `ui_gate.py` went red on every screen mid-Task-#2, `git stash` +
  rebuild + re-run proved it pre-existing in under 5 minutes instead of an open-ended debugging
  detour. Now codified in `anti_block.md` — do this FIRST on any gate failure, don't investigate the
  failure itself until you know whose fault it is.
- **STATUS.md as the coupling point.** CHUNK C item 4 had already predicted the exact gate-noise
  state Task #2 hit ("content changed under every screen, the old numbers are noise now... baseline
  re-pin happens once after A+C land") — written down BEFORE it happened, during earlier planning.
  When it did happen, recognizing "this is the thing STATUS already told us to expect" turned a
  scary red gate into a non-event. This only works if STATUS stays current; a stale STATUS would
  have cost the same debugging detour isolation now prevents by a different route.

**What surprised us**
- **Task #1's coupling.** Race stats, core stat bonuses, and kit swaps could NOT land as separate
  commits — `CoreCampaignTests` exercises the full kit at once, so partial landings regressed the
  suite (383/392, worse than an even-more-partial 387/392 attempt before it). The plan absorbed this
  by scoping Task #1 as one deliberately large atomic slice rather than forcing an artificial split.
  Lesson: before slicing a feature, check whether its own test suite exercises components together —
  if it does, the SLICE boundary should match the TEST boundary, not an arbitrary size target. A
  340min atomic slice that can't split is not a process failure; a 179min slice that COULD have
  forked its verification phase is.
- **Bind-gap bugs hide behind sample text.** Three separate bugs this session (`ResolveBind`'s
  per-type switch missing a case) all looked identical: a card silently rendering the manifest's
  static SAMPLE text instead of real data, with no crash, no red gate signal specific to it — only
  caught by actually reading rendered text against expected content. `ui_gate.py`'s numeric scores
  did NOT catch these on their own (sample text can score fine against a reference if the reference
  was captured from the same placeholder state). Lesson: a "matches the design PNG" gate is
  necessary but not sufficient — screenshot review must include reading the actual text for
  plausibility, not just comparing pixel positions.
- **Tooling artifacts can look exactly like render bugs.** The recurring `-3,-3px shift` finding
  looked, for a while, like a systemic renderer offset worth escalating. It was `fidelity_diff.py`'s
  own tie-break default (reports `(-3,-3)` for any 0%-fidelity content mismatch, not a real
  position). Root-caused only because the fork checked whether every flagged instance sat at exactly
  0.0% fidelity — a pattern that shouldn't exist if the shift were real and partial. Lesson: before
  trusting a tool's diagnostic OUTPUT, sanity-check the tool's own tie-break/default behavior on the
  degenerate case (total mismatch), especially when the "finding" is suspiciously uniform.

**Carried forward as protocol (see linked docs)**
- Fork open-ended verification phases by default — `metrics.md`.
- Isolate before diagnosing any gate failure — `anti_block.md`.
- Gate-relaxing changes route through STATUS.md's "Needs Doug", named `BASELINE-UPDATE-REQUEST` —
  `drift_guardrails.md`.
- Log every closed slice's minutes/lines to `metrics.csv` so "are we slicing well" becomes a query.
