# DROP AUDIT — 2026-07-04 (pass 3: B12 CORRECTED worn-armor convention; supersedes pass 2 same day)

Per CLAUDE.md "Ship the design SOURCES in every drop": rides with this drop to `Somnium-GH/Roguebane`
and is regenerated per drop. Drop contents = `Roguebane.Content/` (from local `Content/`) +
`design/` (1920×1080 renders + `design/00-assets-*` sheets; `design/dchtml/` = all `*.dc.html` +
`support.js` + `style_tokens.js` + `attribute-model.js` + `asset-manifest.js` + `gear_catalog.json`
+ the full `proto/` + this audit).

**Confirm-to-close (engine verified LANDED per the 2026-07-04 payload): B0, B0b, B1a, B3, B5, B6.**
Still-open payload items covered by this drop: B1b (guards live in `extract_merge.js` +
`roster_save.js` + this audit's key-set diff) · B2-GO (weapons/families/CON ladder/armor icons/figure
regen incl. elf_ranger neckline — pass 1; worn armor now per corrected B12) · B4 · B7 · B8 (states
authored; glow design now LOCKED, see FYI) · B10 · B11 · **B12 corrected (this pass)**.

## ‼ REMOVED this pass (the delete script — a file drop can't express deletions)
The 2026-07-04-morning worn-armor build was MIS-BUILT (type×core×race cross-product + a "plain"
type) and is fully retracted. It never reached the repo — if any earlier same-day staging WAS
applied, delete:
1. **Every file matching `Roguebane.Content/sprites/body/<fig>/<part>_(str|dex|int)_*.png`** across
   the 12 player figure dirs (human_/elf_ × grunt/warden/adept/summoner/reaver/ranger) — 1344 files
   (112 per standard figure, 48 per robe figure). Base parts are untouched; correct post-delete dir
   counts: grunt 33 · warden/reaver/ranger 21 · adept/summoner 12 (per race).
2. **`figures.*.armor` blocks in `layout.json`** (replaced by the top-level `worn` block).
`Content.mgcb` + `asset-manifest.js` in THIS drop are rebuilt from disk and reference none of the
removed files (1308 textures — see (c)); applying this drop's mgcb + the deletes together is safe.

## a) What changed THIS pass — B12 corrected worn-armor part set (744 files)
Built from the single consolidated spec (supersedes pass-2's per-figure themed layers AND B2-GO item
3's generic layers):
- **FULL PART SPRITES**, race-first: `sprites/gear/worn/<race>/<slot>/bare_<condition>.png` ·
  `…/<slot>/<type>_<tier>_<condition>.png` (generic, core-agnostic) ·
  `…/<slot>/<core>/<type>_<tier>_<condition>.png` (themed — ONLY the core's favored line).
- `race` ∈ human/elf, every slot per race (no shared arms/legs); `slot` ∈ head/chest/arms/legs (ONE
  sprite per slot — engine reuses for both arms/legs); `type` ∈ str/dex/int — **no "plain" type, the
  unarmored terminal is `bare`**; `tier` ∈ 1..4 numeric (palettes: str Iron→Steel→Mithral→Dwarven ·
  dex leather ladder · int cloth ladder); `condition` ∈ healthy/damaged/broken, no disabled variants
  (§6e). int = chest+head only (§6c); CON shields = hand items, no worn parts.
- **Themes (favored line only, never a cross-product):** Grunt→str practical kit · Warden→str
  reinforced edges/rivet rows/gold shield-boss/full-face visor helm · Adept→int teal runic hem +
  restrained sigil · Summoner→int bone/chain trim + dark band + deeper hood · Reaver→dex twin-blade
  X etch + studs · Ranger→dex fur(tusk) trim + quiver strap. Non-favored lines render GENERIC art.
- **Counts (complete near-term set, both races):** bare 24 + generic 240 (str 96 / dex 96 / int 48)
  + themed 480 (96/96/48/48/96/96) = **744** — matches the payload's completeness target exactly.
- **Inventory for your asset-exists probes:** `layout.json` top-level **`worn`** block
  (root/races/slots-per-type/tiers/conditions/bare/themes/sprite-template/fallback) — expanding it
  enumerates all 744 paths. Fallback chain (engine): themed → generic → generic healthy → bare →
  bare healthy, so partial coverage in future drops is always safe.
- **Geometry:** race BASE body plan (human baseline ± race build: elf slim/ears/skin), deliberately
  core-agnostic; composition with per-core figure geometry stays the engine's §7/§17 #15 morph
  question (proceeding on the art convention as instructed — no new body-shape variation authored).
- elf_ranger neckline: the figure-part fix landed pass 1; the themed ranger chest parts carry the
  strap at mid-chest (never the neckline).
- Generator: `proto/roster_gen.js` worn module rewritten to this spec (`buildWornSet`,
  `wornGeom`/`wornSlotGrid`/`bareSlotGrid`, `WORN_TYPES`/`WORN_SLOTS`/`FAVORED_LINE`);
  `proto/roster_save.js` gained the `worn` output + `layout.worn` merge; player figure base parts
  re-emitted byte-identical after the purge (240 files).

Pass-1/pass-2 items ride along unchanged: weapons ×4 metals, new families, CON shield ladder, armor
card icons, B10 canon catalog names, B11 bows, back-mount art (`bow_<tier>_back`/`sling_<tier>_back`
→ `sockets.back`), B4 Equipment buttons, B7 raceCard bind, B8a beaconNode states, B1b guards.

## b) Binds added/removed
None. `worn` is data/inventory, not a bind; screens/templates/style byte-identical since pass 1.

## c) Rebuilt from disk + reference renders
- `Content.mgcb` ✅ **1308 textures** (= pre-worn 552 + 12 bows/back-mounts + 744 corrected worn;
  pass-2's mis-built 1344 are gone) + 2 copies (`layout.json`,
  `gear_catalog.json`). `asset-manifest.js` ✅ 1308 PNGs. Both reference zero removed files.
- Key-set diff (B1b guard): figures 18/18 kept (armor sub-keys dropped by design — listed in
  Removed), gear 119/119 kept, screens/templates untouched, `worn` added.
- Screen renders `design/01..08` UNCHANGED (no screen source touched). Sheets:
  `00-assets-4-armor.png` regenerated to the corrected set (bare/generic/themed/elf/conditions);
  1-figures + 2-parts unchanged from pass 1 (2-parts lists base parts only — base sprites are
  byte-identical).

## d) Manual design edits (Doug/user) — intentional, not generator output
- (carried) 2026-07-03: removed the "1" / "2" step markers from NewGame's **Race** and **Core Rune**
  headings. No new manual edits since.

## FYI / open on our side
- **B8 glow LOCKED (Doug 2026-07-04, "yes it's that glow"):** build ONE fixed-tick pulse primitive;
  `glow` = that pulse on an outer amber ring/shadow (not the border colour), reference feel =
  CampaignMap's `rb-cglow` ~1.8s ease-in-out breathing. Wire `cityNode` + `beaconNode` `current` to
  it; `actionCard.targeting`'s `pulse` keys off the same primitive. DEV_LOOP_MEMORY #30 tracks the
  engine build.
- Worn-part + back-mount DRAW code is engine-side new work (DEV_LOOP_MEMORY #32): part-sprite
  selection by (race, slot, wear-state) + fallback chain, back-mount behind `legL`.
- Name-length overflow ("Dwarven Steel Short Sword") unchanged — accepted per Doug, treatment parked.
