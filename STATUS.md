# Status

## Current target
**Phase 2 UI track is BUILT (U1-U6) and the shell now runs the real Expedition/Campaign loop.**
Every screen renders from live Core state + the `Roguebane.Content` assets, verified by build +
headless screenshot (RB_SMOKE=1 RB_SCREEN=build|map|combat RB_SHOT=path). 123 headless tests green;
the whole solution builds (Core net8 + Game net9 + Tests).

What remains is genuinely HUMAN: visual polish & fidelity (the assets are placeholder-quality), the
balance "play it" pass, and the parked design calls below. Plus two build-hygiene items a human must
settle (see Debt foot): `Roguebane.Content/` is untracked but the game build now depends on it, and
the SpriteFonts compile from SYSTEM fonts (Consolas/Georgia) — licensing/substitution call before any
distribution.

Next loop-buildable target if resumed: foe/part variety + arming content foes is gated on the balance
envelope (human); otherwise the remaining items need a human. The loop has reached its stop condition.

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
- STR (Arms): attack power (1.0x); scales STR actives. Gates: STR weapons; shields (heavy = STR).
- INT (Head): spell power; keeps spell actives AND passives running. Absorbs old WIS (merged).
- DEX (Legs): evasion; accuracy; +0.25x attack power. Gates: DEX weapons. The 0.25 runs in
  quarter-units (e.g. attack += DEX/4), never a float.
- ARMOR is a LIGHT, survivability effect layer, NOT attribute gear (no stat grant or gate). Each
  piece sits on a part-group; its effect is keyed to TYPE: heavy/plate -> flat PROTECTION (1-4)
  subtracted from the stat-damage a hit deals to that part (blunts attribute erosion); leather (DEX)
  -> EVASION (a chance to avoid the hit) instead of flat protection; head spell-armor -> spell/blind
  protection. Modest by design. The effect RIDES the part's condition (break the part -> its armor
  effect goes), so the cascade survives without a stat threshold. Weapons & shields still gate on
  their stat to wield. Balance note: at the 1-3 damage band, keep flat protection from fully negating
  hits. (Refines the earlier "armor gates on STR/DEX/INT" lines.)
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
- EVASION (leather armor) needs a seeded, deterministic RNG stream — Core is currently 100% RNG-free
  and deterministic. DECISION NEEDED: introduce a seeded PRNG (and where it's threaded) before
  evasion / any chance-based effect ships. Plate (flat protection) works today without it.
- Arming real-content foes (castle layers + skirmishes get Frames + Arsenals so they fight back) is
  gated on balance/feel — the two-sided machinery is built and tested but unarmed in content so the
  loop stays winnable. Decide the enemy power envelope by playing it.
- Balance envelope overall (stat bases, budgets, spoils/prices, march length vs supplies, foe HP,
  charge cost/pool, potion repair) — all placeholder-but-sane; this is the "play it and tune" job.
- CON->HP timing: does chest damage lower MAX HP, or only the available pool? Decide before CON
  combat tuning.
- Combat header TEMPO / PERIL indicators (`design/01`) — semantics undecided; render a placeholder
  and park.
- Run-map fog reveal rules (`design/03`) — WORKING DEFAULT: resource-holds + castle visible afar,
  merchant resolves 1 jump out, everything else `?` until adjacent. Confirm.
- War-party indicator placement on the run map (absent from the `03` render) — pick a spot, park the
  visual for review.
- The five chassis stat blocks (`design/05`) are placeholder values (e.g. Summoner reads "INT-WIS",
  predating the WIS->INT merge) — tune later; defer.
- Part->stat mapping friction (raised, low priority): legs governing ACCURACY (and arms = STRENGTH)
  feels a touch off. Working rationale: legs = stance/footing -> steady aim + dodge. Revisit (e.g.
  move accuracy elsewhere) only if it keeps nagging.

## Debt (provisional work + how to reconcile it)
- (resolved 7b) Rallied support now player-allied: `Support` is a banked, undamageable, intermittent
  auto-fire on the front (Battle owns it); the enemy front-restore is kept but relabelled as the
  boss restoring its own means (Encounter.BossRestoreTick). A castle siege races both streams.
- (resolved) CON block is reserved-CON (a held block absorbs up to the CON it reserves, capped) —
  fixed in the Chassis/CON commit; the unallocated-CON reading is gone.
- (resolved) Combat migrated onto Body: techniques reserve a stat as Actives; Encounter foes are
  HP pools (`Foe`); Entity/AttributePool/Part/Attribute retired. Head-silence is now emergent
  (smash head -> INT drains -> spell reservations cascade off). Old Power/Focus/Vigor gone.
- (G1 resolved — machinery) Two-sided combat shipped: `ICombatTarget`, `Fighter` (player HP), foe
  `Arsenal` + per-foe offense Caster in `Battle`, `Session.Lost`. REMAINING (smaller): (a) foes
  attack the player's HP only — add localized foe->player PART aim; (b) damage mitigation (CON
  block via `Body.BlockMitigation`, armor evasion/protection from G2) is built but not on the
  incoming-hit path; (c) real content foes are still unarmed — arming castle layers is balance/feel
  (human). Reconcile (a)+(b) together when the defensive layer is tuned.
