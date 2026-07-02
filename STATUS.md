# Status

*Lean by design. SHIPPED-work history + rationale live in `git log` (detailed commits) and old STATUS
revisions (`git show <rev>:STATUS.md`) — recoverable, so not duplicated here. Locked design lives in
`design/DESIGN_SPEC.md`. This file = CURRENT state only. (Whittled 2026-07-01 from ~900 lines; nothing
current dropped.)*

## ⇒ HOW TO WORK THIS ARC — read EVERY pass (pixel-perfect · no drift · no premature "done")
ONE screen per pass. The goal is: every screen renders 100% from `layout.json` and matches its
`design/NN-*.png` **pixel-close**.

1. **Render FROM the manifest, never hand-draw.** Draw via the shared renderer (`DrawManifestScreen` /
   `DrawManifestElement`). If a screen needs something the renderer can't do yet, **extend the SHARED
   renderer** (one path for all screens) — never special-case a screen or fall back to hardcoded rects/
   frames. Hardcoded layout is how the old drift happened.
2. **Verify by COMPARISON, every time.** Build to a scratch dir, `RB_MF=<screen>` to render it, then open
   BOTH the shot AND `design/<screen>.png` and walk it element-by-element, listing every delta (position,
   size, colour token, text, font size, chrome/shadow/frame, missing/extra elements, stray borders). Fix
   until the delta list is EMPTY. "Looks like something" is not the bar — matching the reference is.
3. **Drive the STATE, don't trust the picture.** For data-driven screens, drive input→state (pick race/
   core, equip, aim a foe part, redeploy) and confirm LIVE data renders — binds resolve to real values,
   not the template `sample`. A control that looks right but does nothing is not done.
4. **NO DRIFT — manifest + design PNG are the ONLY source of truth.** Don't invent layout, don't add
   borders/boxes/padding not in the design, don't "improve" it. If the manifest looks wrong, FLAG it
   (Needs-CD/human) — never diverge by hand to "fix" it (that re-introduces drift).
5. **"DONE" means ALL of:** renders 100% from the manifest (zero hardcoded layout for that screen);
   delta-list vs the design PNG is EMPTY (you actually read both images); live state drives it; build
   GREEN; `crash.log` clean. Less than that is NOT done — write the exact residual as a checklist line,
   don't check the screen off.
