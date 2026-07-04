# Claude Design payload — CURRENT only (2026-07-03 late)
_Everything previously in this file is CLOSED: payload #11–18, reconcile residuals #1–8, and
addendum A1–A4 all verified LANDED across the two 2026-07-03 drops. Clear them from your dev
memory. History lives in git (`git log -- outputs/CLAUDE_DESIGN_issues.md`). This file always
holds ONLY the open items._

**SENT to CD 2026-07-03 (Doug relayed this exact batch: B0, B0b, B1, B2, B3, B4, B5).** Sending is
NOT the close signal — per process, an item only clears once it's verified LANDED in the repo (send
confirmations drift). Anything added to this file AFTER this note was not part of that relay.

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
B3. **Equipment `coreStats` list (bays/actions/budget) authored as a 2-col grid, wraps wrong:**
    element size `[131,16]` + `item: {flow:"grid", cols:2, size:[62,7], gap:2}` fits 2 columns at
    that width, so a 3-item list wraps: bays+actions share one row, budget lands alone on the next
    (live screenshot, Elf Summoner). This reads as a single label:value stat column elsewhere in the
    design (bays / actions / budget stacked) — please re-author as a single-column vertical list
    (e.g. `flow:"vertical"`, container ~`[62,25]` for 3 rows) rather than a 2-col grid sized for 4
    cells. (FYI: our engine's list layout derives column count from region width and ignores an
    authored `cols` hint, so `cols:2` alone isn't the fix — the size/flow combo is.)

B4. **"open Equipment" button elements missing on Encounter + CampaignMap** (long-standing STATUS
    Debt line, never made a payload — housekeeping catch 2026-07-03). Design intent (locked flow):
    every non-Equipment screen offers the Equipment entry; Encounter's is DISABLED in combat.
    CityMap already has `nav.equipment`; please author the same element (+ disabled state for
    Encounter) on the other two screens.
B5. **Equipment card state families never fire engine-side: no `family` key.** `invCard` authors
    the four border states we need (`equipped`=good/green, `ready`=plain, `dropped`=lockRed,
    `neutral`=dim) and `loadoutCard` authors `slotted`/`empty` — but neither carries a
    `states.family` key (compare `raceCard`/`coreCard`/`techCard`, which do), so the engine's
    family→state resolution skips them and inv cards draw base chrome only. Ask: add `family`
    keys (e.g. `"invCard"`, `"loadoutCard"`). Also flagging a probable naming/semantics pass
    after Doug's equipment-states design session locks the mapping (e.g. `dropped`→`disabled`);
    hold renames until we send the locked table.

B6. **(post-relay addendum — NOT in the batch sent 2026-07-03)** Doug's equipment-states session
    LOCKED the card-state semantics (DESIGN_SPEC §6e). Asks, for the next authoring pass:
    (a) rename the invCard states to the locked vocabulary — `dropped`→`disabled`,
    `ready`→`equippable`, `neutral`→`locked` (engine chases the rename, clean, no shims);
    (b) author HOVER variants for `invCard`/`loadoutCard`/`invTab` (brighten treatment,
    raceCard-style) — equipment authors no hover today; engine ships a flagged generic brighten
    stopgap until yours lands; (c) per the §6e paper-doll lock, DISABLED gear is REMOVED from the
    figure render — so NO dimmed/desaturated armor layer variants are needed (scope savings for
    the B2 regen batch); instead that batch should include a ranged BACK-MOUNT layer so an
    equipped bow/wand can render while the melee hands are full (§17 #22 — until it exists the
    engine draws no ranged mount rather than inventing one).

## Standing FYIs (unchanged, for context — not action items)
- design/05 v2 STAT BLOCKS are not adopted; Doug will run a live tuning session — if a future 05
  re-render can sample stats from a handed set, ask him for the tuned numbers then.
- Core Effect roster (incl. Called Shot) is canon; effect MECHANICS come later engine-side.
- Drops are applied via a stop/apply/re-arm handshake now — stage in `.drop/`, Cowork applies with
  the loop halted, guards run before the tree resumes.