- Rune grants are chassis-extension PARTS only (Hollow Vessel -> +CON, Resonant Core -> +INT). Other
  rune effects (stat multipliers, new techniques, passives) not yet modelled — reconcile by widening
  Mark with more data-driven effect kinds when a non-extension keystone is authored.
- (resolved) Shell rewired off the linear `Session`: `BuildSession.March` mints a `Campaign` (via
  `Forge.EmbarkCampaign`); the run screen renders the chart when choosing and the battlefield when
  fighting, both from live state. `Battle.Encounter` exposed for the combat render. The linear
  `Session`/`Forge.Assemble`/`Sessions.Demo`/`Forged` remain only for the older headless tests.
- Bank-on-arrival vs bank-on-clear (minor): RunMap banks a resource-hold's support the moment the
  player lands, before the node's fight is won. Revisit if it reads wrong in play.
- (resolved) Shell renders all screens with real assets + SpriteFonts through `AssetRegistry`
  (Stat.Con -> constitution, NodeType -> icon, technique id -> glyph w/ fallback, etc.). Remaining is
  visual fidelity (placeholder-quality art) — human.
- (NEEDS HUMAN — build hygiene) `Roguebane.Content/` is UNTRACKED in git but the game build now
  references it (external-source mgcb at `../../Roguebane.Content/`). On a clean clone the content
  build breaks. Decide: commit the 186 PNGs + manifest, use git-lfs, or relocate. The loop did NOT
  commit binaries unilaterally.
- (NEEDS HUMAN — licensing) SpriteFonts compile from SYSTEM fonts (Consolas mono, Georgia serif) to
  make rendering real now. Swap to bundled open fonts (e.g. JetBrains Mono / IM Fell English) before
  any distribution; `display.spritefont`/`mono.spritefont` carry the FontName to change.

## Phase 1 — headless gym + placeholder shell [DONE]
- [x] 1-5. Core skeleton, rune economy, two chassis, techniques+combat tick, enemies+castle.
- [x] 6. MonoGame shell: combat/damage screen DONE.
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
- [x] 6b. Build/loadout + run-map screens: headless BuildSession backbone (cycle chassis, climb
      ladders, toggle techniques, preview minted body, Launch -> Session; 5 tests) plus the shell's
      Build screen and run-map strip. Shell stays thin; rules live in Core. ("Play to feel it" =
      human.)
- [x] 8. (optional) headless balance-sim: ranks BuildSpecs by deterministic ticks-to-clear.

## Phase 2 — the real game [ACTIVE]
Visual truth = the `design/` screen PNGs (STUDY them, not just this list). Asset source =
`Roguebane.Content/` (`ASSET_MANIFEST.md` / `README.md`, built via `Content.mgcb`). Look & grammar =
`design/06-style-frame.png`: warm-muted-dusk palette; TWO fonts (serif display for names, mono for
numbers/state); part-state sprites (ok / damaged / disabled / focus-reticle / gear-socket); the
attribute-pool PIP widget (pips wrap at 10, shrink, fixed footprint, mono number as the anchor, low
numbers <=20); "one frame scales from a body part to a castle wall". HD-pixel: `PointClamp` +
integer scale. Keep "FTL" out of shipped UI/strings (the style-frame label is internal reference only).

UI track (hi-fi — replaced the placeholder rectangles):
- [x] U1. Content pipeline (186 textures via external-source mgcb from `Roguebane.Content/`) +
      `AssetRegistry` (Core id -> safe asset path, tolerant null loads) + two SpriteFonts (display
      serif + mono). RB_SMOKE reports binding coverage (13/13). DEBT: fonts are system Consolas/
      Georgia (licensing); `Roguebane.Content/` untracked (build depends on it).
- [x] U2. Combat screen (`design/01`): backdrop, run resources, player part-composite (limb sprite
      by condition) + HP bar, the attribute-pool pip widget (free/reserved/damaged per stat,
      mono-anchored), foe sprites + focus reticle + HP, action-bar cards (icon, key, stat cost,
      active ring). DEBT: per-card timer/charge/held states; minion bays; tempo/peril header (parked);
      auto-fire lane. Visual fidelity = human.
