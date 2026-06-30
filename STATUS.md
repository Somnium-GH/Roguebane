# Status

## Current target
**RE-OPENED by play (human): starting a run CRASHES, and screens don't match the NEW design renders —
POC is NOT complete; the "DONE" claim below is RETRACTED until these clear.**
- TOP — RUN-START CRASH: FIXED (confirmed via crash.log). Root cause: `DrawGearBar` drew an em-dash `—`
  into the ASCII-only mono font (SpriteFont THROWS on unknown glyphs) on the MAP screen. Fixed
  CENTRALLY — `Game1.Text` now runs `Safe()` (maps common typographic chars → ASCII, replaces anything
  else the font lacks), so NO drawn string can crash again; the em-dash literal is also gone.
  `Program.cs` keeps writing `crash.log`. The fold-to-ASCII guard is now `Core.GlyphSafe.Sanitize`
  (pure, engine-agnostic) with 7 headless tests (245 Core tests); `Game1.Safe` is a thin caller that
  feeds it the font's glyph set — the crash-class is regression-covered headlessly. Non-ASCII drawn-
  literal sweep CLEAN: the only remaining drawn non-ASCII was DrawLoadoutStrip's "-" placeholder (was an
  em-dash) -> fixed; every other non-ASCII in source is comment prose (never drawn). Driven run-start
  STATE regression added (RunStartTests: NewBuild -> March -> assert the map-screen render contract at
  camp/Choosing -- state, options, player HP, resource readouts, bay/loadout/gear sources; 247 Core
  tests). LIVE-DRAW VERIFIED (crash item DONE): a running Roguebane.Game locks the default build output,
  so build to a SCRATCH dir and run that instance instead --
  `dotnet build Roguebane.Game -o <scratch>` then from <scratch> `RB_SMOKE=1 RB_SCREEN=<map|build|combat>
  RB_SHOT=x.png dotnet Roguebane.Game.dll`. All three render clean, exit 0 ("SMOKE OK") -- the run-start
  crash is gone in the live draw, not just headless.
- Fresh design renders landed (`design/01–06`, 06-30) and are now COMMITTED as the rebuild reference.
  Audited each live shot vs its PNG (scratch-dir smoke). Punch list:
  * 03 MAP: top-left SUPPLIES + MUSTERED-SUPPORT panels (pip bars + flavor) DONE + smoke-verified
    (supplies/support moved off the compact top-bar; war-party track relocated to clear them). REMAINING:
    the BIGGER beacon chart (design wants a denser graph vs the current 5-node diamond) + a right-side
    "THE CASTLE — exit" panel. Both land cleanly via the manifest `type:"graph"` rebuild below.
  * 02 BUILD: matches except the already-known DEFERRED items — INVENTORY tabs (GEAR/TECH/MINIONS) +
    rarity item cards, and a RUNE BAG of MARKS/PATHS/KEYSTONES cards (current screen shows rune LADDERS).
    Both input-coupled (need input wiring + mid-run stash).
  * 01 COMBAT: divergence from design/01 is a LOCKED decision (s13 multi-foe layout) — NOT a defect.
  * 05 NEW RUN: a dedicated "Choose Your Core" 5-card grid (figure + stat block + flavor + SELECT, one
    ringed, BEGIN THE MARCH). Build merges this into the BUILD screen's inline selector — rebuild it off
    `screens.newrun` + `design/05` (single-core for now; race step behind the flag).
  * PALETTE: NOT a uniform shift — 05 reads warm-dusk, 02/03 read cooler/navy; renders vary, so leave
    the palette as-is (warm-muted-dusk, DESIGN_SPEC §13) until a palette decision actually locks.
