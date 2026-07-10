# Claude Design payload — CURRENT only (2026-07-05)

**‼ RECONCILED against CD_STATUS.md (pass 8/9, 2026-07-05 — CD's new living open-gap file replaces
DEV_LOOP_MEMORY, ships in every drop under `design/dchtml/`):** verified CD's landed claims against the
real tree, not the memo. CLOSED here: **B18 glyphs** (Flurry/Aimed Shot/Siphon/Barkskin/Sacrifice/Bind +
the Frenzy/Flurry two-badge split) all present in `icons/technique/`; **B21 authoring** (the `parent`
re-anchor — 160 `parent` keys now in `layout.json`). Both now have only ENGINE/OUR residual (dual-pool
draw; recursive parent-box resolve), tracked in STATUS, no longer CD asks. Also verified LANDED this
pass: **B13/B14** (both list-container sizing fixes — see Confirm-to-close). Still OPEN for CD: B18's
Parry/Steel/Suture + Iron Golem/Hound icons, B20 re-extraction, **B22 (merchant sale-card art, no
rush)**, **B26 (NEW — correct your `CD_STATUS.md` #34 armor-reservation text, Doug's ruling: pool
model, armor DOES reserve)**, and the rest of the B-series below.

**‼ RECONCILED against pass 10 (2026-07-06):** B19 (bay→minion rename) landed clean, confirmed via
`DROP_AUDIT.md` — Encounter's combat-minion template correctly diverged to `combatMinionCard` rather
than colliding with Equipment's existing `minionCard`. **Doug's call (2026-07-06): keep them separate —
no unify ask.** The two cards genuinely show different things (combat state vs. build-time slot); your
divergence was the right read, not a compromise. B19 fully CLOSED, nothing further needed. B7/B4/B10/
B24/B25/B23 all confirm-to-close per that same drop.

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
**B13 ✅ · B14 ✅ (2026-07-05, verified on the real tree) — both sizing fixes landed:** `waresShelves`
is now `size:[692,384]` (was 377, needed ≥378 for the 3rd wares row) and `buildMinions` is now
`size:[162,89]` (was 94, needed ≥131 to fit one `loadoutCard`). Both confirmed by direct read of the
current `layout.json`. Thank you — clear both from your dev memory.
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

B19. **✅ CLOSED 2026-07-06 — CANON RENAME (2026-07-05, Doug), landed pass 10, verified on the real
    tree.** "Bay(s)" retired as the minion-slot term everywhere; Encounter now binds singular
    (`minion.hotkey/state/name/cost/gateColor/description`, `loadout.minions`, `core.minionCap`,
    `preview.minionCap`, `minionsBox`) via template `combatMinionCard` (deliberately NOT `minionCard` —
    that name was already Equipment's own build-minion card with a different layout; reusing it would
    have collided). **Doug's call: keep the two templates separate, no unify ask** — they show genuinely
    different things (combat state vs. build-time slot). Nothing further needed. Clear from dev memory.

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

**2026-07-05 batch (Doug — Merchant Wares build kickoff):**

B22. **Merchant gear/technique/rune SALE cards — placeholder now, real art next batch.** We're building
    the buy/sell mechanic now (real gold cost, real stash wiring) on STUBBED presentation — reused/generic
    chrome, flagged as a placeholder per our own hygiene rule, not shipped silently as final. Ask for your
    next batch (no rush, this doesn't block our mechanic work): dedicated SALE card art/states for gear,
    technique, and rune wares distinct from the existing `wareCard` — whatever visual distinction you'd
    want between "this is in my inventory" (`invCard`) and "this is for sale" (a `wareCard` cousin) is
    yours to propose; we have no locked design opinion here yet beyond "don't reuse invCard's chrome
    unchanged forever." We'll keep using `wareCard` + generic labels as the flagged stopgap meanwhile.

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

B24. **`inventory.invItems` (Equipment/GEAR tab) is 1px too narrow for its own authored `cols:2`.**
    `layout.json`'s `invItems` list item authors `"flow":"grid","gap":6,"cols":2,"size":[199,44]` but
    the container itself (`invItems.size = [403,183]`) is exactly 1px short of what 2 columns need
    (`199*2+6 = 404`). Our `ListLayout.GridCapacity`/`.Cells` deliberately compute column count from
    the region's actual width rather than trusting the authored `cols` hint (a prior pass chose this
    on purpose, pinned in `ListLayoutTests.GridCapacityHonestlyReportsAOnePixelColumnShortfall` — we'd
    rather under-seat by design than silently clip a card past the container edge), so today this
    renders as a single column, 8 rows/page, which reads as "only 1 column renders" even though the
    manifest clearly intends 2. Ask: widen `invItems.size[0]` to `≥404` (410+ for a visible margin) —
    a pure geometry fix, no new binds/states needed. We'll pick up the extra column automatically
    once the width actually fits (no engine change required on our side).

B25. **`attrs.cells` (Equipment/Attributes panel pip strip) is 2px too narrow for a 6-capacity stat's
    pips.** `layout.json`'s `attrs.cells` list item authors `attrPip` at `size:[53,9]`, `gap:2`, inside a
    container `rect` of width `326`. A stat with capacity 6 (e.g. Human+Grunt STR: base 5 + Grunt's +1)
    needs `6×53 + 5×2 = 328`px to render all 6 pips — 2px short. Our `ListLayout.Cells` (horizontal flow)
    deliberately drops cells that would spill past the container edge rather than overlapping them (same
    "never render past the region" contract as B24, pinned for the general case in
    `ListLayoutTests` — a prior pass chose honest under-seating over silent clipping), so today the 6th
    pip — which happens to be the one FREE (unreserved) pip — silently disappears, and the bar reads as
    100% spent when 1 unit is actually still free. Ask: widen `attrs.cells`'s rect to `≥328` (330+ for a
    visible margin) — pure geometry, no new binds/states/engine hooks needed. We'll render the 6th pip
    automatically once the width fits.

