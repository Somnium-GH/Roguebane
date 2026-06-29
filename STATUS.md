# Status

## Current target
**6b. Build/loadout + run-map screens** — now unblocked (rework + body wiring + balance + end-to-end
flow all landed). Render the pick-chassis / climb-runes / node-map state the shell already owns.
The actual "play to feel it" remains a human touchpoint.

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
- Forward pressure (war party): each stage's castle marches a war party on the player's CAMP,
  advancing one node-step per player move. Reach+crack the castle disbands it (win the race); the
  war party reaching camp cuts supplies -> no sieges -> lose the run. POC: instant loss on arrival;
  a camp-defense last stand (same combat grammar, you defend) is the later target. Resolves the old
  time-pressure-clock open. See design/DESIGN_SPEC.md §12.

## Attribute model (new — one part, one stat; integer-only for determinism)
- STR (Arms): attack power (1.0x); scales STR actives. Gates: STR weapons; shields (heavy = STR);
  part-mitigating armor (plate mail).
- INT (Head): spell power; keeps spell actives AND passives running. Gates: spell-bonus armor.
  Absorbs old WIS (merged).
- DEX (Legs): evasion; accuracy; +0.25x attack power. Gates: DEX weapons; evasion armor (leather).
  The 0.25 runs in quarter-units (e.g. attack += DEX/4), never a float.
- CON (Chest): HP scaling; stun resistance (passive floor); plus a DEFENSIVE-ACTIVE role — CON
  gates no equipment, so it earns its keep via sustained defensive techniques (shield block,
  Brace) that RESERVE CON while held and absorb up to the CON reserved, capped (FTL shield-layer:
  raise it = power it; drop it = CON returns to the pool). STR carries/equips the shield object;
  CON powers holding the block up. Gates: chassis-extending runes. (Retires the earlier
  "unallocated CON mitigates" idea — it ran backwards to reserving CON for a block.)
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
- Shield-block mechanic: a sustained CON-reserving block absorbs up to the CON reserved (capped) —
  flat-while-held vs depleting/recharging (the old shield-depletion knob). WORKING DEFAULT: flat
  while held, at the low-number scale.
- Arms/legs equipment vs hands: WORKING ASSUMPTION — armor is ONE piece per part-group; weapons
  stay per-hand (preserves dual-wield/Frenzy). Confirm before body-wiring (7a) hard-codes it.
- Minion re-gating after WIS/CHA removal: re-home beast/follower minions onto STR/INT/DEX/CON.
  POC unaffected (skeleton is INT). Decide before minion variety expands.
- "Action speed" capability (was torso) — fold into a stat or drop? Undecided. Not POC-critical.
- CON->HP timing: does chest damage lower MAX HP, or only the available pool? Decide before CON
  combat tuning.

## Debt (provisional work + how to reconcile it)
- (resolved 7b) Rallied support now player-allied: `Support` is a banked, undamageable, intermittent
  auto-fire on the front (Battle owns it); the enemy front-restore is kept but relabelled as the
  boss restoring its own means (Encounter.BossRestoreTick). A castle siege races both streams.
- (resolved) CON block is reserved-CON (a held block absorbs up to the CON it reserves, capped) —
  fixed in the Chassis/CON commit; the unallocated-CON reading is gone.
- (resolved) Combat migrated onto Body: techniques reserve a stat as Actives; Encounter foes are
  HP pools (`Foe`); Entity/AttributePool/Part/Attribute retired. Head-silence is now emergent
  (smash head -> INT drains -> spell reservations cascade off). Old Power/Focus/Vigor gone.
- Foes are single HP pools, not multi-part bodies that fight back. Per-technique aim therefore
  targets a whole Foe; PART-level aim (the locked "per-technique aims its own target PART") waits
  on multi-part foes. Reconcile by modelling a foe as a Body (or part set) + its own Caster aimed
  at the player, stepping both sides; then Caster.Aim takes a part.
- Shell ships only the combat/damage screen. Build/loadout + run-map screens (6b) unbuilt but now
  UNBLOCKED (rework + body wiring + end-to-end flow landed). Next loop target.
- Rune grants are chassis-extension PARTS only (Hollow Vessel -> +CON, Resonant Core -> +INT). Other
  rune effects (stat multipliers, new techniques, passives) not yet modelled — reconcile by widening
  Mark with more data-driven effect kinds when a non-extension keystone is authored.

## POC roadmap
- [x] 1-5. Core skeleton, rune economy, two chassis, techniques+combat tick, enemies+castle.
- [~] 6. MonoGame shell: combat/damage screen DONE. 6b (build/loadout + run-map) parked.
- [x] R. Attribute-model rework (foundational): STR/INT/DEX/CON Body where the live pool is
      DERIVED from intact parts; paired Arms x2 / Legs x2 carry stat shares; part damage subtracts
      that stat (graded, low scale); reservation-drop cascade (lose arm -> STR share -> gear falls
      off); CON block-mitigation from unallocated CON; AttackPower = STR + DEX/4; Repair restores
      parts not HP. 8 tests. NOTE: new Body coexists with old Entity/AttributePool until 7a migrates.
- [x] 7a. Chassis->rune->body wiring on the new model: Chassis carries BodyParts data (Head,
      Chest, Arms x2, Legs x2 with stat shares); NewBody() mints a Body; Grunt/Adept retuned to
      STR/INT/DEX/CON low scale. 4 tests. (Combat-engine migration onto Body tracked in Debt.)
- [~] (migration) Combat engine moved onto Body: Foe HP targets, techniques reserve stats,
      head-silence emergent via cascade. Entity/AttributePool/Part/Attribute retired. 44 tests.
- [x] 7b. Rallied support re-pointed: player-allied undamageable `Support` auto-fires on the front;
      enemy front-restore relabelled boss-self-restore. Castle races both. 3 tests.
- [x] 7c. Per-technique targeting: each active technique carries its own aim (Caster.Aim),
      independent of the default front, falling back to the front when its foe dies. 3 tests.
      (Part-level aim within a multi-part foe waits on multi-part foes — Debt.)
- [x] 7. End-to-end wiring (headless): rune grants bite the body (held Mark grants chassis-extension
      parts as data); Forge.Assemble threads chassis -> runes -> body -> techniques -> run into one
      Session; Sessions.Forged() climbs the Vessel keystone; the Game shell now runs that real flow.
      4 tests. ("Play to feel it" stays a human touchpoint.)
- [ ] 6b. Build/loadout + run-map screens (now unblocked).
- [x] 8. (optional) headless balance-sim: ranks BuildSpecs by deterministic ticks-to-clear.
