# DROP AUDIT — 2026-07-06 (pass 10: payload B geometry fixes + minion vocab rename + confirm-to-close)

**Pass 10 delta (payload B-series, SOURCE-DRIVEN per Doug "do it from dc.html" — no asset churn):**
All geometry was authored in the `.dc.html` sources and re-extracted through the real pipeline
(`proto/extract_all.html` → `extract_merge.js`); numbers below read off the merged `layout.json`.

- **B24 — Equipment GEAR tab now seats 2 columns.** `invItems` grid columns pinned `402px 402px`
  (was `1fr 1fr`, which rounded each card UP to exactly ½ the container and overshot by 1px → the
  engine's honest GridCapacity under-seated to 1 col). Emits `invItems.size [411,183]`, item `[201,44]`
  → `201×2+6 = 408 ≤ 411` (3px slack) → engine seats 2. Widened via the inventory content-wrapper
  padding `16→8`.
- **B25 — Attributes panel now renders the 6th (free) pip.** `attrs.cells` strip widened to design
  rect width **332** (was 326; needed ≥328) by trimming the `attrBar` row gap `11→8`, AND `attrPip`
  capped `max-width:106px` so a 6-pip sample extracts `pipW=53` (not 54) instead of flexing up with the
  container. `6×53+5×2 = 328 ≤ 332` → engine capacity **6**. High pools (e.g. Reaver DEX 10) still flex
  below the cap → no overflow (verified in `02-equipment-reaver.png`).
- **B23 — Inventory tabs fill the row; long labels stop shrinking.** `invTab` fixed `width:262px`
  (design 131) `text-align:center` (was `padding:8px 22px`, ~40px content-width leaving ~290px dead
  space). 3 tabs `3×131+2×4 = 401 ≤ 403` usable → all seat at full size; "TECHNIQUES"/"MINIONS" no
  longer force `TextPxWrapped` to shrink. (Fixed width, not `flex:1`, to dodge the same 1px round-up
  under-seat as B24.)
- **B19 — "bay" vocabulary eradicated; "minion" only.** Encounter + NewGame renamed (Equipment already
  carried no "bay" term). Encounter: template `minionBay`→**`combatMinionCard`**, element ids
  `bayGroupLabel`→`minionGroupLabel` / `bayList`→`minionList`, container bind `loadout.bays`→
  `loadout.minions`, item binds `bay.{icon,hotkey,state,name,cost,gateColor,description}`→`minion.*`.
  NewGame: `core.bays`→`core.minionCap`, `preview.bays`→`preview.minionCap`, part `baysBox`→`minionsBox`.
  Retired `templates.minionBay` deleted from `layout.json` (intentional-removal, so the key-set guard
  passed). **Verified: zero `bay.*` / `loadout.bays` / `core.bays` / `preview.bays` / `minionBay` /
  `bayList` / `bayGroupLabel` / `baysBox` remain anywhere in the manifest.**
  - ⚠ **DEVIATION from B19's literal `minionCard`:** Equipment ALREADY owns the global template name
    `minionCard` (its build minion card — no combat state chip), and template names are a flat global
    namespace in the extractor, so renaming Encounter's `minionBay`→`minionCard` would COLLIDE (the
    later-extracted Equipment card silently overwrites Encounter's, dropping its combat `state` chip).
    Encounter's combat card is therefore **`combatMinionCard`**; Equipment's stays `minionCard`
    (untouched). Flag if you'd rather unify the two into one template.
  - **FYI (not touched):** Equipment's minion item-binds are plural (`minions.*`); Encounter's are now
    singular (`minion.*`, matching its own `technique.*` item convention). B19 targeted only "bay"
    terms, so Equipment was left as-is; the plural/singular split is a minor future-consistency nit.
- **B7 — raceCard head no longer stretches (confirm-to-close).** The source was already corrected in a
  prior pass (one bound `<img height:70 width:auto>` with `data-shadow` + `race.headImage` +
  `sprites/body/{race.id}_grunt/head_healthy`, on its own square element, background panel a plain
  gradient). This pass RE-EXTRACTED so `layout.json`'s `raceCard` now carries the head imageBind on the
  **square rect `[10,22,35,35]`** (aspect 1.0, shadowed) — the portrait-aspect background panel is gone.
- **B4 — confirm-to-close, no change.** `nav.equipment` button already authored on Encounter (DISABLED
  in combat) and CampaignMap (both carry a "payload B4" comment). Nothing owed.
- **B10 — confirm-to-close, no change.** DEX/INT ladder names are already §6c-canon in
  `design/dchtml/gear_catalog.json` (Leather Cap→Hardened Cap→Studded Cap→Reinforced Hood; Cloth Cap→
  Silk Hood→Ornate Circlet→Humming Circlet; Padded→Leather→Studded→Reinforced Leather) and in
  `roster_gen.js`'s per-tier `names[]` arrays; `layout.json` carries no gear `name` fields. No
  "Leather Leather …" drift anywhere.

- **Changed files:** `Encounter.dc.html` (B19), `Equipment.dc.html` (B23/B24/B25), `NewGame.dc.html`
  (B19) + their `drop/design/dchtml/` mirrors; `Content/layout.json` re-extracted (all 6 screens,
  guard passed); `design/02-equipment-<core>.png` ×7 + `reference/screens/equipment-<core>.png` ×7
  re-stitched (all verified EXACTLY 1920×1080). `CD_STATUS.md` #31 removed (resolved). No removals of
  shipped assets.
- **NOT rebuilt (correctly):** `asset-manifest.js` + `Content.mgcb` — **no `Content/**/*.png` add or
  remove this pass** (only `Content/layout.json` changed in the package). No re-render of
  `01-encounter-*` (B19 is an attribute rename → zero pixel change) or `05-newgame` (rename-only +
  B7 already reflected in the current source render).
- **CD_STATUS #31 CLOSED (removed):** Equipment inventory card states (`invCard`/`loadoutCard`/`invTab`
  families) were previously hand-mirrored into `layout.json`; this pass they are genuinely produced by
  `proto/extract_all.html` → `extract_merge.js` (clean merge, guard passed), so the "re-extract to
  confirm the hand-patch" condition is met.