- [x] U3. Build screen (`design/02`): chassis line-up (figures + names, current ringed), runes
      spent/budget, chassis anatomy composite + attribute pip readout, rune ladders (mark/path/
      keystone glyphs, climbed rungs lit), technique palette (selected ring). DEBT: drag-to-equip
      gear/minion inventory tabs; gate markers; sell/buy (needs shop economy UI).
- [x] U4. Run Map (`design/03`): backdrop, run resources (supplies, war-party distance, support
      bank, gold, potions), current beacon, charted jumps as fog-aware cards (via `RunMap.Sees`),
      merchant verbs. DEBT: FULL beacon-chart layout needs per-node coordinates in map data (only the
      current node + its options render today); dedicated war-party marker art.
- [x] U5. Campaign Spine (`design/04`): the shell marches the whole `Campaign` (legs to the Capital,
      auto-advance on a leg win, rest-at-city); a spine readout (castle pip per leg, current lit) +
      campaign-aware end overlays. DEBT: full branching city-graph screen; stat-block Choose-Your-Core
      cards (`design/05`) — the build screen serves as the core picker for now.
- [x] U6. Global chrome: skinned buttons driven by input/interaction state (build CTA, merchant
      verbs). DEBT: true 9-slice scaling; mouse hover; tooltips; a global window frame.

Gameplay track (toward a complete game, per `DESIGN_SPEC`):
- [x] G1. Multi-part foes + two-sided combat. Foe `Frame` of targetable parts; per-technique PART
      aim with localized stat erosion (overkill -> HP, §10); `ICombatTarget` unifies the grammar;
      `Fighter` gives the player a CON-scaled HP pool; structured armed foes run their own Caster on
      the player (their attacks cascade off as their parts break); `Session` reports Lost. DEBT:
      localized foe->player PART aim + damage mitigation (block/armor) wiring; arming real content
      (castle layers) is balance/feel = human. Foe part-maps per assets sheet 2 still to author.
- [~] G2. Gear/inventory system. DONE: WEAPONS are hand-held stat-sticks (zero abilities) that gate
      on stat capacity and fall off when a smashed arm drops the stat; techniques CONSULT them
      (Swing=primary, Frenzy=both) for cost+power (§7). ARMOR plate gives flat protection riding the
      part's condition (G2a). DEBT: leather EVASION + head spell-ward (evasion needs a seeded RNG —
      see Needs human); shields as CON-block OBJECTS; mixed-stat Frenzy; sell/buy + drag-to-equip UI
      (build inventory / Chassis Anatomy); a subtle consultation-reservation recompute edge case.
- [~] G3. Five chassis as data (Grunt, Warden, Adept, Summoner, Reaver) selectable at New Run via
      `Chassrium.Roster`. DONE: stat bases + budgets (legible identities, tests assert them). DEBT:
      slot/part/bay/action-count shapes wait on the gear (G2) + bay (G7) systems; values placeholder.
- [~] G4. Full node-map run. DONE: `RunMap` (branching links, supplies 1/jump, fog reveal rules,
      resource-hold support banking) + `Expedition` driving combat at each node, merchant HP service,
      flee. DEBT: merchant SHOP needs economy (G8); Unknown currently resolves to a light skirmish
      (author distinct unknown payloads); foe variety per node is thin pending G7.
- [~] G5. War-party forward pressure. DONE: per-jump march on a track; overrun (or supplies dry
      short of the castle) = lose; cracking the castle wins; fresh war party per leg (via G6). DEBT:
      last-stand camp defense (instant loss for now).
- [~] G6. Campaign spine. DONE: `Campaign` marches N legs to the Capital, one body/caster/loadout/
      Stash carrying through; fresh map+war-party each leg; HP rests at each city, part wounds + gold
      persist. DEBT: per-leg escalation (distinct maps, tougher castles) is content tuning; branching
      city graph + cities-taken visual (design/04) for the UI track.
- [~] G7. Data-driven content breadth. DONE: richer runes (Marks grant techniques/minions beyond
      extension-parts — resolves the rune-effects Debt); minions (bay-bound, INT-funded, cascade-shed;
      2 types); magic/charge resource (finite, INT-scaled, refills out of combat; name/tuning
      deferred). DEBT: more foes/parts variety + foe part-maps (with G1); affixes on the magic tier;
      minion variety re-gated onto other stats (parked); auto-summon of rune-granted minions in build.
- [~] G8. Economy. DONE: gold/spoils per cleared node, merchant repair-potion shop + paid HP service,
      potions restore PARTS out of combat (the healing split), Stash carries across legs. DEBT:
      sell/buy gear + runes at shops; consumable variety; spoils/price tuning (human balance).

Discipline: build REAL partials; park genuine open questions in "Needs human" and route around them;
never block. Stop only when all that remains is play + balance, then surface the queue.
