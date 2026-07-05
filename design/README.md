# Roguebane — design/ (screen + asset reference renders)

Full-frame 1920×1080 renders of every POC screen, plus asset reference sheets. These are the
visual source of truth the `../Content/` asset package is built to match (locked palette — see
`../Content/ASSET_MANIFEST.md`).

## Screens
| # | file | screen | layer |
|---|---|---|---|
| 01 | `01-encounter-<core>.png` | Encounter / damage — you · battlefield · foe, Attribute Pool, Action Bar + Minion Bays | Layer 4 |
| 02 | `02-equipment-<core>.png` | Equipment / loadout — chassis figure, attributes, Inventory tabs, Rune Bag | between-battles |
| 03 | `03-citymap.png` | City Map — half-blind FTL beacon chart, Supplies/Support | Layer 2 |
| 04 | `04-campaignmap.png` | Campaign Map — branching route of cities to the Capital | Layer 1 |
| 05 | `05-newgame.png` | New Game — Choose Your Core | run start |
| 06 | `06-style-frame.png` | Style Frame — palette, cutaway grammar, type, pool study | north-star |
| 07 | `07-merchant.png` | Merchant — §12 shop | between-battles |
| 08 | `08-reticle-mounts.png` | Reticle Mounts — focus-reticle study | — |

### Encounter / Equipment are core-parameterised — one render PER core
`Encounter.dc.html` and `Equipment.dc.html` take a `core` enum prop (grunt/warden/adept/summoner/
reaver/ranger, see `../core-kits.js`). Each core is a distinct whole-screen state (its action bar
reflows off per-core technique-slot + minion-bay counts — the minion column collapses entirely at
0 bays), so **every core ships as its own 1920×1080 render**: `01-encounter-<core>.png` (×6) and
`02-equipment-<core>.png` (×6). No annotation labels — each screen self-identifies via its in-UI core
name / figure (these are the direct game-dev correctness reference). NewGame is NOT permutated — it
shows all six cores in one grid, so it stays a single `05-newgame.png`.

Regenerate them with the persistent driver `../proto/screen_perms.js` (core order + `RB_setCore` +
the per-permutation tile→stitch recipe); pipeline = `../proto/screen_capture_prep.js` (3×3 tiler) +
`../proto/ref_stitch.js` (stitch).

## Asset sheets (composited DIRECTLY from the live files — labeled with native size)
| file | contents |
|---|---|
| `00-assets-1-figures.png` | assembled composites (`../proto/roster`), chassis Cores, minions, gear (weapons & shields) |
| `00-assets-2-parts.png` | every modular body part by figure — healthy → damaged → broken (grunt also bare/armored) |
| `00-assets-3-ui.png` | attribute/technique/rune/node/resource icons, all 17 pips, reticles, buttons, backdrops |

Re-render any screen by opening its `.dc.html` and screenshotting at the design width (1920).

**The three asset sheets are GENERATED, never hand-built** — `../proto/sheet_gen.js` is the persistent
source of truth. It composites each sheet straight from `../Content/{sprites,icons,ui,bg}` and
`../proto/roster`, so the sheets can never drift out of sync with the real package. Regenerate after
any asset change:
```
const src = await readFile('proto/sheet_gen.js'); (0,eval)(src);
await RB_generateSheets({readImage,createCanvas,saveFile,readFile,ls,log}, 'ui'); // 'figures' | 'parts' | 'ui'
```
(one sheet per call — the parts sheet reads ~240 PNGs). Edit the `SHEETS` config in that file to change
what appears.
