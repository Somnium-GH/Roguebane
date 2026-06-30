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

## 2. Gear — SEPARATE, mounted (not baked)
EQUIPMENT (weapons, shields, held items) is the player's loadout and SWAPS, so it must NOT be baked into
the figure. Today `roster_gen.js` paints it into the grid via the `front:`/`back:` callbacks — for the
modular export, STOP doing that: export each piece as its own PNG with a `gear.<id>` entry
`{ "pivot":[x,y] }` (the grip point), and the combat MODULAR parts (§1) must be exported WITHOUT any
equipment painted on. Mount at runtime = align `gear.pivot` to `figure.sockets.handL/handR`.
- DISTINCTION: this applies only to equipment. ANATOMICAL features that aren't swappable — wings, tusks,
  horns, robe sleeves — STAY part of their body part (bake them into head/torso/arm as today).
- The FLATTENED thumbnail figure may keep its signature gear baked in (it's a static portrait for the
  build / New Run cards). Only the modular combat parts must be gear-free.

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

## 6. Safe landing — ADDITIVE, don't break the running game
The game won't read `layout.json` or the new part structure until the loop builds the consumer. Until
then, land everything ADDITIVELY so the current build keeps running:
- ADD the new figure-namespaced parts (`sprites/body/<figure>/<part>_<state>.png`), `layout.json`, and
  the flattened figures ALONGSIDE existing assets. Do NOT remove or rename what the current game already
  loads (e.g. `sprites/body/<part>/base_<state>.png`, current thumbnails) until the game has cut over.
- Keep `Content.mgcb` IN SYNC: every file it references must exist, and each new PNG needs its own entry.
  A removed/renamed file still referenced by mgcb = the content build FAILS and the game won't run
  (hard break). Regenerate mgcb together with the assets.
- `layout.json` is plain data, not a texture: copy it into the Content output (NOT the mgcb texture
  pipeline). The game reads it directly; ignored until consumed = safe.
- After the game is on the manifest, a cleanup slice retires the now-dead old assets + their mgcb entries.

## 7. UI specifics — containers, lists & completeness (this is what closes the drift)
Static `anchor+offset+size` covers fixed panels, but the real UI drift is DATA-DRIVEN regions the game
guessed/omitted (rune bag, inventory tabs, action bar, attribute readout). For those the screen manifest
emits a CONTAINER + an item TEMPLATE + a binding, so the game lays out the repeats deterministically:
```
{ "id":"runeBag", "anchor":"TopRight", "offset":[-16,16], "size":[260,400], "z":10,
  "binds":"runes",
  "item": { "template":"runeCard", "size":[244,40], "flow":"vertical", "gap":6, "pad":[8,8] } }
```
- `binds` = the Core collection; the game instances one `item.template` per entry, flowing them
  (`flow`: vertical | horizontal | grid, with `gap` / `pad` / `cols`) inside the container.
- A template is itself a mini-layout: its sub-elements (icon, name, cost, gate marker) are anchor+offset
  +size WITHIN the card. Define each template once under `templates.<name>`.
- Tabs = a container with a `tabs:[…]` list + the active tab's content container.
- Fixed readouts that are still per-stat (the Attribute Readout's 4 bars + gate markers) use the same
  container+template (binds: the 4 stats).

COMPLETENESS RULE: `screens.<id>.elements[]` must be the COMPLETE inventory for that screen — the game
renders exactly that set, so it CANNOT silently drop the rune bag, the inventory tabs, or the gate
markers (today's drift). The manifest is the machine source of truth; `design/SCREENS.md` is the
human-readable mirror of the same inventory. Emit every element the mockup draws.

## 8. Capture EVERY visual detail, not just geometry (kills the remaining drift)
Geometry alone leaves the loop guessing fonts, colours, text, borders, states — so capture the full
render spec. Two parts: a shared `style` block (once) + a complete per-element spec.

SHARED `style` (emit once, from `design/06-style-frame.png` + `ASSET_MANIFEST.md`):
```
"style": {
  "palette": { "ink":"#ece0cb","muted":"#9a8468","amber":"#d9a441","blood":"#b23b32",
               "panel":"#1d150e","border":"#5a4636","str":"#c2553f","int":"#6f8fc4",
               "dex":"#82a85e","con":"#cf9a44","ember":"#…","outline":"#15100c", "…":"…" },
  "fonts":   { "display":{ "role":"serif — world & names" }, "mono":{ "role":"numbers/state/captions" },
               "sizes":{ "title":…, "body":…, "caption":… } },
  "partStates": { "ok":"…","damaged":"…","disabled":"…","focus":"reticle","gearSocket":"hatch" },
  "pip": { "wrapAt":10, "shrink":true, "fixedFootprint":true, "anchor":"monoNumber", "max":20 }
}
```
Elements reference palette TOKENS (`"color":"int"`) and font roles — never raw hex — so it stays
consistent + themeable. The `partStates` grammar is the canonical condition→sprite mapping the figure
parts (§1) use.

PER-ELEMENT spec (extends §3/§7) — emit whatever the element has:
```
{ "id":"runeBag", "type":"panel|text|bar|pip|card|icon|tabs|button|figure|list",
  "anchor":…, "offset":…, "size":…, "z":…,
  "fill":"panel", "border":{ "color":"border", "w":1 },
  "title":{ "text":"Rune Bag", "font":"display", "color":"ink", "align":"left" },
  "caption":{ "text":"found on the march · socket or sell", "font":"mono", "color":"muted" },
  "text":{ "binds":"runes.budget", "font":"mono", "color":"amber", "align":"right" },  // or "content":"<literal>"
  "icon":"icon/attr/int",
  "states":{ "selected":{"border":"amber"}, "disabled":{"color":"muted"}, "active":{…}, "locked":{…} },
  "widget":{ /* bar: segments[base,marks,current] + gateMarker + value; pip: uses style.pip; tabs: tabs[]+activeContent */ }
}
```

COMPLETENESS (visual): every element carries geometry + font + colour-token + content/bind + border/fill
+ states + any widget params. If the mockup shows it — a panel title, a caption/tip, a section header,
a bar segment, a gate marker, a pip row, a card's tier ring, a tab's active state, an equip button, a
leader-line part label, the backdrop — it's in the manifest. Then the game reproduces the screen with
ZERO invented styling, and the vision review just confirms.
