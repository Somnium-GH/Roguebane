# Roguebane — ASSET MANIFEST

**Art target = HIGH-fidelity retrofantasy EGA/VGA** — a crisp, high-resolution rendering of the
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
## sprites/body/{part}/ — MODULAR base + armor layers (PNG32, transparent, PointClamp)

Each part is **two stacked layers** — a bare `base` limb and an `armor` overlay that acts as a
proxy on top (armor renders over the limb when equipped; strip it and the base shows through).
Each layer has three condition states: `healthy` · `damaged` · `broken`. The renderer composites
`base_<cond>` then, if armored, `armor_<cond>` at the part's body anchor. One `arm`/`leg` asset is
reused for both sides (mirror the right). Centered, grid-aligned, full part size.

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `body/head/base_*` | head limb | Combat, Build | `parts[Head].condition` | 32×36 | `healthy·damaged·broken` | placeholder |
| `body/head/armor_*` | head armor (helm) | Combat, Build | `head.armorId` + `armor.condition` | 32×36 | `healthy·damaged·broken` | placeholder |
| `body/chest/base_*` | chest limb | Combat, Build | `parts[Chest].condition` | 40×40 | `healthy·damaged·broken` | placeholder |
| `body/chest/armor_*` | chest armor (plate) | Combat, Build | `chest.armorId` + condition | 40×40 | `healthy·damaged·broken` | placeholder |
| `body/arm/base_*` | arm limb (×2 mirror) | Combat, Build | `parts[Arm].condition` | 20×44 | `healthy·damaged·broken` | placeholder |
| `body/arm/armor_*` | arm armor (vambrace) | Combat, Build | `arm.armorId` + condition | 20×44 | `healthy·damaged·broken` | placeholder |
| `body/leg/base_*` | leg limb (×2 mirror) | Combat, Build | `parts[Leg].condition` | 20×48 | `healthy·damaged·broken` | placeholder |
| `body/leg/armor_*` | leg armor (greave) | Combat, Build | `leg.armorId` + condition | 20×48 | `healthy·damaged·broken` | placeholder |

*Armor variants per part:* `armor_*` (steel plate) plus `robe_blue_*` · `robe_violet_*` ·
`robe_cloth_*` (caster robe colorways) — each with `healthy·damaged·broken`. The renderer picks
the variant by `armorId`, so an Adept/Summoner wears a robe through the same modular slot a Warden
wears plate.

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `sprites/gear/weapon_{sword,axe,mace,greatsword,dagger,spear,bow,staff,staff_summon}` | weapon → arm | Combat, Build | `loadout.arms.weaponId` | ~16×52 | — | placeholder |
| `sprites/gear/shield_{round,tower,kite,buckler}` | shield → off-arm | Combat, Build | `loadout.offArm.shieldId` | ~28×42 | — | placeholder |
| `sprites/gear/club_ogre` | foe weapon | Combat | `enemy.weaponId` | 20×56 | — | placeholder |

*Armor is a separate modular layer, NOT baked into the limb — so a part can be bare, armored, or
have its armor shatter (→ `broken`) independently of the flesh underneath. The showcase
`sprites/char/player_knight` is the composite of base+armor+weapon+shield at `healthy`.*

## sprites/foe/ogre/{part}/ — MODULAR ogre limb parts (base only, no armor)

Same per-part decomposition as the hero, but the ogre is bare (no armor layer). Head, torso, and
mirrored arm/leg, each `base_{healthy·damaged·broken}` (gash → torn-away chunk, raw flesh). The
club is a separate gear overlay. Skeleton minion is intentionally NOT decomposed yet (one whole
sprite; a single damaged variant may come later).

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `foe/ogre/head/base_*` | ogre head | Combat | `enemy.parts[Head].condition` | 36×36 | `healthy·damaged·broken` | placeholder |
| `foe/ogre/torso/base_*` | ogre torso | Combat | `enemy.parts[Core].condition` | 52×36 | `healthy·damaged·broken` | placeholder |
| `foe/ogre/arm/base_*` | ogre arm (×2 mirror) | Combat | `enemy.parts[Arm].condition` | 20×40 | `healthy·damaged·broken` | placeholder |
| `foe/ogre/leg/base_*` | ogre leg (×2 mirror) | Combat | `enemy.parts[Leg].condition` | 22×34 | `healthy·damaged·broken` | placeholder |
| `sprites/gear/club_ogre` | ogre club | Combat | `enemy.weaponId` | 20×56 | — | placeholder |

## sprites/foe/{foe}/{part}/ — decomposed foes (base limb states, no armor)

