# Claude Design payload

This file holds only what CD still needs to act on. Open asks are alphabetized by topic under
**## Open**, each its own heading — scan the headings to find something, don't read top to bottom.
Sending an item to CD is not the close signal; it clears only once verified landed in the repo, and
moves to **## Confirm-to-Close** (one line, nothing to do but clear it from memory).

## Open

### Armor-Reservation Doc Correction (B26)
`CD_STATUS.md` #34 has the model backwards — it states armor is "threshold-gated only (no pool
pips), only weapons + the shield object reserve pool points." That's not the design. Doug's ruling:
**"Armor consumes pool, eradicate incorrect design documentation in that regard."** The real model
(already locked in our `DESIGN_SPEC.md` SUSTAIN MODEL paragraph, already how `Body.cs` behaves): worn
armor is a standing reservation against the shared per-stat pool, exactly like an active technique or
a wielded weapon — a full plate kit CAN visibly crowd out a technique activation on the same stat, not
just a one-time equip threshold. No engine or DESIGN_SPEC change needed on our side. Ask: fix your #34
entry so future asks/QA built off it don't drift.

### Attribute Bar Order, Color & Rename (B28)
`attrBar`'s two number slots need a reorder, a color-binding fix, and a rename — one coordinated ask,
same element. `design/dchtml/Equipment.dc.html` (~line 106-109) and its extracted `layout.json:10901`
currently read, left to right: `attrs.alloc` (total capacity), `/`, `attrs.available` (current free,
colored via `a.availColor`). Every other pool readout in the game (HP, Supplies, Charge, Summons, the
Equipment/minion "slotted" counts) shows current-value / max — this is the one place that puts the
max on the left. Confirmed native to your own authored source, not our extraction — a `.dc.html`/
`layout.json` ask only. **Full spec:** left slot = `attrs.available` (unallocated, decreases as
gear/techniques reserve it); right slot = the total-capacity field, RENAMED from `attrs.alloc` to
**`attrs.total`** (matches how HP/Supplies/Charge already talk about themselves — current/max, never
"alloc"). The right slot's color is CONDITIONAL: white/neutral when `attrs.damaged == 0`, only
switches to the flag color when `attrs.damaged > 0` — move `availColor` off `attrs.available` and
onto `attrs.total`. Rename `attrs.alloc`/`pool.attr.alloc` → `attrs.total`/`pool.attr.total`
everywhere authored, clean rename, no dual-name fallback, then re-extract. Our side renames the
matching bind-resolution switch in lockstep.

### Audit Key-Set Diff (B1b)
Add a key-set diff (screens/templates vs. the previous manifest) to your pre-ship audit, so a
silently dropped screen can't ship again (the campaignmap-loss class — you re-included it, the guard
ask still stands; we run the same diff drop-side).

### Bow Sprites Missing (B11)
The gear catalog + sprite set covers every ladder except bows (Short/Long/Compound/Elven — §6d ranged
slot). The old `sprites/gear/bow.png` doesn't cover the new convention. Ask: 4 bow sprites
(`bow_short`, `bow_long`, `bow_compound`, `bow_elven`) + catalog rows; engine ids will chase.

