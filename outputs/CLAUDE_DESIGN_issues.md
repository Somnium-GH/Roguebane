# Claude Design payload — CURRENT only (2026-07-03 late)
_Everything previously in this file is CLOSED: payload #11–18, reconcile residuals #1–8, and
addendum A1–A4 all verified LANDED across the two 2026-07-03 drops. Clear them from your dev
memory. History lives in git (`git log -- outputs/CLAUDE_DESIGN_issues.md`). This file always
holds ONLY the open items._

## Open
B0. **resourceStrip still clips SUMMONS on citymap + equipment** (encounter is FIXED by your
    uniform-chip pass: 203 = 4x47 + 3x5 gap, all four seat — verified on the gate shot). The other
    two screens author the strip at 197 wide with gap 8: four 47-wide chips need 4x47 + 3x8 = 212.
    Numbers to make 4 seat at 197: gap 3 (188+9=197 exact), or widen the strips to 212, or narrow
    the chips. Engine clips overflowing cells, so SUMMONS drops silently there today.
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
