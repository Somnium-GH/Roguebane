# Status

## Current target
**7a. Chassis->rune->body wiring on the new Body model.** Put body parts (Head, Chest, Arms x2,
Legs x2) on Chassis as data, widen base pools, and migrate Caster/Session/Encounter/Game off the
old Entity/AttributePool onto Body (see Debt). Item R (the Body model) is built and green.

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
- STR (Arms): attack power (1.0x); scales STR actives. Gates: STR weapons; shields (heavy = STR);
  part-mitigating armor (plate mail).
- INT (Head): spell power; keeps spell actives AND passives running. Gates: spell-bonus armor.
  Absorbs old WIS (merged).
- DEX (Legs): evasion; accuracy; +0.25x attack power. Gates: DEX weapons; evasion armor (leather).
  The 0.25 runs in quarter-units (e.g. attack += DEX/4), never a float.
- CON (Chest): HP scaling; stun resistance; block mitigation = UNALLOCATED CON absorbs damage per
  hit up to a cap (the FTL shield-layer analog — hold CON back to tank, spend it to act). Gates:
  chassis-extending runes.
- CHA dropped; WIS merged into INT (five -> four).
- Parts & multiplicity: Head x1, Chest x1, Arms x2, Legs x2. Paired parts each take damage
  independently and each carry a SHARE of their stat (one arm = half STR). Armor is one piece per
  part-group, part-mapped (helm->head, breastplate->chest, arm-armor->arms, greaves->legs);
  weapons are held in hands. Lose one arm -> lose its STR share -> equip thresholds can drop and
  gear falls off.
- SCALE (locks the balance envelope): integers stay LOW. ~20 is a HUGE attribute; damage and
  healing move in 1-3 steps (FTL-like); a hit subtracts 1-3 from the targeted part's stat, repair
  restores 1-3. Keeps attack-power and mitigation math in single digits.
- Core mechanic: damage to a part directly subtracts that part's stat from the live pool until
  repaired. This single rule unifies graded degradation, equip/ability fall-off, and the
  allocation economy. Smash arms -> STR drops -> plate/shield deactivate -> torso exposed.

## Needs human (route around these; the loop skips them)
- HP-vs-stat damage split — WORKING DEFAULT (accepted, revisit in play): attacks deal stat damage
  to the targeted part; HP only takes damage from penetrating/bypassing sources or from overkill
  once a part bottoms out.
- CON block-mitigation mechanic: flat per-hit reduction (min(unallocated CON, cap)) vs a
  depleting/recharging buffer (old shield-depletion knob). WORKING DEFAULT: flat per-hit, low scale.
- Arms/legs equipment vs hands: WORKING ASSUMPTION — armor is ONE piece per part-group; weapons
  stay per-hand (preserves dual-wield/Frenzy). Confirm before body-wiring (7a) hard-codes it.
- Minion re-gating after WIS/CHA removal: re-home beast/follower minions onto STR/INT/DEX/CON.
  POC unaffected (skeleton is INT). Decide before minion variety expands.
- "Action speed" capability (was torso) — fold into a stat or drop? Undecided. Not POC-critical.
- CON->HP timing: does chest damage lower MAX HP, or only the available pool? Decide before CON
  combat tuning.

## Debt (provisional work + how to reconcile it)
- Rallied support is coded as a repair-stream on the enemy front (Encounter.RallyTick) — WRONG
  DIRECTION vs the locked design. Re-point it to the player's banked, undamageable, intermittent
  auto-fire ON the castle.
- TWO entity models coexist: new `Body` (Stat/BodyPart/Active, the locked model) vs old
  `Entity`+`AttributePool`+`Part` (Power/Focus/Vigor) still powering Caster/Session/Encounter/Game.
  Reconcile in 7a: migrate combat/session/shell onto Body, retire Entity/AttributePool/Attribute,
  retune Technique.Cost + Encounter defenders to the 4-stat low scale.
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
- [x] R. Attribute-model rework (foundational): STR/INT/DEX/CON Body where the live pool is
      DERIVED from intact parts; paired Arms x2 / Legs x2 carry stat shares; part damage subtracts
      that stat (graded, low scale); reservation-drop cascade (lose arm -> STR share -> gear falls
      off); CON block-mitigation from unallocated CON; AttackPower = STR + DEX/4; Repair restores
      parts not HP. 8 tests. NOTE: new Body coexists with old Entity/AttributePool until 7a migrates.
- [ ] 7a. Chassis->rune->body wiring on the new model (parts on chassis as data, widen pools).
- [ ] 7b. Re-point rallied support to player auto-fire on the castle.
- [ ] 7c. Per-technique targeting in the combat/casting model.
- [ ] 7. End-to-end playable: pick chassis -> allocate runes -> run -> siege. Play to feel it.
- [ ] 6b. Build/loadout + run-map screens (after rework + body + balance pass).
- [x] 8. (optional) headless balance-sim: ranks BuildSpecs by deterministic ticks-to-clear.
