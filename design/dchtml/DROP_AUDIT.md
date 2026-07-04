# DROP AUDIT — 2026-07-04 (payload B1b / B2-GO / B4 / B5 / B6a / B6b / B7 / B8a)

Per CLAUDE.md "Ship the design SOURCES in every drop": rides with this drop to `Somnium-GH/Roguebane`
and is regenerated per drop. Drop contents = `Roguebane.Content/` (from local `Content/`) +
`design/` (1920×1080 renders; `design/dchtml/` = all `*.dc.html` + `support.js` + `style_tokens.js`
+ `attribute-model.js` + `asset-manifest.js` + `Content/gear_catalog.json` + the full `proto/` +
this audit).

Confirm-to-close (already verified LANDED, no regressions found this pass): **B0, B0b, B1a, B3.**
Supersedes the previous audit (payload B4/B5/B6a/B6b/B7/B8a — all still current, folded in below).

## a) What changed

**B2-GO — figure + gear asset regen batch (hold lifted).** `proto/roster_gen.js` extended (programmatic,
never hand-authored — the golden rule):
- **Weapon TYPES** (one silhouette per type, 4 STR-metal tiers each — Iron/Steel/Mithral/Dwarven Steel,
  palette swap only per §6c: "not reshaped art"): Longsword, Axe, Mace, Claymore, Battleaxe, Warhammer
  (STR), Dagger, Rapier, Short Sword (DEX). 36 sprites.