- MANIFEST validated COMPLETE (human review of the full 3305-line `layout.json`; parses clean — an
  earlier "truncated" read was a stale mount, ignore). runmap/campaign/newrun are now RICHLY spec'd
  (templates coreCard/legendRow/beaconNode/cityNode, `type:"graph"` containers + `anchor:"nodePoint"`,
  bundled open fonts) — rebuild those 3 screens FROM the manifest; they're no longer stub-blocked.
  Consumer must learn `type:"graph"` (place nodes from map/campaign data in the region) + `nodePoint`
  (node-relative parts) + IMAGE PARTS (done: `TemplatePart`/`PlacedPart` now carry an optional `Image`,
  so a card part can be a figure sprite instead of text; CardTemplate schema test accepts sample-OR-image).
  MODEL HALF DONE: `Element` now parses `content` (literal text) + `item` ({template, flow, gap, size} for
  list/graph containers) -- pinned by LayoutManifestTests against the real manifest (chart=graph/beaconNode,
  build action bar=horizontal/techCard, newrun=coreCard). RENDER half remains: the Game consumer must
  iterate map/campaign nodes (graph) + bound lists and STAMP the item template at each cell (CardTemplate
  already places a template at an origin; wire data -> positions).
  cityNode is labels-only; castle stays the generic node icon (procedural castle parked — not
  mission-critical). REVIEW FIXES:
  * coreCard figure is HARDCODED (`image: …/chassis/grunt.png` sample) — BIND each card's image to its
    OWN core's figure, else all 5 New-Run cards show Grunt.
  * Decorative glyphs `✦ ◉ ✓` render as `?` (GlyphSafe maps `· — →` but not these). ADD the glyphs the
    design uses to the SpriteFont character regions so they render; keep GlyphSafe as the safety net.
  * Normalize element `type` (some images/panels are typed `"text"`, e.g. combat `backdrop`) — or ensure
    the consumer keys off image/fill/item presence.
  * PARKED (revisit later): two literal `content` subtitles (campaign/newrun) are truncated in the
    manifest — the text itself may be outdated, so not worth an extractor fix yet.

## Prior integration record (the "DONE" claim below is RETRACTED per the re-open above)
**Shell wired to `layout.json` — integration DONE; combat layout RESOLVED (locked, see s13). POC functionally complete; only the low-value Equipment inventory-tabs polish remains (deferred).**
Core manifest toolkit COMPLETE + pinned: `Layout/` LayoutManifest (parse), StageComposer + FigureBinding
(figure assembly), ScreenLayout (anchor->rect), PaletteColor, CardTemplate. Game consumes it via
`LayoutRegistry` + `ManifestUi` (tolerant ElementRect/Color). Figures (player + foes) compose from the
manifest; chassis figure key threaded through assembly (Expedition/Campaign.FigureId). Clean content
build certified from scratch (226 Core tests, all 5 screens smoke clean, no stale-.xnb).
- Four stale-asset bugs fixed (foe sprite, icon maps, pips, rune path); full asset-string sweep clean.
- design/02 build: attribute-readout bars + anatomy stat callouts. Gear-on-figure draws the actual
  wielded weapon. design/03 map: node legend + supplies remaining/max + support banked/holds.
- Combat: foe figure variety (ogre/troll/bandit/skeleton) + name tags; minion sprites in the bay lane;
  header titled by node type (SIEGE/SKIRMISH/RESOURCE HOLD).

VERIFY loop: `RB_SMOKE=1 RB_SHOT=x.png RB_SCREEN=<build|combat|map> dotnet run --project Roguebane.Game`
renders a PNG + exits (headless); add `RB_CHASSIS=<0-4>` to pick a chassis on the build screen. Smoke
every visual change. GOTCHA: spritefonts are ASCII-only — a non-ASCII glyph in Text() THROWS at draw.
All 5 chassis figures + 3 live screens verified clean (no overlaps/clipping after the layout sweep).
All usable foe figures confirmed too: ogre/troll rendered in castle combat; bandit/skeleton render by
structural equivalence (identical 7-part/21-file layout). wraith/gargoyle stay excluded (incomplete art).

