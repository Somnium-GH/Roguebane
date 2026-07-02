# Status

## ✅ layout.json RESTORED (2026-07-01 pm) — the PNG-clobber is fixed by a clean re-drop
Valid JSON (5690 lines) carrying the newest CD work: `imageBind` (beaconNode → node icons as PNGs, incl.
skirmish, via `icons/node/{node.type}` — so "skirmish" isn't a literal), frame-v3 `repeat`+`centerFill`,
`cityNode`, the instrumentation. The BUILD GAP is CLOSED (2026-07-02): the three new icons are mirrored
into the GAME-side `Roguebane.Game/Content/Content.mgcb` (the one the build actually reads — CD's
`Roguebane.Content/Content.mgcb` copy is not wired; unify later) and skirmish renders on the map.

*Lean by design. SHIPPED-work history + rationale live in `git log` (detailed commits) and old STATUS
revisions (`git show <rev>:STATUS.md`) — recoverable, so not duplicated here. Locked design lives in
`design/DESIGN_SPEC.md`. This file = CURRENT state only. (Whittled 2026-07-01 from ~900 lines; nothing
current dropped.)*

## ⇒ HUMAN DIRECTIVES — 2026-07-02 (revisions WIN; fold into the render arc / after the current slice)
FLAGGED FIXES (from live screenshots):
- ~~Enter still passes the OLD build screen~~ DONE (2026-07-02): BEGIN now marches straight to the
  CityMap (`Redeploy` + Run screen) — no build gate; the fixed kit keeps the bar non-empty.
- ~~Equipment must be the FULL screen, not the LOADOUT popover~~ MOSTLY DONE (2026-07-02): popover
  DELETED; E / the EQUIPMENT button opens the real Equipment screen; Esc returns to the caller. In a
  run the screen reads the LIVE state (run body/reservations, run bar, run minions) and card clicks
  power/unpower on the campaign; pre-run they slot/unslot the build; rune Climb stays pre-run (mid-run
  rune mutation is design-open). RESIDUAL: an Encounter button (disabled in combat) + a CampaignMap
  button need manifest elements (Needs-CD); a drawn BACK affordance likewise (Esc carries it).
- **AUTO-ATTACK button isn't wired** — believed ALREADY WIRED at the Encounter cut-over (slice 14:
  the combat.autoAttack element click toggles the one global AUTO, and its label reads
  "AUTO-ATTACK ON" when lit). If the screenshot predates that build, re-test; else report repro.
- ~~Resource-count readout top-right~~ DONE (2026-07-02) for supplies / gold / charge (the drop's new
  charge icon renders) on Encounter + CityMap + Equipment, InRun-gated. SUMMONS joins when its §9
  resource model lands (not yet built).
- ~~War-party indicator~~ DONE (2026-07-02): camp token LEFT, castle RIGHT, the covered-ground fill
  loads LEFT→RIGHT, and the host token slides right→left toward camp (it previously moved AWAY from
  camp — inverted fraction). The manifest doom bar (icons already camp-left/castle-right) matches.
- ~~Targeting reticles don't sit on the foe's body parts~~ ENGINE DONE (2026-07-02): reticles anchor
  on the figure's ACTUAL limb rects via the manifest transform (`FigureBinding.StatOf` + a screen-rect
  union, both arms as one group) — AIMING centres on the hovered part while picking, FOCUS marks each
  locked part-aim (verified on the aimed head). Band strips remain the click hit-test. REMAINING
  Needs-CD: the demonstrative "how-it-mounts" design screens to pixel-match against.
- **SHIELD BAR + active-shield BUG:** shields are ALWAYS PASSIVE (reserve-the-stat) — an active-cast
  shield ability is a BUG; make shield sources passive-only (§6b). Add a **SHIELD BAR**: PIPS
  (filled/empty) + a per-pip **REGEN progress bar** (time-to-next-pip) while the source is active, wired
  to `ShieldPool` (layers + CON-scaled regen). Pool scales beyond 4 → render a VARIABLE N of pips
  gracefully. CD designs the bar (best-stab, Doug refines tomorrow); loop wires it.
- ~~CityMap start node = CAMP~~ DONE (2026-07-02): `NodeType.Camp` end-to-end — the origin authors as
  Camp, fog always shows it (your own origin), re-entering it spawns NO fight (safe ground, like the
  merchant), and the chart blits the camp token. Core-tested (297 green).
