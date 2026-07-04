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
  attribute heals** — it does NOT leave the slot. The cascade *is* the combat depth. **[OPEN §17: when an
  attribute drops below MULTIPLE items' requirements, a rule / ITEM-RANKING decides which disables first.]**
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
- **block** (CON), **stoneskin/barkskin/steelskin/diamondskin** (INT, passive spell), **parry** (DEX,
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

**STR — heavy/plate (Head, Chest, Arms, Legs):**
- Head: Skull Cap → Barbute → Great Helm → Crowned Helm
- Chest: Breastplate → Splint Mail → Half Plate → Full Plate
- Arms: Vambraces → Splint Vambraces → Banded Gauntlets → Plate Gauntlets
- Legs: Greaves → Splint Greaves → Half-Plate Legs → Full Plate Legs

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

### 6d. Weapons — the wield model [LOCKED, 2026-07-03; a couple numbers OPEN, tagged below]
Weapons are pure stat-sticks (base damage only) — techniques own ALL timing/effects (§7, unchanged).
- **No dynamic hand-reservation.** Rejected in favor of the simpler rule: a technique doesn't reserve
  hands over time the way it reserves an attribute — it just REQUIRES a matching weapon config to be
  PRESENT (equipped + arms unbroken) the instant it fires. Weapon attacks are instantaneous-if-charged;
  there's no hold/release lifecycle for hands beyond that instant check.
- **Equip configurations:** `1h + shield` · `2×1h` (dual-wield) · `2×2h` (own two two-handers —
  greatsword, bow, wand — but only ONE is ever physically wielded; see primary/secondary below) ·
  `1× ranged` (bow/wand, both hands, worn on the back when not the active primary).
- **Lockout conditions [LOCKED]:** a dual-wield technique needs BOTH arms unbroken with two 1h weapons
  equipped, or it's locked out; a 2h technique (incl. bows/wands) needs BOTH arms unbroken, or it's
  locked out; a shield needs a FREE arm — it drops whenever both arms are already committed to a 2h
  weapon or a dual-wield pair. Same rule for foes (§8 symmetry): arms/legs are separately targetable and
  breakable on both sides.
- **Primary/secondary auto-promotion [LOCKED]:** within a hand-slot category, first-equipped = PRIMARY,
  second = SECONDARY. Unequipping the primary promotes the secondary; a newly-equipped item always fills
  the (now-empty) secondary slot — it never displaces an existing primary. Guarantees a technique never
  finds a null primary while a valid secondary sits idle. **[OPEN]** whether swapping which one is active
  mid-combat costs an action/cooldown or is free — tune later.
- **Handedness [LOCKED]:** a player-facing Left/Right-handed setting (cosmetic only) fixes which physical
  arm renders the primary weapon vs. the off-hand item, so a broken/bare-arm visual always matches the
  item that would actually drop from that specific arm.
- **Ranged weapons share the both-hands/back-slot model.** **Bows (DEX):** fully ignore the shield pool,
  gated by Charge — unchanged, §10. **Wands (INT):** a second ranged flavor, PARTIALLY bypass the shield
  pool (so Charge isn't a bow-only resource) — **[OPEN §17]** the exact bypass split/math; the shape
  (partial, not full) is locked, the number is not.
- **Names [blessed initial, tune later]:** Bows — Short Bow → Long Bow → Compound Bow → Elven Bow.
  Wands — Adept Wand → Twisted Wand → Gemstone Wand → Glowing Wand.

## 7. Race + Core rune, and the three-layer architecture [LOCKED]
Identity is **two axes** (FTL ship + layout):
- **RACE** — sets **starting attributes + base HP** (the ONLY source of base attrs; Core runes add none).
  Start with **Human + Elf**. Placeholder blocks from design/05 (tune later): **Human** STR3/INT3/DEX3/
  CON3, HP20 (balanced, no edge); **Elf** STR2/INT3/DEX4/CON2, HP14 (keen + fleet, frail). Verified
  winnable for EVERY core at these low-scale stats with the intended PART-AIM play (disable the boss's
  arm), CoreCampaignTests. **All race×core combos allowed for the POC** (restriction matrix deferred, §17 #4).
- **CORE RUNE** (the Shadowbane "Core rune") — sets **LAYOUT**: rune budget, action-bar size (#
  techniques), # minion bays, and a **Core Effect** (its signature effect, stronger than a keystone —
renamed from "apex"). **Races GATE which
  core runes they may take** (an SB-style restriction matrix). New Run = pick Race → pick Core rune
  (race-allowed). *(This replaces the old single "Chassis" concept — §18. The archetypes
  Grunt/Warden/Adept/Summoner/Reaver/**Ranger** are CORE RUNES; Race is the new orthogonal axis. (Ranger =
  the DEX marksman, the reference build for the bow / shield-pierce economy, §10 — added to stress-test
  the data-only core-rune path.)*

Three layers over the shared attribute pool:
1. **Race + Core rune = the LOADOUT.** Race sets base attrs+HP; the Core rune sets layout (budget / bays /
   action-bar size / apex). Together they are your **Loadout** — the assembled identity you take into a
   run (above the rune economy; not bought). *(This is the freed-up "Loadout" term; the old "Core" label
   retires.)*
2. **Equipment (installed things).** Weapons (in hands), armor (per-group slots), minion-bay contents,
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

**Verbs are NOT bound to weapons.** A weapon is a stat-stick; techniques *consult* what's equipped
("Swing" = primary weapon; "Frenzy" = both, cost = sum). Techniques are a findable/slottable layer.

## 8. Combat: single enemy, parts, targeting/firing [LOCKED]
**Combat is always against ONE enemy** (a human foe, an atypical creature, the castle, or a special
resource fight) — which may be **multi-PART**. The only targeting is **part aim within that one enemy**;
there is no multi-foe list and no default/front target.

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

**Targeting PRESENTATION [LOCKED 2026-07-03]:** while TARGETING, the **cursor IS the reticle** — the OS
cursor hides, the reticle sprite draws at the cursor position and **snaps/centres onto the hovered limb
band**; click locks it there; cancel restores the cursor. **NO box affordances** during targeting — no
whole-foe hover rectangle, no band outline boxes in-game (design/08's dashed bands are doc annotation
only). A locked mount shows the **FOCUS reticle with an animated pulse** (fixed-tick driven,
deterministic) plus an **AIM TAG reading the technique's HOTKEY NUMBER** — not its name (names overflow
and collide when several actives aim the same part); multiple locks on one part **stack their numbers**.
Action-bar cards are **numbered 1–N** (the hotkey), so tag ↔ card correspondence is visual. (Supersedes
design/08's name-tag mount; CD updates design/01 + design/08.)

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
- Minions live in **dedicated bays** (Core-rune-set count), **instant toggle**, not an action-bar cast.
- A bay **requires its gate in real time**; below it the minion goes **idle (not destroyed)** and
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
- **WANDS (INT ranged, §6d):** a second Charge-spending weapon line, alongside bows — PARTIALLY bypasses
  the shield pool (some of the hit still absorbs against the pool, some goes through) rather than the
  bow's full bypass. **[OPEN]** the exact split/math — not yet designed, do not invent a number for it.

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
  `CoreEffectDesc`, e.g. Grunt = *Hollow Vessel*) the build cards render (§13).
  **Core Effect roster CANON [ADOPTED 2026-07-03 from design/05 v2 — CD's pass accepted as the design]:**
  Grunt *Hollow Vessel* — healed for unspent budget points after each encounter · Warden *Unbroken
  Aegis* — shield points regenerate at twice their CON-scaled rate · Adept *Overchannel* — spells
  reserve no INT while the head stays above three-quarters · Summoner *Legion* — surviving minions'
  Summons are refunded on Redeploy · Reaver *Bloodrush* — every part you break refunds a charging
  technique · Ranger *Called Shot* (renamed from *Piercing Focus*) — ranged techniques ignore the foe's
  shield and cover bonuses. Names+copy are LOCKED; the MECHANICS get built in a dedicated effect-model
  pass — until then card copy may describe unbuilt behavior (flagged, acceptable). **EXCEPTION — the
  Summoner's is REAL + LOCKED (2026-07-02)** and already built (§9/§14).
- **Prerequisite ladder [LOCKED]:** big runes need a minimum in an attribute, met by RACE base
  (efficient) **or** by spending budget on Marks to climb (mostly-refunded-but-leaks tax). Native
  qualification beats climbing.
- **Permanence [LOCKED]:** runes are permanent commitments with an in-family minor→major upgrade;
  committing to a family locks you in. Planting, not gambling.
- **Thesis [LOCKED]:** the **generalist can out-specialize a specialist** by funding a climb with budget
  surplus to reach a keystone its Core rune wasn't built for — all-in and fragile.
- **[OPEN]** budget math (≈55% keystone / ≈120% budget-core targets); keystone taxonomy; gold buying
  low-tier runes (the "mostly found" lean is solid).

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
  - **[OPEN §17]** the weighted-shuffle / distribution algorithm (balanced attr-lean variety; e.g. "prefer
    1–2 passives, rarely 0, when techniques show") — a separate tuning pass.
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
  three columns: **Race** (head sprites + attr/HP card) | **Core Rune** (rune icon + budget/actions/bays +
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
    minions in **bays**; **no TEMPO/PERIL header**.

## 14. Economy / resources summary [LOCKED, modulo §10]
- **Attributes (STR/INT/DEX/CON):** live pool; from RACE base + carrying parts + Marks; no gold buy.
- **Charge:** finite, refillable (loot/gold, out of combat); spent by SHIELD-IGNORING techniques (incl.
  all bows) to bypass the shield pool (§6b/§10). NOT a generic magic cost.
- **Summons:** finite, refillable (merchant/loot); SPENT to field a minion, on top of reserving its gate
  stat (§9). The Summoner's Core Effect refunds it for surviving minions on Redeploy (§11).
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
4. Race roster beyond Human/Elf; full Core-rune roster; the **race↔core-rune restriction matrix** (§7)
   — POC: ALL combos allowed; the matrix is a later content pass.
5. Minion-type acquisition; **CON-as-minion-resource** (§9).
6. Campaign specifics (§12) — city count, procgen vs authored, supply costs; war-party arrival = instant
   loss vs **camp-defense last stand**; crafting from siege resources (floated).
7. Healless compensation archetype(s) — heal-spam vs damage-tank (§6b/§10).
8. Shield numbers — per-source point caps + regen rates; the shield-wall troops→points formula (§6b).
9. Balance/feel tuning overall (stat bases, budgets, spoils/prices, supplies vs march length, foe HP,
   damage, DEX-haste rate/cap, CON→HP ratio, evasion %, castle cadences) — the "play it and tune" pass.
10. The Core-rune stat blocks (design/05 + the new Ranger) are placeholder — tune later.
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
16. ITEM-RANKING / auto-unequip priority (§6): when a broken part drops an attribute below MULTIPLE items'
    requirements, a rule/ranking decides which gear disables first — needs design + tuning. Feeds the
    gear system (design-open).
17. ~~MERCHANT SCREEN~~ RESOLVED 2026-07-03: design/07 v2 + the manifest `merchant` screen ARE the
    design; popover retired; click-to-buy receiving LOCKED in §12. Residual OPEN: ware pricing/rarity
    economy tune (part of the balance pass).
18. Wand partial-shield-bypass math (§6d/§10) — the shape (partial, unlike bows' full bypass) is locked,
    the split is not; needs a real design pass before Core implements it.
19. Weapon primary/secondary swap timing (§6d) — whether re-activating the secondary two-hander mid-combat
    costs an action/cooldown or is free; tune later.

## 18. DROPPED — must not resurface
- **"Chassis" as the identity model** → split into **Race + Core rune** (§7).
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
- **Summoning as an action-bar technique** → bays with instant toggle, §9.
- **Armor on the action bar; armor that gates/grants attributes** → per-group slots; light effect layer
  keyed to type (STR = part-damage mitigation, DEX = evasion, INT = spell dmg, shield = block recharge —
  full ladders §6c), §6/§13.
- **Verbs bound to weapons** → weapons grant zero verbs; techniques consult gear, §7.
- **Multiple consumable stockpiles** → one **Charge** resource + gated bays, §10.
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