Every foe is decomposed like the ogre — head · torso · arm (×2 mirror) · leg (×2 mirror), each
`base_{healthy·damaged·broken}` (gash → torn chunk + raw). No armor layer (foes are bare).

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `foe/ogre/{head,torso,arm,leg}/base_*` | ogre parts | Combat | `enemy.parts[*].condition` | 36×36 … 22×34 | `healthy·damaged·broken` | placeholder |
| `foe/bandit/{head,torso,arm,leg}/base_*` | hooded rogue | Combat | `enemy.parts[*].condition` | 32×32 … 20×34 | `healthy·damaged·broken` | placeholder |
| `foe/wraith/{head,torso,arm,leg}/base_*` | cloaked spirit | Combat | `enemy.parts[*].condition` | 32×32 … 20×34 | `healthy·damaged·broken` | placeholder |
| `foe/troll/{head,torso,arm,leg}/base_*` | hulking brute | Combat | `enemy.parts[*].condition` | 32×32 … 20×34 | `healthy·damaged·broken` | placeholder |
| `foe/gargoyle/{head,torso,arm,leg}/base_*` | winged stone fiend | Combat | `enemy.parts[*].condition` | 32×32 … 20×34 | `healthy·damaged·broken` | placeholder |

## sprites/char/chassis/ — selectable Core figures (New Run; placeholder)

One figure per Core, shown on the Choose-Your-Core screen and as the in-game body. Flat
grid-aligned silhouettes, distinct per archetype.

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `char/chassis/grunt` | core figure · generalist | New Run | `chassis.id` | 64×92 | — | placeholder |
| `char/chassis/warden` | core figure · bulwark | New Run | `chassis.id` | 64×92 | — | placeholder |
| `char/chassis/adept` | core figure · caster | New Run | `chassis.id` | 64×92 | — | placeholder |
| `char/chassis/summoner` | core figure · binder | New Run | `chassis.id` | 64×92 | — | placeholder |
| `char/chassis/reaver` | core figure · duelist | New Run | `chassis.id` | 64×92 | — | placeholder |

## sprites/minion/ — summon figures (Combat; placeholder, whole sprites)

Whole figures (no part-decomposition yet — a single damaged variant may come later).

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `minion/skeleton` | minion · bone soldier | Combat | `bay.minion.id` | 52×64 | — | placeholder |
| `minion/wisp` | minion · spirit | Combat | `bay.minion.id` | 40×52 | — | placeholder |
| `minion/hound` | minion · beast | Combat | `bay.minion.id` | 60×44 | — | placeholder |
| `minion/golem` | minion · stone brute | Combat | `bay.minion.id` | 54×66 | — | placeholder |
| `minion/imp` | minion · winged devil | Combat | `bay.minion.id` | 48×52 | — | placeholder |

## sprites/char/ — composited figures actually rendered by the screens (PNG32, transparent, PointClamp)

High-fidelity chunky pixel-art (flat 2-tone + bold black outline), drawn whole rather than as part-stacks — this is what Combat & Build display today. Part *damage* reads from the Attribute Pool + small per-part damage bars, not from swapping these sprites (see [OPEN] for final per-part art).

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `sprites/char/player_knight` | hero figure | Combat, Build | `chassis.id` | 64×74 | — | placeholder |
| `sprites/char/ogre` | foe figure | Combat | `enemy.id` | 72×78 | — | placeholder |
| `sprites/char/skeleton` | minion figure | Combat | `bay.minion.id` | 50×64 | — | placeholder |

## icons/ — attributes, techniques, runes, nodes, resources (PNG32, transparent)

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `icons/attr/{str,int,dex,con}` | attribute emblem | all | static attribute id | 48×48 | 4 | placeholder |
| `icons/technique/{swing,frenzy,firebolt,disarm,brace,cleave}` | technique glyph | Combat, Build | `technique.id` | 48×48 | 6 | placeholder |
| `icons/rune/{mark,path,keystone}` | rune tier glyph | Build | `rune.tier` | 40/48/56 | 3 | placeholder |
| `icons/node/{camp,skirmish,control,merchant,mountain,unknown,castle}` | map token | Run Map | `node.type` + `node.revealed` (→`unknown`) | 56 (castle 128) | 7 | placeholder |
| `icons/resource/{supplies,support,spoils,hp}` | resource glyph | Run Map, Spine, Combat | resource readouts | 48×48 | 4 | placeholder |

## ui/ — pips, reticles, buttons (PNG32, transparent; buttons 9-sliceable)

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `ui/pip/{pip_full,pip_empty,pip_damaged}` | pool pip | Combat | per-pip: reserved\|free\|damaged | 24×24 | 3 | placeholder |
| `ui/reticle/{focus,secondary}` | targeting bracket | Combat | `technique.aim.role` | 96×96 | 2 | placeholder |
| `ui/button/button_{normal,hover,down,disabled}` | button skin | all | input/interaction state | 160×44 (9-slice) | 4 | placeholder |

*Button **labels are runtime text** (drawn over the skin), never baked into the asset.*

## bg/ — backdrops (PNG32, opaque, 480×270 native → integer upscale)

| id | type | screen | drives-from (Core) | size px | variants | status |
|---|---|---|---|---|---|---|
| `bg/combat_field` | scene backdrop | Combat | active encounter biome | 480×270 | — | placeholder |
| `bg/build_alcove` | scene backdrop | Build | static (loadout) | 480×270 | — | placeholder |
| `bg/map_chart` | scene backdrop | Run Map | static | 480×270 | — | placeholder |
| `bg/spine_road` | scene backdrop | Campaign Spine | static | 480×270 | — | placeholder |

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