### CityMap Node Hover/Current States (B8)
CityMap's beacon-graph nodes have no CD-authored hover or current-position treatment at all (checked
DESIGN_SPEC — never specified). Today the engine hardcodes a bare stopgap in C# (hover = swap border
between two flat colors; current = static amber ring, no animation) because there's no template/
states data for these nodes — unlike `cityNode` (CampaignMap's spine template), which DOES author
`states.current: {border:"amber", glow:true}`. Note `glow` isn't implemented engine-side anywhere
either (dead data), so even where you've signaled a pulsing/glowing intent, we have no primitive for
it yet. Two asks: (a) author real hover/current states for the CityMap beacon nodes, not just the
CampaignMap spine; (b) tell us what `glow:true` should actually look like (steady? pulse rate?) so we
build the primitive once and wire both screens to it.

### Encounter Attribute Pool — CON Row + 6th Pip Both 1px Short (B31)
The Encounter screen's Attribute Pool (the `poolRows`/`poolRow` template — a separate element from
Equipment's working `attrReadout`/`attrBar`) drops its 4th row and its 6th pip, both from the same
1px-authoring-shortfall class as B25 (equipment pips) and B30 (action-bar lists). Root-caused against
the real `layout.json`, confirmed on the Equipment reference which is sized correctly and shows both:
- **CON row missing** (`layout.json:5825` `poolRows`): container `size:[309,77]`, item `poolRow`
  `size:[309,15]` gap `6`, vertical → 4 rows need `4*15 + 3*6 = 78`, region offers only `77`. The 4th
  row (CON) never gets placed. Ask: grow `poolRows.size[1]` to ≥78 (a couple px slack recommended —
  this was a zero-margin fit even before the 1px shortfall).
- **Attribute bars show 5 pips where capacity is 6** (`layout.json:10500` `poolRow`'s cells part):
  pip-strip rect width `207`, `poolPip` cell `size:[33,10]` gap `2`, horizontal → 6 pips need
  `6*33 + 5*2 = 208`, region offers only `207`. The 6th (free) pip drops. Ask: widen that cells
  part rect width to ≥208. (Equipment's equivalent `attrBar` cells rect is `332` vs. the `328` six
  pips need — 4px slack, which is why it renders all 6 there and never here.)
Both are on the `poolRows`/`poolRow` template only; Equipment's `attrReadout`/`attrBar` is already
correct and unaffected. No engine change on our side — same as B30, purely a CD-authored region
resize. Recommend a few px slack on both so a future 5th row / 7th pip class doesn't reopen the trap.

### Encounter Shell Hosts Non-Combat — Quest, Camp, Nothing-Here (B29)
The Encounter screen shell needs to host non-combat arrivals — Quest, "nothing here," and Camp —
plus a quest card template and a header-button state change. `NodeType.Quest` had no Game-layer
template at all (the crash from entering one is fixed engine-side, not a CD ask). Today it's patched
as an ad-hoc popover floated over the CityMap chart (`Game1.CityMap.cs`'s `DrawQuestScreen()` — a
flagged, explicitly-not-finished stopgap: raw panel, literal "QUEST [PLACEHOLDER]" title, generic Y/N
buttons). Wrong host: the quest prompt should render inside the Encounter screen shell instead
(foeless, no enemy present) — "partly to make it feel like you're moving" — and the same shell needs
two more cases: a node with no quest and no combat ("nothing here," likely no popover, just the
arrive beat), and Camp, which should also become a foeless "empty encounter" so a player can
pre-activate techniques and have them already charging before the next real fight (only Sustained/
shield techniques auto-activate on encounter entry now — Timered attacks wait for the player, so Camp
is where you'd prep them). Ask: (1) a real `quest` card template — prompt text + Accept/Decline
actions, matching the Merchant/Equipment panel chrome (quest content/catalog is Doug's own separate
pass, this is just the template); (2) confirm the Encounter shell can render foeless for all three
cases; (3) the CityMap's RETREAT button needs a second visual state — relabels to REDEPLOY, turns
gold, once a node clears, replacing a standalone engine-only overlay that exists today. A DEX-gated
timer on Retreat/Redeploy availability is coming but the exact shape/UX is still undecided on our
side — don't build against a guessed timer yet.

### Figure + Gear Asset Regen Batch (B2-GO)
The commissioning order — the whole batch:
1. **Weapon sprites** — ONE silhouette per type, FOUR material palettes (Iron → Steel → Mithral →
   Dwarven Steel), hand-socket mounts per LAYOUT_CONTRACT: Longsword · Axe · Mace · Claymore ·
   Battleaxe · Warhammer · Dagger · Rapier · Short Sword.
2. **New gear families:** Sling (1H, pairs with shield) · Staff (2H) · Charm + Tome as OFFHAND
   hand-socket mounts · wands are HAND items now (hand-socket mount; dual = one per hand; never
   alongside bow/sling) · a ranged BACK-MOUNT layer (bow/sling) so an equipped ranged weapon renders
   while melee hands are full.
3. **Armor worn-layers:** DONE — see B12 in Confirm-to-Close, no longer part of this batch's open
   scope.
4. **Race × core figure regen** on the established part/z-list contract (robe figures stay
   legitimately ~12-part); fix the elf_ranger chest-accent neckline in this batch (strap sits too
   high, reads as fused to the head).
5. **Ship with:** updated figure defs + asset inventory in `layout.json`, mgcb source updates (we
   mirror game-side), refreshed 00-assets sheets. Our smoke-figures + asset-exists probes verify
   completeness on landing — an inventory list in the drop notes helps us confirm fast.

Note: figure-MORPH mechanics residuals (§7/§17 #15) are our composition questions, not art blockers —
proceed on the current contract; propose contract changes in drop notes, don't block.

### Merchant Sale Card Art (B22)
Merchant gear/technique/rune SALE cards — placeholder now, real art next batch, no rush, doesn't
block our mechanic work. We're building the buy/sell mechanic (real gold cost, real stash wiring) on
stubbed presentation — reused/generic chrome, flagged as a placeholder, not shipped silently as final.
Ask for your next batch: dedicated SALE card art/states for gear, technique, and rune wares distinct
from the existing `wareCard` — whatever visual distinction you'd want between "in my inventory"
(`invCard`) and "for sale" (a `wareCard` cousin) is yours to propose; no locked design opinion here
beyond "don't reuse invCard's chrome unchanged forever."

### Minion Column Reflow at 0 Cap (B27)
`CD_STATUS.md` #33's claimed "minion column collapses at 0 minion-cap, width scales with count"
reflow does not exist — confirmed by live render (Adept, Reaver at 0 minion-cap), not just reading
the schema. `equipment`'s `minionColumn` (`layout.json:7215`) is a fixed `size:[170,99]` panel, no
conditional-visibility or data-driven-width field anywhere in the schema. At MinionCap 0 it still
renders full-width with a correct "MINIONS - 0/0 slotted" label, just an empty list body — no crash,
no wrong data, just always-full-width. Same class of finding as the tab-row dead-space items below.
Ask: if you want the column to shrink at 0 cap or scale with live count, that needs either (a) a new
conditional-width/hide-when-empty field on `Element`, or (b) telling us which cap tiers map to which
pixel widths so it's threshold-authored per screen state — your call. Not blocking, low urgency,
cosmetic only.

### Re-Extraction for the v6 Roster (B20)
The big one — the engine renders only what the manifest authors, so everything here is render-blocked
until it lands.
1. The 01/02-`<core>` refs show elements the manifest doesn't author yet: per-core STAT-BONUS chips
   (`core.statBonus` list on the NewGame coreCard + Equipment identity block; one colored chip per
   non-zero stat: Grunt +1 all · Warden +5 CON · Adept +5 INT · Summoner +3 INT/+2 CON · Reaver +5
   DEX · Ranger +4 DEX/+1 CON · Barbarian +4 STR/+1 CON), action-bar cards with rules text (name +
   cost + italic description + footer state line), and the "minions" label vocabulary. New Core-Effect
   copy is in `design/systems/CORE_RUNES.md` — some rules text runs long (Ranger's compound sentence),
   size the coreEffect rects for it.
2. NewGame re-author for the grown roster: 5 races × 7 cores (raceCards/coreCards currently seat
   2×6 — cells past the container silently drop, so the screen can't show the new picks). Each core
   tile carries its core's BG color + Core-Effect trim color — publish the per-core accent tokens in
   the style block so the engine reads them instead of our flagged stopgap palette.
   - **coreCard Core-Effect accent is a full fill behind the text — contrast bug (Doug #7).** The
     `coreCard` effect block (`layout.json:12681`, rect `[8,174,136,41]`) carries
     `colorBind:"core.accent"`, which the engine renders as a FULL-RECT fill — so the per-core accent
     paints the entire block behind the effect name (y183) and desc (y196), a contrast violation Doug
     reported directly. The **Equipment** screen's `coreEffectBlock` (`:7035`) is the correct
     reference: it shows the accent as a LEFT-BORDER sliver (`border.sides:["left"]`) over a plain
     `ink` panel, no fill. Ask: on the re-authored `coreCard`, apply `core.accent` to the block's
     LEFT-BORDER color (the trim sliver), NOT as a full-rect `colorBind` fill — match the Equipment
     treatment so the effect text stays legible.
3. Refresh the stale refs to v6 data: 05-newgame (new roster + tile colors), 03/07 if their strips
   show core identity; a `01/02-*-<core>` set for any future core additions. Race-card art/portraits +
   final race blurbs/tags for Dwarf/Halfling/Half-Giant (Doug supplies copy) ride here too.
4. FYI: mock numbers in the refs that disagree with the systems docs (e.g. Claymore "6 dmg · 1.4×" vs
   WEAPONS.md's 7 dmg · 1.3×) are non-canon — docs win, no action needed unless you'd rather
   regenerate the copy.

### Resolution-Independence Invariant — No Absolute Positioning (B21)
The CD-authoring half of this is DONE (the `parent`-field re-extraction landed, 160 `parent` keys
across the 6 screens, confirmed on the real tree) — confirm-to-close, clear the build work from
memory. The remaining half is engine-side (recursive parent-box resolution), not a CD ask. What's
still worth keeping visible: the standing rule this bug produced, which should live permanently in
your dev-loop guide so the class of bug can't recur —

> **No absolute positioning (resolution-independence).** A screen element's position must be a pure
> function of its anchor, its design-px offset, its parent box, and the screen size — never its raw
> pixel coordinate on the fixed 1920×1080 authoring stage. Every positioned element gets an explicit
> `data-anchor` (one of the 9 anchors) OR is nested in a parent and positioned relative to it. Grouped
> elements (panel + contents, readout + pips, card innards) are authored as true parent→child
> containment so the group reflows as one unit — never as sibling stage-absolutes. Before shipping a
> screen, re-render it at a non-1920×1080 size (≥1 larger + ≥1 off-aspect) and confirm zero drift/
> gaps/overlap. A layout that only lines up at exactly 1920×1080 is a bug.

Also written into `design/LAYOUT_CONTRACT.md` §3, so it's binding regardless of whether this note
stays here.

### Technique + Minion Icons Still Needed (B18)
Flurry, Aimed Shot, Siphon, Barkskin, Sacrifice, Bind, and the Frenzy/Flurry two-badge split-fill are
all landed (confirm-to-close, see below). **Still needed for the v6 kits — the only open icons:**
Parry, Steel, Suture (technique glyphs — absent from the tree); minion icons Iron Golem, Hound (only
`skeleton.png` is in `icons/minion/`). Icons + mgcb source (we mirror game-side). (Rapier/Staff/
Charm/Tome weapon sprites are tracked in Figure + Gear Asset Regen above, not here.)

### Technique Action-Bar Lists Are 1px Short For a 4th Card (B30)
Root-caused STATUS.md items 10/11 (Doug's "4th technique invisible" + "action bar shows the wrong
abilities" reports): both are the SAME bug, and it's purely a region-width shortfall in your authored
sizes, not an engine cap. `ListLayout.Cells` (`Roguebane.Core/Layout/ListLayout.cs:44`) deliberately
drops any cell whose right edge would exceed its list region (overflow:hidden, by design — the same
rule that clips an oversized HP-pip strip) — but for BOTH technique bars, the region is exactly 1px
narrower than 4 cells actually need:
- `equipment` screen's `loadoutList` (`layout.json:7187-7213`): region `size:[613,89]`, item
  `size:[149,89]` gap `6` → 4 cells need `4*149 + 3*6 = 614`, region only offers `613`. The 4th
  `loadoutCard` silently drops. (Its sibling `techSlotCount` label right above it already samples
  `"TECHNIQUES · 3 / 4 slotted"` — the 4-slot design is already assumed elsewhere on the same screen,
  just not sized for.)
- `encounter` screen's `techList` (`layout.json:5930-5955`): region `size:[433,136]`, item
  `size:[104,136]` gap `6` → 4 cells need `4*104 + 3*6 = 434`, region only offers `433`. Same drop.
Both parent panels have plenty of spare width to absorb the fix (`equipment`'s `actionBar` is 810px
wide, `encounter`'s is 630px, vs. the ~613-620/~433-440 the lists themselves occupy) — this isn't a
"nothing fits" constraint, just a rounding/authoring gap. Ask: widen `loadoutList.size[0]` to ≥614 and
`techList.size[0]` to ≥434 (a little slack beyond the exact minimum recommended, so a future 5th slot
class doesn't reopen the same 1px trap). Not a "first card repeats 3×" data bug on our side — we
confirmed `ListData`/`ResolveBind` correctly resolve each of the 4 distinct equipped techniques
per-index; the 4th one just never gets a cell to draw into, and older reports of "wrong abilities on
the action bar" were the same silent 3-cell cap, not a real duplication.

## Confirm-to-Close (landed and verified — nothing to do, just clear these from memory)
- **B0 · B0b · B1a · B3** — evening 2026-07-03 drop, all verified landed.
- **B4** — "open Equipment" button elements on Encounter + CampaignMap, landed.
- **B5 · B6** — `invCard`/`loadoutCard`/`invTab` `states.family` + hover overlays, landed.
- **B7** — raceCard head-portrait imageBind moved to the correct (square) element, landed.
- **B9** — folded into Figure + Gear Asset Regen (B2-GO) above, no separate item.
- **B10** — gear catalog display-name drift vs. §6c canon, corrected.
- **B12** — worn-armor part set (race-first, full-part convention): 744 files landed clean, 0
  cross-product leaks, no "plain" type, 0 missing/extra. Convention is canon in LAYOUT_CONTRACT §12a /
  DESIGN_SPEC §7a. Remaining wiring (game-mgcb mirror + engine themed-half draw) is ours, not a CD ask.
- **B13 · B14** — `waresShelves`/`buildMinions` sizing fixes, landed.
- **B17** — Dwarf + Halfling figure batch, landed and exceeded (Half-Giant + Barbarian came too, both
  now canon).
- **B19** — "bay(s)" retired as the minion-slot term everywhere, landed; combat vs. build-time minion
  cards deliberately stay separate templates, no unify ask.
- **B23** — Equipment tab buttons (`invTab`) widened to fill their row, three labels readable.
- **B24** — `inventory.invItems` widened to fit its authored 2-column grid.
- **B25** — `attrs.cells` pip strip widened so a 6-capacity stat's 6th (free) pip renders.

## Standing FYIs (context, not action items)
- **Name lengths:** the "Dwarven Steel Short Sword" (24ch) class overflows current card name rects —
  Doug accepts the overflow for now; final treatment is a parked Doug+Cowork decision. Don't
  unilaterally re-rect, but flag preferred options if you have them.
- **Tier ladders for the new families** (for card copy/labels): Sling Shepherd's → Braided → Sinew →
  Giantsbane · Staff Wooden → Twisted → Ornate → Humming · Charm Wooden → Bone → Ornate → Humming ·
  Tome Old Worn → Leather → Ornate → Glowing. Wands/bows keep their existing ladders.
- **Tier-4 signature rule:** magic gear's top-tier adjective is supernatural (Humming/Glowing);
  mundane gear's is not — keep the split in any generated copy.
- **Core Effect roster** was replaced (v6) — the old §11 names (incl. Called Shot) are retired; the
  engine is building the new effects' mechanics now (no longer display-only).
- **The v6 stat blocks are adopted canon:** race bases Human 5/5/5/5 · Elf 4/6/4/4 · Dwarf 4/4/4/6 ·
  Halfling 4/4/6/4 · Half-Giant 6/4/4/4, plus the per-core bonuses in `design/systems/{RACES,
  CORE_RUNES}.md` — the NewGame re-render (Re-Extraction above) samples from these.
- **Drops apply via a stop/apply/re-arm handshake:** stage in `.drop/`, Cowork applies with the loop
  halted, guards run before the tree resumes.