- ~~Friendly/self vs enemy TARGET-SIDE bug~~ DONE (2026-07-02): `Technique.Side` {Enemy|Self} added
  (Bandage declares Self — its heal already auto-picks the caster's most-damaged part at discharge);
  `IsPassive` derives from ShieldLayers>0 so a source can't be authored active. CardPress now: a powered
  SELF tech never enters targeting; a powered PASSIVE source toggles OFF (§6b) — never the FSM.
  Core-tested (296 green). The SHIELD BAR UI waits on CD's bar design.

RENAME **apex → Core Effect** — CODE DONE (2026-07-02): `CoreEffectName/CoreEffectDesc` + all code
comments/tests swept (keystone "apex-tier" untouched). REMAINING: the manifest's `apex*` bind names are
CD-owned — the Game resolver keeps matching them until CD renames the manifest side (flip the resolver
keys in the same pass); docs (STATUS slice notes / spec prose) sweep with CD's rename. [DESIGN_SPEC §17 #14]

MINION RESOURCE = **Summons** [LOCKED §9/§14] — BUILT (2026-07-02): Caster carries MaxSummons/
SummonsLeft (Forge sizes runs at bays+2, placeholder; bare test casters stay unlimited); a FRESH summon
spends 1 (uniform across gates) + its gate; a drained gate stat now IDLES the minion (stays summoned,
silent, holds its bay) and it re-raises FREE on recovery — the old drain-dismiss cascade + its test are
superseded. Merchant stocks summons (seeded x1-2, M key) + the in-run readout shows summons N/M (no
icon authored — Needs-CD). Core-tested (300 green). REMAINING: minion DEATH (no HP model yet — killed
re-pay untestable) + the Summoner Core Effect refund (next).

MERCHANT: mechanics SPEC'd (§12). PARTIAL (2026-07-02): resource stock (seeded per node — supplies 1-3,
charge 1-2, capped top-ups, placeholder prices flagged to the economy tune) + the 1-HP buy + the premium
FULL-heal are BUILT + Core-tested (298 green) + wired into the stopgap popover (H/F/S/C). REMAINING:
Summons stock (needs the §9 resource model). The 5-CATEGORY gear stock MODEL is BUILT (2026-07-02):
`MerchantStock.Roll` — seeded/reproducible, ≤4 of 5 sections, 3 picks per section (pool-capped),
techniques always-5-when-present + 2nd-rarest, rank-2 runes rarer, keystones NEVER; section weights
placeholder (shuffle algorithm OPEN §17). Shape Core-tested over a 400-seed sweep (300 green).
WIRED (2026-07-02): Expedition rolls the stock ONCE per merchant node from its seed (GearSalt) —
weapons/armor buy from the roll (purchases consume it; the static Shops lists are retired from the
merchant path), and techniques/minions/runes are OFFERED (exposed lists) but not yet buyable — their
receiving models (mid-run palette/bay/rune mutation) stay the design-open gate. The standard map's
"b" merchant deterministically stocks dagger+plate so the existing buy/equip tests hold unchanged. The SCREEN LAYOUT waits on a CD design PNG (popover = flagged stopgap).

**AFTER HiFi completes/blocks + all outstanding identified bugs resolved:**
- **Summoner CoreRune — real Core Effect:** on Redeploy, refund Summons for SURVIVING minions (§11). The
  first real Core Effect; build it.
- **CONTINGENCY (only if HiFi work is exhausted):** refactor the uber `Game1.cs` — split by responsibility
  (SRP), codify SOLID where it sensibly applies. The giant class/file is the target.

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
   Race-column pixel-walk (2026-07-01, live vs design/05): positions/values MATCH the manifest; the
   residual deltas are (a) text ~1.5x wider than the mock — the SYSTEM display font is wider than the
   design font at the same fontPx (fixed by the bundled-font swap, tracked in Verify mechanics), and
   (b) the known Needs-CD chrome: no card bg/panel part, no whole-card CHOSEN ring (only the chip is
   in the template), race.headImage draws its gradient slot but head sprites don't exist, ✓ folds.
   No renderer-side bugs found in the walk.