REMAINING (deliberate / not safe 1-min fragments):
- COMBAT exact design/01 bottom-panel rebuild: SUPERSEDED by a locked decision (DESIGN_SPEC s13) — for
  1-3 foes, large foes (clear limb-band PART-aim, the core mechanic) beat a prominent bottom attribute
  pool; combat keeps its working hand-placed vertical-spread layout (foes large, pool in the YOU panel),
  which is the canonical MULTI-FOE layout. design/01's bottom-dominant pool is the single-foe ideal.
  So combat is DONE for multi-foe; the only open combat item is a future 1-foe mode (would revisit) and
  the battlefield minionField figure (still parked — its manifest slot overlaps the working lanes).
- EQUIPMENT screen (design/02): no screen state yet (only Build/Run). Inventory tabs (GEAR/TECH/MINIONS) +
  item cards, Rune Bag, click/drag-equip — INPUT-COUPLED features needing input wiring + mid-run stash.
- Asset gaps (see section): skirmish node icon, wraith/gargoyle figure art, torso bare-variant (plate
  invisible on the figure), bundled open fonts.

FEATURE-FLAG asset-gated work — `Features` toggle DEFAULT OFF: build FULLY but render NOTHING when off;
flip when art lands. Cases: WAR-PARTY advance UI (war-party token + camp marker); RACE + CORE-RUNE
two-step NEW RUN (race art). Keep the current single-core New Run until race art exists.


## Recently shipped (one-liners; full detail in git log)
Combat thesis loop is whole and tested (226 Core tests). Highlights — all pinned + RB_SMOKE-verified:
- Targeting/firing FSM (requireAim, target-driven fire, one global AUTO; no fire button); foe PART-aim
  via limb bands; shell-input mapping extracted to Core (CombatTargeting).
- Minions fight (auto-summoned to bays); combat lanes (bay lane, rallied-support "RALLIED +N").
- Gear END-TO-END (Core): merchant buy -> Stash pack -> equip honoring gates; leather evasion; Body.Unequip.
- Build attribute readout + gate markers; bank-on-clear; campaign spine strip (linear).
- THIS PHASE (layout-manifest integration): see "Current target" above.

## Locked decisions
- Enemy threat: LIGHT/winnable for now (goal = combat dwell, not difficulty); full envelope = later balance.
- Pacing: fixed ~10 tick/sec clock; cooldowns in seconds (weak 4-6s, strong 12-15s); small dmg; 30s+ fights.
- Randomness: seeded deterministic PRNG (same seed = same run).
- CON→HP: CON = BONUS HP on a natural base, 1 CON = 2 HP; chest dmg drops CON → bonus shrinks → MAX HP
  drops + current caps down.
- DEX = haste: base cooldown from technique/weapon; DEX shortens ~1.5-2%/pt, capped ~28%.
- Minion gating: data {stat | none | alt-cost}; default INT; a chassis/minion may override (ungated
  retinue; HP-cost summon).
- TEMPO/PERIL header: DROPPED.
- Default loadout: FIXED per-chassis kit (data), grown by finds; NO build-time pick gate.
- Targeting/firing: the FSM in Current target.
- Fidelity review: the LOOP self-reviews each RB_SMOKE shot vs the design PNG + design/SCREENS.md
  (automatic every pass); human review ON REQUEST only.
- Per-technique aim. Degradation = GRADED via direct stat-capacity reduction.
- Rallied support: player-allied, UNDAMAGEABLE, intermittent auto-fire ON the castle (NOT enemy repair).
  Castle = boss; HP permanent in-encounter; it may restore its own systems.
- Healing split: potions/magic restore PARTS (never HP); HP only out-of-combat (shop/quest).
- War party: marches on CAMP 1 step/move; crack the castle disbands it; reach camp = supplies cut = lose.
  Instant-loss now; camp last-stand later. (DESIGN_SPEC §12)
- Roguebane.Content is TRACKED.

