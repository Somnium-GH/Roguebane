# Roguebane — ASSET MANIFEST

## Two-tier art direction — READ FIRST
Roguebane renders in **two deliberately different visual registers**, and an asset belongs to exactly one:

**Tier 1 — the UI chrome is HIGH-DEF, sleek, refined.** Everything that frames play — panels, bars,
pips, technique/attr chips, badges, buttons, reticles, type — is crisp and clean: hard black borders,
saturated fills, anti-aliased glyphs, high contrast, NO dithering, NO pixelation. **Depth is allowed and
intentional on interactive chrome** — buttons, inventory-selection tiles, and the map node tokens carry
real bevel/emboss/gloss; flat-shaded pictographs (attr/rune/resource glyphs, pips) stay flat. It reads
like a modern, tightly-drawn fantasy game UI. These atoms are not hand-drawn in
canvas — they are **captured from the live `.dc.html` screens** (ASSET_GEN_METHOD.md / §12 of
`LAYOUT_CONTRACT.md`), reproducing the design pixel-perfect (depth included) by construction.

**Tier 2 — the world art is LOW-RES 8-bit.** Characters, gear, minions and backdrops are chunky,
blocky, flat-shaded pixel art with a single bevel and crisp nearest-neighbour edges (the locked
proportions + rules live in `ART_RULES.md`). Think NES/early-VGA storybook, upscaled but keeping its
coarse soul. This is the ONLY tier that is "8-bit"; the UI is not.

The mistake to avoid: making the UI look cartoonish/blocky to "match" the sprites, or making the sprites
slick to "match" the UI. They are intentionally different. When in doubt: chrome = Tier 1 (capture from
the screen), world = Tier 2 (generate from `roster_gen.js`/`bg_gen.js`).

**Art target (Tier 2 sprites)** = HIGH-fidelity retrofantasy EGA/VGA — a crisp, high-resolution rendering of the
oldschool 8-bit / EGA-VGA storybook look (Zeliard-style), as if that retro art were upscaled to HD
while keeping its old-school soul: polished, decent quality, NOT crude or low-res. (Separate
from the label/highlight scaffolding, which is not the style.) Current placeholders are fine while
we nail format + structure — just don't mistake placeholder crudeness for the target look.

**UI palette (locked to the screens):** STR `#c2553f` · INT `#6f8fc4` · DEX `#82a85e` · CON `#cf9a44`;
amber `#d9a441`, ember `#c8643c`, blood `#b23b32`; ink `#ece0cb` / muted `#9a8468`; panels `#1d150e`/`#17110b`,
borders `#4a3729`/`#5a4636`, outline `#120a0c`. Every icon/pip/button/backdrop here is generated in these exact
values so an engine renders pixel-identical to the `.dc.html` mockups.

**Pipeline:** MonoGame DesktopGL content pipeline (`Content/Content.mgcb`). All textures are
32-bit RGBA PNG; sprites/icons/ui carry transparency, backdrops are opaque. Native sizes are the
"HD pixel" sizes — sample with `SamplerState.PointClamp` and integer-scale in the render shell to
keep edges crisp. **No third-party IP** appears in any file, name, or string.

**Core/shell split:** every asset row lists *drives-from* — the `Roguebane.Core` state field the
render shell reads to pick the asset. The shell maps state→asset; it never decides rules.

**Labeling note:** the colored-limb edges / capability call-outs on the prototype screens are
**prototype scaffolding** (a separate, strippable debug layer) — they are NOT baked into any
sprite or string here. Attribute *icons* legitimately carry attribute color; body part *sprites*
are neutral material (stone/flesh), with damage shown by cracks + shading, never by color.

`status`: **placeholder** = stand-in art, correct format/size/binding; **ext** = external
dependency; **OPEN** = binding or content undecided (see foot).

---

## sprites/ — bodies, foe, minions, gear (PNG32, transparent, PointClamp)

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|

