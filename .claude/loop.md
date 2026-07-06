# /loop guidance — ONE task per run, then STOP.
The built-in /loop re-fires for the next task. SHORT runs = clean context. Do NOT batch many tasks
or grind 30+ min — that pollutes context. One good slice, commit, stop. If a slice's VERIFY phase is
open-ended (drive N states, compare N screenshots), fork it — keeps this loop's own turn short even
when the fork itself runs long.

Deeper protocol detail (gate-approval channel, park-vs-stop, metrics methodology, retro) lives in
`.claude/protocols/INDEX.md` — not inlined here on purpose. Read a linked doc only when its trigger
condition (named in the index) actually applies.

Per run:
1. SYNC then READ. `git pull --rebase` FIRST so any external changes land before you work (STATUS,
   design, or code may have been updated outside the loop). Then READ `STATUS.md`.
   Human edits may revise ALREADY-SHIPPED work (a checked item is NOT locked).
   Those revisions WIN — do them first. Else pick the SINGLE most valuable item not in "Needs human".
   A RENAME revision is CLEAN: chase every usage, delete the old names + any compat shims (no back-compat
   unless a feature flag is explicitly asked — see CLAUDE.md).
2. BUILD that one item for real — a working slice, not a stub. Honor `CLAUDE.md`. Dep not built yet?
   Build the real partial that compiles+runs; log the rest as Debt. Stub only as last resort (log it).
   NEVER invent undesigned mechanics (resources/effects/conditions/content absent from `DESIGN_SPEC`) —
   surface them (Needs human), don't add them, even in sample/test content.
3. VERIFY by type. Build must be GREEN (`dotnet build` clean) before commit — a red build is never done.
   On ANY runtime crash, READ `bin/Debug/net9.0/crash.log` (Program.cs writes the full exception+stack)
   and fix from the stack — don't guess.
   - Core logic → headless tests. NEVER commit red.
   - UI/screen → build+run, save an RB_SMOKE shot AND confirm `crash.log` is clean; READ the shot + the
     matching `design/NN-*.png` + the `design/SCREENS.md` checklist; fix until it MATCHES — actually look.
     Run `python tools/ui_gate.py` GREEN before commit. A claim of visual improvement/regression must
     cite the gate's NUMBERS (per-element scores / border+text probes / collision count) — never only
     eyeballing. Never render text/chrome as an EMPTY box: unresolved content suppresses (or ships a
     FLAGGED shell label for a primary CTA).
     **MEASUREMENT IS SACRED: never change scoring/masks/thresholds/drives in a way that RAISES a
     score without a STATUS-logged human approval FIRST. Fix the render, not the ruler.** Channel:
     `.claude/protocols/drift_guardrails.md`. Masks need Doug's approval, each, logged. Before
     masking a "state divergence", ALIGN THE DRIVE to the ref
     state instead. "Matches, it's just AA" claims require the alignment-search offset (≤0.5px) + a
     clean geometry-diff row — not eyeball. A screen is "done/at floor" only on UNBLURRED scores +
     geometry-diff clean + the enumerated residual list re-verified.
     Smoke renders ONE state, so also drive empty/edge states (that's how the em-dash crash slipped past).
     Required ART missing/wrong (designer gap, not your code) and not composable from primitives →
     log under "Asset gaps (Needs Claude Design)" in STATUS, mark BLOCKED, move on.
   - Interactive behavior → DRIVE the input→state flow and ASSERT the spec. A screenshot shows LOOK,
     not BEHAVIOUR — don't trust a rendered control to do what it implies.
4. Genuine human-need (unmade decision, feel call, secret)? Add to "Needs human" with cold-start
   context; route around it. Can't go green after a real try? Park it the same way. Don't thrash.
   A failing gate that isn't yours: ISOLATE first (`git stash`, re-run, compare — don't assume),
   then park-not-block if proven pre-existing. Full protocol: `.claude/protocols/anti_block.md`.
5. COMMIT one small semantic slice. Update `STATUS.md`: check off, fix Debt + "Needs human", set the
   next target. Any NEW Needs-CD finding goes BOTH places in the same pass: the STATUS line AND a
   relay-ready item appended to `outputs/CLAUDE_DESIGN_issues.md` (the standing CD outbox — one entry
   per item, cold-start context, concrete ask). Items clear from that file only when verified LANDED
   in the repo, never on "sent". Keeping the outbox current is part of DONE. CONVERSELY, when this slice
   implements the engine consumer for something CD's dev-memory (`DEV_LOOP_MEMORY`) opened as
   ENGINE-PENDING, record the closure in `CD_CLOSED_ITEMS.md` (repo-root — the REVERSE of the outbox; CD
   reads it in the repo to confirm-to-close + clear its own dev-memory): CD item # · what shipped ·
   `file:symbol` evidence · date. That file is a DURABLE confirmation log, not a work queue. Keep STATUS LEAN (prune resolved/stale lines). If the slice changed LOCKED design, also
   reconcile `design/DESIGN_SPEC.md` (the canon). Append one line to `.claude/protocols/metrics.csv`
   (date, task tag, minutes, lines_changed via `git show --shortstat`, estimate_minutes, method —
   schema in `metrics.md`). PUSH the commit when you can (remote reachable); never
   force-push. If the step-1 pull or the push hits a CONFLICT you can't resolve cleanly, park it in
   "Needs human" rather than forcing. Then STOP — one task done.

When everything left is in "Needs human", OR the loop is TRULY STARVED (no unblocked, drop-independent,
valuable work remains) or BLOCKED (waiting on an external drop / a human decision): **STOP LOOPING** — say
so in one line. Do NOT spin, invent busywork, lower the bar, or grind low-value churn to appear busy. A
clean stop with a clear "blocked on X" beats a wasted pass.
STARVED/BLOCKED must be PROVEN, not felt: emit the ENUMERATED per-element remaining-delta list (each
tagged CD / system / human, with a reason). No backed-up list ⇒ you are NOT starved — keep pixel-perfecting
what's achievable. Missing systems/content NEVER block perfecting the elements that DO exist. VERIFY
DETERMINISTICALLY (element coverage + content bound-not-sample + a design-PNG diff), not by eyeballing.
One red gate ≠ STARVED/BLOCKED — see `.claude/protocols/anti_block.md` for isolate-then-park.