## Attribute model (one part = one stat; integer-only)
- STR (Arms): attack power 1.0x. Gates STR weapons; equips shields (heavy = STR).
- INT (Head): spell power; keeps spell actives + passives up. Absorbs old WIS.
- DEX (Legs): evasion; accuracy; +0.25x attack (DEX/4, int); HASTE (cooldown −%/DEX). Gates DEX weapons.
- CON (Chest): bonus HP (1:2); stun resist; the DEFENSIVE stat — powers the CON "block" source and scales
  SHIELD-POINT REGEN (all sources). STR still wields the heavy shield OBJECT; CON powers its block. Shield
  model = the Phase 3 SHIELDS revamp (passive stat-reserving technique with regenerating 1-dmg points),
  NOT a flat cap. Gates chassis-extending runes.
- CHA dropped; WIS → INT (5 → 4).
- ARMOR = light effect layer, NOT attribute gear: plate → flat 1-4 protection vs the stat-dmg to its part;
  leather (DEX) → evasion; head spell-armor → spell/blind protection. Rides the part's condition (break
  part → effect gone). Weapons/shields gate on their stat to wield.
- Parts: Head x1, Chest x1, Arms x2, Legs x2. Paired parts take damage independently, each carries a SHARE
  of the stat (one arm = ½ STR). Armor one piece per part-group; weapons in hands. Lose a limb → lose its
  stat share → gear can fall off.
- SCALE: low ints; ~20 = huge; damage/heal in 1-3; a hit subtracts 1-3 from the targeted part's stat.
- CORE MECHANIC: part damage subtracts that part's stat from the live pool until repaired — unifies graded
  degradation, equip/ability fall-off, and the allocation economy.

## Needs human (loop skips these)
- Balance/feel tuning (placeholder-sane, tune in play): tick 10/s; cooldowns + damage; DEX haste 2%/pt
  cap 28%; CON→HP 1:2 + base 8; evasion %; shield amounts/regen; armed-foe HP/strike; castle cadences;
  budgets/spoils/prices; supplies vs march length.
- Five chassis stat blocks (design/05) are placeholder — tune later.
- Part→stat friction (legs = accuracy, arms = STR) — low-pri revisit only if it nags.
- Fonts: SpriteFonts use system Consolas/Georgia — swap to bundled open fonts before distribution.

## Locked this round (were Needs-human)
- MULTI-FOE COMBAT LAYOUT — LOCKED (DESIGN_SPEC s13). Large foes (clear limb-band PART-aim) over a
  prominent bottom attribute pool; current vertical-spread layout is canonical for 1-3 foes. design/01's
  bottom-dominant pool = single-foe ideal. Loop-decided given the part-aim tradeoff; revisit for a 1-foe mode.