## sprites/body/<figure>/ — PER-FIGURE modular parts + `Content/layout.json` (LOCKED idiom)

The flat-bevel roster (see `proto/roster_gen.js`, `ART_RULES.md`, `LAYOUT_CONTRACT.md`) emits **per-figure**
parts — each figure has its own `head/torso/armL/armR/legL/legR/boots` (robe figures: `torso`(robe)/`armL`/`armR`/`head`;
some carry `back`(wings) or `frontGear`), every part as `<part>_<state>.png` for `healthy·damaged·broken`.
Figures: `human_grunt human_warden human_adept human_summoner human_reaver human_ranger` (the 6 human cores) plus the
non-human core permutations and standalone foes `skeleton bandit wraith ogre troll gargoyle`. Flattened
thumbnails live in `proto/roster/<figure>.png`. Damage = darken + hairline crack (broken adds a corner
chip), shown by shading/cracks never color.

**RACE × CORE axis.** Identity = a race BASE-BODY (skin/hair ramp, ears, build) × a core kit
(the archetype, formerly "chassis"). EVERY playable race emits its 6 cores as decomposed figures keyed
`<race>_<core>` (LAYOUT_CONTRACT §1 — no bare core ids): humans are `human_grunt human_warden
human_adept human_summoner human_reaver human_ranger`; Elf adds `elf_grunt elf_warden elf_adept elf_summoner
elf_reaver elf_ranger` (leaner build, pointed ears baked into `head`, cooler/paler skin ramp; same part set +
damage/bare states + layout.json entry as the human cores). `ranger` ("THE MARKSMAN") is a light-leather DEX
duelist-at-range core — shield-ignoring, wields the detached `sprites/gear/bow` mounted to `handL` (no
off-hand piece, unlike grunt/warden's shield hand). Standalone foes (`skeleton bandit wraith
ogre troll gargoyle`) keep bare ids — they have no race axis. Adding a race = one entry in the `RACE`
table in `roster_gen.js`, then rerun the driver.

**`Content/layout.json`** is the coordinate source of truth (per `LAYOUT_CONTRACT.md`): `figures.<id>` =
`{size, pivot, z, parts:{<part>:{rect:[x,y,w,h]}}, sockets:{handL,handR,neck,shoulderL,shoulderR}, mounts}`
in figure-space px; `gear.<id>` = `{pivot}`; `screens.<id>` = responsive design-space (960×540) UI manifests.
The game composites parts at `rect` in `z` order (verified pixel-identical to the flattened PNG) and mounts
each `gear` PNG by aligning its `pivot` to the figure's hand socket. Detached gear PNGs: `sprites/gear/{sword,
round_shield,tower_shield,dagger,club,staff}.png`.

**Screen manifests (§3/§7/§8/§9).** `layout.json` also carries the full UI layout for `combat` + `build`
(others coarse — see [OPEN]): `style` (single palette/font/partStates/pip table, mirrored from
`style_tokens.js`), `templates.<name>` (per-card mini-layouts: rect + colour-token + font-role + sample
per sub-part), and `screens.<id>.elements[]` (anchor + design-space offset/size + z + binds + colour
token + font role; data-driven regions carry `item:{template,flow,gap,cols,size}`). These are NOT
hand-authored: `proto/screen_extract.js` walks the live instrumented `.dc.html` DOM (`[data-el]`,
`data-anchor/binds/container/template/flow/z`) and projects computed geometry/colour into the manifest,
so same DOM ⇒ byte-identical manifest. `style_tokens.js` is the one style source both the screens and
the extractor read. Re-run after any screen edit; `layout.json` ships via `/copy:` (plain data, not a
texture).

*Swept:* the earlier painterly `sprites/body/{arm,chest,head,leg}/`, `sprites/foe/*`, `sprites/char/*`,
and redundant `sprites/gear/weapon_*`·`shield_*` assets were removed — the per-figure tree + generated
gear are the only character art now. `Content.mgcb` mirrors the on-disk tree exactly.

## sprites/gear/ — detached weapons + shields (generator-produced, pivots in layout.json)

Six gear PNGs from `proto/roster_gen.js`, each mounted by aligning its `gear.<id>.pivot` (in
`Content/layout.json`) to a figure's hand socket. Flat-bevel idiom, 1px outline.

| id | type | screen | mounts to | status |
|---|---|---|---|---|
| `sprites/gear/sword` | one-hand blade (points up) | Combat, Build | hand socket | hi-fi |
| `sprites/gear/dagger` | off-hand dagger (points down) | Combat, Build | hand socket | hi-fi |
| `sprites/gear/club` | brute club | Combat | hand socket | hi-fi |
| `sprites/gear/staff` | caster staff + orb | Combat, Build | hand socket | hi-fi |
| `sprites/gear/round_shield` | round shield + boss | Combat, Build | off-hand socket | hi-fi |
| `sprites/gear/tower_shield` | royal tower shield | Combat, Build | off-hand socket | hi-fi |
| `sprites/gear/bow` | 3-segment recurve silhouette + taut string (Ranger's DEX weapon — shield-ignoring, one-handed grip only) | Combat, Build | hand socket | hi-fi |

## sprites/minion/ — summon creatures (generator-produced, single flat-bevel sprites)

Whole-sprite minions from `proto/roster_gen.js` (the `MIN.*` specs), same locked idiom as the
roster: per-part 1px outline, flat fill + bottom/right bevel.

| id | type | screen | drives-from (Core) | status |
|---|---|---|---|---|
| `sprites/minion/skeleton` | bone summon (skull + ribcage) | Combat | `bay.minion.id` | hi-fi |
| `sprites/minion/wisp` | teal spirit (floating orb) | Combat | `bay.minion.id` | hi-fi |
| `sprites/minion/hound` | dark beast (quadruped) | Combat | `bay.minion.id` | hi-fi |
| `sprites/minion/golem` | stone construct | Combat | `bay.minion.id` | hi-fi |
| `sprites/minion/imp` | red demon (horns + tail) | Combat | `bay.minion.id` | hi-fi |

## targeting — combat HUD affordances (PNG32, transparent)

**UI atoms come from TWO sources (no hand-painting, no hallucinated twins).**
(a) **Captured from the live screens** (§12 of `LAYOUT_CONTRACT.md`, via `proto/atom_capture.js` — the
screen's own rendered `[data-atom]` nodes sliced to PNG, listed in `proto/atom_registry.json`): the
technique glyph chips `icons/technique/{swing,frenzy,firebolt,disarm,brace}` (+ `shot`, reconstructed — see icon table) and the pool-pip states
the pool pips `ui/pip/*` — token-stamped per colour from `proto/atom_slice.js` (`pip_full_<colour>` solid; `pip_reserved_<attr>` black −45° hatch; `pip_empty`/`pip_empty_<resource>` dark socket, dashed frame on resources; `pip_debuff`/`pip_damage` amber/red +45° hatch), AND the map node tokens `icons/node/*` — CAPTURED flat from the RunMap node DOM via `proto/atom_capture.js` (ASSET_GEN_METHOD.md).
(b) **Generated as deterministic vector shapes** by **`proto/ui_atoms_gen.js`** (for atoms that are NOT on
the screens as polished art), coloured from `style_tokens.js`: `icons/attr/{strength,intellect,dexterity,
constitution}`, `icons/rune/{mark,path_minor,path_major,keystone}`, `icons/resource/{supplies,support,spoils,hp,charge,summons}`,
`icons/minion/skeleton`, `ui/reticle/{focus,focus_p0,focus_p1,focus_p2,secondary,aiming}`. Re-run to reproduce identically.
(`ui/reticle/target_tag` RETIRED 2026-07-03 review — the dropped-pin target pin was unnecessary; `aiming` is now RED, the cursor while a technique actively targets.)

Generators + capture (the whole package is reproducible from these): `roster_gen.js` (figures+gear+layout.json),
`bg_gen.js` (backdrops), `ui_gen.js` (buttons), `ui_atoms_gen.js`
(attr/rune-TIER/resource/reticle/minion icons), `atom_capture.js` + `atom_slice.js` (technique chips, pips, and
map node tokens — captured/stamped from the screens; see ASSET_GEN_METHOD.md), `rune_capture.js` (the five
Core-rune identity tokens `icons/rune/core_*` — captured from NewGame's own decagon+glyph cards, dual-bg
transparency recovery), `frame_gen.js` (the 9-slice
`ui/frame/*` ornate panel/card chrome — LAYOUT_CONTRACT §10), `mgcb_gen.js` (Content.mgcb
from disk), `screen_extract.js` (screens→layout.json, now also emitting `shadow`/`frame`/gradient `fill` per
element from `data-shadow`/`data-frame` tokens and CSS gradients, and `binds` on template sub-parts, deduped —
see DEV_LOOP_MEMORY.md). `node_icons_gen.js` is DEPRECATED (hand-canvas).

Note: **action cards and the auto-attack toggle are NOT standalone PNGs** — the game COMPOSES them
from `layout.json` `templates.techCard` / `templates.minionBay` + `style` tokens (exactly as the
`.dc.html` mock draws them inline), so there is nothing to blit and nothing to drift. The auto-attack
control uses the shared `ui/button/button_{on,normal}` chrome.

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `ui/reticle/focus` · `secondary` | locked-target brackets | Combat | `technique.aim.role` | 96×96 | 2 | hi-fi |
| `ui/reticle/focus_p0` · `p1` · `p2` | FOCUS PULSE frames (§8: engine cycles them on the fixed tick; p0 ≡ focus, the canonical/frozen frame) | Combat | `targeting.focus` · `layout.json` foeReticle `frames` | 128×128 | 3 | hi-fi |
| `ui/reticle/aiming` | THE CURSOR while a technique actively targets (red, dashed; click tile → cursor until click a part / right-click) | Combat | targeting mode active | 128×128 | — | hi-fi |
| `sprites/body/overlay_disabled` | cutaway "disabled" part overlay (beyond healthy/damaged/broken) | Combat | `parts[*].disabled` | 48×48 | — | hi-fi |

## icons/ — attributes, techniques, runes, nodes, resources (PNG32, transparent)

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `icons/attr/{strength,intellect,dexterity,constitution}` | attribute swatch (NO glyph — plain stat-colour box) | all | static attribute id | 120×120 | 4 | hi-fi · deterministic colour box (engine may draw a tinted rect instead — see UI_ASSET_MAP.md) |
| `icons/technique/{swing,frenzy,firebolt,disarm,brace,shot}` | technique glyph chip | Combat, Build | `technique.id` | 120×120 | 6 | hi-fi · deterministic chip + screen glyph, re-centred (§12); `shot` (bow's shield-piercing shot, DEX-green ➳) is reconstructed via `RB_buildChipOverlay` — the locked Encounter row has no shot card to capture |
| `icons/rune/{mark,path_minor,path_major,keystone}` | rune tier glyph — shape encodes tier: diamond(4)/pentagon(5)/hexagon(6)/octagon(8) | Build | `rune.tier` | 120×120 | 4 | hi-fi · deterministic polygon per tier (`ui_atoms_gen.js`) |
| `icons/rune/core_{grunt,warden,adept,summoner,reaver,ranger}` | Core-rune identity token — decagon (10-gon) shape encodes the "Core" tier, per-core accent fill + carved glyph (✚◈✦❖⚔↗) | New Game | `core.id` | ~412×412 | 6 | hi-fi · CAPTURED from the live NewGame core cards (`proto/rune_capture.js`, dual-bg transparency recovery) — not hand-drawn; supersedes the inline-SVG-only token (DEV_LOOP_MEMORY #2) |
| `icons/node/{camp,resource,merchant,unknown,castle,skirmish}` | map token | Run Map | `node.type` + `node.revealed` (→`unknown`) | 220 (castle 413) | 6 | hi-fi · captured WITH a smooth high-res emboss (gloss + soft bevel) from the RunMap nodes; transparent corners via dual-bg recovery (ASSET_GEN_METHOD.md). `skirmish` = the dedicated combat node (red ⚔ on dark, blood border; reveals 1 jump out like merchants) — captured from CityMap's live `b1` exemplar |
| `icons/resource/{supplies,support,spoils,hp,charge,summons}` | resource glyph | resource readout top-right of EVERY in-run screen (Encounter/CityMap/CampaignMap/Equipment/Merchant) + merchant provisions/healing | resource readouts (`run.resources`, `data-image-bind="icons/resource/{resource.id}"`) | 120×120 | 6 | placeholder · `charge` = the shield-PIERCE resource (steel heater shield run through by a `hit`-red bolt); `summons` = the minion-deploy resource (§9/§14: mint spirit rising from a teal summoning circle) — both `ui_atoms_gen.js` |
| `icons/map/{enemy_host,enemy_host_near}` | enemy war-party marker — a LEFT-facing mounted knight + red war banner + barding; rides the leading edge of the Run Map DOOM BAR as the horde marches from the castle (right) toward your camp (left). Reaching camp = you lose; `enemy_host_near` brightens the red as it closes | Run Map (doom bar) | `enemy.advance` distance + near-camp danger flag | 60×52 | 2 | world-art · deterministic flat-bevel (ART_RULES) via `proto/party_gen.js` |

## ui/frame/ — 9-slice ORNATE panel/card chrome (LAYOUT_CONTRACT §10, PNG32, transparent centre)

**The "engine does heavy lifting" split.** Shadows are NEVER a PNG — every drop shadow/text-shadow is
engine-drawn from a `style.shadows.<token>` spec (`{dx,dy,blur,color,opacity}`) a screen references via
`data-shadow="<token>"`; adding a shadow anywhere is a markup change, not a new asset. Ornate/carved
frames ARE small painted assets because a nine-patch border is not something the engine can draw from
colour tokens alone — `proto/frame_gen.js` paints the reusable set once; a screen references one by
`data-frame="<token>"` and the extractor emits `{asset, slice:[L,T,R,B], repeat, centerFill}` so the
engine nine-patch-blits it at ANY element size. **v3 frames declare `repeat:'tile'` + `centerFill:true`**
(payload v4 flag-3: design targets the ideal — edges TILE along their axis and the painted center tiles
both axes; CSS analogue `border-image-repeat: round`. The engine's current stretch blit is the
conforming side — see DEV_LOOP_MEMORY). Reserve frames for STATIC/neutral chrome (a legend box, a HUD
footer) — state-coloured borders (ready/targeting/locked, danger banners like the RunMap doom bar) stay
flat engine-drawn borders so their colour can change per state; do not force those onto a frame token.

⚠ **Every `data-frame` element MUST carry `border:<w>px solid transparent`** matching its
`border-image-width` — Chrome does not paint border-image on a zero-border-width element (this is why
CityMap's supplies/support cards rendered frameless until 2026-07-01). And screen CAPTURES must run
`proto/frame_render_shim.js` first — html-to-image drops border-image entirely (ASSET_GEN_METHOD.md).

| id | slice [L,T,R,B] | used by (token) | status |
|---|---|---|---|
| `ui/frame/panel` | [60,60,60,60] @ 240×240 | `data-frame="panel"` — CityMap `legend` (enclosed corner panel only; Encounter `hudFooter`, Equipment `bottomBand`, CampaignMap `footer`, NewGame `topBar`/`confirmBar` are full-bleed edge-to-edge HUD bars — a boxed frame reads wrong on them since they touch the screen edges, so they use a single accent-edge border + drop shadow instead, see DEV_LOOP_MEMORY) | hi-fi · **v3: tiled rope-carved band (period 24 divides the 120px edge span), painted seamless parchment-mottle center, corner medallions + bevel; single band** |
| `ui/frame/card` | [36,36,36,36] @ 144×144 | `data-frame="card"` — Equipment `inventory`, CityMap `supplies`/`musteredSupport` | hi-fi · **v3: tiled rope band (period 18 divides the 72px span), painted center, medallions** |

## ui/ — pips, reticles, buttons (PNG32, transparent; buttons 9-sliceable)

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `ui/pip/pip_full_{str,int,dex,con,supplies,support}` (+ generic `pip_full`) | filled pool pip, per colour | Combat, Run Map | attribute/resource colour | 128×80 | 7 | hi-fi · DETERMINISTIC from tokens |
| `ui/pip/pip_reserved_{str,int,dex,con}` (+ generic `pip_reserved`) | gear-reserved pip, per attr | Combat | gear reservation | 128×80 | 5 | hi-fi · black −45° hatch over colour |
| `ui/pip/{pip_empty,pip_empty_supplies,pip_empty_support}` | free socket — generic + special dashed resource empties | Combat, Run Map | empty / supplies / support | 128×80 | 3 | hi-fi · dashed coloured frame on resources |
| `ui/pip/{pip_debuff,pip_damage}` | debuff / damage pip (never recolour) | Combat | debuff\|damaged | 128×80 | 2 | hi-fi · amber/red +45° hatch (§12) |
| `ui/reticle/{focus,focus_p0,focus_p1,focus_p2,secondary}` | targeting bracket + pulse frames | Combat | `technique.aim.role` · `frames` cycle | 128×128 | 5 | hi-fi |
| `ui/button/button_{normal,hover,down,disabled,on}` | button skin (one set 9-slices to EVERY button) | all | input/interaction state + toggle | 320×88 (9-slice, corners 12px) | 5 | hi-fi · v2 @ 1080-class density (§11 — 2× the 160×44 design box; engine 9-slice corners are 12px now, not 6px): black border + double engraved line + state accent + top-sheen gloss + corner rivets + bevel (`proto/ui_gen.js`) |

*Button **labels are runtime text** (drawn over the skin), never baked into the asset.*

## bg/ — backdrops (PNG32, opaque, 1920×1080 native — 1080-density per LAYOUT_CONTRACT §11, not upscaled)

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `bg/combat_field` | scene backdrop | Combat | active encounter biome | 1920×1080 | — | placeholder |
| `bg/build_alcove` | scene backdrop | Build | static (loadout) | 1920×1080 | — | placeholder |
| `bg/map_chart` | scene backdrop | Run Map | static | 1920×1080 | — | placeholder |
| `bg/spine_road` | scene backdrop | Campaign Spine | static | 1920×1080 | — | placeholder |
| `bg/merchant_stall` | scene backdrop | Merchant (DESIGN_SPEC §12 shop screen) | static | 1920×1080 | — | placeholder · procedural lantern-lit trade tent (striped awning + counter band), `bg_gen.js` scene 5 |

## fonts/ — type (external)

| id | type | screen | drives-from | format | status |
|---|---|---|---|---|---|
| `fonts/display` | display serif | all | — | `.spritefont` (IM Fell English / open substitute) | ext |
| `fonts/mono` | data mono | all | — | `.spritefont` (JetBrains Mono / open substitute) | ext |

---

### [OPEN] (do not invent)
Minion type-gating beyond the INT skeleton (§9); the finite magic/charge resource (name + icon,
§10); foe roster beyond the placeholder ogre; whether part art ships as fixed sprites vs 9-slice
at final fidelity; the damage-tier count if it moves off 0..3. Bindings for these are stubbed,
not decided.