- **New families**, each its own 4-tier ladder (mundane tiers flat, ONLY the top tier glows —
  Sling: Shepherd's→Braided→Sinew→Giantsbane (DEX,1H, pairs with a shield) · Staff tiers: Wooden→
  Twisted→Ornate→Humming (INT,2H) · Charm: Wooden→Bone→Ornate→Humming (INT,OFF) · Tome: Old Worn→
  Leather→Ornate→Glowing (INT,OFF) · **Wand (NEW — wands are HAND items now, §6d):** Adept→Twisted→
  Gemstone→Glowing (INT,HAND, dual-wieldable, never with bow/sling). 20 sprites.
- **CON shield ladder** (Wooden Shield→Iron Buckler→Kite Shield→Tower Shield) reusing the existing
  round/tower-shield silhouette family at escalating size. 4 sprites.
- **Armor icons** — STR heavy plate under the LOCKED new names (Helm/Breastplate/Vambraces/Greaves —
  old Skull Cap/Barbute/etc. names retired, not regenerated) × 4 metal tiers; DEX leather (same 4
  slots) × 4 leather tiers; INT robe (Chest+Head ONLY, §6c) × 4 cloth tiers. 40 sprites.
  These are inventory/card-icon art, same family as the existing sword/shield standalone PNGs —
  actually WEARING armor as a body-layer overlay is a separate unbuilt "morph" system (unchanged
  scope, see `DEV_LOOP_MEMORY.md`).
- **Ranged BACK-MOUNT socket** (§6d/§17 #22): every figure now emits `sockets.back` (over the shoulder
  blades) in `layout.json` — a data addition so the engine can mount an equipped bow/sling there when
  melee hands are full. No new art — this is the socket a future back-mount render hangs off.
- **elf_ranger (+ human_ranger, same core-rune spec) chest fix**: the quiver strap moved from
  crowding the neckline down to mid-chest (matches the warden's chest-emblem placement) — this WAS
  the original B2 ask, done as part of the batch per instruction.
- Total: **100 new `Content/sprites/gear/*.png`**, `Content/gear_catalog.json` (name/attr/slot/tier
  per id, for card-copy authors), `Content/layout.json` `figures`/`gear` sections regenerated,
  `design/00-assets-1-figures.png` + `design/00-assets-2-parts.png` refreshed, `proto/roster_mockup.png`
  refreshed, `attribute-model.js` STR armor sample names updated to the locked convention (Steel
  Helm / Steel Breastplate — old Steel Helmet/Plate retired).
  Figure-morph mechanics residuals (§7/§17 #15) are the engine's composition question, not an art
  blocker — proceeded on the current per-figure-part contract as instructed.

**B4 — "open Equipment" entry missing on Encounter + CampaignMap.** Added to both (Encounter:
DISABLED skin, sealed during combat; CampaignMap: enabled, footer right slot) — CityMap already had it.

**B5/B6a/B6b — Equipment inventory card states.** `invCard`/`loadoutCard`/`invTab` renamed to the
§6e locked vocabulary (`equipped/equippable/disabled/locked`), `family` keys added (previously
missing — engine's family→state resolution was skipping both cards), hover states added to all
three (overlay-tint for cards, brighten-step for the tab strip). Now reference shared
`style_tokens.js` `interactionStates` families instead of one-off inline JSON.

**B7 — raceCard head portrait bind.** Moved off the background panel (stretching the landscape head
source into a portrait aspect) onto the actual shadowed `<img>` element.

**B8a — CityMap beacon nodes.** Added `states.beaconNode` (hover + `current`/amber+glow) mirroring
CampaignMap's `cityNode`. B8b (what `glow` should look like) answered in `DEV_LOOP_MEMORY.md` #30 —
recommend reusing the existing `pulse` primitive rather than building a second one.

**B1b — key-set diff as a standing pre-ship guard.** `proto/extract_all.html` already always runs
the FULL screen set (comment in the file cites the campaignmap-loss incident) and `extract_merge.js`
refuses a merge that would drop a previously-present screen/template key — that guard ran clean on
every re-extraction referenced by the prior audit. This audit's own pre-ship check: diffed this
drop's `Content/layout.json` top-level key set (`figures/gear/screens/templates/style`) and each
`screens.*`/`templates.*` key against the previous manifest — no keys lost.

**Binds added/removed:** none beyond the B2-GO `sockets.back` data addition (a coordinate, not a bind).

## b) Rebuilt from disk
- `Content.mgcb` ✅ — **552 textures** (was 452; +100 B2-GO gear PNGs) + 2 copies (`layout.json`,
  `gear_catalog.json`, both plain data). `asset-manifest.js` ✅ — 552 PNGs.
- `invCard`/`loadoutCard`/`invTab`/`beaconNode`/`raceCard` manifest changes were hand-mirrored into
  `Content/layout.json`'s `templates` section (matching the family-name `data-states` shape the
  extractor would emit); NOT yet re-verified by a fresh `proto/extract_all.html` run — flagged OPEN
  in `DEV_LOOP_MEMORY.md` #31.

## c) Reference renders
Re-rendered via the tile-capture + stitch pipeline, all confirmed exactly **1920×1080**:
`design/01-encounter.png`, `design/02-equipment.png`, `design/03-citymap.png`,
`design/04-campaignmap.png`, `design/05-newgame.png` (+ `reference/screens/*.png` twins) — B4/B5/B6/
B7/B8a screen changes. Plus `design/00-assets-1-figures.png` + `design/00-assets-2-parts.png` for
the B2-GO gear/figure regen. Merchant + Style Frame untouched.

## d) Manual design edits (Doug/user) — intentional, not generator output
- (carried) 2026-07-03: removed the "1" / "2" step markers from NewGame's **Race** and **Core Rune**
  headings. No new manual edits since.

## FYI
- B2-GO's weapon/armor icons are NOT yet hand-socket-mounted for the new types — figure mounts still
  point at the base `sword`/`dagger`/`bow`/`staff` ids. Tier-specific mounting is a game-state wiring
  question for the engine side, not a generator gap.
- Name-length overflow (e.g. "Dwarven Steel Short Sword") is accepted per Doug's call — not re-rected
  this drop.
- Doug's other locks (Core Effect canon, stat blocks pending tuning session, merchant receiving,
  §13 aspect-fill) unchanged; no design action taken this drop.
