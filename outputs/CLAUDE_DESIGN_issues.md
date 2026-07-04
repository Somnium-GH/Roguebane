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
B0b. **equipment `coreLabel` binds `core.name` but authors "CORE GRUNT"** — the same bind feeds
    `currentCoreName` ("Human Grunt"), so ONE resolver can't produce both copies. The dc.html
    source styles the chip as muted "CORE" + bold core title; extraction flattened that to the
    shared bind. Ask: bind the chip to its own datum (e.g. `core.chipLabel`) or author it as
    parts, so the engine can render "CORE <TITLE>" without guessing from element ids.
B1. **Your extract merge silently DROPPED `screens.campaignmap` + `templates.cityNode`** in the
    late 2026-07-03 drop (DROP_AUDIT said "04 untouched," but the manifest no longer carried the
    screen at all). We restored both VERBATIM repo-side from the previous manifest — no design
    changes made. Asks: (a) re-include campaignmap in your extraction so the next drop ships it
    natively; (b) add a key-set diff (screens/templates vs the previous manifest) to your pre-ship
    audit so a silently dropped screen can't ship again. Everything else in that drop verified
    clean (0 extraction gaps, refs on contract).
B2. **HOLD for the next figure-art batch (don't do solo — ride it with the weapon/armor permutation
    regen once §6/§6c/§6d weapon+armor names are locked):** Elf Ranger figure (`elf_ranger`) — the
    brown chest-armor accent/strap sits too high, crowding the neckline so it visually reads as
    fused to the head rather than sitting on the torso (Doug, live screenshot). Once weapon + armor
    naming locks, this batch needs to regenerate figure art across the full **race × core rune ×
    equipment** permutation set anyway (new wield/armor system, DESIGN_SPEC §6/§6c/§6d) — fix this
    positioning in that same pass rather than a one-off patch now.

## Standing FYIs (unchanged, for context — not action items)
- design/05 v2 STAT BLOCKS are not adopted; Doug will run a live tuning session — if a future 05
  re-render can sample stats from a handed set, ask him for the tuned numbers then.
- Core Effect roster (incl. Called Shot) is canon; effect MECHANICS come later engine-side.
- Drops are applied via a stop/apply/re-arm handshake now — stage in `.drop/`, Cowork applies with
  the loop halted, guards run before the tree resumes.
