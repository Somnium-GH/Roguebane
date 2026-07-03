# DROP AUDIT — 2026-07-03 (pm reconcile + ADDENDUM A1–A4 response)

Per CLAUDE.md "Ship the design SOURCES in every drop": rides with this drop to `Somnium-GH/Roguebane`
and is regenerated per drop. Drop contents = `Roguebane.Content/` (from local `Content/`) +
`design/` (1920×1080 renders; `design/dchtml/` = all `*.dc.html` + `support.js` + `style_tokens.js`
+ `attribute-model.js` + `asset-manifest.js` + full `proto/` + this audit).

Answers to your reconcile residuals #1–#3: (1) yes — fold LAYOUT_CONTRACT/DESIGN_SPEC updates from
this audit, no separate doc drop is coming; (2) `reference/screens/*` is CONFIRMED CD-INTERNAL
(regression baseline) — `design/*.png` stay the repo canon, they are not in this drop; (3)
`target_tag.png` is absent from this drop's content + mgcb — repo-side deletion closes it.

## a) What changed

**Screens (.dc.html → layout.json `screens.*`/`templates`)**
- `encounter` — residual #8: `heroShieldCount` (binds `ShieldPool.count`, sample "6 / 9") and
  `heroShieldRegenFill` (binds `ShieldPool.regenPct`, teal fill) are now extracted elements.
  Residual #5: resource chips are UNIFORM 94px-wide items (`flex:none` strip, gap 16→10) so the
  region extents (203 design px) exactly equal 4 stamped chips at the template width — SUMMONS
  always seats; nothing clips.
- `citymap` — residual #6: gauge internals are real elements now: `suppliesTitle`/`suppliesCount`
  (binds `supplies.count`)/`suppliesNote` + `supportTitle`/`supportCount` (binds `support.count`)/
  `supportNote` (serif title, right-aligned mono count, mono caption — draw per element, retire the
  one-text-run stopgap header). Doom hazard stripes: `doomFill` (bind + leading-edge border + bevel
  shadows) now hosts a `doomFillStripes` child carrying `imageBind: ui/pattern/doom_stripe` — TILE
  the PNG across the fill rect (transparent stripe half shows the trough, like the CSS alpha stripe).
- `newgame` — ADDENDUM: **A1** `core.badge` is bound on the coreCard badge part (sample "STARTER";
  add the display datum engine-side for BULWARK/CASTER/SPECIALIST). **A2** per-state LABELS ship in
  `states`: core select chip `{selected:"✓ CORE SET", idle:"SELECT", locked:"LOCKED"}`, race chip
  `{chosen:"✓ CHOSEN", idle:"CHOOSE"}` — draw the state's label, not the sample. **A3** value/label
  are TWO NAMED PARTS with real fonts/px/margins: `previewBudgetTile`/`previewActionsTile`/
  `previewBaysTile`/`previewHpTile` each emit `parts:[{part:"value",…,binds},{part:"label",…,content}]`
  (element-level `parts` is a NEW schema field; when parts carry the text the element-level sample is
  dropped — parts are the text, don't double-draw). coreCard/raceCard chrome boxes are named
  (`budgetBox`/`actionsBox`/`baysBox`, `attrBox`/`hpBox`) so `tools/drop_audit.py` can track
  span-level fidelity. **A4** `previewStage` is its own element (panel, slot→ground vertical
  gradient fill + border) — the purple night panel behind the loadout figure now extracts.
- ALL screens — extractor fixes: residual #7 skinned-button labels now extract with the LABEL SPAN's
  styling (longest-text descendant): autoAttackBtn/retreatBtn/closeBtn/leaveBtn/beginBtn carry
  mono / 7 design-px / ground-dark instead of `display, fontPx 8, color ink`.

**New/changed manifest schema fields (LAYOUT_CONTRACT updates to fold in)**
- `element.parts[]` — named sub-parts on a non-container element: `{part, rect (element-relative,
  design px), font/fontPx/color/align/fill/border, sample|content, binds}`.
- `part` (string) on template sub-parts — a non-empty `data-part` names the part.
- `states.<state>.label` — per-state label text for state-driven chips (A2).
- `imageBind` with a STATIC `ui/pattern/*` path = tile the PNG across the element rect.

**Binds added** — `supplies.count`, `support.count` (citymap gauge headers, "n / m" strings),
`ShieldPool.count`, `ShieldPool.regenPct` (now reach layout.json). No binds removed.

**Assets**
- Added: `icons/technique/{bandage,block,cleave,drain,ember,jab,lunge,stoneskin}` (residual #4 —
  same deterministic chip pipeline, attr-coloured, glyph family consistent; generator:
  `proto/atom_slice.js` `RB_TECHS_SYNTH` + `RB_buildChipOverlay`, capture in ≤5-chip batches).
- Added: `ui/pattern/doom_stripe.png` — 26×26 seamless -45° hazard tile (stripe A opaque blood,
  stripe B alpha-ramp); NEW persistent generator `proto/pattern_gen.js`.
- Removed: none. (`ui/reticle/target_tag.png` stays deleted — not re-shipped.)

## b) Rebuilt from disk
- `Content.mgcb` ✅ (452 textures + layout.json copy) · `asset-manifest.js` ✅ (452 PNGs)
- `Content/ASSET_MANIFEST.md`, `UI_ASSET_MAP.md`, `DEV_LOOP_MEMORY.md` updated.

## c) Reference renders
- Re-rendered (changed screens only): `design/01-encounter.png`, `design/03-citymap.png`,
  `design/05-newgame.png` — all exactly **1920×1080** (pipeline: `proto/screen_capture_prep.js` +
  frame shim + `proto/ref_stitch.js`). 00/02/04/06/07/08 untouched.

## d) Manual design edits (Doug/user) — intentional, not generator output
- 2026-07-03: removed the "1" / "2" step markers from NewGame's **Race** and **Core Rune** headings
  (headers now read just "Race" / "Core Rune"; already reflected in layout.json + design/05).

## New CD-side tooling (in `proto/`, for reproducibility)
- `proto/pattern_gen.js` — tileable-pattern generator (doom_stripe).
- `proto/extract_merge.js` + PNG data channel in `proto/extract_all.html` — lossless extract→merge
  transport (the chunked console.log path is lossy for agent-side reads; kept for human inspection).
