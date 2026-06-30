# Status

## Current target
**Targeting/firing FSM — DONE (core + shell, incl. global AUTO).** (most shipped, 167 tests.)
Player casters run `requireAim`: a powered technique fires ONLY at its own explicit aim, never falling
back to a front, so untargeted HOLDS. Firing is target-driven (no fire button) — charged+aimed
discharges. AUTO is ONE GLOBAL toggle: ON = no module clears its target after firing (all keep firing at
the same target); OFF (default) = each fires once when charged+targeted, then clears. Shown as a lit/unlit
button (no +/- glyph). AUTO affects ONLY that button — foe/part highlights come solely from active
TARGETING (pick-prompt + limb bands + hover band); no persistent locked-aim ring (which module hits
which foe/limb reads off the card tags, F1:H). Engine casters (foe offense/sim/legacy Session) keep default-front auto-fire.
Shell: per-module controls (left-click inactive=power, active=enter targeting+clear; foe-click=aim+exit;
right-click=cancel/unpower), locked + pick-prompt reticles, targeting card ring, no FIRE button / focus
cursor. Pinned by PlayerTargetingFsmTests (incl. GlobalAutoGovernsEveryModule) + integration; RB_SMOKE OK.
**G1 foe PART aim — DONE (data + shell).** Content foes carry a multi-part frame; the combat surface
splits each foe into anatomical limb bands (head/arms/chest/legs), highlights the hovered band while
targeting, and a band click aims the module at that part (Aim(tech, foe, part)). Locked limbs stay
ringed; card tags read the limb (F1:H). Pinned in FoeArmingTests; RB_SMOKE shows a head-aimed module
eroding the head stat (foe HP untouched).
**Shell-input FSM — now headless-testable.** The targeting click→state mapping is extracted into Core
(CombatTargeting); Game1 only feeds it press intents. Pinned by CombatTargetingTests (9). No behaviour
change (RB_SMOKE identical).
**Minions now fight** (were dead in play): Forge auto-summons the chassis MinionKit + rune grants into
bays at assembly. Summoner ships Skeleton+Shade. Exposed via Exp.MinionCount/Minions. Pinned in MinionTests.
**Combat minion-bay lane — DONE.** BAYS lane paints a slot per chassis bay (filled occupant disc + tag +
power, or empty), from Exp.Minions/Bays. Combat RB_SMOKE (Summoner @ castle) shows 2/3 bays filled.
**Combat surface lanes — DONE** (PART-aim limb bands, minion-bay lane, rallied-support lane "RALLIED +N").
**Build screen attribute readout — DONE** (pips show free/reserved/damaged per stat + gate markers: the
kit's per-stat demand notch + /N, red when over-pool). Inventory tabs / drag-equip / equipped-gear-on-
anatomy still blocked on gear/minion equip (G2/G7).
**Bank-on-clear — DONE.** Resource-holds bank rallied support only when their fight is won (combat driver
via RunMap.BankHold); standalone nav still banks on arrival. Pinned in ExpeditionTests.
**Spine strip — DONE** (design/04 partial): Capital peak marker + cities-taken counter on the linear leg
chain. Full branching city-graph parked (needs a branching campaign model — Needs human).
**G2 gear inventory + equip — DONE (Core).** Stash carries a gear pack; Gearing moves pieces on/off the
body honoring the gates; Body.Unequip added. Pinned in GearingTests (6).
**G2 gear acquisition — DONE (Core).** Merchant sells weapons/armor (Shops stock, placeholder prices)
into the Stash pack via Expedition.BuyWeapon/BuyArmor. The acquire→carry→equip loop is now whole and
Core-testable; only the SHELL surface is missing. Pinned in ExpeditionTests.
**Merchant gear UI — DONE.** Gear stock as buy chips (name+price, dimmed when unaffordable) → BuyWeapon/
BuyArmor. Map RB_SMOKE shows sword/dagger/plate with affordability.
**G2 gear END TO END — DONE.** Buy at merchant → Stash pack → equip out of combat (Expedition equip
passthroughs → Gearing) → EQUIPPED readout + click-to-equip PACK chips on the map gear bar. Pinned in
ExpeditionTests + GearingTests; map RB_SMOKE verified. Remaining: equipped gear drawn ON the anatomy
sprite (a sword on the arm etc.) — minor art polish, not blocking.
**Gear-on-anatomy — DONE** (composed markers: armor rings its part, weapon shows in hand; real gear
sprites = art asset gap). G2 gear is now fully end-to-end.
**Leather armor evasion — DONE** (Shops.Hide; dodge rides part condition; pinned in ArmorEvasionTests).
**The high-value unblocked queue is now EXHAUSTED.** What remains is human-gated, asset, or low-value
polish (one line each):
- Human/design: campaign topology (§04 branching vs linear); HP-vs-stat split → foe→player PART aim
  (whole-HP foe contract pinned by FoeOffenseTests); balance/feel tuning (the whole "Needs human" block).
- Asset (Claude Design): real gear/figure/weapon SPRITES (gear-on-anatomy uses composed markers);
  bundled open fonts (Consolas/Georgia placeholders).
- Deferred/speculative: SpellWard armor (needs a spell/blind model); INT-channel sustained kind (no beam
  content); build-screen drag-to-equip + categorized inventory tabs (click-equip already works).
Other remaining (Debt): build-screen inventory tabs + drag-equip (blocked on G2/G7); Choose-Your-Core
screen design/05 (build screen doubles as picker — locked OK); campaign city-graph design/04.

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
- FOE→PLAYER PART aim — SHIPS. Foes erode player PARTS (HP-vs-stat default accepted: a hit eats the
  targeted part's stat; HP only via penetrate/overkill). WHICH limb = a per-foe TARGETING PERSONALITY,
  as data: SMART (best for its build) | RANDOM | INEPT (botches a good pick). Localize CON-block/evasion
  onto part hits here.
- WAR-PARTY indicator — a TOP-edge track, castle (right) → camp (left), advancing each move (try top first).
- Armor one-per-group + weapons per-hand — LOCKED. Fog reveal rules — LOCKED.
- Campaign topology + shields — revamped this round; see Phase 3.

## Asset gaps (Needs Claude Design)
*Loop logs here when a screen needs ART that's missing/wrong in Roguebane.Content and can't be composed
from primitives. Route each to Claude Design. (Hi-fi transition: design/ASSET_HIFI_BRIEF.md.)*
- (none logged yet)

## Debt (active — with reconcile trigger)
- BUILD screen attribute readout + gate markers DONE. Still lacks inventory tabs (gear/tech/minions) +
  drag-to-equip and equipped-gear on the anatomy. Blocked on gear/minion equip (G2/G7).
- Combat surface: PART-level aim UI DONE (limb bands + part-aim); minion-bay lane DONE. Still no
  rallied-support lane.
- CON block + evasion mitigation are on the WHOLE-HP path; localized on PART hits waits on foe→player PART
  aim (G1). (Both reconcile with the Phase 3 SHIELDS revamp — current code is still the old flat block.)
- INT beams are fast Timered bolts (Sustained=every-tick was a firehose at 10/s); for a true channel, add a
  per-tick damage-scaled sustained kind. (Speculative — defer until a channel weapon is authored.)
- Leather armor evasion now FUNCTIONAL + content (Shops.Hide) + tested. SpellWard still deferred (no spell model).
- Mouse is click + hover only — no drag-to-equip, tooltips, rebinding; PAUSE/FLEE are plain rects (U6).
- Rune grants = chassis-extension PARTS only; add more data-driven Mark effect kinds when a non-extension
  keystone is authored.
- G1: foes attack player HP only (no localized foe→player PART aim); real content foes still unarmed beyond
  the light feel-pass arming (power envelope = human).

## Roadmap
- Phase 1 [DONE]: Core skeleton, rune economy, chassis, techniques + deterministic combat tick,
  enemies+castle, the attribute rework, chassis→body wiring, combat migration onto Body, balance-sim.
- Phase 2 [ACTIVE]: UI U1-U6 built (combat/build/map/spine/chrome; fidelity continuing via self-review).
  Gameplay G1-G8 built or partial (multi-part foes, gear/weapons + plate armor, 5 chassis, node-map run,
  war-party, campaign spine, data-driven runes/minions/magic, economy). Remaining actionable work = the
  Debt above + the targeting FSM fix. Visual truth = design/ PNGs + design/SCREENS.md; look = DESIGN_SPEC
  §13 + ASSET_HIFI_BRIEF.md. Keep "FTL" out of shipped UI text.

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