B23. **Equipment tab buttons (`invTab`) too narrow for two of their three labels, and the row leaves
    ~290px of dead space.** `invTab` template is `size:[40,18]` with its label text drawn into a
    card-local rect `[12,5,16,8]` (16px wide) — sized around "GEAR" (4 chars). The `invTabs` list
    (`layout.json:6394`) authors its item `size` the same `[40,18]`, `gap:4`, so 3 tabs occupy only
    `3*40+2*4=128px` of the container's ~403px usable width (`size:[419,29]` minus pad) — the rest
    sits empty. "TECHNIQUES" (10 chars) and "MINIONS" (7 chars) don't fit the 16px label rect at the
    authored `fontPx:6`; our renderer (`TextPxWrapped`) already auto-shrinks a single-line label
    that overflows its rect rather than clipping or spilling into neighboring chrome (a small mercy —
    nothing breaks), but the shrink is severe enough that the longer labels read as tiny/hard-to-read
    next to "GEAR" at full size. No engine change would fix the readability or the dead space — both
    are authored geometry. Ask: widen `invTab`'s `size`/label rect (and the `invTabs` item `size`) so
    the 3 tabs actually fill their container width evenly — e.g. something near `(403-2*4)/3 ≈ 131px`
    each — and widen the label rect enough that "TECHNIQUES" doesn't need heavy shrinking at the
    authored font size. Pure `layout.json` geometry, no new binds/states/engine hooks needed.

B26. **CORRECTION — `CD_STATUS.md` #34 has the armor-reservation model backwards; please fix your own
    tracking entry.** #34 states the intended model as "armor is threshold-gated only (no pool pips);
    only weapons + the shield OBJECT reserve pool points." **That's not the design.** Doug's explicit
    ruling (2026-07-06): **"Armor consumes pool, eradicate incorrect design documentation in that
    regard."** The correct model — already locked in our `DESIGN_SPEC.md` SUSTAIN MODEL paragraph and
    already how `Body.cs` behaves today — is the **POOL model**: worn armor is a standing reservation
    against the shared per-stat pool, exactly like an active technique or a wielded weapon. A full plate
    kit CAN visibly crowd out a technique activation on the same stat; it is not merely a one-time equip
    threshold. No engine or DESIGN_SPEC change needed on our side (already correct) — this ask is just
    for your #34 entry to stop stating the opposite, so future asks/QA built off it don't drift.

B27. **`CD_STATUS.md` #33 double-check result: the "minion column collapses at 0 minion-cap, width
    scales with count" reflow does NOT exist — confirmed by live render, not just reading the
    schema.** Verified against both live-0-minion-cap cores (Adept, Reaver): `equipment` screen's
    `minionColumn` (`layout.json:7215`, `parent: "actionBar"`) is a fixed `size:[170,99]` panel —
    no conditional-visibility or data-driven-width field exists anywhere in the `Element`/`Item`
    schema, so there is nothing for our renderer to key off even if we wanted to collapse/scale it.
    At MinionCap 0 the column still renders full-width with its left border and a correct
    `"MINIONS - 0 / 0 slotted"` label, just with an empty list body (`buildMinions`, no cards) —
    confirmed via `RB_SMOKE`, no crash, no wrong data, just an always-full-width empty box. This is
    the SAME class of finding as B23/the tab-row dead-space ask: pure authored geometry, no missing
    engine hook. Ask: if you want the column to shrink at 0 cap or scale with `minions.slotLabel`'s
    live count, that needs either (a) a new conditional-width/hide-when-empty field on `Element`, or
    (b) us being told which cap tiers should map to which pixel widths so it can be threshold-authored
    per screen state — your call on which. Not blocking (data is correct either way); low urgency,
    cosmetic only.

B28. **`attrBar`'s alloc/available pair reads backwards vs. every other pool readout in the game
    (Doug playtest #13, "reserved/total order flipped").** `layout.json:10901` (`attrBar` template):
    the two number slots are laid out `attrs.alloc` (total capacity) at rect x=410, then a literal
    `"/"` glyph at x=418, then `attrs.available` (current free) at x=425 — i.e. **TOTAL / AVAILABLE**,
    left to right. Every other pool readout in the manifest (HP, Supplies, Charge, Summons, the
    Equipment/minion "slotted" counts) shows **current-value / max**, e.g. `"14/20"`, `"7/8"` — this
    is the one place that puts the max on the left. Doug reads the swapped order as "2/11" showing as
    "11/2." Confirmed this is pure element order/position, not a resolver bug: `Game1.ManifestRenderer.cs`
    binds `attrs.alloc`/`attrs.available` by MEANING already (see the comment at `:1641`, "that swap was
    a smaller earlier bug's half" — a real tuple-position mix-up already fixed once; swapping which
    VALUE each bind name returns now would reintroduce exactly that class of bug, just hidden one level
    deeper, and isn't ours to do per the manifest-is-CD-owned rule anyway). Ask: swap the two rects'
    contents in `attrBar` — `attrs.available` first (x≈410), then `/`, then `attrs.alloc` last
    (x≈425) — so it reads `available / alloc` like every other readout. No new binds/states/engine
    hooks needed, pure `layout.json` element reorder.

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
