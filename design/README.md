# Roguebane — design/ (screen + asset reference renders)

Full-frame 1920×1080 renders of every POC screen, plus asset reference sheets. These are the
visual source of truth the `../Content/` asset package is built to match (locked palette — see
`../Content/ASSET_MANIFEST.md`).

## Screens
| # | file | screen | layer |
|---|---|---|---|
| 01 | `01-combat.png` | Combat / damage — you · battlefield · foe, Attribute Pool, Action Bar + Minion Bays | Layer 4 |
| 02 | `02-build.png` | Build / loadout — chassis figure, attributes, Inventory tabs, Rune Bag | between-battles |
| 03 | `03-runmap.png` | Run Map — half-blind FTL beacon chart, Supplies/Support | Layer 2 |
| 04 | `04-campaign-spine.png` | Campaign Spine — branching route of cities to the Capital | Layer 1 |
| 05 | `05-new-run.png` | New Run — Choose Your Core | run start |
| 06 | `06-style-frame.png` | Style Frame — palette, cutaway grammar, type, pool study | north-star |

## Asset sheets (every asset centered at integer scale, grid-aligned, labeled w/ size)
| file | contents |
|---|---|
| `00-assets-1-figures.png` | character composites, chassis Cores, minions, gear (weapons & shields) |
| `00-assets-2-parts.png` | modular body parts — hero (base · plate · robe×3) + all decomposed foes (head/torso/arm/leg) |
| `00-assets-3-ui.png` | attribute/technique/rune/node/resource icons, pips, reticles, buttons, backdrops |

Re-render any screen by opening its `.dc.html` and screenshotting at the design width (1920).
Regenerate the asset sheets from the `../Content/sprites|icons|ui|bg` trees.
