# Roguebane — design spec (current, authoritative)
*Single source of design truth. STATUS.md is build-STATE and points here; this is the design canon —
keep it current as decisions lock. Last full reconciliation: folded in Race+CoreRune, single-enemy
combat, the targeting/firing FSM, the shield revamp, DEX haste, CON bonus-HP, combat pacing, the
campaign-map topology, foe personalities, heal techniques, and the layout-manifest pipeline. Latest
pass (2026-06-30): the simultaneous part+HP damage rule, combat symmetry (one shared AI framework),
merchant HP-healing + potions-as-techniques, any-direction CityMap movement, the Retreat/Redeploy flow,
the fidelity primitives (LAYOUT_CONTRACT §10), and the screen nomenclature.*

## 0. How to read this — READ FIRST
Status tags: **[LOCKED]** decided, build on it · **[OPEN]** genuinely undecided, do NOT invent (see
§17) · **[DROPPED]** abandoned, must not resurface (§18).

**Cardinal rule:** when something isn't specified here, it is OPEN, not yours to decide — surface it.

**IP guardrail.** FTL, Shadowbane, Path of Exile are named only as private design references for shared
vocabulary. They must NEVER appear in shipped assets, UI, text, names, or art; do not replicate FTL's
interface/trade dress. Same systems grammar, our own identity (the retrofantasy skin, §13).

**Layout/assembly is data, not guesswork.** All sprite assembly + UI layout is driven by a generator-
emitted manifest — see `design/LAYOUT_CONTRACT.md` (figure part rects/sockets/z, gear pivots, screen
elements as anchor+offset+size, the shared style block) and `design/SCREENS.md` (per-screen checklist).

**Content/economy numbers are data, not guesswork, either.** `design/systems/*.md` (RACES/CORE_RUNES/
TECHNIQUES/WEAPONS/ARMOR) hold the operative content tables and ARE canon — don't hand-recompute a kit's
total demand in prose. Doug keeps a balance spreadsheet outside the repo (cost list + per-core kit sum +
per-race clearance check) that these tables get reconciled against whenever he shares an update. The
2026-07-05 pass found CORE_RUNES.md/RACES.md had drifted from that model on Barbarian's STR demand (hand
math said 15, the real model says 10, Half-Giant fits exactly) — corrected, logged in STATUS.md.

---

## 1. Concept [LOCKED]
A roguelike where **the player *is* the socketed thing**: the body is the frame that holds runes,
weapons, abilities — sockets are on the *character*, not on equipped gear. Real-time-with-pause combat
shown as a **side-on cutaway of the body you've configured**, parts lighting up and taking *localized*
damage. Art = **retrofantasy storybook**, high fidelity (§13).

Design refs (private): **FTL** (presentation, run structure, allocate-power feel), **Shadowbane** (rune
lineage incl. the "Core rune", city-siege homage), **Path of Exile** (prerequisite ladders, keystones,
deep build space). Distinctive core = **attributes-as-live-allocatable-power** driving a classless,
budget-based build economy.

## 2. Name [OPEN]
Working title **Roguebane**. Not final.

## 3. Core pillars [LOCKED]
- **The body is the board.** A structured entity of parts + sockets; building and fighting both happen
  *to that body*.
- **Attributes are reactor power** (§4–5): a live pool you *allocate*, not a static sheet.
- **One combat grammar** (§8): every foe — first enemy to final castle — is a *structured thing with
  targetable PARTS*. **Combat is always 1-on-1** (one enemy, possibly multi-part); you erode its parts
  under the clock and bring it down.
- **The run marches on a castle** (§12). Resource holds bank **support** that fires for you at the
  finale (inverted rebel fleet). After each castle you press toward a final **Capital City**.
- **The thesis** (§11): a player can exploit a **Core rune's** structure to build something it wasn't
  obviously built for, and it feels clever and good. Everything serves this.

---

## 4. Combat substrate [LOCKED]
**Real-time with pause.** Techniques charge on timers; pause to re-allocate and decide.
- **Pacing:** the battle runs on a **fixed ~10-tick/sec clock** (decoupled from frame rate,
  deterministic — rates use the tick, not frame time). Technique cooldowns are in **real seconds**: a
  weak attack ~4–6s, a strong one ~12–15s; damage is small (1–3) so fights last 30s+. Long by design.
- **Firing model = the targeting FSM in §8** (no fire button; charged + targeted fires).
- **Always single-enemy** (§3/§8): one structured foe at a time. No multi-foe encounters, no
  "front" fallback target.

## 5. Attributes & the allocation economy [LOCKED]
**Four attributes:** **STR, INT, DEX, CON.** A **live, allocatable pool** like reactor power — not
consumed-and-regenerated, not a divisor.
- An active action **reserves** its attribute(s) while active/charging.
- **Timered** returns its reservation when it fires; **sustained/passive** holds it while active.
- **Real-time requirement:** if a stat drops below an active's requirement before it fires, the action
  **deactivates** until it recovers; the freed stat is available meanwhile.
- Parallelism limited only by the pool (multiple timers at once if you can reserve them all).
- Attributes come **only from RACE base + the parts that carry them + Marks** (§11). **No buying flat
  stats with gold.**

### 5a. SCALE — low numbers [LOCKED]
Small integers. **~20 is HUGE.** Damage/healing move in **1–3 steps**; a hit subtracts 1–3 from the
targeted part's stat; repair restores 1–3. Integer-only (fractions in sub-units, e.g. DEX's 0.25× as
`DEX/4`). Keeps the balance envelope tight.

## 6. The four attributes & the body [LOCKED]
**One part, one stat.** Each attribute physically lives in a body part; damaging the part subtracts
that stat from the live pool until repaired — unifying degradation, equip/ability fall-off, and the
allocation economy.

| Attribute | Part | Governs / scales | Gates |
|---|---|---|---|
| **STR** | Arms (×2) | attack power (1.0×); STR actives | STR weapons |
| **INT** | Head (×1) | spell power; keeps spell actives + passives up | spells need INT reserved *(absorbs old WIS)* |
| **DEX** | Legs (×2) | evasion; accuracy; +0.25× attack; **HASTE** (shortens cooldowns ~1.5–2%/pt, cap ~28%) | DEX weapons; **bows** (shield-ignoring, §10) |
| **CON** | Chest (×1) | **bonus HP** (1 CON = 2 HP on a natural base); stun resist; powers shields (§6b) | body-extending runes; wields the shield OBJECT (heavy = CON, moved off STR 2026-07-03) |

- **WIS** merged into INT; **CHA** dropped (§18).
- **CON → HP [LOCKED]:** CON grants BONUS HP atop a natural base (1:2). The base is the **RACE's HP**
  (§7). Chest damage drops CON → the bonus shrinks → MAX HP drops and current HP caps down with it. That
  loss is **PERMANENT within the fight**: repairing the chest brings CON (and the MAX ceiling) back, but
  does **NOT refund** the HP already lost — HP is restored only out of combat (vendor / post-fight, §10).
  Active heals + potions repair PARTS only, never HP.
- **Part multiplicity & damage:** `Head×1, Chest×1, Arms×2, Legs×2`. Paired parts take damage
  independently and each carry a SHARE of their stat (one arm = ½ STR). Weapons held in hands; lose an
  arm → lose its STR share → can fall below a gear's equip threshold, and that gear is then **DISABLED**
  (no bonus/defense, shown **RED** in Equipment) but stays **ASSIGNED** and **re-activates when the
  attribute heals** — it does NOT leave the slot. The cascade *is* the combat depth. **[RESOLVED
  2026-07-03 → §6e: highest-requirement-first, ties last-equipped-first.]**
- **Broken-limb HARD OVERRIDE [NEW LOCKED, 2026-07-03]:** a broken ARM removes its hand-slot outright —
  no weapon can be held there regardless of which stat it gates (a DEX bow is exactly as arm-gated as a
  STR sword; §6d has the full wield model). A broken LEG hard-zeroes EVASION outright, overriding any
  residual DEX from Marks or other non-leg sources. Both are a PHYSICAL-capability gate layered on top of
  (never a replacement for) the raw attribute-pool math, and apply to player AND foe alike (§8 symmetry).
- **Armor [LIGHT effect layer — not attribute gear]:** one piece per part-GROUP slot (Head/Chest/Arms/
  Legs); does NOT grant or gate the attribute pool itself. Four types, each keyed to the attribute whose
  part they protect — full tier ladders + blessed initial numbers in **§6c** (2026-07-03). Rides the
  part's condition: damage a part enough (after any active techniques reserving that attribute have
  already freed what they can) and its armor drops to the same **DISABLED (shown RED in Equipment)**
  state gear already uses above — it stays ASSIGNED (remembered) for re-equip, but stops giving its
  bonus, until the governing attribute heals past its threshold. Weapons/shields gate on their own stat.

### 6b. Blocks & the shield system [LOCKED]
A **shield SOURCE is a PASSIVE technique** that **reserves its stat** in the action bar and is **ON by
default** (most builds carry one for baseline survivability); it can be toggled off in combat to free
the stat. It maintains a **pool of SHIELD POINTS** (FTL-style layers, no hard cap): **each point absorbs
1 damage and is consumed on hit; points REGENERATE on a timer scaled by CON** (+ rune effects). The
source sets the amount/regen and its stat:
- **block** (CON, REQUIRES a shield OBJECT equipped — §6c/§6d; the only source gated on a physical item,
  not just its stat), **stoneskin/barkskin/steelskin/diamondskin** (INT, passive spell), **parry** (DEX,
  low cap), **bind** (STR, low cap), **shield-wall** (CON, scales with rallied troops).
