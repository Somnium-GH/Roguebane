# Roguebane — asset hi-fi transition brief (for Claude Design)
*Hand this to the Claude Design session. Goal: take the existing PLACEHOLDER asset set to SHIPPABLE
hi-fi, render every asset correctly, and fill the genuine gaps. Keep names/sizes/structure so the
game's pipeline keeps binding.*

## Where we are
`Roguebane.Content/` has good COVERAGE — attribute/technique/rune/node/resource icons, pips,
reticles, buttons, backdrops, the 3 figures, 5 chassis cores, minions, gear, and modular hero/foe
body parts (base + plate + robe layers; healthy/damaged/broken). The pipeline binds them and the game
renders from real state. The PROBLEM: the art is placeholder fidelity (the style frame says so), so
in-game it reads ULTRA-8-bit / prototypal. We need the transition to SHIPPABLE hi-fi.

## The look (north-star — do not drift)
HIGH-fidelity retrofantasy: a crisp, high-resolution rendering of oldschool EGA/VGA storybook fantasy
(Zeliard-style) — as if that retro art were UPSCALED TO HD while keeping its old-school soul. Retro
Think modern 8-bit, thinly outlined to accomodate bevel, blocky but well painted in a pixelated by at a high res manner.
It should not look mega man like or 3d rendered and flattened.
Things should be scaled such that they are large and fill space on the screen sprite wise, but they should not look like giant elongated overly tall thimgs, prefer the typical squashed 8bit style for things, just scaled up.
Bevel *should* be used but it should be a low radius high segments weighted normal bevel always.
STYLE, high QUALITY. Warm-muted-dusk palette (`design/06-style-frame.png` + `ASSET_MANIFEST.md`).
NOT crude blocks, NOT low-res, NOT programmer pixel art. Hand-painted shading + volume, clean 1px
borders, readable silhouettes, real depth/light within the retro idiom. One frame scales from a body
part to a castle wall.

## The ask
1. ELEVATE every existing asset to shippable hi-fi. Keep the SAME element set, file NAMES, sizes, and
   folder layout (the content pipeline + `AssetRegistry` bind by those) — just raise the art: more
   detail, proper shading, palette discipline, crisp `PointClamp`-friendly edges. Render at the native
   "HD pixel" sizes in the manifest so they stay sharp when integer/letterbox-scaled.
2. RENDER CORRECTLY — fix any placeholder/blank/incorrect exports. KNOWN: the MINIONS row has TWO
   blank 110x140 boxes (asset sheet 1) — identify what they should be and fill them, or remove them.
3. ADD the missing assets the new combat/targeting flow needs (list below).
4. Constraints: cross-platform-safe filenames (no spaces; no Windows-reserved stems like con/aux/nul);
   transparency where the manifest specifies; NO third-party IP in any asset or name.

## Missing / new assets to add (targeting + firing flow)
- Action-card STATES: a "READY/charged" treatment and a "charging" treatment (glow/frame/fill) so a
  ready technique reads at a glance; plus a "held/dry" treatment.
- AUTO-ATTACK toggle: an on/off control affordance (icon or labelled switch).
- A distinct AIMING cursor/reticle for "choosing a target" vs the existing locked-target `focus` /
  `secondary` reticles; and a small per-technique TARGET tag (which foe/part a technique is aimed at).
- Inventory TAB chrome (GEAR / TECHNIQUES / MINIONS) and rune/gear CARD frames (optional — the build
  can compose these from primitives, but real art reads better).
- (Confirm) any cutaway part overlays beyond healthy/damaged/broken (e.g. a "disabled" state).

## Deliver
The updated `Roguebane.Content/` set — same structure + names + sizes — at shippable hi-fi, with
blank/incorrect exports fixed, the new assets added, and `ASSET_MANIFEST.md` updated for anything new.
Keep it pipeline-clean (it builds via `Content.mgcb`).
