# Claude Design payload — CURRENT only (2026-07-05)

**‼ RECONCILED against CD_STATUS.md (pass 8/9, 2026-07-05 — CD's new living open-gap file replaces
DEV_LOOP_MEMORY, ships in every drop under `design/dchtml/`):** verified CD's landed claims against the
real tree, not the memo. CLOSED here: **B18 glyphs** (Flurry/Aimed Shot/Siphon/Barkskin/Sacrifice/Bind +
the Frenzy/Flurry two-badge split) all present in `icons/technique/`; **B21 authoring** (the `parent`
re-anchor — 160 `parent` keys now in `layout.json`). Both now have only ENGINE/OUR residual (dual-pool
draw; recursive parent-box resolve), tracked in STATUS, no longer CD asks. Still OPEN for CD: B18's
Parry/Steel/Suture + Iron Golem/Hound icons, B20 re-extraction, and the B-series below.

**‼ RECONCILED against the 2026-07-05 v6/roster drop (Cowork):** figures for **5 races × 7 cores**
(dwarf + halfling as asked, PLUS half_giant + barbarian unasked — both adopted: Half-Giant is now a
locked race, Barbarian a locked core, see `design/systems/RACES.md`/`CORE_RUNES.md`), worn sets for
all 5 races incl. barbarian(str) themes, regenerated figures/gear/worn manifest sections, and the 14
per-core 01/02 reference PNGs (all exactly 1920×1080 — thank you, contract held). Item states swept
below: **B17 ✅ closed (exceeded)**, B15/B16 folded into the new **B20** (the copy/chips exist in your
refs now; what's missing is the EXTRACTION so the engine can render them), B18 updated (frenzy +
skeleton icons arrived; the rest still needed), B12 stays closed.
**‼ CORRECTION 2026-07-04 (Doug) — the worn-armor batch relayed earlier today was MIS-BUILT.** CD
generated themed art for EVERY armor type × core × race (a full cross-product) plus an armor type
literally named "plain". Neither is the design. The corrected, CONSOLIDATED worn-armor convention now
lives in **B12** below and **SUPERSEDES** the earlier B12 (themed layer) and B2-GO item 3's armor
bullet + its race-split addendum — they were one system described twice on a line-first convention,
which is how the mistake slipped in. Two fixes at the heart of it: **(1) "plain" is not an armor type**
— the unarmored part is `bare` (DEX's display name "Plain leather" + the bare-body fallback are what got
conflated); **(2) themed art exists ONLY for each core's OWN favored line, never the cross-product.**
**RESOLVED 2026-07-04 — CD regenerated to B12's convention, verified clean on landing (744 files, no
cross-product, no "plain" type, 0 missing / 0 extra). Confirm-to-close B12 below; remaining wiring is
loop-side (game-mgcb mirror + engine), not a CD ask.** Everything else in the earlier relay (weapons,
new gear families, figure regen) stands.
**SENT to CD 2026-07-04 (Doug): B12 + the race-split addendum — now CORRECTED per the banner above.**
Sending is NOT the close signal (same rule as every prior relay) — items stay OPEN below until verified
LANDED in the repo on the next audit; don't clear from dev memory yet.
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
**B17 ✅ (2026-07-05) — Dwarf + Halfling figure batch landed AND exceeded** (half_giant + barbarian
came too, now both canon; worn sets + manifest sections regenerated cleanly, merge guard held). The
race-card art/portrait + final blurbs/tags residue moves into B20. Thank you — clear from dev memory.
B0 ✅ · B0b ✅ · B1a ✅ · B3 ✅ landed in the evening drop. B9 → merged into B2-GO below.
**B12 ✅ (2026-07-04) — the CORRECTED worn-armor set landed clean:** 744 files on the race-first
full-part convention, cross-product leak check EMPTY, no "plain" type, complete (0 missing / 0 extra),
elf_ranger neckline strap now mid-chest. Thank you — clear from your dev memory. Remaining worn wiring
(game-side mgcb mirror + engine themed-half/draw) is OURS, not a CD ask.
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
    3. **Armor worn-layers — SEE B12 (corrected, consolidated).** This item's original text (generic
       worn-layers on a line-first path, race-split on head/chest only, arms/legs shared) is
       SUPERSEDED by B12 below: full self-contained part sprites, race-first, EVERY slot per race,
       `bare` (not "plain") as the unarmored fallback, generic art core-agnostic, themed art only each
       core's favored line. Names / tiers / conditions unchanged (STR Helm/Breastplate/Vambraces/
       Greaves under the material ladder — NOT the retired prestige names; DEX/INT ladders §6c;
       healthy/damaged/broken; no disabled variants, §6e). Build the whole worn-armor set from B12.
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
B13. **Merchant `waresShelves` list is ONE PIXEL short of fitting its own authored 3-row layout.**
    Element size `[692,377]`, item `{template:"shopSection", flow:"vertical", gap:12, size:[692,118]}`
    — three rows need `3×118 + 2×12 = 378px`, the container is `377px`. Our list layout's overflow
    rule silently drops any cell that doesn't fully fit rather than partial-rendering it, so the whole
    3rd wares section (e.g. Minions) vanishes even though the page-count math says it should show
    (Doug: "buy out one category and another appears that was invisible" — that's this). Ask: bump
    `waresShelves.size[1]` to at least 378, ideally 382+ for margin.
B14. **Equipment `buildMinions` list is sized for ZERO cards, not one.** Element size `[94,89]` with
    no `item.size` override, so it falls back to `templates.loadoutCard`'s own size `[131,89]` —
    wider than the container. Our list layout drops any cell that doesn't fully fit, so with a
    131-wide card and a 94-wide container, NOTHING renders regardless of how many minions are
    slotted (live screenshot: "MINIONS - 1/1 slotted" header correct, card area totally blank).
    Compare `loadoutList` (technique action bar, same `loadoutCard` template): container `[681,89]`
    with `item.size` explicitly `[131,89]` — sized to fit ~5 cards. `buildMinions` needs the same
    treatment: widen to fit `Bays × (131 + gap)`, matching how the technique bar is sized for its
    own bay count.

**2026-07-05 batch (v6 design session + the roster drop's follow-ups):**

B20. **RE-EXTRACTION to the per-core refs + the v6 roster (the big one — the engine renders only what
    the manifest authors, so everything here is render-blocked until it lands):**
    1. The 01/02-`<core>` refs show elements the manifest doesn't author yet: **per-core STAT-BONUS
       chips** (ex-B16 — `core.statBonus` list on the NewGame coreCard + Equipment identity block; one
       colored chip per non-zero stat: Grunt +1 all · Warden +5 CON · Adept +5 INT · Summoner +3 INT/
       +2 CON · Reaver +5 DEX · Ranger +4 DEX/+1 CON · **Barbarian +4 STR/+1 CON**), **action-bar cards
       with rules text** (name + cost + italic description + footer state line), and the **"minions"
       label vocabulary** (fold B19 into the same regen). New Core-Effect copy (ex-B15) is in
       `design/systems/CORE_RUNES.md` — some rules text runs long (Ranger's compound sentence), size
       the coreEffect rects for it.
    2. **NewGame re-author for the grown roster: 5 races × 7 cores** (raceCards/coreCards currently
       seat 2×6 — cells past the container silently drop, so the screen literally can't show the new
       picks). Include Doug's tile treatment: **each core tile carries its core's BG color + Core-Effect
       trim color** — please publish the per-core accent TOKENS in the style block so the engine reads
       them instead of our flagged stopgap palette (STATUS Chunk C.2).
    3. Refresh the stale refs to v6 data: 05-newgame (new roster + tile colors), 03/07 if their strips
       show core identity; a `01/02-*-<core>` set for any future core additions. Race-card art/portraits
       + final race blurbs/tags for Dwarf/Halfling/Half-Giant (Doug supplies copy) ride here too.
    4. FYI so it doesn't surprise you: mock numbers in the refs that disagree with the systems docs
       (e.g. Claymore "6 dmg · 1.4×" vs WEAPONS.md's 7 dmg · 1.3×; Stoneskin/Barkskin pool text) are
       treated as NON-canon — docs win; no action needed unless you'd rather regenerate the copy.

B18. **Technique + minion ICONS — RECONCILED against CD_STATUS #34/#36 (pass 8/9), verified on the real
    tree.** LANDED since last relay (present in `Roguebane.Content/icons/technique/`, confirm-to-close —
    clear from your dev memory): **Flurry ✅ · Aimed Shot ✅ · Siphon ✅ · Barkskin ✅ · Sacrifice ✅ · Bind ✅**
    (pass 8). And the **Frenzy/Flurry two-badge split-fill treatment ✅** — CD_STATUS #36 re-captured both
    `frenzy.png`/`flurry.png` as the split STR/DEX glyph (pass 9); verified both modified on the tree. That
    closes the "Frenzy needs a two-badge revision + Flurry's icon" ask outright. Stoneskin's existing icon
    stays for the T2.
    **STILL NEEDED for the v6 kits (the only open icons):** **Parry, Steel, Suture** (technique glyphs —
    absent from the tree); minion icons **Iron Golem, Hound** (only `skeleton.png` is in `icons/minion/`).
    Icons + mgcb source (we mirror game-side). (Rapier / Staff / Charm / Tome weapon sprites are in B2-GO.)
    Engine-side FYI (OURS, not a CD ask): the `either`/`payAttr` dual-pool manifest field + split-cost
    two-row draw (CD_STATUS #36 consequences) is engine work, tracked in STATUS — the glyph art is done.

B19. **CANON RENAME (2026-07-05, Doug) — "bay(s)" is retired as the minion-slot term; vocabulary is
    "Minions" only, no separate slot noun.** DESIGN_SPEC/CORE_RUNES/SCREENS canon already updated our
    side. Your manifest still spells the retired term into ids/binds: template `minionBay`; element ids
    `bayGroupLabel`/`bayList` (note: `bayGroupLabel`'s rendered TEXT already correctly says "MINIONS" —
    only the id is stale); binds `bay.hotkey`, `bay.state`, `bay.name`, `bay.gateColor`, `bay.cost`,
    `bay.description`, `bay.amount`, `loadout.bays`, `core.bays`, `preview.bays`; part `baysBox`. Ask:
    next regen, rename these to the "minion" equivalent (e.g. `minionCard` template, `minion.hotkey`,
    `minion.state`, `minion.name`, `minion.gateColor`, `minion.cost`, `minion.description`,
    `minion.amount`, `loadout.minions`, `core.minionCap`, `preview.minionCap`, `minionsBox`) — same
    class of ask as the earlier B6 vocabulary rename. Our renderer's bind-key literals wait for this
    drop to land before renaming in lockstep (avoids breaking the live binding mid-flight).

**2026-07-05 — STRUCTURAL directive (Doug): eradicate absolute positioning. This applies to EVERY
screen, not a single asset — please treat it as a standing correctness rule, not a one-off fix.**

B21. **ABSOLUTE POSITIONING IN THE MANIFEST — the resolution-scaling bug. Eradicate it everywhere.**
    **[STATUS 2026-07-05 — LANDED (verified): the `parent`-field re-extraction (CD dev-memory #35) is now
    in our `layout.json` — 160 `parent` keys across the 6 screens, confirmed on the real tree. The CD
    AUTHORING half of B21 is DONE — confirm-to-close, clear from your dev memory. The remaining half is
    ENGINE/OURS — recursive parent-box resolution in the interpreter — and it's already the #1 HIFI
    priority at the top of STATUS.md (today nothing reads `parent`, so every parented child mis-places;
    that's the NewGame screenshot Doug hit). NOT a CD ask anymore. Original ask kept below for the audit
    trail; delete this item once the engine resolver lands and the screens verify.]**
    Symptom Doug is hitting: screens line up at exactly 1920×1080 but DRIFT, GAP, and OVERLAP as the
    window grows past 1080 — badly at 2300px+ and at any off-16:9 aspect. Root cause is baked into the
    emitted manifest: **elements that visually belong to the RIGHT / BOTTOM / CENTER are anchored
    `TopLeft` and given a large ABSOLUTE offset**, so their on-screen position is only correct at the
    2×-design size they were authored at. The offset should be relative to the zone the element lives in;
    instead it's a raw top-left pixel coordinate that the engine can't reflow.

    Evidence (`encounter`, current `layout.json`, design space 960×540):
    - `foeLabel` — anchor `TopLeft`, offset `[929,34]`. It's the "Foe" title on the RIGHT edge (the
      dc.html authored it `right:26px`). Correct is `TopRight`, offset ≈ `[-31,34]`.
    - The whole foe cluster — `foeHp [750,321]`, `foeHpPips [750,330]`, `foeReticle [796,143]`,
      `foeAimTags [800,128]` — is all `TopLeft`-absolute, while `foeFigure` (the SAME foe) is correctly
      `BottomRight [-45,-217]`. So above 1080 the foe's figure sticks to the right edge but its HP bar and
      the TARGETING RETICLE slide toward center-left and detach from the foe. That reticle-off-the-enemy
      drift is baked into the data, not an engine glitch.
    - Top-right combat controls `autoAttackBtn [699,4]` / `retreatBtn [802,4]` / `equipmentBtn [867,4]`,
      and the action bar's minion column `minionGroup` / `bayGroupLabel` / `bayList [~785,387]` — same
      class: `TopLeft`-absolute where they belong to the right. `poolLegend [10,528]` belongs to the bottom.
    - Containment is also lost: `heroHp` / `heroHpLabel` / `heroHpPips` / `heroHpValue` are four separate
      `TopLeft` siblings all at ~`[69,32x]` instead of ONE anchored `heroHp` container with its labels
      positioned RELATIVE to it. Children don't ride their parent, so the stack shears apart as the frame
      grows. Same shape on `foeHp*` and `heroShield*`.

    **The fix (apply to every screen — `encounter` is worst, but they all carry it):**
    1. **Anchor every positioned element to the zone it belongs to.** Author an explicit `data-anchor`
       (one of the 9: TopLeft/Top/TopRight/Left/Center/Right/BottomLeft/Bottom/BottomRight) on EVERY
       element — never let the extractor default to TopLeft. The dc.html already encodes the intent via
       `right:` / `bottom:` / `translateX(-50%)` / nesting; make the extraction honor it (or annotate it)
       so a right-edge element emits `Right`/`TopRight`/`BottomRight` + a small EDGE-RELATIVE offset, not
       a ~900px top-left absolute.
    2. **Emit grouped children RELATIVE to their parent, not as stage-absolute siblings.** A panel + its
       labels, an HP readout + its pips, a card + its innards → author as real parent→child containment
       (§7 container/template, or §12 element `parts[]` with element-local rects). The parent anchors once;
       the children live inside it and reflow as ONE unit.
    3. **No element may depend on its incidental 1920×1080 pixel position.** After re-extraction, every
       element's position must be a pure function of (anchor, offset, parent box, screen size) — the §4
       determinism promise, actually honored.
    4. **Self-verify before shipping:** re-render each screen at a size OTHER than 1920×1080 — at minimum
       one larger (e.g. 2560×1440) and one off-aspect (e.g. 2560×1080) — and confirm zero drift / gaps /
       overlap. A screen that only lines up at 1920×1080 is not done.

    **Add this to your CLAUDE.md / dev-loop guide as a permanent invariant so it can't recur:**
    > **No absolute positioning (resolution-independence).** A screen element's position must be a pure
    > function of its anchor, its design-px offset, its parent box, and the screen size — NEVER its raw
    > pixel coordinate on the fixed 1920×1080 authoring stage. Every positioned element gets an explicit
    > `data-anchor` (one of the 9 anchors) OR is nested in a parent and positioned relative to it; no
    > element defaults its anchor or relies on where it "lands" at 1920×1080. Grouped elements (panel +
    > contents, readout + pips, card innards) are authored as true parent→child containment so the group
    > reflows as one unit — never as sibling stage-absolutes. Before shipping a screen, re-render it at a
    > non-1920×1080 size (≥1 larger + ≥1 off-aspect) and confirm zero drift/gaps/overlap. A layout that
    > only lines up at exactly 1920×1080 is a bug.

    We've also written this invariant into `design/LAYOUT_CONTRACT.md` §3 (the shared contract), so it's
    binding regardless. Engine note (OURS, not a CD ask): once the manifest anchors correctly, any drift
    that remains is our anchor/scale interpreter — we own that half; this item is the authoring/extraction
    half, which is where the baked-in TopLeft absolutes come from.

B12. **CLOSED 2026-07-04 — delivered + verified clean (744 files, no cross-product, no "plain",
    0 missing / 0 extra); see Confirm-to-close above. Convention is now canon in LAYOUT_CONTRACT §12a /
    DESIGN_SPEC §7a. Kept below for reference.** WORN-ARMOR PART SET — CONSOLIDATED CONVENTION (Doug).
    SUPERSEDES the
    earlier B12 (per-core theme) AND B2-GO item 3 (generic layers)** — they were one system described
    twice on a line-first path, which is how the type×core×race cross-product + a "plain" type slipped
    into your build. Build the ENTIRE worn-armor set from this one spec.

    **Sprite model = FULL PART SPRITES (Doug, 2026-07-04):** each file is a COMPLETE, ready-to-blit
    part image — the race's body part with that armor already drawn in. NOT a transparent overlay. The
    engine swaps the whole part sprite by (race, slot, wear-state); no runtime compositing.

    **Path convention (race-first):**
    ```
    sprites/gear/worn/<race>/<slot>/bare_<condition>.png                    // unarmored part — the fallback terminal
    sprites/gear/worn/<race>/<slot>/<type>_<tier>_<condition>.png           // GENERIC armored part (core-agnostic)
    sprites/gear/worn/<race>/<slot>/<core>/<type>_<tier>_<condition>.png    // THEMED — ONLY each core's favored line
    ```
    - `race` ∈ {human, elf} today — **author every slot for every race** (Doug: cook a part for each
      body part per race even where a current morph doesn't articulate it — future races may need it;
      this deliberately drops the earlier "arms/legs are identical, share one sprite" optimization).
    - `slot` ∈ {head, chest, arms, legs}. `bare`: all four, every race. GENERIC/THEMED armored slots
      follow the line: str & dex = all four; **int = chest + head only** (no arm/leg robe, §6c); CON
      shields are a hand item — NO worn body parts.
    - `type` ∈ {**str, dex, int**}. **There is NO "plain" type.** The unarmored part is `bare`. (The
      "plain" you generated came from DEX's display name "Plain leather" + the bare-body fallback — the
      token is `dex`, the unarmored art is `bare`.)
    - `tier` ∈ 1..4 (bare has no tier). `condition` ∈ {healthy, damaged, broken}.

    **THEMED = FAVORED LINE ONLY — not a cross-product.** A `<core>/<type>` file exists ONLY where
    `type` is that core's OWN favored line: Grunt→str, Warden→str, Adept→int, Summoner→int, Reaver→dex,
    Ranger→dex (DESIGN_SPEC §7a). A core wearing a NON-favored line (gear is swappable, §7) renders the
    GENERIC art for that line — no theme, core irrelevant to the render. **Do NOT author `<core>/<type>`
    for any type that isn't that core's favored line** (that was the mis-build).

    **Fallback chain (engine — so partial coverage is always safe):** themed (race/slot/core/type_tier_
    cond) → generic (race/slot/type_tier_cond) → generic same type, healthy → bare (race/slot/bare_cond)
    → bare healthy. Ship incrementally by race / tier / condition; nothing breaks between drops.

    **The six themes (Doug's identity calls, LOCKED — art execution is CD's):**
    - **Grunt** (str) — versatility: plain, practical, well-kept soldier's kit; the deliberate "middle
      of the road," nothing another theme couldn't have.
    - **Warden** (str) — block & armor: the heaviest-READING of the two str treatments — thicker
      plates, reinforced edges/rivets, a shield-boss motif echoed across pieces. "Built to not move."
    - **Adept** (int) — spellcasting: arcane runic trim/embroidery, a flowing silhouette, a restrained
      magical accent (don't overdo glow).
    - **Summoner** (int) — minions/binding: the SAME robe silhouette as Adept but darker/heavier — bone
      or chain trim, ritual sigils — so the two stay distinct while sharing the line.
    - **Reaver** (dex) — dual-wield: light, aggressive, agile cut; streamlined, less bulk, twin-blade
      etch motif.
    - **Ranger** (dex) — bow & pet: tracker/nature motifs — fur trim, quiver details, earthy palette —
      distinct from Reaver despite sharing leather.

    **Completeness target (2 races, the full near-term set):**
    - Bare: 2 races × 4 slots × 3 conditions = **24**
    - Generic armored: str 96 + dex 96 + int 48 = **240**
    - Themed (favored line per core): Grunt 96 + Warden 96 + Adept 48 + Summoner 48 + Reaver 96 +
      Ranger 96 = **480**
    - **≈ 744 total.** Multi-night — ship by race / tier / condition; the fallback chain covers any gap.

    **Out of scope / OUR side, not a CD blocker:** these parts are per (race, slot), core-agnostic
    except themed — so they do NOT carry the per-CORE body silhouette (Warden bulk etc.) the existing
    `sprites/body/{race}_{core}/…` figures have. How the worn-part set composes with per-core figure
    geometry is an OUR-side engine/morph question (§7/§17 #15), deferred — proceed on this art
    convention, flag contract ideas in your drop notes, don't block. No new body-shape variation here.
    Also fold in the original B2's **elf_ranger chest-accent neckline fix** (strap sits too high, reads
    as fused to the head) while regenerating elf chest parts.

    **Ship with:** updated figure/gear defs + asset inventory in `layout.json`, mgcb source updates (we
    mirror game-side), refreshed 00-assets sheets. Our SMOKE FIGURES + asset-exists probes verify
    completeness on landing — an inventory list in the drop notes helps us confirm fast.

## Standing FYIs (for context — not action items)
- **Tier ladders for the new families** (for card copy / labels): Sling Shepherd's → Braided →
  Sinew → Giantsbane · Staff Wooden → Twisted → Ornate → Humming · Charm Wooden → Bone → Ornate →
  Humming · Tome Old Worn → Leather → Ornate → Glowing. Wands/bows keep their existing ladders.
- **Tier-4 signature rule:** MAGIC gear's top-tier adjective is supernatural (Humming/Glowing);
  mundane gear's is not — keep the split in any generated copy.
- **Name lengths:** the "Dwarven Steel Short Sword" (24ch) class overflows current card name rects —
  Doug ACCEPTS overflow for now; final treatment is a parked Doug+Cowork decision. Don't
  unilaterally re-rect, but flag preferred options if you have them.
- The v6 stat blocks are ADOPTED canon (race bases Human 5/5/5/5 · Elf 4/6/4/4 · Dwarf 4/4/4/6 ·
  Halfling 4/4/6/4 · **Half-Giant 6/4/4/4**, + the per-core bonuses/layout numbers in B20.1 /
  `design/systems/{RACES,CORE_RUNES}.md`) — the 05 re-render (B20.3) samples from these.
- Core Effect roster was **REPLACED** (v6; folded into B20.1) — the old §11 names (incl. Called Shot)
  are retired; the engine is building the new effects' MECHANICS now (they're no longer display-only).
- Drops are applied via a stop/apply/re-arm handshake now — stage in `.drop/`, Cowork applies with
  the loop halted, guards run before the tree resumes.