- FOE→PLAYER PART aim — SHIPS. Foes erode player PARTS (HP-vs-stat default accepted: a hit eats the
  targeted part's stat; HP only via penetrate/overkill). WHICH limb = a per-foe TARGETING PERSONALITY,
  as data: SMART (best for its build) | RANDOM | INEPT (botches a good pick). Localize CON-block/evasion
  onto part hits here.
- WAR-PARTY indicator — a TOP-edge track, castle (right) → camp (left), advancing each move (try top first).
- Armor one-per-group + weapons per-hand — LOCKED. Fog reveal rules — LOCKED.
- Campaign topology + shields — revamped this round; see Phase 3.

## Asset gaps (Needs Claude Design)
*Loop logs here when a screen needs ART that's missing/wrong in Roguebane.Content and can't be composed
from primitives. Route each to Claude Design. (Art direction: DESIGN_SPEC §13.)*
- Skirmish node icon: removed by the drop with no replacement; map renders the `unknown` "?" as a
  stopgap (label disambiguates). Needs a dedicated combat-node icon.
- Foe creature variety: DONE for ogre/troll/bandit/skeleton (per-encounter assignment in Sieges —
  raiders bandit/skeleton, castle ogre/troll). STILL UNUSED: wraith (PARTIAL art — only 12 of the
  21 part files, renders with gaps) and gargoyle (24 files, nonstandard part layout) — both need art
  completion/normalization before wiring.

## Debt (active — with reconcile trigger)
- BUILD screen attribute readout + gate markers DONE. Still lacks inventory tabs (gear/tech/minions) +
  drag-to-equip and equipped-gear on the anatomy. Blocked on gear/minion equip (G2/G7).
- Combat surface: PART-level aim UI DONE (limb bands + part-aim); minion-bay lane DONE. Still no
  rallied-support lane.
- Part-group EVASION already localizes on PART hits (Caster.Hit reads EvasionPercent(part)); it is live
  wherever foe part-aim is on. CON block is still WHOLE-HP only — localizing it on PART hits reconciles
  with the Phase 3 SHIELDS revamp (current code is still the old flat block).
- INT beams are fast Timered bolts (Sustained=every-tick was a firehose at 10/s); for a true channel, add a
  per-tick damage-scaled sustained kind. (Speculative — defer until a channel weapon is authored.)
- Leather armor evasion now FUNCTIONAL + content (Shops.Hide) + tested. SpellWard still deferred (no spell model).
- Mouse is click + hover only — no drag-to-equip, tooltips, rebinding; PAUSE/FLEE are plain rects (U6).
- Rune grants = chassis-extension PARTS only; add more data-driven Mark effect kinds when a non-extension
  keystone is authored.
- G1: foe->player PART aim MECHANISM shipped + headless-tested (FoeTargeting SMART/RANDOM/INEPT; Foe.Aim
  data; Battle wires it via the Encounter.FoePartAim opt-in). STAGED OFF in all live content
  (FoePartAim=false): persistent part erosion with NO part-heal yet (Phase 3 #4) strips the loadout and
  makes the run unwinnable (verified — campaign flips to a DPS stalemate). RECONCILE: flip FoePartAim on
  per-encounter once part-heals ship; authored personalities already sit dormant in Sieges. Real content
  foes still unarmed beyond the light feel-pass arming (power envelope = human).

## Roadmap
- Phase 1 [DONE]: Core skeleton, rune economy, chassis, techniques + deterministic combat tick,
  enemies+castle, the attribute rework, chassis→body wiring, combat migration onto Body, balance-sim.
- Phase 2 [ACTIVE]: UI U1-U6 built (combat/build/map/spine/chrome; fidelity continuing via self-review).
  Gameplay G1-G8 built or partial (multi-part foes, gear/weapons + plate armor, 5 chassis, node-map run,
  war-party, campaign spine, data-driven runes/minions/magic, economy). Remaining actionable work = the
  Debt above + the targeting FSM fix. Visual truth = design/ PNGs + design/SCREENS.md; look = DESIGN_SPEC
  §13. Keep "FTL" out of shipped UI text.

## Phase 3 — combat depth + Race/CoreRune rename [SCOPED, not started]
Big slice after the current combat polish. Do it in SMALL /loop slices. Reconcile DESIGN_SPEC sections
DURING the phase with SURGICAL edits (do NOT rewrite/clobber). LOCKED below; OPEN parked at the foot.

LOCKED:
- SINGLE-ENEMY combat: always vs ONE enemy (it may be multi-PART: a human foe, an atypical creature, the
  castle, or a special resource fight). DROP multi-foe / multiple targets entirely — the only targeting is
  PART aim within the one enemy. (Simplifies the front-target machinery.)
- RENAME "Chassis" -> RACE + CORE RUNE (the real names; like FTL ship + layout):
  * RACE = starting attributes + base HP. Start with Human + Elf (more later).
  * CORE RUNE = LAYOUT (rune budget, # techniques, # minion bays) + apex effects/bonuses (stronger than a
    keystone). RACE GATES which core runes it may take (SB-style restriction matrix).
  * New Run = pick RACE -> pick CORE RUNE (race-allowed). Today's 5 "chassis"
    (Grunt/Warden/Adept/Summoner/Reaver) BECOME core runes; Race is the new orthogonal axis.
  * Code: rename Chassis -> CoreRune everywhere; add Race; split Chassrium into Races + CoreRunes.
- LONG COMBAT by design. Most builds carry a DEFENSIVE SOURCE (shield / stoneskin / DEX-evasion); dropping
  it = high hit odds. A build with NONE must compensate (cheap/near-instant part-heals, or high damage +
  evade/CON + frequent heals).
- ENEMY HP high but SCALES with tier so early play keeps pace; ~1/4–1/3 damage taken is typical when unspecialized.
- HEALS standard — they repair PARTS, never HP ("repair systems"): Potion (item; strong ones rare),
  Bandage (CON ability), Cure Wounds (INT spell). Starting healless < starting shieldless in penalty.
- SHIELDS (revamped): a shield SOURCE is a PASSIVE TECHNIQUE that RESERVES its stat in the bar and is ON
  by default (most builds carry one for survivability); toggleable off in combat to free the stat. It
  maintains a POOL of SHIELD POINTS (FTL layers, no hard cap) — each absorbs 1 damage and is CONSUMED on
  hit; points REGENERATE on a timer scaled by CON (+ rune effects). The SOURCE sets amount/regen + its
  stat: block (CON), stoneskin/barkskin/steelskin/diamondskin (INT), parry (DEX, low cap), bind (STR, low
  cap), shield-wall (CON, scales with troops). Every class should have a viable block source. (Supersedes
  the old "flat while held" CON-block.)
- CAMPAIGN MAP: a forward-biased city GRAPH (NOT a fixed linear list). Movement is free-ish but generally
  forward, GATED by SUPPLIES (deplete as you move). Run dry → you must WAIT; each waiting turn has a CHANCE
  of an encounter (may resupply — affordable, free, or useless). The enemy war-party advances the whole
  time toward your camp + capital; reaching them = LOSE. Unblocks the design/04 branching screen.

OPEN (park; decide as the phase starts):
- Healless compensation archetype(s): heal-spam vs damage-tank — define.
- "Ready to block Nx faster" mechanic (the healless trade) — design it.
- Shield damage caps per source + actual numbers (relative only so far) — balance.
- CON as a MINION resource (CON funds minions -> a no-INT cleric-caster) — floated; ties to minion alt-cost gating.
- Shield Wall troops->layers formula (floor(troops/4) is illustrative) — balance.
- Race roster beyond Human/Elf; full Core-rune roster; the race<->core-rune restriction matrix.

Suggested order: (1) single-enemy + part-aim simplification; (2) Chassis->Race+CoreRune rename + race-gated
New Run; (3) shield-levels system; (4) part-repair heals; (5) defensive-source defaults in starting kits;
(6) enemy-HP scaling. DESIGN_SPEC touch points to reconcile as each lands: §4/§6/§7/§10/§11/§16.

## Layout & assembly system (manifest-driven) [FOUNDATION — do before the fidelity rebuild]
Fixes the EXPLODED figure and makes ALL layout pixel-perfect + viewport-independent by consuming the
generator's emitted coords (contract: `design/LAYOUT_CONTRACT.md`). The generator already computes every
part rect + hand socket; it will now EMIT them (`Content/layout.json` + modular part PNGs) instead of
flattening them away. Gated on Claude Design shipping that; until then build the consumer against the
schema with a small hand-stub manifest, then swap to the real one.
- LayoutRegistry: load `Content/layout.json` (figures + gear + screens).
- Stage composer: draw a figure by blitting its part sprites at manifest `rect` in `z` order, swapping
  part STATE (healthy/damaged/broken) by Core part condition, mounting gear pivots on hand sockets;
  uniform- (ideally integer-) scale the whole figure into its slot, pinned by `pivot`. RETIRE Game1's
  hard-coded `DrawHumanoid` offsets (the cause of the explosion).
- UI from manifest: place every element by anchor+offset+size; RETIRE magic-number rects.
- Viewport (aspect-independent, no bars): background SCALE-TO-COVER; HUD anchored to real screen edges
  (fills any aspect); pixel stage integer-scaled + centered. Only the final transform reads the backbuffer.
- Determinism: position = f(manifest, figure->slot scale, screen). A screenshot matches the mockup BY
  CONSTRUCTION — the loop's visual review becomes confirmation, not guess-and-nudge.
