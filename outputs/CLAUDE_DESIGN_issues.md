# Claude Design payload — CURRENT only (2026-07-04)
**SENT to CD 2026-07-04 (Doug): B12 (per-core-rune armor theme commission) + the race-split ADDENDUM
folded into B2-GO's item 3.** Sending is NOT the close signal (same rule as every prior relay) — both
stay OPEN below until verified LANDED in the repo on the next audit; don't clear from dev memory yet.
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
**B5 ✅ · B6 ✅ (2026-07-03, reconciled against your latest memory dump):** verified in `layout.json`
— `invCard`/`loadoutCard`/`invTab` all carry `states.family`, the locked §6e vocabulary
(`equipped`/`disabled`/`equippable`/`locked`), and `hover` overlays. Thank you — both fully landed,
clear them from your dev memory. (The follow-up work is ours: our own renderer had a stale string
literal from before the rename — not a CD ask, nothing more needed from your side on this.)

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
       **ADDENDUM (2026-07-04, found after this item was sent — please fold in): HEAD and CHEST
       pieces need a RACE-SPECIFIC variant, not one generic sprite shared across human/elf.** Checked
       the real figure rects (`layout.json`): elf heads are landscape (152×104) vs human's near-square
       (~104-112²) — a shared sprite will stretch exactly like the raceCard head-portrait bug; elf
       torsos run ~9-10% narrower than human's at the same core. ARMS and LEGS are confirmed fine as
       ONE sprite (identical rect sizes across race, only repositioned) — no race split needed there.
       So: Helm/Breastplate/Cap/Circlet/Cloth-Cap-etc. (any head or chest piece, any line) → author
       TWO variants (human, elf); Vambraces/Greaves/Bracers/Leggings → ONE is fine. Path convention
       + full reasoning: LAYOUT_CONTRACT §12a.
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

B10. **Gear catalog display-name drift vs the §6c canon (engine uses the spec names; your
    catalog names are display-only today, but fix before anything binds them):** DEX head reads
    "Leather Leather Cap"/"Hardened Leather Cap"/... (canon: Leather Cap → Hardened Cap →
    Studded Cap → Reinforced Hood); DEX chest "Leather Leather Armor"... (canon: Padded Armor →
    Leather Armor → Studded Leather → Reinforced Leather); INT head "Cotton/Silk/Ornate/Humming
    Hood" (canon: Cloth Cap → Silk Hood → Ornate Circlet → Humming Circlet). Sprite IDS are fine —
    the engine adopted them as gear ids (armor_dex_head_plain etc.); only the `name` fields drift.
B11. **Bow sprites missing from the gear batch:** the catalog + sprite set covers every ladder
    EXCEPT bows (Short/Long/Compound/Elven — §6d ranged slot). The old sprites/gear/bow.png
    covers nothing in the new convention. Ask: 4 bow sprites (bow_short, bow_long, bow_compound,
    bow_elven) + catalog rows; engine ids will chase.

