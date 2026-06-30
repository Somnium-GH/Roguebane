# Roguebane — screen build checklists
*The acceptance rubric for UI work. For each screen: build it, render an `RB_SMOKE` screenshot, then
READ the screenshot AND the design PNG as images and confirm every REQUIRED element is present, in
roughly the right region, bound to the right Core state. A screen is DONE only when its checklist
passes and it structurally matches the design.*

Three outcomes per checklist element: (1) PRESENT & correct = pass; (2) missing in the build but
composable from primitives (panels / text / bars) or existing sprites = BUILD it; (3) a required ART
asset is missing or wrong in `Roguebane.Content` (a designer gap) = BLOCKED — log it under "Asset gaps
(Needs Claude Design)" in `STATUS.md` and raise it; don't fake it.

Match ELEMENTS + LAYOUT + STATE binding; placeholder art quality is fine. **IGNORE design cruft**
(over-labels, dev annotations, the "FTL" reference text — none of that ships). Designs are 1920x1080;
the game's design space is 960x540 (halve design coords). Grammar: warm-muted-dusk palette, serif
display font for names, mono for numbers/state, the attribute PIP widget (wrap at 10, fixed footprint,
mono anchor), part-state sprites, `PointClamp`.

## 01 Combat — `design/01-combat.png`
Three columns: YOU (left) | BATTLEFIELD (center) | FOE (right); header top; action bar bottom.
- [ ] Header: encounter name + locale; pause/allocate indicator. (NO tempo/peril — dropped.)
- [ ] YOU: cutaway body, all parts (Head, Chest, Arms x2, Legs x2), each drawn by condition.
- [ ] YOU: HP bar ("heals only out of combat"), bound to Fighter Hp/MaxHp.
- [ ] ATTRIBUTE-POOL PIP WIDGET (the most prominent element): a row per stat, pips split
      free / reserved / damaged, colour-keyed, mono number anchor. Bound to Body capacity/reserved.
- [ ] BATTLEFIELD: minion(s) in bay state; the rallied-support auto-fire lane.
- [ ] FOE: each foe a structured body with targetable PARTS + HP; current target ringed by a reticle.
- [ ] ACTION BAR: a card per loadout technique — icon, stat cost, COOLDOWN fill, state
      (ready / charging / held / dry), and the technique's current TARGET indicator.
- [ ] Minion-bay lane (toggle, idle/active).
- [ ] Run resources: supplies, banked support, war-party distance, gold, potions.

## 02 Build / Loadout — `design/02-build.png`
Chassis Anatomy (left) | Attribute Readout + Inventory (center) | Rune Bag (right); Current Core +
Action Bar loadout (bottom). **This is the worst current gap — most of the below is missing.**
- [ ] Header: title + chassis selector (5 cores, current ringed) + runes spent/budget.
- [ ] CHASSIS ANATOMY: cutaway with EQUIPPED GEAR per part (helm/plate/arms/greaves), each part
      labelled with its stat.
- [ ] ATTRIBUTE READOUT: per stat a bar of base + Marks + current, with the GATE marker + final
      value; the cold-climb tip text.
- [ ] INVENTORY with TABS (GEAR / TECHNIQUES / MINIONS): active tab lists items; gear shows part +
      gating stat; click/drag to equip onto the matching part.
- [ ] RUNE BAG: budget free/total + spoils; MARKS / PATHS / KEYSTONES as named cards (cost + gate +
      tier); socket / sell verbs.
- [ ] CURRENT CORE stat block: gear / arms / bays / actions / budget / base.
- [ ] ACTION BAR loadout: the chassis's FIXED starting kit pre-slotted (NO "pick a technique" gate);
      minion bays alongside.

## 03 Run Map — `design/03-runmap.png`
- [ ] Supplies meter (X/Y, 1/jump); Mastered Support (banked, rains at the castle).
- [ ] Half-blind beacon CHART: nodes as a graph (needs per-node coords in map data), charted (solid)
      vs uncharted (dotted) links; "YOU ARE HERE" at the current beacon.
- [ ] Fog: `?` unknown beacons; resource-holds + castle visible afar; merchant resolves 1 jump out.
- [ ] Node-type icons: camp / merchant / resource-hold / unknown / castle.
- [ ] War-party advance track/marker (ADD — not in the render).
- [ ] Flee verb; castle "max scale" note; gold/potions readout; campaign spine strip.

## 04 Campaign Spine — `design/04-campaign-spine.png`
- [ ] Branching CITY GRAPH (castle icons) from start to the Capital; cities-taken counter.
- [ ] Current city highlighted; taken / available / unreached routes distinguished.
- [ ] Capital marked as the strongest/peak castle.

## 05 New Run / Choose Your Core — `design/05-new-run.png`
- [ ] FIVE core cards (Grunt/Warden/Adept/Summoner/Reaver): figure, archetype label, stat block
      (gear/arms/bays/actions/budget/base), flavor line.
- [ ] Select + "Begin the March". (Today the build screen doubles as the picker — a dedicated screen
      is the target.)
