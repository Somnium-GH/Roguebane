# Roguebane — layout contract (generator → game)
*The generator is the source of truth for COORDINATES, not just art. `roster_gen.js` (and the screen
mockup generator) already compute every position; this contract says: EMIT them as data so the game
composes pixel-identically, on any resolution, with zero guessing. Generated, never hand-authored
(golden rule). One `Content/layout.json` carries it all; part PNGs sit under `sprites/body/...`.*

## Coordinate rules
- Units = integer pixels in **figure-space** (the part PNGs' own exported pixel grid) for figures, and
  **design-space** (960×540 reference) for screens. Origin top-left, y-down (MonoGame default).
- The game treats a figure as ONE unit: composite its parts in figure-space, then uniform-scale the
  whole figure into its on-screen slot. So figure-space is internal + scale-independent.
- Everything below is what the generator must RECORD during assembly and write out — it's the same
  arithmetic `human()`/`robe()` already do (`tL/tR/tTop/tBot`, arm rects, `handLx/handRx/handY`,
  `hL/hR/hTop/hBot`), just transformed by the trim+scale `toCanvas` applies (`(coord − minx) × S`).

## 1. Figures — modular parts + manifest
For each figure, in ADDITION to the flattened figure PNG (keep it — thumbnails/New Run use it):
- **Export each part as its own trimmed PNG** per damage state:
  `sprites/body/<figure>/<part>_<state>.png` where part ∈ {head, torso, armL, armR, legL, legR,
  boots, …robe figures list only the parts they have} and state ∈ {healthy, damaged, broken}.
- **Emit the figure entry** in `layout.json` under `figures.<id>`:
```
"grunt": {
  "size":   [W, H],                        // figure-space bbox the parts compose into
  "pivot":  [feetCenterX, baselineY],      // the point the game pins to the stage ground line
  "z":      ["back","legL","legR","boots","torso","armL","armR","head","frontGear"],
  "parts":  { "head":{"rect":[x,y,w,h]}, "torso":{"rect":[...]}, "armL":{"rect":[...]},
              "armR":{"rect":[...]}, "legL":{"rect":[...]}, "legR":{"rect":[...]}, "boots":{"rect":[...]} },
  "sockets":{ "handL":[x,y], "handR":[x,y], "neck":[x,y], "shoulderL":[x,y], "shoulderR":[x,y] }
}
```
- `rect` = where that part's PNG is blitted in figure-space (top-left + size). `z` = paint order (the
  exact order `human()` paints today). `sockets` = mount points; weapons/shields mount their own pivot
  to `handL/handR` (the script's `handLx/handRx/handY`). Robe figures: omit torso/legs/boots, keep arms.
- A new race/foe needs NO game changes — it just adds its `figures.<id>` entry + part PNGs.

## 2. Gear — its own pivot
Each weapon/shield PNG gets a `gear.<id>` entry: `{ "pivot":[x,y] }` (the grip point that lands on a
hand socket). Mount = align gear.pivot to figure.sockets.handL/handR.

## 3. Screens — responsive UI manifest (fills any aspect)
For each screen, emit `screens.<id>`:
```
"combat": {
  "designSize":[960,540],
  "elements":[
    { "id":"attrPool", "anchor":"BottomLeft", "offset":[16,-16], "size":[220,140], "z":10, "binds":"Body.pool" },
    { "id":"actionBar","anchor":"Bottom",     "offset":[0,-12],  "size":[760,76],  "z":10, "binds":"loadout" },
    { "id":"foePanel", "anchor":"Right",      "offset":[-24,0],  "size":[300,400], "z":5,  "binds":"encounter.foe" }
    // ...
  ]
}
```
- `anchor` ∈ {TopLeft,Top,TopRight,Left,Center,Right,BottomLeft,Bottom,BottomRight}. `offset` = design-px
  from that anchor (negatives pull inward from right/bottom). The HUD glues to real screen edges, so it
  **spreads to fill any aspect** — no bars.
- `binds` = the Core state the element reads (advisory; the game wires it).

## 4. Determinism
Every on-screen position is a pure function of (manifest, figure→slot scale, screen size). Same
manifest ⇒ identical layout on any monitor. A screenshot matches the mockup BY CONSTRUCTION — the
loop's visual review becomes a confirmation, not a guess-and-nudge loop.

## 5. Output checklist (per regenerate)
- `sprites/body/<figure>/<part>_<state>.png` (modular parts, all states)
- flattened `<figure>.png` (thumbnails)
- `Content/layout.json` (`figures`, `gear`, `screens`)
- `ASSET_MANIFEST.md` updated for any new ids
- cross-platform-safe names (no spaces / Windows-reserved stems); no third-party IP.
