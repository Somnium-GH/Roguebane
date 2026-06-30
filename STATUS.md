# Status

## Current target
**Targeting/firing FSM — DONE (core + shell).** (most shipped, 166 tests.)
Player casters run `requireAim`: a powered technique fires ONLY at its own explicit aim, never falling
back to a front, so untargeted HOLDS. Firing is target-driven (no fire button) — charged+aimed
discharges. AUTO off (default) = one-shot (clears target after the shot); AUTO on persists. Engine
casters (foe offense/sim/legacy Session) keep default-front auto-fire. Shell: per-module controls
(left-click inactive=power, active=enter targeting+clear; foe-click=aim+exit; right-click=cancel/unpower),
locked + pick-prompt reticles, targeting card ring, no FIRE button / focus cursor. Pinned by
PlayerTargetingFsmTests + Expedition/Campaign integration; combat RB_SMOKE verified.
Next actionable (pick one):
- G1 foe PART aim: author multi-part foe Frames as DATA so left-click can target a foe PART (vs whole
  foe); then wire part-aim in the shell (Caster already supports Aim(tech, foe, part)). Unblocks the
  localized CON-block/evasion-on-part-hit debt too.
- Shell-input behaviour: the targeting click→state mapping is reviewed + visually verified but not
  headless-tested (MonoGame input). Consider extracting a thin testable combat-input reducer.
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
- CON (Chest): bonus HP (1:2); stun resist; DEFENSIVE-ACTIVE — shield/Brace reserve CON and absorb up to
  the CON reserved (capped); STR equips the shield, CON powers the block. Gates chassis-extending runes.
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
- HP-vs-stat split — DEFAULT: attacks → the targeted part's stat; HP only via penetrating/bypass or overkill.
- Shield-block — DEFAULT: flat while held (vs depleting/recharging).
- Arms/legs — ASSUMPTION: armor one-per-group, weapons per-hand. Confirm.
- Balance/feel tuning (placeholder-sane, tune in play): tick 10/s; cooldowns + damage; DEX haste 2%/pt cap
  28%; CON→HP 1:2 + base 8; evasion %; CON block cap 3; armed-foe HP/strike; castle cadences;
  budgets/spoils/prices; march length vs supplies.
- Fog reveal — DEFAULT: resource-holds + castle visible afar, merchant 1 jump out, else `?` until adjacent.
- War-party indicator placement on the map.
- Five chassis stat blocks (design/05) are placeholder — tune later.
- Part→stat friction (legs = accuracy, arms = STR) — low-pri revisit only if it nags.
- Fonts: SpriteFonts use system Consolas/Georgia — swap to bundled open fonts before distribution.

## Asset gaps (Needs Claude Design)
*Loop logs here when a screen needs ART that's missing/wrong in Roguebane.Content and can't be composed
from primitives. Route each to Claude Design. (Hi-fi transition: design/ASSET_HIFI_BRIEF.md.)*
- (none logged yet)

## Debt (active — with reconcile trigger)
- BUILD screen lacks inventory tabs (gear/tech/minions) + drag-to-equip, the per-stat attribute readout
  with gate markers, and equipped-gear on the anatomy. Blocked on gear/minion equip (G2/G7).
- Combat surface has no PART-level aim UI, no minion-bay lane, no support lane — wait on foe part-maps (G1)
  + the bay/support UI lanes.
- CON block + evasion mitigation are on the WHOLE-HP path; localized on PART hits waits on foe→player PART
  aim (G1).
- INT beams are fast Timered bolts (Sustained=every-tick was a firehose at 10/s); for a true channel, add a
  per-tick damage-scaled sustained kind.
- Mouse is click + hover only — no drag-to-equip, tooltips, rebinding; PAUSE/FLEE are plain rects (U6).
- Rune grants = chassis-extension PARTS only; add more data-driven Mark effect kinds when a non-extension
  keystone is authored.
- Bank-on-arrival vs bank-on-clear (minor): support banks on landing, before the fight is won. Revisit.
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