CON is the through-line: it powers the CON block source AND scales shield-point regen for all sources.
Every class should have a viable block source; a build with none must compensate (heavy heals, or high
damage + evasion). **Counterplay — shield-piercing:** some techniques, and ALL **bows** (§10), IGNORE the
shield pool entirely; that bypass is gated by the **Charge** resource (§10) — powerful but logistically
limited. *(Supersedes the earlier "sustained CON-block, flat-while-held, capped" model, §18.)*
- **ALWAYS PASSIVE [LOCKED — reinforce]:** a shield source is NEVER an active-cast ability. It reserves
  its stat to hold the pool passively; toggling it OFF just frees the stat. (Any "active shield" in the
  build/code is a BUG — make it passive.)
- **SHIELD BAR [UI]:** show the pool as **PIPS** (filled = a live shield point, empty = spent), plus a
  **regen progress bar** that fills to show time-to-next-pip while the source is passively active (the
  regen cadence is CON-scaled, §6b). FTL-reminiscent read, our own skin — no trade-dress. The pip count is
  **NOT capped at ~4** (that was FTL's damage scale); ours scales HIGHER (likely 8+) to mitigate this
  game's larger damage numbers — so the bar must render a **variable, larger N** of pips gracefully.

### 6c. Armor tiers & body-slot ladders [LOCKED core structure; tier-bonus numbers + names are BLESSED
INITIAL values (Doug, 2026-07-03) — an advanced-prototype pass, tune later like §6b/§11]
Four rungs per line (escalating protection/coverage), one line per attribute type, one piece per slot.
**STR's per-tier bonus is PART-damage mitigation only** (reduces how much of the covered part's OWN
attribute a landed hit costs) — this is distinct from, and does NOT reopen, the flat-HP-mitigation role
retired in §8: a hit's HP damage is still stopped only by a shield block or a full evade. STR armor is
the single highest-payoff type and the single highest-risk: STR is one shared pool across both arms, so
a build leaning on it is the one most exposed to the arms-broken cascade (§6 disable, above) — expect a
STR build's armor to go RED across the board when both arms break.

**STR — heavy/plate (Head, Chest, Arms, Legs) [RENAMED 2026-07-03 — material ladder, matching
weapons; the old prestige names → §18]:** tiers = **Iron → Steel → Mithral → Dwarven Steel**, one
plain noun per slot — Head: **Helm** · Chest: **Breastplate** · Arms: **Vambraces** · Legs:
**Greaves** (so "Iron Helm" … "Dwarven Steel Greaves").

**DEX — leather (Head, Chest, Arms, Legs):**
- Head: Leather Cap → Hardened Cap → Studded Cap → Reinforced Hood
- Chest: Padded Armor → Leather Armor → Studded Leather → Reinforced Leather *(corrected 2026-07-03: the
  4-rung dictation repeated "Studded Armor" as both rung 2 and rung 4 — read as the intended escalation)*
- Arms: Leather Bracers → Hardened Bracers → Studded Bracers → Reinforced Bracers
- Legs: Leather Leggings → Hardened Leggings → Studded Leggings → Reinforced Leggings

**INT — robe (Chest + Head ONLY — no arm/leg robe pieces exist):**
- Chest: Cotton Robe → Silk Robe → Ornate Robe → Humming Robe
- Head: Cloth Cap → Silk Hood → Ornate Circlet → Humming Circlet

**CON — shield object (one slot, not a body-part group; equip gate = CON, §6 table):**
- Wooden Shield → Iron Buckler → Kite Shield → Tower Shield

**Per-tier bonuses [blessed initial, tune later]:** STR = −2 part-damage to the covered part, per tier.
DEX = +2% evade per tier, per body part currently worn (stacks across worn pieces). INT = +2 spell
damage per piece worn (2-piece cap: robe + hat). Shield = +2% block-pool recharge per tier (§6b).

**Per-tier equip gates [blessed initial, 2026-07-03]:** STR armor 2 STR · DEX armor 1 DEX · Robe
2 INT · Cap/Circlet line 1 INT · **Shield object: 1 CON/tier [RESOLVED 2026-07-04, blessed initial —
matches the DEX-low-gate principle; a hand-config accessory, not heavy plate].** Ladder: Wooden Shield
(T1) → Iron Buckler (T2) → Kite Shield (T3) → Tower Shield (T4) — names already canon above. At 1
CON/tier even a T2 Iron Buckler (2 CON) sits well inside base race CON (Human 3, Elf 2), so a starting
kit can afford one without a bonus source. **Engine note:** the shield object doesn't exist as data yet
(§6b's "block requires a shield OBJECT equipped" isn't wired) — author it as a `WeaponKind.Shield`
1H off-hand item (reuses the existing Weapon/Wield machinery, no new type) and gate the `brace`
technique's shield-SOURCE on one being equipped as a small, separate follow-up slice.

### 6d. Weapons — the wield model [LOCKED, 2026-07-03; corrected same day — see notes; a couple numbers
OPEN, tagged below]
**Weapons/equipment are stat-sticks AND technique GATES** — not just a damage number. A technique
requires its matching equipment to be PRESENT to be usable at all (same tier of requirement as its
attribute reservation, not a bonus layered on an otherwise-available technique): a dual-wield technique
needs two weapons equipped, one per hand-slot; the CON **block** shield-source (§6b) needs a shield
OBJECT equipped, not just CON reserved — it's the strongest block source, and the only one gated on a
physical item rather than its stat alone. Techniques still own ALL timing/effects (§7, unchanged) — the
weapon never adds its own timer, it only gates + scales.
- **No dynamic hand-reservation.** Rejected in favor of the simpler rule: a technique doesn't reserve
  hands over time the way it reserves an attribute — it just REQUIRES a matching weapon config to be
  PRESENT (equipped + arms unbroken) the instant it fires. Weapon attacks are instantaneous-if-charged;
  there's no hold/release lifecycle for hands beyond that instant check.
- **Two independent equip layers [CORRECTED 2026-07-03]:** a **MELEE hand-config** (main-hand + off-hand,
  2 slots total) and a separate, independent **RANGED slot** (bows/slings — WANDS moved to the
  hand-config 2026-07-03, see the roster below) — a bow does NOT compete with
  melee hands for an equip slot; a character can have a sword+shield AND a bow equipped at once (the
  Ranger core already ships exactly this, §10). Only ONE ranged slot exists at all, so "which of two bows"
  was never actually a contention — there's just one. Both layers still gate on arms: **BOWS
  need BOTH arms unbroken to use, same as any 2-handed melee weapon** (the 1H SLING needs its one
  throwing arm), even though ranged items don't occupy the melee hand-slots.
