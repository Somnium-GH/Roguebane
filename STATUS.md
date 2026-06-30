# Status

## Current target
**Targeting/firing FSM — DONE (core + shell, incl. global AUTO).** (most shipped, 167 tests.)
Player casters run `requireAim`: a powered technique fires ONLY at its own explicit aim, never falling
back to a front, so untargeted HOLDS. Firing is target-driven (no fire button) — charged+aimed
discharges. AUTO is ONE GLOBAL toggle: ON = no module clears its target after firing (all keep firing at
the same target); OFF (default) = each fires once when charged+targeted, then clears. Shown as a lit/unlit
button (no +/- glyph). FIX (re-open): AUTO affects ONLY that button — it must NOT add any highlight on
the foe or its parts (the targeting hover-highlight is separate and stays). Engine casters (foe offense/sim/legacy Session) keep default-front auto-fire.
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
Next actionable:
- Rallied-support lane (shell): paint the banked support (Exp.Map.SupportBank) on the combat screen, and
  during the castle fight show it firing on the boss. Core has Support + SupportBank already.
- Foe -> PLAYER part aim is PARKED in "Needs human" — it depends on the HP-vs-stat split decision, and
  the current whole-HP foe contract is pinned by FoeOffenseTests. Do not flip it unilaterally.
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
  BLOCKS foe→player PART aim: today foes hit player HP (whole-HP, pinned by FoeOffenseTests + the CON-block
  path). Flipping foes to erode player PARTS (and localizing CON-block/evasion onto part hits) needs this
  decision + a feel call on WHICH limb a foe targets. Player→foe part aim already ships; foe→player waits.
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
- Combat surface: PART-level aim UI DONE (limb bands + part-aim); minion-bay lane DONE. Still no
  rallied-support lane.
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
- SHIELDS rework: LEVELS of shielding (FTL-style); each layer absorbs 1 damage; better shields + runes add
  levels. EQUIP = CON (heavy). Regenerates on a timer scaled by CON (mages suffer too without CON runes;
  spell sources get slight relief). Sources: SPELLS Stoneskin/Barkskin/Steelskin/Diamondskin (always-on
  passive, reserve lots of INT, lower caps); ABILITIES Shield Wall (CON; ~floor(troops/4) layers), Parry
  (DEX; low cap), Bind (STR; low cap).

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
