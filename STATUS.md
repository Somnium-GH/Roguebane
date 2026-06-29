# Status

## Current target
**Attribute-model rework (foundational).** Rename/retune attributes to STR/INT/DEX/CON with a
1:1 part binding, and make part damage proportionally subtract that part's attribute from the
live pool (graded), instead of all-or-nothing on destroy. Equip/ability gating then emerges from
the existing reservation-drop rule (a stat falling below an active's requirement deactivates it).
Build this BEFORE the chassis->body wiring so the body is built on the new model, not the old one.

## Design decisions (locked this pass — were "Needs human")
- Part-targeting: PER-TECHNIQUE aim — each technique aims its own target part.
- Degradation: GRADED via direct stat-capacity reduction — part damage subtracts that part's
  attribute from the pool until repaired. Supersedes the old binary-vs-graded question and the
  interim "binary for now".
- Rallied support: player-allied, CANNOT be damaged, intermittent auto-fire ON the castle — NOT
  enemy repair. (Corrects current code; see Debt.) Castle = boss: HP is permanent within an
  encounter; its systems/parts may be restored by its own means.
- Healing split: potions/healing magic restore PARTS (full/partial, scaled by the attribute
  invested), NEVER HP. HP is restored only out of combat — shop services or non-skirmish
  (quest-like) encounters.
- Chassis body: build the REAL body now — body parts on the Chassis as data + widen base pools —
  on the new 4-stat model. (Was item 7's blocker.)

## Attribute model (new — one part, one stat; integer-only for determinism)
- STR (Arms): attack power (1.0x); scales STR actives. Gates: STR weapons; part-mitigating armor
  (plate mail).
- INT (Head): spell power; keeps spell actives AND passives running. Gates: spell-bonus armor.
  Absorbs old WIS (merged).
- DEX (Legs): evasion; accuracy; +0.25x attack power. Gates: DEX weapons; evasion armor (leather).
  The 0.25 runs in quarter-units (e.g. attack += DEX/4), never a float.
- CON (Chest): HP scaling; stun resistance. Gates: shields; chassis-extending runes.
- CHA dropped; WIS merged into INT (five -> four).
- Core mechanic: damage to a part directly subtracts that part's stat from the live pool until
  repaired. This single rule unifies graded degradation, equip/ability fall-off, and the
  allocation economy. Smash arms -> STR drops -> plate deactivates -> torso exposed.

## Needs human (route around these; the loop skips them)
- HP-vs-stat damage split — WORKING DEFAULT (accepted, revisit in play): attacks deal stat damage
  to the targeted part; HP only takes damage from penetrating/bypassing sources or from overkill
  once a part bottoms out.
- Minion re-gating after WIS/CHA removal: re-home beast/follower minions onto STR/INT/DEX/CON.
  POC unaffected (skeleton is INT). Decide before minion variety expands.
- "Action speed" capability (was torso) — fold into a stat or drop? Undecided. Not POC-critical.
- CON->HP timing: does chest damage lower MAX HP, or only the available pool? Decide before CON
  combat tuning.

## Debt (provisional work + how to reconcile it)
- Rallied support is coded as a repair-stream on the enemy front (Encounter.RallyTick) — WRONG
  DIRECTION vs the locked design. Re-point it to the player's banked, undamageable, intermittent
  auto-fire ON the castle.
- Chassis has no parts of its own; Sessions.Demo bolts on a head so the player can cast. Reconcile
  via the attribute rework + chassis->body wiring: parts onto Chassis as data, widen base pools
  (current Grunt/Adept pools are toy thesis values).
- Enemies modeled as single-part encounter defenders, not multi-part foes that cast back.
  Reconcile when an enemy needs its own parts/techniques (own Entity + Caster, step both sides).
- Shell ships only the combat/damage screen. Build/loadout + run-map screens (6b) unbuilt;
  unblocked once the rework + body wiring + a balance pass land.

## POC roadmap
- [x] 1-5. Core skeleton, rune economy, two chassis, techniques+combat tick, enemies+castle.
- [~] 6. MonoGame shell: combat/damage screen DONE. 6b (build/loadout + run-map) parked.
- [ ] R. Attribute-model rework (foundational): STR/INT/DEX/CON, 1:1 part binding, proportional
      part-damage->stat reduction, DEX attack/accuracy, healing split. Tests assert the reduction
      + the gating cascade (e.g. arm damage 