B12. **NEW COMMISSION (2026-07-04, Doug) — per-CORE-RUNE THEME on the worn-armor art. CORRECTED same
    day (Doug caught the first draft overclaiming "no new plumbing" — it hadn't been checked against
    LAYOUT_CONTRACT/ASSET_MANIFEST; see the real path convention now in LAYOUT_CONTRACT §12a).**
    Ground truth: actually WEARING armor on the figure (vs a card/inventory icon) is a system that
    **doesn't exist yet** — B2-GO's "armor worn-layers" is its first build. This commission is the
    THEME layer on top of that, NOT a reuse of something already there. Convention (§12a):
    ```
    sprites/gear/worn/<line>/<slot>_<tier>_<condition>.png                  // GENERIC, race-agnostic slots (arms/legs) — B2-GO
    sprites/gear/worn/<line>/<race>/<slot>_<tier>_<condition>.png           // GENERIC, race-specific slots (head/chest) — B2-GO addendum above
    sprites/gear/worn/<line>/<core>/<slot>_<tier>_<condition>.png           // THEMED, race-agnostic slots (arms/legs) — this commission
    sprites/gear/worn/<line>/<core>/<race>/<slot>_<tier>_<condition>.png    // THEMED, race-specific slots (head/chest) — this commission
    ```
    `line` ∈ {str,dex,int}, `slot` ∈ {head,chest,arms,legs} (int: chest+head only), `tier` 1-4,
    `condition` ∈ {healthy,damaged,broken}. **Themed art is a pure enhancement** — the engine falls
    back race-specific-themed → race-agnostic-themed → race-specific-generic (if the slot needs it) →
    race-agnostic-generic → bare, so shipping PARTIAL coverage (e.g. healthy-only, or T1-only, or
    human-before-elf) never breaks anything; it's fine to ship this incrementally.
    **Clarified (Doug, 2026-07-04): theme applies ONLY when a core wears its OWN favored/starting
    line** (Grunt/Warden=STR, Adept/Summoner=INT, Reaver/Ranger=DEX) — a Warden in DEX leather or an
    Adept in STR vambraces (gear is swappable, nothing stops mixing) gets plain GENERIC art, no theme,
    same as any other core would. Don't author "what if a Summoner wore plate" themed variants — out
    of scope, not needed. **Also out of scope: no new BODY-shape variation.** This is a flat art layer
    over each figure's EXISTING part rect; it doesn't touch the figure's own silhouette (that's the
    already-built race+core figure geometry, untouched by this commission).
    **Mount is race-agnostic ONLY for arms+legs — VERIFIED, not assumed, against the real figure rects
    in `layout.json` (2026-07-04):** every elf HEAD rect is landscape (152×104, aspect 1.46) against
    every human head's near-square (~104-112² depending on core) — the same stretch failure already
    caught on the raceCard head-portrait bug (B7 above); elf TORSO/CHEST runs ~9-10% narrower than
    human's at every core sampled (grunt 144 vs 160, warden 160 vs 176, ranger 136 vs 152). **Head and
    chest each need their OWN art per race** — don't share one sprite across human/elf for those two
    slots, it will visibly distort. Arms and legs, by contrast, have IDENTICAL rect dimensions across
    race in every pair checked (only x-position shifts) — one sprite per cell is fine there, no race
    split needed.
    **Scope LOCKED (Doug, 2026-07-04): the FULL set** — all 4 tiers × all 3 conditions (healthy/
    damaged/broken) for each core's own starting line, race-split where the geometry requires it.
    **Corrected count: ~384 sprites** (head+chest: 6 cores × 2 slots × 4 tiers × 3 conditions × 2
    races = 288; arms+legs: 6 cores × 2 slots × 4 tiers × 3 conditions × 1 [race-agnostic] = 96 — note
    Adept/Summoner have no arm/leg robe pieces at all, §6c, so their full 288-worth is concentrated in
    just head+chest). Nothing here falls back to generic art short of this; this is the complete
    near-term target, not a partial batch. **Likely multi-night — ship incrementally by tier, condition,
    or race if that's faster to produce; the fallback chain (§12a: race-specific themed → race-agnostic
    themed → generic same-condition → generic healthy → bare) covers any interim gap safely** (e.g. land
    human+healthy first, elf/damaged/broken next — nothing breaks between drops).
    **Bounded to each core's OWN starting armor line** (DESIGN_SPEC §7a) — not a full 6-core × 4-line
    cross-product (a Warden theming INT robes has no value; gear is swappable, §7, so cross-equipping
    still works fine with plain generic art, it just won't carry the theme). Six themes, keyed to their
    starting armor line:
    - **Grunt** (STR plate) — versatility, no strong motif: plain, practical, well-kept soldier's kit.
      The visual "middle of the road" — nothing another theme couldn't have, on purpose.
    - **Warden** (STR plate) — block & armor: the heaviest-READING silhouette of the two STR
      treatments — thicker plates, reinforced edges/rivets, maybe a shield-boss motif echoed on the
      other pieces. Reads as "built to not move."
    - **Adept** (INT robe) — spellcasting: arcane motifs — subtle runic trim/embroidery, a flowing
      silhouette, a restrained magical accent (don't overdo glow).
    - **Summoner** (INT robe) — minions/binding: necromantic/ritual motifs on the SAME robe silhouette
      as Adept but reading darker/heavier — bone or chain trim, ritual sigils — so Adept and Summoner
      stay clearly distinct even sharing a line.
    - **Reaver** (DEX leather) — dual-wielding: light, aggressive, agile cut — streamlined, less bulk,
      maybe twin-blade motif etching.
    - **Ranger** (DEX leather) — bow & pet: tracker/nature motifs — fur trim, quiver details, earthy
      palette — distinct from Reaver's leather despite sharing the line.
    Ship with the same deliverable shape as B2-GO (figure defs + inventory in layout.json, mgcb
    mirrors, 00-assets sheet refresh) so the two batches land together if convenient — but this is
    additive scope, call it out separately in your drop notes so it doesn't get lost inside B2-GO.

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
