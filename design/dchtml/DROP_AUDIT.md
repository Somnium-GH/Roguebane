# DROP AUDIT — 2026-07-03 (payload #11–#18 + review pass)

Per CLAUDE.md "Ship the design SOURCES in every drop": this file rides along with the next PR to
`Somnium-GH/Roguebane` and is regenerated per drop. Drop contents = `Content/` (→ `Roguebane.Content/`),
all `*.dc.html` + `support.js` + `style_tokens.js` + `attribute-model.js` + `asset-manifest.js`, the
full `proto/` scripts, `design/*.png`, `reference/screens/*.png`.

## a) What changed

**Screens (.dc.html → layout.json `screens.*`/`templates`)**
- `encounter` — targeting redesign (§8 LOCKED): card aim tag REMOVED, hotkey chips 1–6 on all
  action-bar cards, foe-side aim-tag stack reads HOTKEY NUMBERS (`templates.aimTag`), FOCUS reticle
  pulses via authored `frames`, state chips de-glyphed (`TARGETING`/`LOCKED`), "charging · 0.5s";
  HP strips are templates (`heroHpPip`/`foeHpPip`); pool pips are a nested template (`poolPip`,
  ui/pip imageBinds); `sceneLabel` sample is node TYPE only ("SKIRMISH" — naming model VETOED).
- `equipment` — `backdrop` scene element added; `closeBtn` (✕ CLOSE) top-right; coreLabel/marchState
  moved beside the logo; attr pips are a nested template (`attrPip`); loadout summary shows ONLY real
  Core values (budget/actions/bays/base hp — gear/arms/base rows deleted); technique icons imageBind.
- `citymap` — `backdrop` scene element; `doomTitle` own element; `doomEta` carries `enemy.advance`
  (bind dropped from `doomBar`); supplies/support gauges are pip templates (`supplyPip`/`supportPip`);
  `youAreHere` homed; **four ex-overlay pieces homed**: `campaignStrip` (+label, +`campaignTaken`),
  `packChips` (+label), `equipmentBtn`, `castlePanel`+`fortRows` (old `castleNote` retired).
- `merchant` — heal pips template (`healPip`); pager arrows / LEAVE ship literal labels; wareCard
  chrome + real extents.
- `newgame` — beginBtn ships "BEGIN THE RUN ▶" as content; card/tile chrome parts captured.
- ALL screens — z is now a derived PAINT ORDINAL (one convention, back→front; find the scene by its
  `*.scene` bind, not z==0); list containers emit `item.pad`.

**Binds added/removed**
- Added: `technique.hotkey`, `bay.hotkey`, `tag.hotkey` (aimTag), `targeting.tags`, `Body.hp.points`
  (+`point.live` states) on encounter/merchant HP strips, `Body.hpLabel`, `encounter.foe.hp.points`/
  `hpLabel`, `pool.attr.cells`/`cell.state` (+`ui/pip/{cell.asset}` imageBind), `attrs.cells`,
  `supplies.points`/`support.points` (+`ui/pip/{point.asset}`), `equipment.scene`, `citymap.scene`,
  `map.current`, `campaign.cities`/`city.status`, `campaign.taken` (citymap), `Body.gear`
  (+`gear.item`/`gear.name`/`gear.attr`/`gear.attrColor`), `nav.equipment`, `nav.close`,
  `city.castle.parts` (+`fort.name`/`fort.state`), `icons/technique/{technique.id}` /
  `{loadout.id}` imageBinds.
- Removed: `technique.aimTag` (card-side aim tag deleted), `core.stats` gear/arms/base rows.
- New element fields: `frames` (authored animation frames, foeReticle), `content`+`binds` coexisting
  on gate-bound labels (data-bind-gate), `item.pad`.

**Assets**
- Added: `ui/reticle/focus_p0/p1/p2.png` (pulse frames; p0 ≡ focus).
- Changed: `ui/reticle/aiming.png` (now RED — the cursor while a technique actively targets);
  `ui/frame/panel.png` (240→64px, slice 16) + `ui/frame/card.png` (144→48px, slice 12) — **v4,
  authored at draw size (1:1)**, every usage draws border-image-width == slice.
- Removed: `ui/reticle/target_tag.png` (dropped-pin retired — delete in repo, not just add).

## b) Rebuilt from disk
- `Content.mgcb` ✅ (443 textures + layout.json copy) · `asset-manifest.js` ✅ (443 PNGs)
- `Content/ASSET_MANIFEST.md`, `UI_ASSET_MAP.md`, `DEV_LOOP_MEMORY.md` updated.

## c) Reference renders
- ALL of `design/00–08` + `reference/screens/*` re-rendered; every screen ref is exactly
  **1920×1080** (payload #11 contract; pipeline: `proto/screen_capture_prep.js` + `proto/ref_stitch.js`).

## Repo docs to update in the same PR
`design/LAYOUT_CONTRACT.md`: data-z DEPRECATED (z = paint ordinal), `frames`, `data-bind-gate`
(content+binds), `data-part`, `item.pad`, 1920×1080 ref-export contract. `design/DESIGN_SPEC.md` §8:
targeting presentation as shipped (no boxes, cursor-is-reticle, pulse frames, hotkey tags).
