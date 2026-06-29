# Roguebane — Content/ (asset structure)

High-fidelity retrofantasy EGA/VGA target (see `ASSET_MANIFEST.md` for the art-target statement).
This tree is the **full-implementation structure** — real art drops into the same paths with
**zero restructuring**. Built for the MonoGame DesktopGL content pipeline.

```
Content/
├─ Content.mgcb            # pipeline build file (all PNGs registered)
├─ ASSET_MANIFEST.md       # id · type · screen · drives-from · size/format · variants · status
├─ sprites/
│  ├─ body/   head|chest|arm|leg _dmg{0..3}.png      # player parts, neutral material
│  ├─ foe/    ogre_{head|core|arm|leg}_dmg{0,2}.png  # structured foe parts
│  ├─ minion/ skeleton_{idle|active}.png
│  └─ gear/   weapon_{sword|axe}.png shield_tower.png armor_{helm|plate|greaves}.png
├─ icons/
│  ├─ attr/      {str|int|dex|con}.png
│  ├─ technique/ {swing|frenzy|firebolt|disarm|brace|cleave}.png
│  ├─ rune/      {mark|path|keystone}.png
│  ├─ node/      {camp|skirmish|control|merchant|mountain|unknown|castle}.png
│  └─ resource/  {supplies|support|spoils|hp}.png
├─ ui/
│  ├─ pip/       pip_{full|empty|damaged}.png
│  ├─ reticle/   {focus|secondary}.png
│  └─ button/    button_{normal|hover|down|disabled}.png
├─ bg/           {combat_field|build_alcove|map_chart|spine_road}.png
└─ fonts/        display.spritefont  mono.spritefont        # external type (stub)
```

## Naming convention
`type/family/{name}_{state|variant}.png` — state/variant is **baked into the filename** so the
shell indexes by Core state with no lookup table:
- damage: `_dmg0` (whole) … `_dmg3` (broken)
- toggles: `_idle|_active`, `_normal|_hover|_down|_disabled`
- categorical: the node/technique/attr name is the leaf.

## State → asset binding (Core/shell split)
The render shell composes a frame by reading `Roguebane.Core` and selecting filenames:
- **Body / foe part** → `…/{part}_dmg{clamp(damageTier,0,3)}.png`; gear overlay from the part
  group's equipped `…Id`. A part whose stat is below a gear gate drops the overlay (Core decides;
  shell just omits it).
- **Pool pip** → per segment, `pip_full` (reserved, tinted by reserver in code), `pip_empty`
  (free), `pip_damaged` (lost to part damage).
- **Minion bay** → `skeleton_active` while `bay.active`, else `skeleton_idle`.
- **Map node** → `node/{type}.png`, or `node/unknown.png` when `!node.revealed` (merchants resolve
  one jump out; resource holds + castle are always revealed — that's Core's `revealed` flag, not art).
- **Reticle** → `focus` for the focused target part, `secondary` for other per-technique aims.
- **Backdrop** → one `bg/*` per active screen/biome.

Tint/color that varies at runtime (reserver color on pips, attribute accent) is applied in the
shell via `Color` multiply — the placeholder art is neutral so tint reads true.

Sampling: `SamplerState.PointClamp`, integer scale only. Native sizes are the HD-pixel sizes.

## Not art direction
Prototype scaffolding (colored limb edges, capability labels, highlight rings on the `.dc.html`
mockups) is a **separate debug overlay** — strippable, and deliberately absent from every asset
and string here.
