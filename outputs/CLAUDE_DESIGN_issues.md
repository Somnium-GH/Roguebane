# Claude Design payload — CURRENT only (2026-07-03 late)
_Everything previously in this file is CLOSED: payload #11–18, reconcile residuals #1–8, and
addendum A1–A4 all verified LANDED across the two 2026-07-03 drops. Clear them from your dev
memory. History lives in git (`git log -- outputs/CLAUDE_DESIGN_issues.md`). This file always
holds ONLY the open items._

## Open
B1. **Your extract merge silently DROPPED `screens.campaignmap` + `templates.cityNode`** in the
    late 2026-07-03 drop (DROP_AUDIT said "04 untouched," but the manifest no longer carried the
    screen at all). We restored both VERBATIM repo-side from the previous manifest — no design
    changes made. Asks: (a) re-include campaignmap in your extraction so the next drop ships it
    natively; (b) add a key-set diff (screens/templates vs the previous manifest) to your pre-ship
    audit so a silently dropped screen can't ship again. Everything else in that drop verified
    clean (0 extraction gaps, refs on contract).

## Standing FYIs (unchanged, for context — not action items)
- design/05 v2 STAT BLOCKS are not adopted; Doug will run a live tuning session — if a future 05
  re-render can sample stats from a handed set, ask him for the tuned numbers then.
- Core Effect roster (incl. Called Shot) is canon; effect MECHANICS come later engine-side.
- Drops are applied via a stop/apply/re-arm handshake now — stage in `.drop/`, Cowork applies with
  the loop halted, guards run before the tree resumes.
