# Claude Design payload — CURRENT only (2026-07-03 late)
_Everything previously in this file is CLOSED: payload #11–18, reconcile residuals #1–8, and
addendum A1–A4 all verified LANDED across the two 2026-07-03 drops. Clear them from your dev
memory. History lives in git (`git log -- outputs/CLAUDE_DESIGN_issues.md`). This file always
holds ONLY the open items._

**SENT to CD 2026-07-03, two relays (Doug): B0–B5, then B6–B9.** Sending is NOT the close signal —
an item clears only once verified LANDED in the repo. **RECONCILED against your evening 2026-07-03
drop:** B0 (strips 203/gap 5), B0b (`core.label` split), B1a (campaignmap ships natively), B3
(coreStats vertical) all VERIFIED LANDED — cleared below, thank you; clear them from your dev
memory. B9 is FOLDED into B2-GO (it was scoping for a batch that was on hold; the hold is lifted).

## Confirm-to-close (no action — just clear your memory)
B0 ✅ · B0b ✅ · B1a ✅ · B3 ✅ landed in the evening drop. B9 → merged into B2-GO below.

## Open
B1b. **Add a key-set diff (screens/templates vs the previous manifest) to your pre-ship audit** so a
    silently dropped screen can't ship again (the campaignmap loss class — you re-included it, the
    guard ask still stands; we run the same diff drop-side).
B2-GO. **FIGURE + GEAR ASSET REGEN BATCH — the HOLD IS LIFTED (2026-07-03): the weapon/armor naming
    lock it waited on is done (DESIGN_SPEC §6c/§6d canon; scoping formerly relayed as B9 is folded
    in here). This is the commissioning order — the whole batch, please:**
    1. **Weapon sprites** — ONE silhouette per type, FOUR material palettes (Iron → Steel → Mithral
       → Dwarven Steel), hand-socket mounts per LAYOUT_CONTRACT: Longsword · Axe · Mace · Claymore ·
       Battleaxe · Warhammer · Dagger · Rapier · Short Sword.
    2. **New gear families:** Sling (1H, pairs with shield) · Staff (2H) · Charm + Tome as OFFHAND
       hand-socket mounts · **wands are HAND items now** (hand-socket mount; dual = one per hand;
       never alongside bow/sling) · a ranged **BACK-MOUNT layer** (bow/sling) so an equipped ranged
       weapon renders while melee hands are full (ex-B6c).
    3. **Armor worn-layers** per figure part per line: STR plate under the NEW names (Helm /
       Breastplate / Vambraces / Greaves) with the same four material palettes — do NOT generate
       under the retired prestige names; DEX leather / INT robe / CON shields per their unchanged
       ladders. **No dimmed/disabled variants** (§6e: disabled gear un-renders — scope savings).
       Preserve the existing condition rows (healthy/damaged/broken) + bare-variant fallback scheme.
    4. **Race × core figure regen** on the established part/z-list contract (robe figures stay
       legitimately ~12-part); **fix the elf_ranger chest-accent neckline in this batch** (the
       original B2 — strap sits too high, reads as fused to the head).
    5. **Ship with:** updated figure defs + asset inventory in layout.json, mgcb source updates
       (we mirror game-side), refreshed 00-assets sheets. Our SMOKE FIGURES + asset-exists probes
       verify completeness on landing — a part that doesn't resolve will bounce back, so an
       inventory list in the drop notes helps us confirm fast.
    Note: figure-MORPH mechanics residuals (§7/§17 #15) are OUR composition questions, not art
    blockers — proceed on the current contract; propose contract changes in drop notes, don't block.

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

B6. **Equipment card-state semantics are LOCKED (DESIGN_SPEC §6e) — two asks, still open after the
    evening drop:** (a) rename the invCard states to the locked vocabulary — `dropped`→`disabled`,
    `ready`→`equippable`, `neutral`→`locked` (engine chases the rename, clean, no shims);
    (b) author HOVER variants for `invCard`/`loadoutCard`/`invTab` (brighten treatment,
    raceCard-style) — equipment authors no hover today; our generic brighten stopgap holds until
    yours lands. (The former (c) — no-disabled-variants + ranged back-mount — now lives in B2-GO.)

B7. **raceCard head portrait imageBind likely landed on the wrong element (causes a stretched-head
    render, not the ghost-double we thought we'd fixed).** `raceCard` has two overlapping parts for
    the headshot: rect `[1,1,53,77]` (portrait, aspect 0.69, gradient fill + right border — reads
    like a BACKGROUND PANEL) carries the live `imageBind:"sprites/body/{race.id}_grunt/head_healthy"`;
    rect `[10,22,35,35]` (square, aspect 1.0, drop-shadow — reads like the actual framed PORTRAIT) is
    a static unbound sample hardcoded to `human_grunt`. Source head art is landscape (e.g.
    `elf_grunt/head_healthy.png` = 152×104), so drawing it into the portrait-aspect rect stretches it
    badly; the square rect would render it much closer to native proportions. Ask: move
    `binds:"race.headImage"` + the imageBind path onto the SQUARE/shadowed part, and drop the
    imageBind (if any authoring artifact remains) from the background-panel part — it should stay a
    plain gradient panel with no image.
B8. **CityMap beacon-graph nodes have no CD-authored hover or current-position treatment at all**
    (Doug asked whether this was ever specified — checked DESIGN_SPEC: it isn't, anywhere). Today
    the engine hardcodes a bare stopgap directly in C# (hover = swap border between two flat colors;
    current node = static amber ring, no animation) because there's no template/states data for this
    screen's nodes to read at all — unlike `cityNode` (CampaignMap's spine template), which DOES
    author `states.current: {border:"amber", glow:true}`. Note the `glow` flag isn't implemented
    engine-side anywhere either (dead data) — so even where you've signaled intent for a pulsing/
    glowing current-indicator, we have no rendering primitive for it yet. Two separable asks once
    Doug locks the design: (a) author real hover/current states for the CityMap beacon nodes (not
    just the CampaignMap spine); (b) tell us what `glow:true` should actually look like (steady glow?
    pulse rate?) so we can build the primitive once and wire both screens to it.

## Standing FYIs (for context — not action items)
- **Tier ladders for the new families** (for card copy / labels): Sling Shepherd's → Braided →
  Sinew → Giantsbane · Staff Wooden → Twisted → Ornate → Humming · Charm Wooden → Bone → Ornate →
  Humming · Tome Old Worn → Leather → Ornate → Glowing. Wands/bows keep their existing ladders.
- **Tier-4 signature rule:** MAGIC gear's top-tier adjective is supernatural (Humming/Glowing);
  mundane gear's is not — keep the split in any generated copy.
- **Name lengths:** the "Dwarven Steel Short Sword" (24ch) class overflows current card name rects —
  Doug ACCEPTS overflow for now; final treatment is a parked Doug+Cowork decision. Don't
  unilaterally re-rect, but flag preferred options if you have them.
- design/05 v2 STAT BLOCKS are not adopted; Doug will run a live tuning session — if a future 05
  re-render can sample stats from a handed set, ask him for the tuned numbers then.
- Core Effect roster (incl. Called Shot) is canon; effect MECHANICS come later engine-side.
- Drops are applied via a stop/apply/re-arm handshake now — stage in `.drop/`, Cowork applies with
  the loop halted, guards run before the tree resumes.