- **Engine follow-up (OURS, per B19):** the renderer's bind-key literals update in lockstep now this
  drop lands — new keys: `combatMinionCard`, `minion.{icon,hotkey,state,name,cost,gateColor,description}`,
  `loadout.minions`, `core.minionCap`, `preview.minionCap`, `minionsBox`.
- **Manual design edits (Doug/user) since pass 9:** none.

---

# DROP AUDIT — 2026-07-05 (pass 9: dual-attr Frenzy/Flurry + split glyph capture)

**Pass 9 delta (dual-attribute techniques, Doug — SOURCE ONLY, no asset churn):**
- **Frenzy + Flurry are now payable in STR *or* DEX.** `core-kits.js` technique defs gain
  `either: ['STR','DEX']` (order = STR-top / DEX-bottom). The `resolve()` technique pass picks the pool
  that can afford it (else the one with the most free room, for the lock shortfall), reserves there, and
  returns `payAttr` per technique. New shared helpers: `glyphFill(t)` (solid stat colour, OR a hard 50/50
  top/bottom split for `either` — NO black seam, Doug: it interfered with the glyph), `costSplit(t)`
  (two `{attr,cost,color}` rows), `costLabel` → `"STR/DEX N"`. Rendered on THREE surfaces: the technique
  glyph chip (split fill) + a two-row STR-red/DEX-green cost readout on the **Encounter** action-bar card,
  the **Equipment** loadout card, and the **Equipment** inventory badge (split box). Verified on the
  Reaver core (Finesse −1 → effective 2/2 Frenzy, 1/1 Flurry).
- **Split glyph PNGs re-captured** — `Content/icons/technique/{frenzy,flurry}.png` (+ drop copies)
  re-shot with the 50/50 STR-red/DEX-green split fill via the established capture pipeline
  (`proto/atom_capture.js` `RB_buildChipOverlay` gradient chips → `proto/atom_slice.js`
  `RB_buildTechChips` with new `RB_TECHS_SPLIT` + array-`glyphBg` split support; glyph SHAPE still lifted
  from the live-font capture, keyed against the darker half so both bg halves zero out). Same 120×120
  dims + same paths → **`asset-manifest.js`, `Content.mgcb`, `layout.json` UNCHANGED** (overwrite only, no
  new/removed entries). Now visible in `Asset Review.dc.html`.