2. **Equipment** — CUT OVER: live screen renders via `DrawManifestScreen("equipment")`; the hand-drawn
   build screen (core selector/core block/palette/loadout strip/anatomy tags) is DELETED. Core switching
   is REMOVED from this screen per design/02 (core is fixed between fights — that choice is NewGame's).
   Input by binds: tab strip picks GEAR/TECHNIQUES/MINIONS (`_invTab`, render-side state); TECHNIQUES
   tab lists the palette as inv cards (badge stat+reserve, name) — click slots/unslots; clicking a
   slotted action-bar card unslots; keys 1-9 still toggle; Enter marches (the design's READY TO MARCH
   chip was flattened into the status strip by extraction — Needs-CD; Enter carries the march).
   Live binds: paperDoll figure, attr bars (§6 part labels, free/cap, positional pips), loadout/minion
   cards, core identity, rune budget. RUNE BAG LIVE: one group per PATH ladder (MARKS/PATHS/KEYSTONES
   taxonomy stays OPEN §17 — the model's ladders render); each group shows held-rung + next with
   `Mark.Name` display titles (Vessel Seal I/II, Hollow Vessel, ...), a data-derived effect line
   (sockets/grants — can't drift), live EQUIPPED/EQUIPPABLE/LOCKED state + discounted cost; clicking a
   group card Climbs the ladder (budget-gated in Core). Group header label + row icon/stack are
   bindless in the template (Needs-CD). GEAR tab now lists the RUN's gear (wielded + packed weapons,
   armor; EMPTY pre-run — gear only exists once marching); rarity chip stays gated (no rarity model).
   Not design-done: chrome/label gaps in Needs-CD.
3. **Encounter** — CUT OVER: the fighting screen renders `DrawManifestScreen("encounter")` over the
   battlefield backdrop; only the cleared/lost overlay stays legacy (not part of design/01). Combat
   input reads manifest geometry by binds: card cells press/right-press, the foe hit-box + limb bands
   ARE the foeFigure element, AUTO-ATTACK/FLEE/HELD chips click (labels authored in the shell — the
   design chips were flattened by extraction, Needs-CD). Fielded minions draw as sprites in the
   minionField element. Deleted: DrawFighter/AttributePool/Pips/Foe/Bays/Support/ActionBar + hot/
   toggle buttons + hand rects (~10 helpers). LOST at cut-over (manifest lacks the element — Needs-CD/
   design): per-card AIM TAG (F1:H read), banked/rallied SUPPORT pips (rally lane already in Debt).
   Live binds (all verified over a live castle fight): hero/foe figures + HP, pool rows w/ live
   reservations, technique/bay cards (stat glyph tiles, costs), FSM state chips (DRY/HELD/READY/
   COOLDOWN, idle = no chip), cooldown labels, charge-progress fill widths, bay ACTIVE.
   AIMING state done: the card the targeting FSM is picking for reads an AIMING chip + "locking on"
   label (design/01's Firebolt card), verified live via the encounter smoke's targeting card.
   Gaps: techCard's bindless sample parts (the cost NUMBER + the mid-description damage digit) stamp
   their sample on every card (Needs-CD: bind them).
   Pixel-walk vs the NEW hi-res design/01 (2026-07-02): positions/values match the manifest; deltas are
   the known chrome family PLUS two fresh Needs-CD items — `poolRow` has NO pip parts (the design's
   per-stat pip strips can't render; `ui/pip/*` assets exist), and the re-dropped manifest LOST the
   "Attribute Pool"/"Action Bar" panel titles that design/01 shows (they were elements before the
   re-drop). No renderer-side bugs found. Equipment walked vs the new design/02 too (2026-07-02):
   clean — same known gaps; one family addition: attrBar pips are FLAT FILL parts where the design
   shows the textured `ui/pip/*` states (hatch/reserved) — pips-as-imageBind would close both screens.
   DISPLAY COPY DONE: `Technique.Desc`/`Minion.Desc` (+`DescText`) ship card copy for all 8 techniques
   + both minions; `{power}` resolves from the data at render so copy can't contradict tuning. Bound
   via technique.description/bay.description; Core-tested (290 green).
4. **CityMap** (in progress) — `design/03`. THIS PASS: the manifest `chart` graph element renders LIVE
   via `DrawManifestGraph` (GraphLayout spread over the element region): fog-aware beacon icons,
   charted solid / uncharted dashed links, the current node ringed "you are here", reachable
   deployments numbered. Verified RB_MF=citymap at a live merchant node. Supplies/support/doom/legend panels render
   their manifest chrome (inner pips/rows were flattened by extraction — Needs-CD).
   REMAINING for cut-over: map input on manifest geometry (node clicks); a home for the merchant panel,
   gear bar + EQUIPMENT button (design/03 shows none of them — surface where they live, Needs-CD/human,
   before deleting the legacy citymap screen).
5. **CampaignMap** (render started) — `design/04`. The manifest screen renders: header/eyebrow,
   `campaign.taken` ("N / M" from the live campaign), and `cityGraph` draws one marker per campaign
   LEG spread across the region (taken = solid good links, current framed, onward dotted) with
   "Tier N - TAKEN/CURRENT" labels. City NAMES stay undrawn — §12/§17 leave the city roster OPEN
   (count, procgen-vs-authored), so no names are invented; the design's castle icons are absent from
   the cityNode template (Needs-CD). MODEL gap: §12 Layer 1 locks a forward-biased city GRAPH but
   Campaign is still a linear leg list — graph model + campaign-level supplies/WAIT is its own Core
   pass (surface before building). No screen-flow entry yet (not reachable in game; RB_MF only).
FLOW [FIX — still wrong today]: (1) BUG — Enter still passes through the OLD build screen; NewGame must go
STRAIGHT to CityMap, no build gate / no Enter-through. (2) Equipment must open as a **FULL SCREEN**
(design/02) — KILL the redundant "LOADOUT" popover; route ALL access to the real screen. (3) Hotkey **`e`**
(was `i`) + an "open Equipment" BUTTON on Encounter (DISABLED in combat), CityMap, CampaignMap; a
**BACK/close** on Equipment returns to the CALLER (CityMap already shows EQUIPMENT [E]; add Encounter +
CampaignMap). (4) MERCHANT is an IMPROVISED un-designed POPOVER stopgap — the heal+gear-shop MECHANIC is
designed (§12/§14), the SCREEN is not; spec with Doug + a CD design PNG before building it (design-open,
DESIGN_SPEC §17). Do NOT expand the popover as if it were the design. See DESIGN_SPEC §12.
IMMEDIATE small tasks (not CD): the **FONT task** below — Doug's download approval or a manual TTF
drop into `Roguebane.Content/fonts/` is the gate. (core.icon bind + mgcb icon entries: DONE 2026-07-02.)
**FONT task (ours, no CD — fixes the "wrong font" AND the "?" glyphs in ONE move):** `display.spritefont`
+ `mono.spritefont` still name SYSTEM **Consolas/Georgia** with an **ASCII-only** char region
(`&#32;`–`&#126;`) — that's why every screen shows the wrong font AND why ①②③ ✚ ◉ ✓ render "?". Bundle
the DECIDED fonts (**IM Fell English** display + **JetBrains Mono** mono — free/open; add the `.ttf` to
Content), point each `.spritefont` `FontName` at them, and widen `CharacterRegions` to include
①②③(U+2460–2462) ✓(2713) ✚(271A) ◉(25C9) (+ accents for i18n later). Bake an icon only for a glyph the
font truly lacks. (Supersedes the old "swap fonts before distribution" needs-human note.)

ENGINE TODOs reconciled from CD's gap list (2026-07-01) — NOT already covered above:
- **`imageBind`** — DONE (2026-07-02, CD #15): `TemplatePart.ImageBind` parses (contract-tested), the
  list renderer resolves `{bind}` placeholders per datum generically, and the map graph blits the
  fog-aware node token through it — skirmish ⚔ renders live; the "unknown ?" stopgap mapping retired.
- **Button 9-slice corners** — DONE (2026-07-02, CD #11): DrawButton nine-slices the 320×88 skins
  (was stretch-scaled); source margins 12px, destination corners land at 12 TARGET px via NineSlice's
  new `dstCornerScale` (1/SS for 2x art) so rivets stay native and a 25px-tall button keeps its face
  (Core-tested). If CD meant 12 DESIGN px corners instead, flip the scale to 1 — one constant.
- **Container overflow/scroll** (CD #4): DEFERRED (2 races / 6 cores fit today) — add a scroll/page
  primitive when a list actually overflows; tracked here so it's not lost.
- **Confirm-to-close (lets Doug clear CD's entries):** on the relevant cutovers, confirm `cityNode` reads
  `[8,8]` (CampaignMap, CD #12) and the 5 full-bleed HUD bars ship WITHOUT a `frame` (CD #14 regression fix).
- NOTE: CD #1+7 (shadow/frame/gradient DRAW) is STALE — the foundation renderer already draws them (§10
  order). Frame-v3 tile/repeat + centerFill (CD #16) — DONE 2026-07-02 (NineSlice tiles edge/centre
  patches at native scale, trailing chunk crops 1:1; centerFill:false leaves the middle open;
  Core-tested). Only a gradient-interpolation check remains; CD can close #1+7's basic-draw.

## Verify mechanics
- `RB_MF=<screenId>` renders a screen STRAIGHT from the manifest (safe — live screens untouched): the
  cut-over dev hook. `RB_SMOKE=1 RB_SHOT=x.png RB_SCREEN=<newgame|equipment|encounter|citymap> dotnet …`
  renders a LIVE screen to a PNG headless.
- A RUNNING game locks the default build output → build to a SCRATCH dir and run THAT instance.
- On ANY crash READ `bin/Debug/net9.0/crash.log` (Program.cs writes the full exception + stack).
- SpriteFont CharacterRegions now include ①②③ ✓ ✚ ◉ (+ DefaultCharacter "?") — the state chips render
  real glyphs on the system fonts (crude at small sizes; the bundled-TTF swap improves them).
  `Core.GlyphSafe.Sanitize` still guards anything outside the regions. TTF swap awaits Doug's
  download approval (IM Fell English + JetBrains Mono).

## Debt (active — with reconcile trigger)
- Equipment: no inventory tabs (GEAR/TECH/MINIONS) + drag-to-equip + equipped-gear-on-anatomy + real
  rune-bag cards yet — blocked on gear/minion equip + mid-run gear/rune mutation (design-open). MODEL now
  refined (DESIGN_SPEC §7): a Core rune gives STARTING gear (does NOT lock it), gear is swappable;
  MULTI-SLOT pieces (robe = all slots); figures MORPH (human base + race + core + equipped-gear parts) —
  author morph layers, not per-combo art. Exact morph/slot mechanics OPEN (§17); feeds this when built.
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
- FTL-ism **"jump"** (the beacon-chart verb) — canonical terms are **deployment** (the move/unit) +
  **Redeploy** (the action), **Retreat** (bail a fight). STOP THE BLEEDING: introduce NO new "jump" in any
  text/identifier. UI strings + the two canon docs are fixed; residual "jump" lives only in CODE COMMENTS
  + test prose — LOW-PRIORITY sweep, DEFERRED until the hi-fi render arc is confirmed complete. ("beacon"
  is the other FTL-ism — embedded in the `beaconNode` manifest template + comments; flagged for a
  decision, not auto-changed. "FTL" in comments is an allowed private design ref, leave it.)

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
- Skirmish node icon — CD adding it (the ⚔ marker on b1 + a legend row, ~8px taller: an APPROVED intended
  CityMap delta, not drift). Deliver as a node ICON PNG (`icons/node/skirmish`), NOT a font glyph.
- wraith + gargoyle: ART-QUALITY completion ONLY — NOT a uniform 21-part scheme. Robe figures legitimately
  have ~12 parts (§1 omits torso/legs/boots; adept/summoner are also 12); gargoyle's `back` is contract-
  legal. The renderer composes each figure from its OWN z-list — never assume 21 (fix any consumer/test
  that hardcodes it).
- HI-FI CHROME pass on all 5 screens + backgrounds at 1080 (SENT + in progress at CD). NEXT levers
  (tiled 9-slice EDGES + painted CENTERS) SMEAR under the engine's current STRETCH blit — needs a
  **TILE/repeat mode added to the nine-slice blit (engine/Fable task)**. APPROVED: CD paints the full-
  fidelity tiled edges + painted centers NOW; the engine adds tile mode to CATCH UP (principle: design to
  the IDEAL, engine catches up — LAYOUT_CONTRACT §2). A bounded renderer change, part of the chrome arc.
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
