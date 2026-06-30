# /loop guidance — ONE task per run, then STOP.
The built-in /loop re-fires for the next task. SHORT runs = clean context. Do NOT batch many tasks
or grind 30+ min — that pollutes context. One good slice, commit, stop.

Per run:
1. READ `STATUS.md`. Human edits may revise ALREADY-SHIPPED work (a checked item is NOT locked).
   Those revisions WIN — do them first. Else pick the SINGLE most valuable item not in "Needs human".
2. BUILD that one item for real — a working slice, not a stub. Honor `CLAUDE.md`. Dep not built yet?
   Build the real partial that compiles+runs; log the rest as Debt. Stub only as last resort (log it).
3. VERIFY by type:
   - Core logic → headless tests. NEVER commit red.
   - UI/screen → build+run, save an RB_SMOKE shot; READ the shot + the matching `design/NN-*.png` +
     the checklist in `design/SCREENS.md`; fix until it MATCHES. You can see images — actually look.
     Required ART missing/wrong (designer gap, not your code) and not composable from primitives →
     log under "Asset gaps (Needs Claude Design)" in STATUS, mark BLOCKED, move on.
   - Interactive behavior → DRIVE the input→state flow and ASSERT the spec. A screenshot shows LOOK,
     not BEHAVIOUR — don't trust a rendered control to do what it implies.
4. Genuine human-need (unmade decision, feel call, secret)? Add to "Needs human" with cold-start
   context; route around it. Can't go green after a real try? Park it the same way. Don't thrash.
5. COMMIT one small semantic slice. Update `STATUS.md`: check off, fix Debt + "Needs human", set the
   next target. Keep STATUS LEAN (prune resolved/stale lines). If the slice changed LOCKED design, also
   reconcile `design/DESIGN_SPEC.md` (the canon). Then STOP — one task done.

When everything left is in "Needs human", say so in one line instead of picking work.