6. **Blocked?** Write the SPECIFIC missing capability (what the design needs that the manifest/renderer
   can't express) as a concrete Needs-CD/human item. NEVER claim "exhausted" or silently approximate.
7. One screen per commit; small semantic slice; pull-before / push-after; update this file (check a screen
   only if truly done per #5, else note residual). Then STOP.

## Current target — the manifest-driven screen RENDERER (close the design delta)
The shared render path is the ONE path: `DrawManifestScreen`/`DrawManifestElement` + `ManifestUi` /
`CardTemplate` / `ListLayout` / `GraphLayout` / `StageComposer` / `NineSlice`.

FOUNDATION DONE (slices 1-7 — see git): Element `Shadow`/`Frame`/`Fill`/`Border` model + generic element
renderer (§10 order: shadow → frame|fill → border → content); LIST rendering (item template stamped per
cell, grid flow); LIVE bind resolver (`race.*`/`core.*` → live data, else `sample`); per-attr tiles;
`fontPx` scaling; display data (`Race.Tag/Blurb`, `CoreRune.ApexName/Desc` — DISPLAY-ONLY, no apex EFFECTS).

SLICES (one screen/pass, pixel-verify vs its design PNG):
1. **NewGame** — CUT OVER: the live screen now renders via `DrawManifestScreen("newgame")`; the old
   5-card + `RACE [tab]` screen is DELETED. Input reads manifest geometry BY BINDS (races/cores list
   cells + begin rect — never CD's renameable element ids). `.selection` parts draw only on the chosen
   card (CHOSEN / CORE SET follow clicks; other state binds still gated). Verified LIVE + state-driven:
   RB_CHASSIS=2 moves CORE SET to Adept and the whole preview column follows (figure/role/budget/apex).
   Renderer foundation from this arc: z back-to-front, fill+frame, text-shadow-as-glyph-copy,
   height-capped word-wrap, template-part fill/border.
   NOT design-done — remaining deltas are ALL manifest-expressiveness gaps (Needs-CD below): card/tile
   bg+chrome parts, tile value+label split, `align`, button/chip labels+fills, core-rune icons +
   ①②③/✚ glyph font, head-image assets, hi-fi bg. Re-walk the full delta list when CD's drop lands.
2. **Equipment** (in progress) — a BETWEEN-FIGHTS loadout for the CURRENT core (NO core-switch tabs —
   that's NewGame). THIS PASS the manifest renderer covers its live binds: `Body` paperDoll figure;
   `attrs` attribute bars (key/part-label §6, free/cap readout, 12 positional pips: filled→attr token,
   allocatable→slot, beyond cap→empty); `loadout`/`minions` action-bar cards (name + STAT-reserve from
   the seeded kit); `core` identity; `runes.budget` ("N free / M"). Verified RB_MF=equipment.
   NOT cut over yet: the old hand-drawn Equipment screen keeps its WORKING interactions (palette
   toggles, rune-ladder climbs, redeploy CTA) — cutting now would regress function. Cut when manifest
   input wiring lands (like NewGame's by-binds click cells). Sample-only still: inventory GEAR cards +
   rune-bag group cards (models design-open, see Debt) + tabs (labels lost in extraction, Needs-CD).
3. **Encounter** — `design/01` (single foe, attribute pool, action bar); single-frame panels.
4. **CityMap** — `design/03` (node graph, supplies, war-party RIGHT→LEFT, Equipment button).
5. **CampaignMap** — BUILD it (not implemented) from `design/04`.
FLOW: Equipment reachable BETWEEN fights — from the post-combat Cleared/Redeploy state + CityMap +
CampaignMap — editing the CURRENT loadout (core fixed).

## Verify mechanics
- `RB_MF=<screenId>` renders a screen STRAIGHT from the manifest (safe — live screens untouched): the
  cut-over dev hook. `RB_SMOKE=1 RB_SHOT=x.png RB_SCREEN=<newgame|equipment|encounter|citymap> dotnet …`
  renders a LIVE screen to a PNG headless.
- A RUNNING game locks the default build output → build to a SCRATCH dir and run THAT instance.
- On ANY crash READ `bin/Debug/net9.0/crash.log` (Program.cs writes the full exception + stack).
- SpriteFonts are ASCII-only and THROW on unknown glyphs — `Core.GlyphSafe.Sanitize` folds them; ①②③/✚
  still render "?" (glyph-font task). Swap system fonts → bundled open fonts before distribution.

## Debt (active — with reconcile trigger)
- Equipment: no inventory tabs (GEAR/TECH/MINIONS) + drag-to-equip + equipped-gear-on-anatomy + real
  rune-bag cards yet — blocked on gear/minion equip + mid-run gear/rune mutation (design-open).
- Bow.png not mounted on the figure; `shot` technique needs an icon (Game TODO).
- The FLAT thumbnail dir `sprites/char/chassis/*` + its `Content.mgcb` entries still use the old name.
  (The `layout.json` FIGURE keys are ALREADY `human_`/`elf_`, and CD has ALREADY renamed the screen ids
  to newgame/equipment/citymap/campaignmap/encounter — both done.) Retire the flat `chassis/` dir once
  cards compose from modular parts (the retire-bare directive).
- Gradient fill draws vertical/horizontal only; diagonal deferred (PointClamp sampler).
- Minion `AltCost` summon is an UN-COSTED placeholder — wire the real HP/stat spend when an alt-cost minion ships.
- Shields (Stoneskin) wired + available but not yet in starting kits; SpellWard deferred (no spell model).
- No rallied-support lane in combat yet. Mouse is click+hover only (no drag-to-equip/tooltips/rebinding).
- G1: skirmish foe-part-aim numbers placeholder — tune in play (campaign verified winnable).
- Targeting-FSM bug (no clean repro yet): firing after a weapon charges while UNTARGETED misbehaves —
  watch/fix as the targeting+firing FSM is refined.

## Needs human (loop skips)
- Balance/feel tuning (all placeholder-sane, tune in PLAY): tick 10/s; cooldowns + damage; DEX haste
  2%/pt cap 28%; CON→HP 1:2 +base8; evasion %; shield amounts/regen; armed-foe + castle HP/strike/cadence;
  budgets/spoils/prices; supplies vs march length; race/core stat blocks (design/05).
- Plate armour role: give it a role or retire the kind (flagged in `Shops.cs`).
- Apex EFFECTS: the 5 core signature effects are DISPLAY-ONLY placeholders — design them (+ the effect
  model) in a dedicated pass; NO undesigned mechanics in code meanwhile (CLAUDE.md guardrail).
- Part→stat friction (legs = accuracy, arms = STR) — low-pri, revisit only if it nags.

## Asset gaps (Needs Claude Design) — art missing/wrong, not composable from primitives
- Ranger figure (`human_ranger`/`elf_ranger`) — placeholder card until it lands.
- Glyph coverage (①②③ step numbers; ✚ ◉ ✓ marks) — the manifest USES these but the ASCII SpriteFont
  renders "?". FIX = a bounded Game/font task, NOT CD art: add these codepoints to the SpriteFont
  character regions + use a bundled font that covers them (fold into the planned system→bundled-font
  swap). This is the ONE real dependency for NewGame pixel-perfect; bake a glyph as an icon only if the
  chosen font lacks it.
- Skirmish node icon (map uses the `unknown` "?" stopgap).
- wraith (partial art, 12/21 files) + gargoyle (nonstandard layout) — need completion/normalization before wiring.
- HI-FI CHROME pass on all 5 screens + backgrounds at 1080; + the layout-drift corrective (extra inner
  boxes in cards/headers/footers). Full brief in the pending payload (see Pointers) — relay to CD.
- Manifest-extraction gaps (design shows it, layout.json can't express it — renderer must NOT invent):
  card/tile BACKGROUND+border parts (race/core cards, attr+budget tiles render chrome-less); tile
  VALUE+LABEL flattened to one text element (BASE HP / RUNE BUDGET / ACTIONS / MINION BAYS labels lost);
  `align` never emitted (contract has it — centred tiles/headers render left); button labels dropped
  (beginBtn has no content — design says "BEGIN THE RUN"); STARTER/SPECIALIST chips lack bg fills;
  equipment invTab parts carry NO label bind (all three tabs stamp "GEAR"); equipment's bottom-left
  identity block (design/02: name/role/gear/bays/budget/apex) collapsed to a single `currentCore` text.

## Pointers
- Design canon: `design/DESIGN_SPEC.md`. Capture/layout contract: `design/LAYOUT_CONTRACT.md`.
- FUTURE / open DESIGN questions (healless-compensation archetype, shield numbers + shield-wall formula,
  race/core roster + the race↔core matrix, CON-as-minion-resource, camp-defense last-stand, keystone
  taxonomy, apex-effect design) live in **DESIGN_SPEC §17 OPEN** — not lost, canonical there by design.
- Visual truth / the pixel bar: `design/NN-*.png` + `design/SCREENS.md`.
- Shipped-work history + rationale: `git log` (detailed commits) — intentionally not duplicated here.
- Pending Claude Design payload: `outputs/CLAUDE_DESIGN_issues.md` (relay when ready; CD-resolved items
  await loop-wire + Doug's confirm-to-close — tracked in the space's designer-notes memory).

## Backlog (NOT prioritized — later; do not pull ahead of the render arc)
- **String localization / i18n:** externalize all user-facing strings behind IDs; route every drawn
  string through the renderer's `content`/`binds` text path (build toward it, don't hardcode literals) so
  a locale/string-table layer slots in later at ~zero cost; fonts need target-script glyph coverage
  (today ASCII-only). NOT now — just don't architect against it.