- **Melee hand-config, 1H/2H is per-weapon, not a fixed slot shape:** main-hand + off-hand can each hold a
  1H weapon, a shield (off-hand only, needs a free arm), or — dual-wielding — ANY pairing across the two
  slots, including 2H/2H or 2H/1H, gated purely by **each weapon's own STR requirement** (§6 table, "STR
  weapons" — this is the SAME generic per-weapon threshold everything else already uses, not a separate
  one-hand-vs-two-hand gate). **2H weapons carry a steep, per-tier STR requirement** so that affording two
  at once (or a 2H+heavy-1H pair) is a genuine build-cost tradeoff through the shared STR pool (§5/§6) —
  likely capping such a build below max tier on both pieces rather than being flatly disallowed. Numbers
  OPEN (§17 #9, the balance pass); the SHAPE (single per-weapon threshold, no bespoke dual-wield tax) is
  locked.
- **Lockout conditions [LOCKED]:** a dual-wield technique needs BOTH arms unbroken with two weapons
  equipped (any 1H/2H mix, per the STR gate above), or it's locked out; a solo 2H melee technique needs
  BOTH arms unbroken; a ranged technique (bow/wand) needs BOTH arms unbroken; a shield needs a FREE
  arm — it drops whenever both arms are already committed to a dual-wield pair. Same rule for foes (§8
  symmetry): arms/legs are separately targetable and breakable on both sides.
- **Shield vs. 2H ranged [LOCKED 2026-07-03; updated same day]:** a BOW needs both hands the same way
  a shield needs one — SHIELD × BOW stays an equip-time incompatibility. Same-day update: WANDS left
  the ranged slot, so **wand + shield is LEGAL**; the **STAFF inherits the shield-block** (it counts as
  a held-shield-equivalent for these rules), and SLINGS are 1H shield-COMPATIBLE. Same static-rule
  family, NOT a live timer-contention case — doesn't reopen "no hand-timer gating" above.
- **Sling [LOCKED 2026-07-03 — shape, stat, names; damage number with the balance pass]:** a ONE-HANDED
  ranged weapon, compatible with a shield in the off-hand — the shield-build's ranged option, filling
  the gap bows can't. **Fully ignores the shield pool like a bow** (spends Charge, §10) but at LOWER
  damage than a bow — a weaker, always-available pierce option, not a free upgrade. **DEX-gated,
  1 DEX/tier (confirmed).** Tiers: **Shepherd's Sling → Braided Sling → Sinew Sling → Giantsbane
  Sling.** Damage/tier OPEN (§17 #9).
- **Main-hand/off-hand auto-promotion [LOCKED]:** within the melee hand-config, first-equipped = MAIN-
  HAND, second = OFF-HAND. Unequipping main-hand promotes off-hand; a newly-equipped item always fills
  the (now-empty) off-hand slot — it never displaces an existing main-hand. Guarantees a technique never
  finds a null main-hand while a valid off-hand sits idle.
- **Handedness [LOCKED]:** a player-facing Left/Right-handed setting (cosmetic only) fixes which physical
  arm renders the main-hand weapon vs. the off-hand item, so a broken/bare-arm visual always matches the
  item that would actually drop from that specific arm.
- **NO hand-timer gating between weapon families [RESOLVED 2026-07-03, POC-scope]:** a ranged technique
  and a melee/shield technique may be powered/charged/fired in parallel — techniques never contend for
  "is this hand free right now." The ONLY hand-related gate is the static requirement check already
  above (do you have the necessary unbroken arms + the right weapon equipped); there is no live
  reservation/contention layer on top of it. **Animation (sheathing/drawing) is explicitly OUT OF SCOPE
  for the prototype** — how a bow and a held shield coexist on-screen is a later presentation problem,
  not a rules one; do not let a rendering concern invent a gameplay restriction.
- **[OPEN]** ranged-ONLY builds (no melee weapon equipped at all): viable, but not fully thought through
  (Doug, 2026-07-03) — e.g. they lose access to the CON **block** shield-source specifically (needs the
  shield OBJECT, which lives in melee's off-hand), though other block sources (stoneskin/parry/bind/
  shield-wall, §6b) don't need a held shield and stay available. Revisit during the balance pass.
  (2026-07-03: wands are hand items now — "ranged-only" means bow/sling-focused.)
- **Ranged — bypass degree:** **Bows (DEX):** fully ignore the shield pool, gated by Charge — unchanged,
  §10. **Wands: REDEFINED 2026-07-03** — no longer ranged-slot, no bypass, no Charge; the
  shield-SUBTRACTION model (roster below + §10). Charge stays non-bow-exclusive via the sling.

**THE WEAPON ROSTER [LOCKED 2026-07-03 naming session; damage/req/timer = blessed initial, tune
later].** Every weapon carries a **technique-TIMER MULTIPLIER** (multiplies the consulting
technique's charge timer; **below 1.0× = faster**; cooldowns remain DEX-haste's lever — the
interaction is fine-tuned in balance, attribute benefits stay modest since RUNES are the stronger
effects) plus a **per-tier DAMAGE** and **per-tier equip REQUIREMENT** that consulting techniques
read (§7 — verbs stay techniques; the weapon never adds its own timer entity). **Dual-wield:**
damage formulates from BOTH weapons per the technique's text (rules-text exceptions allowed);
timer multiplier = the **AVERAGE** of the two weapons'.

**Melee tiers = ONE material ladder: Iron → Steel → Mithral → Dwarven Steel** ("Steel Mace",
"Dwarven Steel Claymore"). One silhouette per type; tiers read as material (palette). Card
name-length overflow is ACCEPTED for now (final presentation call parked, Doug+Claude).

| Family | Type | Timer | Dmg/tier | Req/tier |
|---|---|---|---|---|
| STR 1H | Longsword | 1.0× | 4 | 2 STR |
| STR 1H | Axe | 0.9× | 3 | 1 STR |
| STR 1H | Mace | 1.1× | 5 | 3 STR |
| STR 2H | Claymore | 1.3× | 7 | 5 STR |
| STR 2H | Battleaxe | 1.2× | 6 | 4 STR |
| STR 2H | Warhammer | 1.4× | 8 | 5 STR |
| DEX 1H | Dagger | 0.6× | 1 | 1 DEX |
| DEX 1H | Rapier | 0.7× | 2 | 2 DEX |
| DEX 1H | Short Sword | 0.8× | 3 | 3 DEX |

**NO DEX 2H — the bow is DEX's two-hander.** Ranged slot: **Bow** (Short → Long → Compound → Elven;
full bypass + Charge; 2 DEX/tier; dmg/tier OPEN §17 #9) · **Sling** (bullet above; 1 DEX/tier).

**INT implements:** **Wand** (Adept → Twisted → Gemstone → Glowing) — 1H hand item, DUAL-WIELDABLE,
mutually exclusive with the ranged slot (no bow/sling alongside), shield-LEGAL; 2 dmg/tier,
2 INT/tier; damage resolves by shield-SUBTRACTION (§10). **Staff** (Wooden → Twisted → Ornate →
Humming) — 2H, no dual-wield, blocks the ranged slot like a held shield, cannot pair with a shield;
plain BLOCKABLE melee (no subtraction model), 2 dmg/tier, **1 INT/tier — kept deliberately cheap:**
it's the INT build's backup damage + technique gate (insurance when spells drop); raise only if it
gains benefits. **Magic offhands** (off-hand slot; pair with ANY main-hand — "nothing is anyone's
only"): **Charm** (Wooden → Bone → Ornate → Humming) +0.1× MINION attack damage per tier, 1 INT/tier
· **Tome** (Old Worn → Leather → Ornate → Glowing) +0.1× SPELL damage per tier, 1 INT/tier.

**Naming principle [LOCKED]:** MAGIC gear's tier-4 adjective carries a supernatural quality
(Humming/Glowing); mundane lines don't — the split is intentional, never unify it.
**DEX pricing principle [tuning]:** DEX gear requirements stay LOW (raw DEX already pays
evasion/haste/accuracy/+0.25×) — DEX **techniques** are the expensive lever.

### 6e. Equipment screen — card states, clicks, ordering, paper-doll [LOCKED 2026-07-03, states session]
**ONE state family for every inventory card** (GEAR / TECHNIQUES / MINIONS tabs):
- **EQUIPPED** (green border) — active: wielded/worn · slotted on the bar · fielded as a minion.
- **DISABLED** (red) — still ASSIGNED but currently unsustainable: attribute below requirement, arm
  broken, or a §6d gear-gate lost (a slotted technique whose required weapon left). Re-activates when
  the requirement is met again (§6).
- **EQUIPPABLE** (plain) — unequipped, requirements MET.
- **LOCKED** (dim) — unequipped, requirements NOT met; also technique/minion cards when the bar/minion
  capacity is FULL (capacity reads as locked, it is never a displacement conflict).
*(Manifest today authors these as `equipped`/`dropped`/`ready`/`neutral` on `invCard` — rename +
`states.family` keys + hover variants are CD payload items; hover treatment is CD-authored, engine may
ship a FLAGGED generic brighten stopgap meanwhile.)*

**CLICKS (Equipment is only reachable OUT of combat; everything is sealed in combat, §7):**
EQUIPPABLE click → equip/slot/assign · EQUIPPED click → unequip · DISABLED click → unequip (allowed
out of combat) · LOCKED click → inert. **Conflicts AUTO-DISPLACE:** a legal equip always succeeds and
the conflicting piece is unequipped back to inventory — equipping a bow/wand benches a held shield
(§6d incompatibility); a new melee weapon with both hands full displaces the OFF-HAND (main-hand is
never displaced, §6d promotion); armor over an occupied slot displaces the old piece.

**ORDERING [slot index IS the hotkey — techniques 1..T, then minions]:** click slots into the first free
slot; unslot compacts left (no holes); hotkeys renumber positionally. **Reorder = DRAG-AND-DROP:**
dragging a slotted card pulls it off leaving a matching ghost background in its slot; it snaps
INSERTION-style between neighbors (sticky but easy); release locks the new order. Same model for
minions. *[ASSUMED defaults, flag if wrong: drop outside the bar snaps back (cancel); dragging a
palette card onto the bar equips at the insertion point.]*

**DISABLE CASCADE [resolves §17 #16]:** when an attribute can't sustain every equipped item (after
active techniques reserving it free what they can, §6c), items disable **highest-requirement-first**;
ties break **last-equipped-first**. A pure ranking over the current attr level — deterministic,
history-free except ties — so recovery re-enables cheapest-first automatically.

**SUSTAIN MODEL [RESOLVED 2026-07-04, Doug — closes the §6d-vs-§6e ambiguity]:** gear sustain is a
**SUMMED shared pool**, not individual per-item thresholds judged in isolation. Equipping a piece makes
a **standing reservation** against its attribute, exactly like an active technique reserving its stat —
worn/wielded gear AND active techniques all draw on the SAME live pool. **The cascade runs in two
phases when the pool shrinks (part damage):** (1) **active techniques disable first** (they free their
reservation immediately, per the ranking above); (2) **only if the pool is still insufficient after
that** do EQUIPPED ITEMS start disabling, same ranking (highest-requirement-first, ties
last-equipped-first). §6d's "gated purely by each weapon's own per-tier requirement" still governs
whether a piece **can be equipped at all** (the threshold check at equip time); this resolution governs
whether it **stays up** once several things share one shrinking pool — both are true, at different
moments.

**PAPER-DOLL [render = CAPABILITY truth]:** equipped+active gear draws its morph layers (§7);
**DISABLED gear is REMOVED from the render** (bare part — assignment truth stays on the red card);
a weapon in a BROKEN arm never draws (the hand slot is physically gone, §6). Handedness (§6d) picks
which physical arm renders main-hand. Ranged-weapon mount while melee hands are full = **[OPEN §17
#22]** (assumed NOT drawn until a back-mount layer exists — never invent art).

**Screen state:** Equipment always renders the LIVE run state (it is only reachable in-run; BEGIN
marches straight to CityMap). The code's legacy "pre-run build mode" branch is vestigial — retire it
when next touched.

## 7. Race + Core rune, and the three-layer architecture [LOCKED]
Identity is **two axes** (FTL ship + layout):
- **RACE** — sets **starting attributes + base HP**. **SUPERSEDED numbers [v6, LOCKED 2026-07-05]:
  canon = `design/systems/RACES.md`** — FIVE races (Human 5/5/5/5 · Elf 4/6/4/4 · Dwarf 4/4/4/6 ·
  Halfling 4/4/6/4 · Half-Giant 6/4/4/4; baseline 4s, Human +1 across, specialists +2 in one lane).
  v6 also changes the layering: a Core Rune now ADDS an additive stat bonus on top of race base
  (CORE_RUNES.md) — the old "core runes add none" rule is retired (§18). **All race×core combos
  allowed for the POC** (restriction matrix deferred, §17 #4).
- **CORE RUNE** (the Shadowbane "Core rune") — sets **LAYOUT**: rune budget, action-bar size (#
  techniques), # minions, and a **Core Effect** (its signature effect, stronger than a keystone —
renamed from "apex"). **Races GATE which
  core runes they may take** (an SB-style restriction matrix). New Run = pick Race → pick Core rune
  (race-allowed). *(This replaces the old single "Chassis" concept — §18. The archetypes
  Grunt/Warden/Adept/Summoner/Reaver/Ranger/**Barbarian** [added 2026-07-05, LOCKED — budget 14 ·
  actions 3 · minions 1, *Warlord's Might*] are CORE RUNES; Race is the new orthogonal axis. Layout
  numbers + kits for all seven: `design/systems/CORE_RUNES.md` [v6 canon, supersedes the §7a kit
  table where they differ].)*

Three layers over the shared attribute pool:
1. **Race + Core rune = the LOADOUT.** Race sets base attrs+HP; the Core rune sets layout (budget /
   minions / action-bar size / apex). Together they are your **Loadout** — the assembled identity you take into a
   run (above the rune economy; not bought). *(This is the freed-up "Loadout" term; the old "Core" label
   retires.)*
2. **Equipment (installed things).** Weapons (in hands), armor (per-group slots), fielded minions,
   shield sources, runes — configured **between fights** on the **Equipment screen**, sealed in combat.
   A Core rune only ships a **STARTING** Equipment set — it does NOT lock equipment; gear is swappable
   (grown by finds; no build-time "pick a technique" gate). A piece may be **single-slot OR MULTI-SLOT**:
   a robe covers ALL/most body-part slots at once; plate is per-part; etc. The figure RENDERS its
   equipment — swapping gear changes which PARTS draw (robe parts ↔ plate parts), so figures compose as
   **human base → race morph → core-rune morph → equipped-gear parts** (a MORPH model, NOT a pre-rendered
   set per race×core×gear — that would explode the art). Exact morph + multi-slot mechanics = OPEN (§17).
   *(Was called "Loadout"; renamed to Equipment to match its screen.)*
3. **Action bar (verbs).** **Techniques** — live actions; each **consults equipped gear and reserves
   attributes**; **timered** (charge→fire) or **passive/sustained** (holds its reservation).

**Reservation timing [LOCKED 2026-07-04, Doug — clarifies a long-missed regression]:** EQUIPMENT
reserves attributes at EQUIP time (Equipment screen / build time) — a piece of gear should occupy
attribute capacity for as long as it's equipped, cumulatively with everything else equipped (today's
engine gate only checks a single item's `Reserve` against raw `Capacity`, not against what's already
reserved by other equipped items — there IS currently no real cumulative equipment reservation; see
STATUS). **TECHNIQUES reserve attributes ONLY on ACTIVATION during an encounter** — never just for
being slotted on the action bar or sitting in inventory. The Equipment screen must NOT show or apply
any technique-attribute reservation at all; that reservation appears and disappears with in-combat
activation state only. Encounter-time activation reservation (`Body.Activate`/`Reserved`/`Available`)
already works correctly — the bug is scoped to the Equipment screen incorrectly reserving for
techniques that aren't even active yet.

**Default activation state [LOCKED 2026-07-04, Doug — FTL parity]:** nothing starts charging or
passively active at the beginning of an encounter — every technique/minion starts NEUTRAL (off,
uncharged) and the player must activate what they want running, same principle as FTL's system
defaults. This deliberately accepts a new-player-confusion risk (a first-timer might not realize a
passive shield needs manual activation) — the FIX for that is deferred to a future in-game TIP SYSTEM
(permanently disable-able per install), not a "smart default." **Do not build any auto-activate /
smart-default logic to compensate** — ship neutral-by-default now; the tip system is a separate later
feature. (FTL itself solves the same problem with a safe pre-battle screen where you arm before the
first real fight — worth keeping in mind if/when a similar staging beat gets designed here, but not
required to ship this default-state fix.)

**Verbs are NOT bound to weapons.** A weapon is a stat-stick; techniques *consult* what's equipped
("Swing" = primary weapon; "Frenzy" = both, cost = sum). Techniques are a findable/slottable layer.

### 7a. Starting kits & per-core THEME [advanced-prototype defaults, Doug 2026-07-04 — numbers blessed
initial like §6c/§6d, tune later; the goal is a few cores past raw placeholder BEFORE the first real
balance playtest pass]
Each Core rune gets a real starting weapon+armor(+minion) kit and a visual THEME the figure-morph /
armor-worn-layer art (§7, B2-GO) should express — same tier data, different silhouette/trim/palette per
core (see the CD brief, `outputs/CLAUDE_DESIGN_issues.md` B12). **Themes are Doug's identity calls,
LOCKED; the art execution is CD's, per usual (placeholder until delivered).**

| Core | Theme | Starting weapon(s) | Starting armor | Starting minion(s) |
|---|---|---|---|---|
| **Grunt** | Versatility — plain, practical, no strong motif; the "middle of the run" baseline | Iron Longsword + Wooden Shield (T1) | Iron plate, all 4 slots (STR) | none |
| **Warden** | Block & armor — heaviest-reading art, fortified/reinforced motifs | Iron Longsword + Iron Buckler (T2 shield) | Iron plate, all 4 slots (STR) | none |
| **Adept** | Spellcasting — arcane motifs, robe silhouette | Wooden Staff (T1) | Cotton Robe + Cloth Cap (INT) | Skeleton ×1 |
| **Summoner** | Minions/binding — necromantic/ritual motifs, robe | Adept Wand + Wooden Charm (T1) | Cotton Robe + Cloth Cap (INT) | Skeleton + Golem |
| **Reaver** | Dual-wielding — light, aggressive, agile silhouette | 2× Iron Dagger (T1) | Plain leather, all 4 slots (DEX) | none |
| **Ranger** | Bow & pet — tracker/nature motifs | Iron Short Sword + Short Bow (T1) | Plain leather, all 4 slots (DEX) | Hound ×1 |

**Engine gap this surfaces:** `CoreRune` has `DefaultWeapons`/`DefaultEquipment`/`DefaultMinions` but no
`DefaultArmor` — add it (mirrors `DefaultWeapons`, wired into `NewBody` via `Body.Equip`) to actually
assemble these kits. The shield object doesn't exist as data yet either (§6c above). Both are scoped,
separate loop slices — see STATUS.
**CORRECTION (2026-07-04, Doug caught it):** the first pass here claimed per-core armor theming "reuses
the existing figure-morph contract, zero new engine plumbing" — that was wrong, not verified against
`LAYOUT_CONTRACT.md`/`ASSET_MANIFEST.md`. Actually WEARING armor on the figure (as opposed to a card/
inventory icon) is a system that **doesn't exist yet at all** — B2-GO is its first build. Per-core THEME
is a real new dimension on top of that (LAYOUT_CONTRACT §12a, added this pass): a `sprites/gear/worn/
<line>/<slot>_<tier>_<condition>.png` generic layer (required, B2-GO's own scope) plus an optional
`.../<core>/...` themed override, with an explicit fallback chain so partial theme coverage never
breaks (themed → generic same-condition → generic healthy → bare). **Scope LOCKED (Doug, 2026-07-04):
the FULL set** — all 4 tiers × all 3 conditions per core's own line (`outputs/CLAUDE_DESIGN_issues.md`
B12) — flagged as likely multi-night; ship incrementally, the fallback chain covers any gap between drops.
**CORRECTION #2 (2026-07-04, same day — Doug asked "what about race?"):** the first pass assumed
worn-armor art mounts race-agnostically. CHECKED against `layout.json`'s actual figure rects (not
assumed): **HEAD and CHEST/TORSO are NOT race-agnostic** — elf head rects are landscape (152×104)
against human's near-square (~104-112²), the same stretching failure already caught on the raceCard
head-portrait bug; elf torso is ~9-10% narrower than human's at every core sampled. **ARMS and LEGS
ARE race-agnostic** — identical rect sizes across race in every pair checked (grunt/warden/ranger),
only x-position shifts. LAYOUT_CONTRACT §12a now carries an optional race-specific path tier for
exactly the slots that need it. **Corrected total: ~384 sprites, not 288** (12 race-needed slot-
instances × 12 tier×condition cells × 2 races = 288, PLUS 8 race-agnostic slot-instances × 12 = 96).
**CLARIFIED (2026-07-04, Doug):** theme applies ONLY when a core wears its OWN favored line above —
any other line (gear is swappable, §7) renders plain GENERIC race+type art, no theming; core rune is
irrelevant to the render in that case (the fallback chain already does this, this just makes it
explicit). This also means B2-GO's own GENERIC layer needs the same head/chest race-split (any core
can end up wearing any line generically) — flagged back to CD as a B2-GO addendum. **Also explicitly
OUT OF SCOPE: no new body-shape variation** — worn armor is a flat layer over each figure's EXISTING
part rect; the 6 cores' already-distinct body geometry (e.g. Warden's bulkier torso) isn't touched or
expanded by this work.
**CORRECTION #3 (2026-07-04, Doug — CD mis-built the sent batch; convention revised):** the batch CD
produced generated themed art for EVERY armor type × core × race (a full cross-product) plus a "plain"
armor type — neither is the design. The worn-armor art convention is now RACE-FIRST full-part sprites
(canonical: LAYOUT_CONTRACT §12a; CD brief: `outputs/CLAUDE_DESIGN_issues.md` B12), superseding the
line-first path + overlay model + head/chest-only race split above. Three load-bearing corrections:
(1) each file is a COMPLETE part sprite (bare body + armor drawn in), not a runtime overlay; (2) EVERY
body part is authored per race (Doug: future races may need differently-shaped limbs — drops the earlier
arms/legs-shared optimization); (3) the unarmored part is `bare`, there is NO "plain" type, and THEMED
art is favored-line-only (never the cross-product). Revised completeness target ≈744 sprites (bare 24 +
generic 240 + themed 480, 2 races) — multi-night, ship incrementally behind the §12a fallback chain. The
per-core body-silhouette vs worn-part composition is an OPEN our-side engine question (§17 #15), deferred;
no new body-shape art in this batch.
**RESOLVED (2026-07-05, v6):** the floated "Warden's STR armor paid in CON" idea IS the design now —
Core Effect *Fortified* ("plate armor is paid in CON at 1 less per tier", CORE_RUNES.md), REPLACING
*Unbroken Aegis*. Note the v6 kits in CORE_RUNES.md supersede this §7a table where they differ
(Warden's Cleave→Jab; Adept Ember/Siphon/Stoneskin + no minion; Summoner Ember/Sacrifice/Barkskin +
Skeleton only; Reaver Frenzy/Flurry, no heal; Ranger Aimed Shot/Lunge/Bandage + Iron Dagger; new
Barbarian Cleave/Bind/Bandage + Iron Claymore + plate) — the THEME column above stays canon.
**Technique-weapon consult gap (observation, not a decision):** Jab/Cleave/Lunge/Ember/Drain are
self-contained (no `Consults`) — equipping a themed weapon per the table above changes the paperdoll +
equip-gate/cascade behavior (§6/§6d) but NOT those techniques' flat Power today (only `Shot`/`Swing`/
`Frenzy` consult a weapon). Wiring the starting-kit techniques to consult their core's weapon is a
bigger balance-pass question Doug hasn't called yet — flagged, not invented.

## 8. Combat: single enemy, parts, targeting/firing [LOCKED]
**Combat is always against ONE enemy** (a human foe, an atypical creature, the castle, or a special
resource fight) — which may be **multi-PART**. The only targeting is **part aim within that one enemy**;
there is no multi-foe list and no default/front target. **Foe roster canon + the foe/player SYMMETRY
model (frames, gear, arsenals, Foe Effects, T1–T2 numbers): `design/systems/FOES.md`** (2026-07-05;
built-foe specs are flagged prototypes, its IDEAS section is explicitly not-to-build).

- **Every hit deals BOTH [LOCKED]:** part damage (subtracts from the targeted part's stat, graded §6,
  persistent) **and** HP damage — simultaneously, from the same hit. There is NO part-vs-HP split and no
  HP-only-on-overkill path. The ONLY mitigations of a landed hit's HP damage are a **shield block**
  (points absorb it, §6b) or a **full evade** (nothing lands) — STR armor's part-damage mitigation (§6c)
  is a narrower, separate thing: it blunts how much of a landed hit's PART-damage sub-component costs the
  covered part, never the HP damage. Restored: parts by a heal (§10); HP only out of combat (§10).
- **Disable** switches a part off temporarily (disarm/silence/blind/stun/shieldbreak) and returns its
  reserved attribute; recovers over time. **Silence-on-head is emergent** (head damage drains INT →
  spells can't stay reserved; a head *disable* is the hard off).

**Targeting/firing FSM (per technique) [LOCKED]:** techniques START **inactive + untargeted** — nothing
charges, targets, or fires until powered.
- Left-click an **inactive** module → **power it** (reserve stat, begin charging).
- Left-click an **active** module → enter **TARGETING** (reticle up); this **clears** its current target.
- **Target SIDE [LOCKED]:** each technique declares whether it targets the **ENEMY** or the **SELF/ally**.
  Enemy techniques use the foe part-aim below. **Self/heal techniques (Bandage, Cure Wounds) target your
  OWN body** (auto-pick, e.g. most-damaged part) and can NEVER be aimed at the foe. **Passive sources
  (shields) target NOTHING** — they just reserve+hold (§6b), so they never enter the targeting/fire FSM;
  a shield source MUST be flagged passive. (Bug seen: Bandage aimed at enemy parts; a shield acting
  active — both are target-side/passive-flag misses.)
- In targeting, left-click a foe **PART** → set the target (charge proceeds).
- **Charged + targeted → FIRES** (hit/miss by the seeded rolls). **No fire button**, no front fallback;
  charged + untargeted just **holds**.
- Right-click while targeting → cancel (target stays cleared); right-click an active module → **unpower
  it** (returns the stat) and clears its target. A target is **never remembered** (clears on entering
  targeting, cancelling, deactivating, and after firing — unless AUTO).
- **AUTO** (one global lit/unlit toggle, OFF by default): its only job is to **keep the target after a
  shot** so a module keeps firing at it. OFF = fire once, then clear.

**Targeting PRESENTATION [LOCKED 2026-07-03; CORRECTED 2026-07-04, Doug]:** while TARGETING, the
**cursor IS the reticle** — the OS cursor hides, the reticle sprite draws AT THE RAW CURSOR POSITION,
following the mouse continuously. **The earlier "snaps/centres onto the hovered limb band" wording is
WRONG and retracted** — the reticle must never snap/warp to a part's center; hovering only DETECTS
which part the cursor is over (for the click-to-aim hit-test and any highlight/tag logic), it never
repositions the drawn reticle. Click locks the aim on whatever part was detected under the cursor at
that moment; cancel restores the free cursor. **NO box affordances** during targeting — no whole-foe
hover rectangle, no band outline boxes in-game (design/08's dashed bands are doc annotation only). A
locked mount (post-click) shows the **FOCUS reticle with an animated pulse** (fixed-tick driven,
deterministic) plus an **AIM TAG reading the technique's HOTKEY NUMBER** — not its name (names overflow
and collide when several actives aim the same part); multiple locks on one part **stack their numbers**.
Action-bar cards are **numbered 1–N** (the hotkey), so tag ↔ card correspondence is visual. (Supersedes
design/08's name-tag mount; CD updates design/01 + design/08.)

**Part hit-test must use REAL geometry + Z-ORDER [LOCKED 2026-07-04, Doug]:** which part the cursor is
"over" is decided by the figure's ACTUAL painted part rects (per `Figure.Parts`/the LAYOUT_CONTRACT
figure data), never a generic proportional band guess. Where two parts' rects overlap on screen (e.g.
arms sit BEHIND the chest in every figure's paint order), the hit-test must resolve to whichever part
is drawn LAST/frontmost in the figure's `Z` paint order — a click on visually-occluding chest must
register as chest, never the arm hidden behind it. This is the same paint-ordinal convention already
used for UI element rendering (z = paint order, back→front); apply it to part hit-testing too.

**Foes erode the player the same way** — a foe hit deals the same simultaneous part+HP damage, mitigated
only by the player's shields/evasion. Which limb a foe targets = a **per-foe TARGETING PERSONALITY**
(data): **SMART** (best for its build) · **RANDOM** · **INEPT** (botches a good pick).

**Symmetry — ONE shared framework [LOCKED].** The AI drives foes through the SAME technique + attribute +
shield + heal systems the player uses; a foe is just a *body with a loadout and an actor policy*.
Anything the player can do, a foe can (and vice versa). Keep asymmetric exceptions **few and obvious**
(e.g. a canonical troll-regen) and model even those as a **real technique**, never a hardcoded special
case — so both sides stay in one balanced envelope and the sim serves attacker and defender alike.
**Enemy healing** is exactly this: a foe heal runs on a real, tuned technique (cooldown + amount
comparable to the player's), never a free fast tick that out-paces the player's own healing.

**Non-human enemies (future):** distinctive single-enemy part-maps — hydra (redundant heads), golem
(no head → blind-immune), dragon (wings = positioning), scorpion (severable stinger), mounts (two
stacked entities). Direction locked; specific creatures illustrative.

## 9. Minions [LOCKED; some OPEN]
Minions yes; **party no** — one main character.
- Minions live in a **dedicated roster** (Core-rune-set capacity), **instant toggle**, not an action-bar cast.
- A minion **requires its gate in real time**; below it the minion goes **idle (not destroyed)** and
  reactivates free once met. The gate scales minion quality.
- **Gating is data-driven [LOCKED]:** default **INT-gated** (reserve INT). A Core rune / minion may
  OVERRIDE: a different gate stat; **ungated** (core-rune-granted loyal allies — e.g. a knight's retinue);
  or an **alternate cost** (e.g. a caster who summons by spending HP). Encode as `{stat | none | alt-cost}`.
- **Minions cost BOTH [LOCKED 2026-07-02]:** RESERVE the gate stat (real-time, as above) AND spend the
  **SUMMONS** resource to field one. Reserve = the ongoing opportunity cost (competes with your kit);
  Summons = the deploy cost (finite, merchant/loot-refilled, §14). Neither alone: without a resource,
  "ignore the reserve" is too big a freebie and makes any high-gate minion instant-and-free.
- **Summons is paid ONCE, at SUMMON [LOCKED]:** a minion that goes **idle/disabled** (its gate stat
  dropped) is STILL summoned — reactivating it when the stat recovers is **FREE**, no re-pay. Only
  summoning a FRESH minion costs Summons — including **re-summoning a KILLED one** (death = pay again).
- **CON-as-minion-resource: DROPPED** — the resource is **Summons** (not an attribute); the gate stays a
  reserved stat (default INT, overridable). **[OPEN]** minion-type acquisition + the stat→role table
  (which stat gates which minion + what it does; §17 #5).
- **Minions fire on their OWN internal timer [RESOLVED 2026-07-04, Doug — fixes the "Skeleton hits every
  tick" bug]:** a minion is content shaped like a weapon, not a free-firing add-on — it carries its own
  **Timer** (ticks between discharges, same unit as a Technique's Cooldown), and the tick loop fires it
  on THAT cadence, never on every combat tick or piggybacked on whatever the caster happens to be
  pressing. **Archetype split, same total DPS at T1, different lever:** a FAST/WEAK minion (frequent,
  small hits) vs a SLOW/STRONG minion (rare, big hits) — both tuned to roughly **T1 1H-weapon-technique
  parity, not above it** (a minion is bonus pressure alongside the player's own kit, at the cost of a
  reservation + Summons — it should not out-damage what the player's own action bar can do).
  **T1 blessed-initial numbers** (10 ticks/sec clock, matching Jab/Lunge's ~0.4 dmg/s benchmark):
  **Skeleton** (fast/weak, INT-gated, Reserve 2) — Timer 25 ticks (2.5s), Power 1 → ~0.4 dmg/s.
  **Golem** (slow/strong, INT-gated, Reserve 3 — NEW, replaces Shade's role) — Timer 100 ticks (10s),
  Power 4 → ~0.4 dmg/s. Tier growth (once a minion ladder exists, still OPEN) should follow the same
  archetype logic weapons use: a fast minion's tiers mostly SHORTEN its timer, a slow minion's tiers
  mostly RAISE its power — tier is not yet built; T1 is the placeholder per-minion baseline. **Shade is
  likely retired** (it duplicated Skeleton's role with no distinct playstyle, which Golem now fills
  cleanly) — confirm before the loop deletes it.
- **Ranger's PET — DEX-gated Hound [content added 2026-07-04]:** per the DEX minion-role lean already on
  the books (**DEX = utility/evasion, NOT raw DPS** — so a shield-pierce/bow build doesn't also double-
  dip on a hard-hitting pet), Hound ships now as a minor chip-damage placeholder ONLY: Reserve 1 (DEX),
  Timer 40 ticks (4s), Power 1 → ~0.25 dmg/s, deliberately the weakest of the three per-reserve-point.
  The real distinguishing EFFECT (an evasion/accuracy/utility grant, not damage) is intentionally NOT
  invented here — it rides the minion stat→role design pass (§17 #5) as its own slice.

## 10. HP, healing, and Charge (the shield-pierce resource) [LOCKED core; details OPEN]
- **HP** is a small life total, separate from the part/stat layer; **permanent within an encounter**.
- **Healing repairs PARTS, never HP ("repair systems") [LOCKED]:** part-heals are **techniques** (action-
  bar verbs) — **Bandage** (CON), **Cure Wounds** (INT), and **Potion**-flavored heals — amount scales
  with the attribute invested. **"Potions" are NOT purchasable items; a potion is one FLAVOR of heal-
  body-part technique** (found/slotted like any technique), never bought at a shop. **HP** is restored
  only **OUT OF COMBAT, at a merchant**: pay gold to heal HP at **1 HP per (randomized) cost**, the cost
  balanced within loot-rate bounds. Starting with no heal is a smaller penalty than starting with no
  shield source.
- **CHARGE — the shield-pierce resource [LOCKED; name = Charge]:** a finite, refillable pool, distinct
  from the attribute pool, refilled via loot/gold **out of combat** (not regenerated mid-fight). **Blanket
  rule: a SHIELD-IGNORING technique requires Charge and spends it per use.** Charge is the economy of
  *bypassing shields* (§6b) — cheap in attributes but logistically fragile (run dry → no piercing until
  you refill). It is NOT a generic "magic" cost; ordinary (blockable) techniques cost no Charge.
- **BOWS are always shield-ignoring [LOCKED]:** a bow's attacks bypass the shield pool and therefore draw
  Charge. BUILT: `Armory.Bow` is a DEX stat-stick; its consulting verb `Armory.Shot` is ShieldPiercing
  (ChargeCost 1) — power/cost come from the wielded bow. The **Ranger** core ships it (a `DefaultWeapons`
  bow wielded at assembly + `Shot` on its bar). Because Charge is SCARCE (INT-pooled, no mid-fight refill),
  a pierce-only kit runs dry and stalls — so a bow build MUST pair the bow with a Charge-free attack (the
  Ranger carries a DEX melee `Lunge` for sustained damage; the bow is the pierce finisher). Verified by
  CoreCampaignTests. Numbers (bow power 2, reserve 2, Shot cd 3) placeholder.
- **[OPEN]** whether shield-piercing needs any extra "damaging resolution" beyond the bypass — keep it
  simple for now (bypass + Charge cost, nothing more).
- **WANDS [REDEFINED 2026-07-03 — supersedes the partial-bypass + Charge model, §18]:** wands are 1H
  HAND items now (§6d roster — dual-wieldable, mutually exclusive with the ranged slot, shield-legal).
  **No bypass, no Charge.** Wand damage is **REDUCED by the foe's standing shield-point count; the
  shields are NOT consumed** — e.g. tier-3 wand (2 dmg/tier = 6) vs 4 shield points → 2 damage lands,
  the pool stays 4. Wands chip through standing shields; big stacks blunt them. What lands is a normal
  hit (part + HP, §8). Resolves the old split/math OPEN (§17 #18).
- **SLING (§6d, 2026-07-03):** the second Charge-spending line — ONE-HANDED (compatible with a shield,
  unlike bows), fully bypasses the shield pool like a bow, but at lower damage. The shield-build's
  answer to "how do I pierce at all."

## 11. The rune system [LOCKED core; some numbers OPEN]
The build economy. **No fixed rune slots** — runes are bought from a **point budget set by the Core
rune** (grown by progression). The *shape* of the rune loadout is itself a build expression.
- **Core rune (the archetype)** — sits above the budget, not bought; defined by **structural leanings +
  apex effects**, race-gated (§7). Master axis: **specialist** (high RACE base, free structure, full
  rune prices / tight budget) ↔ **generalist** (cheap runes, fat budget, lower base).
- **Marks (stat-shapers)** — cheap, quantitative; purpose is the **prerequisite ladder**; same-attribute
  Marks **overwrite** (stepping-stones).
- **Paths (identity traits)** — modular, **minor → major** magnitude (major = bigger number + a
  qualitative kicker). Simple trait lines, not branching trees.
- **Keystones** — apex, play-defining, real downsides; **rare by price** (no hard cap), prereq-gated.
  POC keystone **Hollow Vessel** (unspent budget converts live into a regenerating resource) — **[effect
  OPEN: the old "aether" target is retired; redefine (vs Charge or a regenerating attribute) before it's
  real. Code currently grants a placeholder +CON — sample content, not the designed effect.]**
  Each CoreRune carries a **Core Effect** (renamed from "apex"; card label + blurb = `CoreEffectName`/
  `CoreEffectDesc`, e.g. Grunt = *Jack of All Trades*) the build cards render (§13).
  **Core Effect roster — REPLACED by the v6 pass [LOCKED 2026-07-05; canon = `design/systems/
  CORE_RUNES.md`]:** the 2026-07-03 roster (*Hollow Vessel* / *Unbroken Aegis* / *Overchannel* /
  *Legion* / *Bloodrush* / *Called Shot*) is RETIRED (§18). The seven current effects (Grunt *Jack of
  All Trades* · Warden *Fortified* · Adept *Resonance* · Summoner *Conscription* · Reaver *Finesse* ·
  Ranger *Fletcher's Luck* · Barbarian *Warlord's Might*) have rules text designed for BUILDING — the
  mechanics are engine work now (STATUS Chunk A), not display-only placeholders. Warden's *Fortified*
  ("plate paid in CON at 1 less per tier") RESOLVES the floated CON-substitution idea (§7a note): it
  replaces *Unbroken Aegis*, decided.
- **Prerequisite ladder [LOCKED]:** big runes need a minimum in an attribute, met by RACE base
  (efficient) **or** by spending budget on Marks to climb (mostly-refunded-but-leaks tax). Native
  qualification beats climbing.
- **Permanence [LOCKED]:** runes are permanent commitments with an in-family minor→major upgrade;
  committing to a family locks you in. Planting, not gambling.
- **Thesis [LOCKED]:** the **generalist can out-specialize a specialist** by funding a climb with budget
  surplus to reach a keystone its Core rune wasn't built for — all-in and fragile.
- **[OPEN]** budget math (≈55% keystone / ≈120% budget-core targets); keystone taxonomy; gold buying
  low-tier runes (the "mostly found" lean is solid).
- **The rune bag = the ONE inventory location for every owned rune [RESOLVED 2026-07-04, Doug — closes
  the "where does a bought Mark live" Needs-human item]:** whether a rune was bought from the Core
  rune's budget, bought at a merchant (§12), looted, or awarded, it lands in the SAME place and renders
  the SAME way — ladder GROUPS showing the held rung + what's climbable next. There is no separate
  "unallocated inventory row" for runes (unlike gear/techniques/minions, which DO have an
  equipped-vs-inventory split, §6e) — **a rune you own IS allocated**, because Marks are same-attribute-
  overwrite stepping stones (§11 above) and there are no fixed rune slots to begin with. A merchant-
  bought Mark is treated exactly like a budget-bought one: it becomes your held rung on that ladder
  immediately, shown via the existing ladder-group display — no new UI state needed.

## 12. Run structure & the map [LOCKED; some specifics OPEN]
Nested layers, macro → micro:
- **Layer 1 — the CAMPAIGN MAP (cities → Capital) [LOCKED]:** a **forward-biased city GRAPH** (not a
  fixed linear list). Movement is free-ish but generally **forward**, **gated by SUPPLIES** (deplete as
  you move). Run dry → you must **WAIT**, and each waiting turn has a **CHANCE of an encounter** (may
  resupply — affordable, free, or useless). All the while the **enemy war party advances** toward your
  camp + Capital; reaching them = **LOSE**. Beat a city's castle → roads onward open; complexity climbs
  to the Capital. **[OPEN]** city count; procgen vs authored; exact supply costs.
- **Layer 2 — the leg = the CityMap (node graph to one castle):** a beacon chart crossed node-by-node.
  **Movement is ANY-DIRECTION [LOCKED]:** move to any linked node, forward OR back (e.g. return to a
  merchant to heal). **Every move costs 1 supply AND advances the war party one step** — that reciprocal
  cost is the whole tradeoff; backtracking is allowed but never free. Half-blind fog (resource-holds +
  castle visible afar, merchant resolves 1 deployment out, else hidden until adjacent). **Retreat** bails an
  active fight (§8; flow below).
- **Layer 3 — node types:** skirmish (combat); resource-hold (win to **bank support**); merchant (shop,
  spec below); unknown (fogged); the castle (exit).
- **MERCHANT (out-of-combat shop) [LOCKED spec 2026-07-02]:** each visit stocks RANDOM but SCARCE:
  - **Resources** (small quantities): **Supplies, Charge, Summons**.
  - **Healing** (tier-reasonable random price, expensive for a full repair, §10): a **1-HP** buy + a
    **FULL-heal** buy.
  - **Gear across 5 categories** — Armor / Weapons / Techniques / Minions / Runes — tier-appropriate (with
    occasional outliers), rarity-weighted:
    * **Always 3** random tier-appropriate picks from {Armor, Weapons, Minions, Runes}; up to 4 of the 5
      sections appear per visit — most visits are just Armor/Weapons/Minions (a 2–3-stock rotation).
    * **Techniques: always 5 when present**, but techniques are the 2nd-rarest to appear.
    * **Runes: EXTREMELY rare; good runes EXCEEDINGLY rare; Keystones NEVER (drop-only).**
  - **Presence-roll weights [LOCKED 2026-07-03, Doug]:** the "advanced prototype" IS the shipped
    mechanic — an independent per-category presence roll (not a guaranteed-all stock), each category
    rolled separately against its own weight, capped at 4 of the 5 sections per visit. Locked numbers
    (`Roguebane.Core/MerchantStock.cs`, already implemented, just de-flag it): **Armor/Weapons/Minions
    = 80%** presence chance each ("common," independent rolls) · **Techniques = 25%** · **Runes = 8%**
    (rank-2+ Marks additionally need a 33% survival roll on top of the section appearing at all,
    keeping "good runes exceedingly rare" distinct from "runes rare"). No further tuning pass needed
    to ship the POC — these ARE the numbers. (A future PASS may still reshape this — e.g. attr-lean
    variety within a section — but that's a new idea, not finishing this one.)
  - **[NOTE]** placeholder rune behavior: POC currently just seeds runes into inventory at run start.
  - **Receiving [LOCKED 2026-07-03]:** every ware is a **click-to-buy tile** (per design/07) — no
    separate ceremony. Purchases land in INVENTORY: technique → palette, minion → minion inventory,
    rune → rune bag (Climb rules unchanged); **slotting stays Equipment's job**. Weapons/armor keep
    their existing buy→stash flow. Blocked only by gold.
  - The merchant SCREEN is DESIGNED: design/07 (v2, 2026-07-03) + manifest `merchant` screen — the
    popover stopgap is retired; §17's screen-spec item is CLOSED.
- **Layer 4 — encounter resolution:** every node is the one combat grammar (§8); the **castle is that
  grammar at max scale** (gate/wall/catapult/ballista + boss-tier, map-scaled damage; its "systems"
  are its parts).
- **Banked support [LOCKED]:** player-allied, **undamageable**, an **intermittent auto-fire stream** on
  the castle, scaled by how much you banked (inverted rebel fleet).
- **The war party (forward pressure) [LOCKED]:** marches on your **camp** (one step per move); **crack
  the castle → it disbands** (win the race); **reach camp → supplies cut → lose**. POC: instant loss on
  arrival; a **camp-defense last stand** (same combat grammar, you defend) is the design target.
  **UI:** a **top-edge track, castle (right) → camp (left)**, advancing each move.
- **The core tension (a race):** supplies cap how far you roam; banked support is your finale damage;
  the war-party clock caps your time. Tune so banking *a little* is viable and only **greed** loses.
- **Flow — Redeploy / Retreat, Equipment between fights [LOCKED; timing OPEN]:** **Retreat** = the
  in-combat action to bail an active fight; **Redeploy** = the out-of-combat action to advance (a move on
  the CityMap). **A resolved fight does NOT auto-return to the map** — the player explicitly Redeploys.
  **Equipment access:** NewGame leads STRAIGHT into the run (CityMap) — Equipment is NOT a post-NewGame
  gate (do NOT let Enter pass through an old build screen). Equipment opens as a **FULL SCREEN**
  (design/02), NEVER a popover. Reach it via the **`e`** hotkey or an **"open Equipment" button** on
  Encounter (DISABLED during combat), CityMap, and CampaignMap; a **BACK/close** control on the Equipment
  screen returns to the CALLER. Out of combat = **editable** (current core). In combat the access is
  disabled (a read-only in-combat view is a deferred idea, not now). Redeploy is **timed**: a lockout that **DEX shortens**
  (haste, §6) — design-locked, *not yet built (flow first)*. **[OPEN]** an FTL-style commit-to-
  destination (pick where you Redeploy/Retreat to in the same act) — deferred; for now Redeploy just
  routes to the CityMap.
- **[OPEN]** a crafting system fed by siege resources.

## 13. Presentation — the designer's core [LOCKED]
- **Cutaway grammar.** Side-on cutaway of the character — body-as-board — systems taking *localized*
  part damage. The look *is* the mechanic.
- **Retrofantasy art [HIGH fidelity]:** crisp, high-res rendering of oldschool 8-bit / EGA-VGA storybook
  fantasy (Zeliard-style) — warm, hand-painted, fairytale — as if **upscaled to HD while keeping its
  old-school soul.** Retro *style*, high *quality* (NOT crude/low-res). Deep art canon = Claude Design's ART_RULES.
- **Layout/assembly is MANIFEST-DRIVEN, viewport-independent.** Sprite assembly (figure parts at emitted
  socket/rect/z; gear mounted at hand sockets) and UI (elements as anchor+offset+size, dynamic
  containers/templates, the shared style block) come from the generator-emitted manifest — see
  `design/LAYOUT_CONTRACT.md` + `design/SCREENS.md`. **Aspect-independent fill:** background
  scale-to-cover; HUD anchored to real edges; the pixel stage integer-scaled + centered (no bars).
- **Screens & canonical names [LOCKED]** (design/ PNGs 01-05): **NewGame** (design/05 — ONE screen,
  three columns: **Race** (head sprites + attr/HP card) | **Core Rune** (rune icon + budget/actions/minions +
  APEX card) | **Loadout** (the assembled Race+CoreRune: composed figure + combined stats + apex); BEGIN
  THE RUN), **Equipment** (design/02 — the Equipment cutaway:
  race+core anatomy, rune budget + ladder, equipped gear, action bar; a **between-fights** screen
  reachable from Encounter/CityMap/CampaignMap, **NOT** a post-NewGame gate), **Encounter** (design/01 —
  combat cutaway taking localized part damage), **CityMap** (design/03 — the fog-aware leg node graph +
  merchant), **CampaignMap** (design/04 — the Layer-1 city route). Actions: **Retreat** (in combat) /
  **Redeploy** (out of combat). *(Old names, do not resurface: New Run, Build/Chassis, Combat/Siege,
  Run-map, Campaign spine, March, Flee.)*
- **Two registers:** world/combat bolder/more atmospheric; the **build screen clean + legible** — same
  high-fidelity bar, less flourish.
- **Combat layout:** three zones, **you | battlefield | foe** (the foe zone holds **ONE structured
  enemy** — possibly multi-PART, e.g. the castle's layered **gate / wall / keep** as parts of one foe).
  Combat is single-enemy (§8/§18); the multi-foe experiment is **retired** — the code MAY keep the
  capability latent if it stays neat, but the DESIGN is one foe (body-part aim is already the focus;
  more foes at once is too much — the FTL lesson).
  - *You* = cutaway (Head×1, Chest×1, Arms×2, Legs×2), each showing condition + stat; armor overlays.
  - **The attribute pool is a prominent element** (reserved vs free, incl. shield reservations) — with a
    single foe, **design/01's prominent bottom-panel pool IS the layout**: the bottom band is free, and
    the one foe stays large enough for clear limb-band PART-aim (the core mechanic).
  - *Action bar* = technique cards with parallel charge timers + state (ready/charging/held/dry), a
    per-technique target tag, and the **AUTO** lit/unlit toggle. **No fire button.**
  - *Battlefield* = autonomous minion(s) + the rallied-support auto-fire stream.
  - *Foe* = the ONE structured enemy with targetable PARTS + the targeting reticle; it carries its
    creature figure (ogre/troll/bandit/skeleton wired; wraith/gargoyle art pending).
  - Corrections from old wireframes: parallel action bar (not one-hand); **no poise/stagger bar**;
    minions fielded as their own roster (no separate slot term); **no TEMPO/PERIL header**.

## 14. Economy / resources summary [LOCKED, modulo §10]
- **Attributes (STR/INT/DEX/CON):** live pool; from RACE base + carrying parts + Marks; no gold buy.
- **Charge:** finite, refillable (loot/gold, out of combat); spent by SHIELD-IGNORING techniques (incl.
  all bows) to bypass the shield pool (§6b/§10). NOT a generic magic cost.
- **Summons:** finite, refillable (merchant/loot); SPENT to field a minion, on top of reserving its gate
  stat (§9). The Summoner's Core Effect (*Conscription*, v6 2026-07-05) makes fielding spend NO
  Summons at all — replacing the old Legion surviving-minion refund (§11/§18).
- **HP:** small life total; restored only out of combat.
- **Parts:** localized; damage subtracts the part's stat; heals repair parts.
- **Shield points:** consumed 1:1 by damage; regen on a CON-scaled timer (§6b).
- **Gold/spoils:** loot/sell currency; shops; sink for the charge resource, consumables, maybe low runes.
- **Rune budget (points):** the build economy; from the Core rune; separate from gold + in-combat pools.
- **Supplies:** map-traversal fuel. **Banked support:** from resource holds, spent as castle auto-fire.

## 15. Tech & architecture [LOCKED — context for the designer]
MonoGame (C#), DesktopGL, Steam-first, code-first (no editor). **Logic-core / render-shell split:**
`Roguebane.Core` is pure, headless, deterministic simulation (all rules); `Roguebane.Game` is a thin
shell that reads Core state, draws it (from the layout manifest, §13), feeds input back as commands —
**no game rules in the shell.** Content is **data, not code** — assets + layout are generator-emitted
(`design/LAYOUT_CONTRACT.md`); the shell renders whatever the Core + manifest say.

## 16. Build state [pointer]
The original POC ("does exploiting structure feel good?") is realised; the game now spans the full loop
(build → march → single-enemy combat → economy → campaign). Current targets, debt, and the
Race+CoreRune / single-enemy / shields / campaign-map slices live in **STATUS.md** — this section just
points there so the canon stays design-focused.

---

## 17. OPEN questions — do NOT invent answers
1. Game name (§2).
2. Charge (§10) — RESOLVED: name = Charge, consume-per-use, IN the POC (the shield-pierce economy).
   Still open: whether shield-piercing needs an extra damaging-resolution beyond the bypass.
3. Keystone taxonomy; budget math (≈55%/≈120%); gold buying low-tier runes (§11).
4. ~~Race roster beyond Human/Elf; full Core-rune roster~~ MOSTLY RESOLVED 2026-07-05 (v6): FIVE races
   (RACES.md, Half-Giant locked via Cowork) × SEVEN cores (CORE_RUNES.md, Barbarian added). Still open:
   the **race↔core-rune restriction matrix** (§7) — POC: ALL combos allowed; a later content pass —
   and further roster growth.
5. Minion-type acquisition; **CON-as-minion-resource** (§9) — DROPPED, resource is Summons.
   PARTIAL 2026-07-04: Skeleton/Golem (INT, damage-lean) + Hound (DEX, utility-lean placeholder) are
   content now (§9). Still OPEN: the full stat→role table (STR minion undecided; DEX's real
   utility/evasion EFFECT, not just low damage) + a formal tier ladder for minions.
6. Campaign specifics (§12) — city count, procgen vs authored, supply costs; war-party arrival = instant
   loss vs **camp-defense last stand**; crafting from siege resources (floated).
7. Healless compensation archetype(s) — heal-spam vs damage-tank (§6b/§10).
8. Shield numbers — per-source point caps + regen rates; the shield-wall troops→points formula (§6b).
9. Balance/feel tuning overall (stat bases, budgets, spoils/prices, supplies vs march length, foe HP,
   damage, DEX-haste rate/cap, CON→HP ratio, evasion %, castle cadences) — the "play it and tune" pass.
10. ~~The Core-rune stat blocks are placeholder~~ SUPERSEDED 2026-07-05: v6 gives cores ADDITIVE stat
    bonuses + layout numbers (CORE_RUNES.md) and races their v6 blocks (RACES.md) — blessed-initial,
    tuned at the balance-playtest pass like everything else (#9). Known deliberate outlier: Barbarian's
    kit over-demand (CORE_RUNES.md note).
11. Part→stat friction (legs = accuracy, arms = STR) — low-pri revisit only if it nags.
12. Action speed beyond DEX haste — none planned; revisit only if needed.
13. Redeploy lockout tuning (the DEX→lockout curve) and the FTL-style commit-to-destination on
    Redeploy/Retreat (§12) — both deferred; flow ships first.
14. RESOLVED 2026-07-02 — a core rune's signature effect is the **Core Effect** (renamed from the
    placeholder "apex"). Sweep `apex`→`Core Effect` across docs + code (`ApexName/ApexDesc` →
    `CoreEffectName/CoreEffectDesc`) + the manifest `apex*` binds/labels. (Leave §11's keystone
    "apex-tier" wording — different meaning.)
15. Figure MORPH model + MULTI-SLOT equipment (§7): figures = human base + race morph + core-rune morph +
    equipped-gear parts (a morph model, not per-race×core×gear art); a piece may cover multiple part slots
    (robe = all/most). Exact morph mechanics + the multi-slot slot model — design BEFORE building the
    gear-swap system (today gear is starting-set only; GEAR cards are sample/design-open).
16. ~~ITEM-RANKING / auto-unequip priority~~ RESOLVED 2026-07-03 (§6e): disables highest-requirement-
    first, ties last-equipped-first — a pure ranking over the live attr level. Numbers ride the
    balance pass like everything else.
17. ~~MERCHANT SCREEN~~ RESOLVED 2026-07-03: design/07 v2 + the manifest `merchant` screen ARE the
    design; popover retired; click-to-buy receiving LOCKED in §12. Residual OPEN: ware pricing/rarity
    economy tune (part of the balance pass).
18. ~~Wand partial-shield-bypass math~~ RESOLVED 2026-07-03: wands REDEFINED — 1H hand items, no
    bypass, no Charge; damage minus the foe's STANDING shield count, shields not consumed (§6d/§10).
19. ~~Weapon primary/secondary swap timing~~ MOOT (2026-07-03): retired with the "benched two-hander"
    framing it depended on — dual-wield is simultaneous, not benched (§6d correction).
20. ~~Sling stat + names~~ RESOLVED 2026-07-03: DEX confirmed (1 DEX/tier), ladder locked (Shepherd's →
    Braided → Sinew → Giantsbane Sling); damage/tier rides the balance pass (#9), as does bow dmg/tier.
    ~~CON-shield equip-gate number~~ RESOLVED 2026-07-04: 1 CON/tier (§6c).
21. Ranged-only builds (no melee weapon at all, §6d) — viable in principle but not fully thought through;
    they lose the CON **block** shield-source specifically (needs a held shield) though other block
    sources don't need one. Revisit during the balance pass.
22. Ranged-weapon RENDER MOUNT (§6e): where an equipped bow/wand draws while the melee hands are full —
    assumed default: NOT drawn until a back-mount figure layer exists (fold into the figure-art regen
    batch, payload B2); do not invent art meanwhile.

## 18. DROPPED — must not resurface
- **"Chassis" as the identity model** → split into **Race + Core rune** (§7).
- **"A Core rune carries NO attributes"** → v6 (2026-07-05): cores grant ADDITIVE stat bonuses on top
  of race base (CORE_RUNES.md); race remains the only source of BASE attrs + HP.
- **The 2026-07-03 Core Effect roster** (*Hollow Vessel* / *Unbroken Aegis* / *Overchannel* / *Legion* /
  *Bloodrush* / *Called Shot*) → the v6 seven-effect roster with buildable rules text (CORE_RUNES.md);
  Summoner's old Legion refund mechanic retires in favor of *Conscription*.
- **"Bay(s)" as the minion-slot term** → "Minions"/minion capacity only (2026-07-05 canon rename).
- **Multi-foe combat / multiple targets / a "front" fallback target** → **single enemy**, part-aim only (§8).
- **A fire button; a focus/selected-technique cursor; techniques starting powered/targeted; AUTO on by
  default** → the targeting FSM (§8): start inactive+untargeted, charged+targeted fires, AUTO off.
- **The sustained "CON-block, flat-while-held, capped" model** → the **shield-point system** (passive
  source, regenerating 1-dmg points), §6b.
- **The five-attribute model (STR/DEX/INT/WIS/CHA)** → **four** (WIS→INT, CHA dropped), §6.
- **"Unallocated CON passively mitigates"** → CON powers active shield sources + regen, §6b.
- **Binary-only part degradation** → graded stat-capacity reduction, §6.
- **Rallied support as enemy-side repair** → player's undamageable auto-fire on the castle, §12.
- **Healing magic/potions restoring HP** → heals repair PARTS; HP is out-of-combat only, §10.
- **The old direct part→capability map** (head→accuracy, torso→action-speed, …) → capabilities flow
  through the part's stat, §6/§8.
- **Fixed rune slots (1 Core / 6 Path / 4 Mark / 1 Keystone)** → budget economy, §11.
- **Branching skill-trees with forks** → simple minor→major trait lines, §11.
- **"Spread"/divisor strength; stamina-per-swing** → attribute reservation, §5.
- **One-hand-at-a-time / serial player actions** → parallel-by-allocation, §5.
- **Poise / stagger bar; a TEMPO/PERIL combat header** → dropped (§13).
- ~~Separate muster/upkeep resource for minions → cost is the gate~~ — **RE-INTRODUCED 2026-07-02** as
  **Summons**: a minion is gated by a reserved stat AND costs Summons to field/re-summon (§9/§14).
- **Weapon durability / maintenance** → covered by the disable layer, §8.
- **Party members / companions** → minions only, §9.
- **Summoning as an action-bar technique** → minions with instant toggle, §9.
- **Armor on the action bar; armor that gates/grants attributes** → per-group slots; light effect layer
  keyed to type (STR = part-damage mitigation, DEX = evasion, INT = spell dmg, shield = block recharge —
  full ladders §6c), §6/§13.
- **Verbs bound to weapons** → weapons grant zero verbs; techniques consult gear, §7.
- **Multiple consumable stockpiles** → one **Charge** resource + gated minions, §10.
- **Charge as a generic "magic-tier" cost (any spell/magic verb draws it)** → Charge is spent ONLY by
  SHIELD-IGNORING techniques (all bows + designated pierce verbs) — the shield-pierce economy, §10.
- **Integer-only / letterbox-only scaling that bars out** → aspect-independent fill (cover bg + anchored
  HUD + integer pixel stage), §13.
- **C++/SDL, editor-centric / ECS / Godot** → MonoGame DesktopGL, code-first, no ECS, §15.
- **HP damaged only via penetrating/bypass or part-overkill** → every hit deals part damage AND HP
  damage at once, mitigated only by a shield block or a full evade, §8.
- **Potions as purchasable shop items; a merchant that sells potions/lets you drink to heal** → a
  "potion" is one flavor of heal-body-part TECHNIQUE (found/slotted, never bought); the merchant sells
  only out-of-combat HP healing (gold → HP at 1 HP per randomized cost), §10/§12.
- **Auto-attack as a per-weapon setting** → AUTO is ONE GLOBAL toggle; when on, a fired weapon re-fires
  on its next charge at the kept target, §8.
- **Wand as a RANGED-slot, Charge-spending, partial-bypass weapon** → wands are 1H HAND items with the
  shield-SUBTRACTION model (no Charge, shields not consumed), §6d/§10 (2026-07-03).
- **STR-armor prestige names** (Skull Cap/Barbute/Great Helm/Crowned Helm; Splint Mail/Half Plate/Full
  Plate; Splint/Banded/Plate Gauntlet lines; Half-Plate/Full Plate Legs) → the weapon MATERIAL ladder
  (Iron/Steel/Mithral/Dwarven Steel) on plain slot nouns (Helm/Breastplate/Vambraces/Greaves), §6c.
- **"Mithril" spelling** → **Mithral** (IP-safe), §6d ladder.
- **DEX 2H spear** (floated + reversed same day, 2026-07-03) → NO DEX two-hander; the bow is DEX's 2H.
