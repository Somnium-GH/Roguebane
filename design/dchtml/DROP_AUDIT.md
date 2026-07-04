# DROP AUDIT — 2026-07-04 (payload B0 / B0b / B1 / B3 reconcile)

Per CLAUDE.md "Ship the design SOURCES in every drop": rides with this drop to `Somnium-GH/Roguebane`
and is regenerated per drop. Drop contents = `Roguebane.Content/` (from local `Content/`) +
`design/` (1920×1080 renders; `design/dchtml/` = all `*.dc.html` + `support.js` + `style_tokens.js`
+ `attribute-model.js` + `asset-manifest.js` + full `proto/` + this audit).

## a) What changed

**B0 — resourceStrip clipped SUMMONS on citymap + equipment (also applied to campaignmap +
merchant, which shared the same authored numbers and would have clipped the same way once a
4th resource showed).**
- Root cause: those four screens authored the strip container at a measured 197 design-px (from a
  3-resource capture) with content-width chips + 16px gap, while a live 4-resource run needs
  4×47 + 3×8 = 212 — 15px more than the box, so the engine clipped the last cell.
- Fixed by mirroring Encounter's already-proven fix (payload 2026-07-03 pm residual #5): every
  `resourceItem` chip is now a fixed 94 design-px (47 in layout.json, `flex:none`) and the
  container gap is a fixed 10 design-px (5 in layout.json) — region extents now equal stamped
  extents, so 4 chips always seat. Re-extracted; `resourceStrip` now reads size 203 / gap 5 /
  chip 47 on Equipment, CityMap, CampaignMap, and Merchant (matches Encounter's working numbers).

**B0b — equipment coreLabel bind.** Already landed in the previous drop (payload A5): `coreLabel`
binds its own `core.label` datum, distinct from `currentCoreName`'s `core.name`. No further
design-side change needed this drop beyond a stray stray `<br>` cleanup in the chip's sample markup
(cosmetic; no manifest effect).

**B1 — campaignmap/cityNode extraction loss.** Already root-caused, fixed, and guarded in the
previous drop (`extract_all.html` always runs the full screen set; `extract_merge.js` refuses a
merge that would drop a previously-present screen/template key). No regression this drop — the
guard passed cleanly on this run's re-extraction of all six screens.

**B3 — Equipment coreStats list.** Re-authored from a 2-col grid (sized for 4 cells, so a 3-item
list wrapped budget onto its own row) to a single-column vertical list — `coreStats` now reads
`flow:"vertical"`, container size `[66,34]` (3 rows), matching how the same budget/actions/bays
stat block reads elsewhere in the design.

**Binds added/removed:** none. This drop is a pure layout/list-config fix — no new display data,
no removed binds.

## b) Rebuilt from disk
- `Content.mgcb` ✅ (452 textures + 1 copy — `layout.json`, unchanged) · `asset-manifest.js` ✅ (452
  PNGs, unchanged). No assets added/removed this drop.
- Key-set diff guard (`extract_merge.js`) ran clean across the full re-extraction: no screen or
  template key was lost vs. the previous manifest.

## c) Reference renders
Re-rendered (changed only, all confirmed exactly **1920×1080**): `design/02-equipment.png`,
`design/03-citymap.png`, `design/04-campaignmap.png`, `design/07-merchant.png` (+ their
`reference/screens/*.png` twins). Encounter and NewGame untouched — no re-render.

## d) Manual design edits (Doug/user) — intentional, not generator output
- (carried from previous audit) 2026-07-03: removed the "1" / "2" step markers from NewGame's
  **Race** and **Core Rune** headings. No new manual edits since.

## FYI
- B2 (Elf Ranger chest-armor accent position) is intentionally HELD for the next figure-art batch —
  not addressed this drop; rides with the upcoming race × core rune × equipment permutation regen.
- Doug's other locks (Core Effect canon, stat blocks pending tuning session, merchant receiving,
  §13 aspect-fill) unchanged from the previous audit; no design action taken this drop.
