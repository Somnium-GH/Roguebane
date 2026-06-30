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
- [~] Header: title by node type (SIEGE/SKIRMISH/RESOURCE HOLD) + pause; locale text not shown.
- [x] YOU: cutaway body from the manifest figure composer, all parts drawn by condition.
- [x] YOU: HP bar bound to Fighter Hp/MaxHp; figure labelled with its chassis.
- [x] ATTRIBUTE-POOL PIP WIDGET: per-stat coloured pips (free/reserved/damaged), mono anchor.
      (In the YOU box, not yet the prominent bottom panel — that waits on the combat manifest rebuild.)
- [x] BATTLEFIELD: minion sprites in the bay lane; rallied-support lane (banked / RALLIED +N).
- [x] FOE: structured creature figure(s) with targetable PART bands + HP bar + reticle + name tag.
- [x] ACTION BAR: card per technique — icon, stat cost, cooldown fill, state (RDY/charging/held/dry),
      per-card target tag; global AUTO toggle, no fire button.
- [x] Minion-bay lane (filled occupant sprite / empty outline).
- [x] Run resources: supplies X/max, support banked/holds, war-party distance, gold, potions.
- [ ] design/01 EXACT layout (prominent bottom attribute-pool + action-bar panels, figures in the open
      battlefield) — deferred to the coherent combat manifest rebuild (see STATUS).

## 02 Build / Loadout — `design/02-build.png`
Chassis Anatomy (left) | Attribute Readout + Inventory (center) | Rune Bag (right); Current Core +
Action Bar loadout (bottom). **This is the worst current gap — most of the below is missing.**
*(Terminology: "Chassis" → RACE + CORE RUNE per DESIGN_SPEC §7; the screen's "Chassis Anatomy" /
"Current Core" labels + the design/02 PNG predate the rename — update labels to Race + Core rune.)*
- [x] Header: title + chassis selector (5 cores, current ringed) + runes spent/budget.
- [~] CHASSIS ANATOMY: figure cutaway + per-part STAT callouts (INT/CON/STR/DEX); wielded weapon drawn
      on the figure. Equipped-armour-per-part overlay partial (torso has no bare/armoured variant — art gap).
- [x] ATTRIBUTE READOUT: per stat a bar of base + Marks + current with the GATE marker + final value.
- [ ] INVENTORY with TABS (GEAR / TECHNIQUES / MINIONS) + click/drag equip — input-coupled, deferred
      (equip works on the run-map gear bar today).
- [ ] RUNE BAG (MARKS / PATHS / KEYSTONES cards + socket/sell) — current screen shows rune LADDERS instead.
- [x] CURRENT CORE stat block: str/int/dex/con / bays / budget / actions.
- [x] ACTION BAR loadout: the FIXED starting kit pre-slotted (no pick gate); MINION BAYS preview strip
      shows the chassis retinue (sprite + power per bay) alongside.

## 03 Run Map — `design/03-runmap.png`
- [x] Supplies meter (X/max); Mastered Support (banked/holds).
- [x] Half-blind beacon CHART: node graph, charted (solid) vs uncharted (dotted) links; YOU ARE HERE.
- [x] Fog: `?` unknown beacons; resource-holds + castle visible afar; merchant resolves 1 jump out.
- [x] Node-type icons (+ a CHART legend): camp / merchant / resource-hold / unknown / castle.
- [~] War-party advance track (top, closing-distance read); dedicated war-party token/camp icon = art gap.
- [x] Flee verb; gold/potions readout; campaign spine strip.

## 04 Campaign Spine — `design/04-campaign-spine.png`
- [ ] Branching CITY GRAPH (castle icons) from start to the Capital; cities-taken counter.
- [ ] Current city highlighted; taken / available / unreached routes distinguished.
- [ ] Capital marked as the strongest/peak castle.

## 05 New Run — RACE then CORE RUNE — `design/05-new-run.png` (needs redesign; current PNG is core-only)
Two-axis pick (DESIGN_SPEC §7), ideally a COMBINED experience rather than two hard steps:
- [ ] Pick a RACE: a card per race (start: Human, Elf) — figure + base attributes + base HP + flavor.
- [ ] Pick a CORE RUNE: cards for the core runes the chosen race ALLOWS (others greyed/hidden) —
      figure/identity + layout block (budget / #techniques / #bays / apex effect) + flavor.
- [ ] COMBINED ideal: a race selector + a core-rune grid that filters/updates to the race's allowed
      set, previewing the resulting race-base × core-rune-layout combo. (Old single "Chassis" pick is
      retired — design/05 PNG predates this.)
- [ ] Begin the March.