- **NO screen-render churn.** Technique glyph PNGs are ENGINE-BLIT ONLY — the screens draw the design-font
  glyph — so no `design/0N-*.png` was re-shot for the glyph capture (ASSET_GEN_METHOD "engine-only asset
  needs no re-render"). The Reaver `design/01-encounter-reaver.png` / `02-equipment-reaver.png` are still
  behind the SOURCE by the live split treatment (glyph chip + two-row cost the SCREENS draw) — refresh
  those two when a screen-render pass runs; the glyph-PNG capture does not gate it.
- **Changed files:** `design/dchtml/{core-kits.js, Encounter.dc.html, Equipment.dc.html}` +
  `design/dchtml/proto/atom_slice.js` (split-chip generator) in the drop; `Content/icons/technique/
  {frenzy,flurry}.png` (+ drop copies) re-captured. ⚠ **`core-kits.js` was MISSING from the drop
  entirely** (both dchtml mirrors) despite the screens importing it — ADDED this pass so the drop is
  reproducible. No removals, no manifest/mgcb changes.
- **Engine follow-ups:** see **CD_STATUS #36** — dual-attr `either` cost still owes the `layout.json`
  manifest field + the runtime which-pool reserve decision + the two-row split-cost draw. The split glyph
  PNGs shipped this pass, so only the field + cost-draw remain. (Not restated here — CD_STATUS is canon.)
- **Drop reconciliation (surgical parity sweep, this pass):** the drop had drifted well beyond the
  session's own edits — brought fully back in sync WITHOUT a full re-stage: added the entirely-missing
  **`ui/` group** (32 assets: pips/buttons/frames) to `drop/Roguebane.Content/`; synced stale
  `layout.json` (root re-extraction, +5.5KB — the `parent`/anchor work never dropped); synced the whole
  **`proto/`** (26 files were missing incl. `asset_incremental.js`, `resolve_check.html`, `screen_perms.js`;
  9 stale generators); synced 6 stale dchtml sources (`CityMap, CampaignMap, NewGame, Merchant, Figure,
  style_tokens.js`) + added `Core Loadouts.dc.html` + `Inventory Tabs.dc.html`. Verified parity: Content
  3141/3141 (bg5+icons49+sprites3055+ui32), `Content.mgcb` + `ASSET_MANIFEST.md` + all 24 `design/*.png`
  IDENTICAL, all 17 dchtml sources in sync. (Empty `Canvas.dc.html` scaffold intentionally excluded.)
- **Artifact rename (going forward):** `CD_STATUS_MEMORY.md` → **`CD_STATUS.md`**; it now ships in every
  drop (`drop/design/dchtml/`) alongside this audit, and the two are deduplicated — CD_STATUS holds the
  canonical open-gap prose, the audit references `CD_STATUS #N`.
- **Manual design edits (Doug/user) since pass 8:** the dual-attr direction itself (STR-top/DEX-bottom,
  "let the colours meet — drop the black seam, it interferes with glyphs").

---

# DROP AUDIT — 2026-07-05 (pass 8: v6 technique glyphs captured — incremental)

**Pass 8 delta (technique-glyph capture, incremental fast path):**
- **6 v6 technique glyphs captured** — `icons/technique/{siphon, sacrifice, barkskin, flurry, aimed_shot,
  bind}` (120×120 chips, glyph shape from the design-font render, glyphBg = core-kits `T` attr colour;
  siphon/barkskin INT, flurry/aimed_shot DEX, bind STR, sacrifice minion-cost grey). Closes CD_STATUS
  #34.4 / pass-7 "B18 glyph capture (incl. Bind)" for the full v6 roster. Reproducible: added to
  `proto/atom_slice.js` `RB_TECHS_V6`; captured via one overlay/screenshot batch + one `run_script`.
- **Incremental, no waste (the point of this pass):** technique glyphs are ENGINE-BLIT ONLY — the
  screens draw the design-font glyph — so **no `design/0N-*.png` render changed and none was re-shot.**
  The manifest+mgcb were APPENDED (`proto/asset_incremental.js` `RB_addAssets`, not the ~3000-file disk
  walk): **3135 → 3141 PNGs.** Chips + both manifest files were written straight into `drop/` in the
  same pass — no drop re-stage. New tooling this pass: `proto/asset_incremental.js`, `atom_slice.js`
  `RB_TECHS_V6` + `outDirs` arg; instructions in ASSET_GEN_METHOD.md "Incremental adds" + CLAUDE.md.
- **Changed files (adds only):** `Content/icons/technique/{siphon,sacrifice,barkskin,flurry,aimed_shot,
  bind}.png` (+ drop copies); `asset-manifest.js` + `Content/Content.mgcb` (+ drop copies) — 6 new
  entries. Binds unchanged (the screens already `data-image-bind` `icons/technique/{id}`; the 404s those
  6 ids threw now resolve). No removals. No manual design edits this pass.

---

# DROP AUDIT — 2026-07-05 (pass 7: Barbarian core + Half-Giant race, on top of pass 6)

**Pass 7 delta (Barbarian core + Half-Giant race, Doug 2026-07-05):**
- **Barbarian — new 7th core (B15/B16/B18).** Rune `core_barbarian` (glyph ⚒). Core Effect **Warlord's
  Might** — "Two-handed swords cost 2 less strength to equip" (resolver applies −2 to claymore equip).
  Stat bonus **+4 STR · +1 CON**. v6 kit: Iron Claymore (2H) + Iron plate ×4 · Cleave, **Bind** (new STR
  shield source — B18 icon PENDING capture, renders design-font glyph meanwhile), Bandage · **3 actions
  / 1 minion / 14 budget** (Doug). Favored worn line = STR → new **Barbarian STR worn theme** authored
  across all 5 races (savage hide-strap + tusk/fur accents). Figure = light-medium-brown hide with a
  gold-buckled lace + pale fur collar (Doug: "not the green"). Added to Encounter/Equipment `core` enum
  + NewGame grid (now 7 cores → 3 pages of 3 with the pager).
- **Half-Giant — new 5th race (B17).** STR affinity **6/4/4/4**, HP 18. First TALL body-morph: +3 torso
  rows, longer legs/arms, slightly wider + bigger head — reads clearly taller than the other races while
  **tuned to fit the fixed paper-doll frames** (native 408 vs human 360, ~13%; Doug tuned down twice so
  it doesn't overflow). Full figure/part/worn set for all 7 cores; worn race set now
  {human, elf, dwarf, halfling, half_giant}. Added to the `race` selectors.
- **"BAYS" → "MINIONS"** everywhere user-facing (NewGame core-card + Loadout labels, Equipment identity
  stat) per Doug; element ids/binds unchanged.
- **NewGame core cards** enlarged + given a **STARTING KIT** panel (weapons · armor line · skills ·
  minion from the v6 kit) so the taller card fills down to the pager; Effect + kit fonts enlarged.
- **Counts:** roster now **41 figures** (5 races × 7 cores + 6 foes); **3135 PNGs** (asset-manifest +
  Content.mgcb rebuilt from disk). layout.json re-extracted; 00-assets-1 figures sheet + roster_mockup
  (full 35 race×core grid) rebuilt.
- **Renders refreshed this pass:** `design/05-newgame.png`; `design/01-encounter-{barbarian,summoner,
  ranger}.png`; `design/02-equipment-{barbarian,summoner,ranger}.png` (the new core + the two cores whose
  minion now blits its real per-type deploy sprite). The grunt/warden/adept/reaver enc+eq renders from
  pass 6 remain accurate (no minion, core-only stat chips unchanged) — not re-shot.
- **Still OUR side:** B18 technique GLYPH capture (now also **Bind**); half-giant worn↔figure morph
  composition (§7/§17 #15). Everything else below is the pass-6 record.

---

# DROP AUDIT — 2026-07-05 (pass 6: two new races + v6 balance + race selectors + body-morph)

Rides with this drop to `Somnium-GH/Roguebane` (regenerated per drop). Drop contents =
`Roguebane.Content/` (from local `Content/`, repo-name mapping applied) + `design/` (1920×1080 renders
+ `design/00-assets-*` sheets; `design/dchtml/` = all `*.dc.html` + `support.js` + `style_tokens.js` +
`attribute-model.js` + **`core-kits.js`** + `asset-manifest.js` + `gear_catalog.json` + the full
`proto/` + this audit).

**Headline:** the race roster grew **2 → 4** (Dwarf + Halfling, B17) with the first real **body-morph**
(dwarf stout+short, halfling small+swift, both with wider/shorter or shorter heads); Encounter +
Equipment gained a **race selector** alongside the core selector; and every attribute / loadout /
technique / minion across NewGame, Equipment and Encounter was **resynced to the v6 balance sheet**
(the number gospel), driven by a rewritten `core-kits.js`.

## ‼ STATUS — PROTOTYPE, tuned to v6 (not silent drift)
Core Effects are the payload-B15 PROTOTYPE roster (Jack of All Trades / Fortified / Resonance /
Conscription / Finesse / Fletcher's Luck), superseding §11 canon. All numbers are the **v6 balance
sheet** (2026-07-05 session): race bases, per-core stat bonuses, reserve costs, default loadouts.

## ‼ REMOVED this pass (the delete script — a file drop can't express deletions)
1. **No assets removed.** Only ADDS (2 new races' figures/parts/worn, minion deploy sprites) + edits.
   The repo's existing `human_*`/`elf_*` figure + worn trees are unchanged in shape; the `dwarf_*` /
   `halfling_*` trees are new.
2. If the repo still carries a `Content/sprites/minion/` (singular) directory, note the deploy sprites
   ship under `sprites/minions/` (plural) — the game-side mirror should use the plural path.

## a) What changed THIS pass
- **`core-kits.js` — REWRITTEN to the v6 sheet + made race-aware.** Now the ONE source for NewGame too
  (not just Encounter/Equipment). Adds: `RACES` (Human 5/5/5/5 · Elf 4/6/4/4 · Dwarf 4/4/4/6 ·
  Halfling 4/4/6/4, v6 §B) + `raceHp` (10 + 2×CON); `CORE_BONUS` + `pools(core,race)` +
  `statBonus`/`statBonusFull` (payload B16 per-core stat bonuses); v6 **effect discounts** in the
  resolver (`gearGate`/`techCost`/`bayCost`: Grunt −1 all, Warden plate→CON −1/tier, Reaver two-weapon
  −1, Ranger bow −1/tier); the v6 default kits + demonstration scenarios; and `resolve(core, race)` /
  `poolRows(…, race)` / `inventory(…, race)` all threaded with race. **ARMOR NOW RESERVES POOL PIPS**
  (v6 §C "Requirement = armor + weapons + skills + minions") — this closes the pass-5 DROP_AUDIT Doug
  call in favor of the summed-pool reading.
- **Two new races (B17) — full figure/part/worn batch**, generated by `proto/roster_gen.js` (the
  persistent generator; golden rule). New morph axes on the `RACE` table: `short` (leg/robe-collar),
  `armD` (arm length), `headW`/`headH` (head box) — the first true body morph. Dwarf: stout (wider
  torso), short legs, head +2 wider / −1 shorter, ruddy skin + ginger hair. Halfling: elf-slim build,
  small ear points, head −1/−2, WARM skin (explicitly not elf pallor) + chestnut hair. Emitted for all
  6 cores each → `sprites/body/{dwarf,halfling}_<core>/` (parts + damage states) and the full B12
  worn-armor set → `sprites/gear/worn/{dwarf,halfling}/…` (**worn set now 1488 files across 4 races**).
- **Race selector on Encounter + Equipment** — new `race` enum prop (human/elf/dwarf/halfling) beside
  the existing `core` prop; pools, HP, figure, identity label all reflect it. NewGame's race column now
  reads its 4 races + all data from `core-kits.js` (no more local duplicated tables) and every race can
  bear every core (v6 clearance; the old Elf↛Warden hard-disallow retired).
- **HELD indicator on the Equipment demo** (Doug) — same `heldBadge` idiom as Encounter, gated on
  `combat.paused`: Equipment can be opened during combat (combat holds; loadout read-only in combat).
- **Per-core STAT-BONUS boxes (B16)** — NewGame core cards show the core's additive bonus in the EXACT
  race-card attr-box idiom (colored square / big value / little label), one box per non-zero stat
  (Grunt shows all four +1s, singles centered). Equipment identity block shows the same via compact
  chips. Loadout preview folds the bonus into the effective attr tiles (no separate badges — Doug).
- **NewGame core grid → 3-per-page pager** (Doug) — bigger single-row cards (152×324 design-space) with
  a Merchant/Equipment-style `corePager` (`button_pager.png`), 6 cores across 2 pages.
- **Per-type minion DEPLOY sprites** — `sprites/minions/{skeleton,golem,hound,imp,wisp}.png`; the
  Encounter field minion + the Encounter/Equipment bay icons now blit the real per-type sprite
  (closes the old skeleton stand-in). (B18 technique GLYPH icons are still an ASSET_GEN capture pass —
  see FYI below; the action bar renders the design-font glyph meanwhile, unchanged.)
- **`Figure.dc.html`** — figure enum extended with the new race grunts (no logic change; it already
  reads `layout.json` generically).
- **Merchant `waresShelves`** bumped to fit its 3rd row (`min-height:768px` → extractor size[1]=384,
  closing payload B13); **Equipment `buildMinions`** now sizes to the bay count (B14 closed —
  layout.json size [162,89], item [78,89]).

## b) Binds added/removed
- New engine display data the manifest now carries: `core.statBonus` (list template `statBonusChip`
  on Equipment, `statBonusBox` on NewGame), `cores.page*` (NewGame `corePager` cluster mirroring the
  Merchant/Equipment pagers), `combat.paused` on the Equipment `heldBadge`, and the `race` selector is
  a screen prop (no new bind — the engine supplies the chosen race like it does core). Encounter/
  Equipment figure + minion image binds now resolve per race/type.
- `Content/layout.json` **re-extracted** (`proto/extract_all.html` → `extract_merge.js`) so
  `screens.encounter` / `.equipment` / `.newgame` + all templates reflect every edit above — NOT a
  hand-patch. Key-set diff guard passed (no screen/template lost). Figures/gear/worn sections
  regenerated by `roster_save.js` (4 races, mounts reconciled into `roster_gen.js` so a regenerate no
  longer reverts them — closes CD_STATUS #34.3).

## c) Rebuilt from disk
- `asset-manifest.js` + `Content.mgcb` rebuilt from disk — **2298 PNGs** (was ~1309; +~989 from the two
  new races' body parts + worn set + minion sprites). Both walkers were parallelized (the serial walk
  outgrew the 30s script budget at this asset count).
- **Screen renders refreshed (13 total, all verified 1920×1080):** `design/01-encounter-<core>.png` ×6,
  `design/02-equipment-<core>.png` ×6, `design/05-newgame.png`. `design/00-assets-1-figures.png` +
  `proto/roster_mockup.png` rebuilt with a race-morph comparison row + the full 24 race×core grid.
  (03-citymap / 04-campaignmap / 06-style / 07-merchant / 08-reticle unchanged this pass.)

## d) Manual design edits / user-accepted decisions (Doug/user) — intentional, not generator output
- **Doug: race art calls** — Dwarf shorter + wider/shorter head "to rip the bandaid off on body morph";
  Halfling elf-like with a shorter head "but not awkwardly and without the pale color."
- **Doug: HELD on Equipment** — "it's actually read-only technically" during combat; badge only.
- **Doug: stat-bonus display** — boxes consistent with the race attr boxes; Grunt shows all 4 (not
  summarized); center the ones with fewer; Loadout just adds it up (no separate badges); the race+core
  aggregated "+2 ALL" chip row was tried and REVERTED (identity block shows core-only; pools add it up).
- **Doug: core cards** — bigger, 3 per page, paged like the other pagers.
- **v6 loadout accuracy (Doug's balance session is gospel):** Ranger's melee is now an **Iron Dagger**
  (v6), not the short sword; Summoner's Barkskin (T1 INT shield) + Sacrifice; Adept's Siphon +
  Stoneskin; Bandage/Frenzy/Flurry/Aimed Shot costs are the v6 sheet numbers.

## FYI / open on our side
- **B18 technique/minion GLYPH icons** — the minion DEPLOY sprites shipped; the technique glyph PNGs
  (frenzy/flurry/aimed_shot/siphon/barkskin/sacrifice) are still an ASSET_GEN capture pass (CD_STATUS
  #34.4), not in this drop. Action bar renders the design-font glyph meanwhile — no 404 regression, the
  imageBind just falls through until the capture lands.
- **Body-MORPH composition (§7/§17 #15)** — the worn-armor parts are per (race, slot), core-agnostic
  (except themed); how they compose with per-core figure geometry + the new race build deltas is still
  the engine/morph question. The art convention proceeds; flagged, not blocking.
- **Ranger DEX utilization (v6 §E):** Human Ranger's full kit needs DEX 10 but even a healthy Human
  pool is 9 — so the heavy Aimed Shot lapses at full activation (shown as utilization in the Encounter
  render). Halfling Ranger fits it exactly. This is the v6 mono-attribute-scaling flag surfacing in UI,
  not a bug.
