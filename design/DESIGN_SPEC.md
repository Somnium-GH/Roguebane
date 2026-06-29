# Roguebane — design spec (current, authoritative)
*Single source of design truth for the Claude Design designer. Supersedes any earlier
consolidated spec. Last reconciled with the active design + engineering decisions.*

## 0. How to read this — READ FIRST
Status tags govern everything below:
- **[LOCKED]** — decided. Build on it. Do not contradict it.
- **[OPEN]** — genuinely undecided. **Do NOT invent an answer.** All open items are gathered in
  §17; if you need one resolved to proceed, ask — do not fill the gap yourself.
- **[DROPPED]** — considered and explicitly abandoned. **Must not resurface** (§18). Reintroducing
  a dropped idea is a regression, not a contribution.

**Cardinal rule for the designer: when something isn't specified here, it is OPEN, not yours to
decide. Surface it; don't invent it.** Most of the "inventing things" problem is filling §17/§18
gaps with guesses. Don't.

**IP guardrail.** FTL, Shadowbane, and Path of Exile are named in this doc **only as private design
references** so we share a vocabulary. They must NEVER appear in shipped assets, UI, text, names,
or art, and we do not replicate FTL's exact interface or trade dress. Same systems grammar, our own
identity (the retrofantasy skin, §13, is what makes us not a clone).

---

## 1. Concept [LOCKED]
A roguelike where **the player *is* the socketed thing**: the character's body is the frame that
holds runes, weapons, and abilities — sockets are on the *character*, not on equipped gear.
Real-time-with-pause combat shown as a **side-on cutaway of the body you've configured**, its parts
lighting up and taking *localized* damage. Art direction is **retrofantasy storybook** (§13).

Design references (private): **FTL** (presentation, run structure, allocate-power-across-systems
feel), **Shadowbane** (rune lineage, city-siege homage), **Path of Exile** (prerequisite ladders,
keystones, deep build space). The distinctive core is **attributes-as-live-allocatable-power**
driving a classless, budget-based build economy.

## 2. Name [OPEN]
Working title **Roguebane**. Not final.

## 3. Core pillars [LOCKED]
- **The body is the board.** You are a structured entity of parts and sockets; building yourself
  and fighting both happen *to that body*.
- **Attributes are reactor power** (§4–5): a live pool you *allocate*, not a static sheet.
- **One combat grammar everywhere** (§8): every foe, first enemy to final castle, is a *structured
  thing with targetable parts*. You disable/cripple parts, manage allocation under the clock, bring
  it down.
- **The run marches on a castle** (§12). Resource control points along the way **bank support** that
  fires for you at the finale — the rebel fleet, inverted. After each castle you advance toward a
  final **Capital City** and its strongest castle.
- **The thesis** (§11): a player can exploit a chassis's *structure* to build something it wasn't
  obviously built for, and doing so feels clever and good. Everything serves this.

---

## 4. Combat substrate [LOCKED]
**Real-time with pause.** Techniques run on timers; pause to re-allocate attributes and decide.
Time-based effects are literal ("can't defend for 10s"). Fixed-timestep, deterministic.

## 5. Attributes & the allocation economy [LOCKED]
**Four attributes** (was five — see §18): **STR, INT, DEX, CON.** They are a **live, allocatable
pool**, like reactor power — not consumed-and-regenerated, not a divisor.
- An active action **reserves** its required attribute(s) while active or charging.
- A **timered** action returns its reservation when it **fires**. A **sustained** action holds its
  reservation while held, returns it on release.
- **Real-time requirement:** if an attribute drops below an active action's requirement *before it
  fires*, the action **loses all progress and deactivates** until the attribute recovers. While
  deactivated, that attribute is **free** for other use.
- Parallelism is limited only by the pool — multiple timers run at once if you can afford to reserve
  them all simultaneously ("three greatswords at once" is allowed if you have the STR).
- Attributes come **only from chassis base + the parts that carry them + Marks** (§11). **No buying
  flat stats with gold.**

### 5a. SCALE — low numbers [LOCKED]
Everything runs on **small integers**. **~20 is a HUGE attribute.** Damage and healing move in
**1–3 steps**. A hit subtracts **1–3 directly from the targeted part's stat**; repair restores 1–3.
This keeps attack-power and mitigation math in single digits and locks the balance envelope.
Integer-only (determinism): fractional coefficients run in sub-units (e.g. DEX's 0.25× attack as
`DEX/4`), never floats.

## 6. The four attributes & the body [LOCKED]
**One part, one stat.** Each attribute physically lives in a body part; damaging the part subtracts
that stat from the live pool until repaired. This single rule unifies degradation, equip/ability
fall-off, and the allocation economy.

| Attribute | Part | Governs / scales | Gates (equip / power) |
|---|---|---|---|
| **STR** | Arms (×2) | attack power (1.0×); STR-based actives | STR weapons; **equips shields** (heavy = STR); part-mitigating armor (plate) |
| **INT** | Head (×1) | spell power; keeps spell actives **and** passives running | spell-bonus armor *(absorbs old WIS)* |
| **DEX** | Legs (×2) | evasion; **accuracy**; **+0.25× attack power** | DEX weapons; evasion armor (leather) |
| **CON** | Chest (×1) | HP scaling; stun resistance (passive floor) | **defensive-active**: see §6b |

- **WIS** merged into INT; **CHA** dropped (§18).
- **Part multiplicity & damage:** `Head×1, Chest×1, Arms×2, Legs×2`. **Paired parts each take damage
  independently and each carry a SHARE of their stat** (one arm = half your STR; one leg = half your
  DEX). **Armor is one piece per part-group**, part-mapped (helm→head, breastplate→chest,
  arm-armor→arms, greaves→legs); **weapons are held in hands** (hand count anatomical). Lose one arm
  → lose its STR share → you can fall below a weapon/shield/plate equip threshold and that gear
  **drops off**. The cascade *is* the combat depth.

### 6b. CON is a defensive-active stat [LOCKED]
CON gates no equipment, so it earns its keep actively. **Sustained defensive techniques (shield
block, Brace) RESERVE CON while held and absorb up to the CON reserved, capped** — raise the block =
power it; drop it = CON returns to the pool. **STR carries/equips the shield object; CON powers
holding the block up.** The tension: pour CON into a block and you're tanky but you've starved that
CON from other use. *(This retires the earlier "unallocated CON passively mitigates" idea — it ran
backwards to reserving CON for a block; see §18.)*

## 7. Actions — the three-layer architecture [LOCKED]
The most important structural idea. Three layers over the shared attribute pool:
1. **Chassis (Core).** Defines *anatomy*: action-bar size, armor slots, minion bays, the body's
   parts, the rune budget. Per-chassis structural difference is the identity engine. Sits above the
   rune economy; not bought with budget.
2. **Loadout (installed things).** Equipped weapons (in hands), armor (per-group slots), minion-bay
   contents, shields, runes — configured **between fights**, sealed during combat.
3. **Action bar (verbs).** **Techniques** — the live actions you fire. Each technique **consults the
   relevant equipped gear and reserves attributes**; each is **timered** or **sustained**. Small,
   Core-set size.

**Verbs are NOT bound to weapons. Weapons grant zero abilities** — a weapon is a stat-stick object
with properties. Techniques *consult* whatever is equipped: "Swing" consults your primary weapon;
"Frenzy" consults *both* weapons (cost = sum of their reservations). Weapon = potential; technique =
expression. Techniques are their own findable/slottable layer.

## 8. Parts, localized damage, targeting [LOCKED]
Every entity (player and enemy) is a **structured thing with targetable parts**. Capability flows
through the part's stat (§6), so harming a part degrades what that stat does.
- **Persistent damage** subtracts from the part's stat (graded, §6); restored only by a **non-trivial
  repair** (a potion/repair that takes time and exposes you).
- **Disable** switches a part off temporarily (disarm = hand off; silence = head off; blind; stun;
  shieldbreak), and **returns the part's reserved attribute to the pool**. Disables recover over
  time; you can spend an action to clear one faster.
- **Silence-on-head is now emergent:** head damage lowers INT → spells can't stay reserved (graded);
  a head *disable* is the hard off.
- **Targeting [LOCKED]: per-technique aim** — each technique aims its own target part.
- **Non-human enemies (future):** distinctive part-maps — hydra (redundant heads), golem (no head →
  immune to blind), dragon (wings as a positioning part), scorpion (severable stinger), mounts (two
  stacked entities). Direction is locked; specific creatures are illustrative.

## 9. Minions [LOCKED, with open re-gating]
Minions yes; **party no** — one main character only.
- Minions live in **dedicated bays** (Core-set count). **Instant toggle activation**, not an
  action-bar cast.
- A bay **requires its type's attribute in real time**; below requirement the minion goes **idle
  (not destroyed)** and reactivates free once the attribute recovers. The attribute scales minion
  quality.
- **[OPEN]** minion-type **gating** must be re-homed now that WIS/CHA are gone (was CHA followers /
  WIS beasts / INT undead / STR-subordination). POC's skeleton stays INT-gated.
- **[OPEN]** how a minion *type* is acquired (generic bays + schematics vs. chassis-flavored).
- No separate upkeep/muster resource — the cost of fielding a minion **is** its reservation (§18).

## 10. HP, healing, the magic/charge resource [LOCKED core; details OPEN]
- **HP** is a small life total, separate from the part/stat layer. **HP damage is permanent within
  an encounter** (player and castle alike).
- **Healing split [LOCKED]:** potions and healing magic **restore PARTS** (full or partial, scaled
  by the attribute invested), **never HP**. **HP is restored only out of combat** — a shop healing
  service or a non-skirmish (quest-like) encounter.
- **A finite, refillable magic/charge resource** (name TBD) fuels **special / rule-breaking / magic**
  effects and weapon affixes, refilled via loot/gold, **distinct from the attribute pool**. Basic
  actions cost only attribute reservation (free to repeat); spikes/bypassers additionally draw this
  finite resource — which is *why* magic is logistically fragile despite being cheap in attributes.
- **[OPEN]** its name; consume-per-use vs. deplete-via-allocation; how present in the POC.

## 11. The rune system [LOCKED core; some numbers OPEN]
The build economy. **No fixed rune slots** — runes are bought from a **point budget set by the
chassis** (grown by progression). The *shape* of your rune loadout is itself an expression of build.
- **Core / chassis (the archetype)** — sits above the budget, not bought. A named chassis defined by
  **structural leanings, not granted powers** (race/class is pure flavor). Sets anatomy + budget.
  Master axis: **specialist** (high base stats, free structure, but full rune prices / tight budget)
  ↔ **generalist** (cheap runes, fat budget, but low base stats).
- **Marks (stat-shapers)** — cheap, quantitative; their purpose is the **prerequisite ladder**.
  Same-attribute Marks **overwrite** (don't stack); smaller ones are stepping-stones.
- **Paths (identity traits)** — modular traits in **escalating magnitude (minor → major)**; the major
  contains the minor's effect (bigger number) **plus a qualitative kicker**. Simple trait lines, not
  branching trees. Layered additively within budget.
- **Keystones** — apex, play-defining runes with real downsides; **rare by price** (no hard cap),
  prerequisite-gated. POC keystone: **Hollow Vessel** (unspent budget converts live into regenerating
  attribute/aether — rewards running almost no runes; the generalist's "convert surplus" exploit).
- **Prerequisite ladder [LOCKED]:** big runes require a minimum in an attribute, met **either** by
  chassis base (efficient) **or** by spending budget on Marks to climb (a tax). The climb is
  **mostly refunded but leaks** (partial refund), so native qualification beats climbing.
- **Permanence [LOCKED]:** runes are permanent commitments with an **in-family minor→major upgrade
  path**; committing to a family locks you to it. Early commitment is planting, not gambling.
- **Thesis [LOCKED]:** the **generalist can out-specialize a specialist** by funding an attribute
  climb with budget surplus to reach a keystone its chassis wasn't built for — but spends everything,
  so it's all-in and fragile.
- **[OPEN]** budget math (≈55% keystone / ≈120% budget-chassis targets); keystone taxonomy;
  gold buying low-tier runes (the "mostly found" lean is the solid part).

## 12. Run structure & the map — layered [LOCKED; some specifics OPEN]
The map is **nested layers, macro → micro:**
- **Layer 1 — campaign spine (cities).** A sequence of cities, each held by a castle, marching to a
  final **Capital City** whose castle is strongest and most complex. Beat a castle → road to the next
  opens. Complexity escalates along the spine. **[OPEN]** city count; procgen vs. authored (POC = one
  leg).
- **Layer 2 — the leg (node graph to one castle).** A node map crossed node-by-node; **movement burns
  Supplies** (the fuel analog). **Limited freedom of movement** — pick a route through a branching
  graph; you can't touch every node. The leg ends at the castle. **[OPEN]** how wide the freedom is;
  how Supplies cost scales.
- **Layer 3 — node types (the texture of a leg):**
  - **Skirmish point** — a combat encounter; default traversal node.
  - **Resource control point / mine** — a siege you win to **bank support** (and resources); the
    strategic must-stop. Skipping or losing it forfeits that support.
  - **Merchant town** — low hostility: shops, quest-like encounters, and where **HP is restored** as
    a service (§10).
  - **Mountainous region** — resource-rich but more hostile (wildlife, undead): high-risk detour.
  - **Flee** — bail out of a node.
- **Layer 4 — encounter resolution.** Every node resolves into the one combat grammar (§8). The
  **castle is that grammar at max scale** (gate, wall, catapult, ballista).
- **The siege finale [LOCKED]:** you **+ banked support** vs. the castle. **Banked support = the
  inverted rebel fleet:** player-allied, **cannot be damaged**, an **intermittent auto-fire stream
  that just chips the castle from time to time** (an abstracted stream, not simulated units), scaled
  by how much you banked. The castle is the boss: it deals **reasonable, boss-level, map-tier-scaled
  damage to you**, and has **"systems"** (its parts) you deal with to swing a hard run. Under-bank
  support and the castle likely grinds you down first.
- **Forward pressure — the enemy war party [LOCKED]:** each stage's castle sends a **war party that
  marches on your camp** (your rear supply base and staging point). It advances **one step per node
  you move**, so every detour to bank support also lets it gain ground. **Crack the castle and its war
  party disbands** — that's how you win the race. **Let the war party reach camp and your supplies are
  cut — no more sieges — and you lose the run.** A fresh war party per leg. The two-way race (you
  besiege their castle while they counter-march on your base) is the forward-movement pressure. The
  map should read it at a glance: camp (rear), the advancing war-party token, your position, and the
  castle (front), with a closing-distance indicator.
  - **[OPEN] knobs:** arrival = **instant loss** (POC default) vs. a **camp-defense last stand** in
    the same combat grammar — you become the defender — as the design target; whether the party can be
    slowed beyond cracking the castle.
- **The core tension (now a race):** Supplies meter *how much map you can afford to cross*; banked
  support is the sum of control points won, spent at the castle; the **war-party clock** caps *how
  long you have*. Detour for support and you bleed HP/Supplies **and give the enemy ground**; rush the
  castle and you arrive intact but under-banked. Tune so banking *a little* is always viable and only
  **greed** loses the race.
- **[OPEN]** a crafting system fed by siege resources.

## 13. Presentation — the designer's core [LOCKED]
- **Cutaway grammar.** The screen is a **side-on cutaway of the character** — the body-as-board —
  systems visible and taking *localized* part damage. The look should *be* the mechanic.
- **Retrofantasy art [HIGH fidelity]:** a crisp, high-resolution rendering of oldschool 8-bit /
  EGA-VGA storybook fantasy (Zeliard-style) — warm, hand-painted, fairytale, not sci-fi — as if that
  retro art were **upscaled to HD while keeping its old-school soul.** Retro *style*, high *quality*
  (NOT low-res, NOT crude). This skin is our identity and what keeps us from reading as a clone.
  (The POC's placeholder rectangles are build scaffolding, NOT this target look.)
- **Two screens [LOCKED]:**
  1. **Cutaway build/loadout screen** — the between-combat config screen. Sealed during combat. Shows
     chassis anatomy, the rune budget + prerequisite ladder, equipped weapons/armor, the action
     loadout.
  2. **Combat damage screen** — shown during combat; the cutaway taking localized part damage.
- **Two registers [LOCKED]:** world/combat can run bolder, more atmospheric, more zoomed; the
  **build screen must stay clean and legible** — a dense socket/budget interface needs clarity, so
  dial back flourish and busyness there. Both at the **same high-fidelity retro bar**; the build
  screen just trades some flourish for legibility.
- **Combat screen layout [LOCKED]:** three vertical zones, **you | battlefield | foe**.
  - *You* = the cutaway body with its parts: **Head×1, Chest×1, Arms×2, Legs×2**, each showing its
    localized damage state and its stat; armor pieces overlay part-groups.
  - **The attribute pool is the most prominent element** — the central mechanic must not be buried.
    Show reserved vs. free at a glance (incl. CON held in a block).
  - *Action bar* = technique slots with **parallel** timers (timered + sustained; a held shield-block
    reads as a sustained CON reservation).
  - *Battlefield* = autonomous minion(s) + the **rallied-support auto-fire stream** firing into the
    background toward the foe.
  - *Foe* = the structured enemy/castle with **targetable parts** and **per-technique targeting
    reticles**.
  - Corrections from earlier wireframes: the action bar is **parallel**, not one-hand-at-a-time; there
    is **no poise/stagger bar**; minions live in **bays**, not action-bar slots.

## 14. Economy / resources summary [LOCKED, modulo §10]
- **Attributes (STR/INT/DEX/CON):** live allocation pool; from chassis base + carrying parts + Marks;
  no gold purchase; low-number scale (§5a).
- **Magic/charge resource (name TBD):** finite, refillable; fuels magic-tier effects + affixes.
- **HP:** small life total; restored only out of combat.
- **Parts:** localized; damage subtracts the part's stat; potions restore parts.
- **Gold:** loot/sell currency; shops; sink for the charge resource, consumables, maybe low-tier runes.
- **Rune budget (points):** the build economy; separate from gold and in-combat resources.
- **Supplies:** map-traversal fuel. **Banked support:** accumulated from control points, spent as
  castle auto-fire.

## 15. Tech & architecture [LOCKED — context for the designer]
MonoGame (C#), DesktopGL, Steam-first, code-first (no editor). **Logic-core / render-shell split:**
`Roguebane.Core` is pure, headless, deterministic simulation (all rules); `Roguebane.Game` is a thin
shell that reads Core state, draws it, sends input back as commands — **no game rules in the shell.**
Content is **data, not code.** What this means for design: visuals render *whatever the Core says*,
so part/stat/damage states the art must depict come straight from the model above — design to that
model, not around it.

## 16. The POC (vertical slice) [LOCKED]
A *gym*, not a game — crudest functional real-time-with-pause (rectangles + timers, not animation),
to answer: **does exploiting a chassis's structure to build something it wasn't built for feel good?**
- Two chassis: **Grunt** (generalist) + one **specialist**.
- Real attributes: **STR + INT** do the core work; CON enters via the defensive-active block (§6b)
  if we test the block tension; DEX minimal.
- ~5 techniques (Swing, Frenzy, Firebolt, Disarm, Brace) + one minion bay (a skeleton, INT-gated).
- Minimal rune economy: budget; a few Marks (the ladder); one minor→major family (the refund climb);
  one keystone (Hollow Vessel).
- 2–3 enemies with targetable parts + one structured castle (gate/wall/catapult/ballista).
- One fixed siege: 2 control points → the castle; banked support fires; a flee option.
- Out of scope: procgen, gold/shops beyond minimal, meta-progression, save, sound, animation.

---

## 17. OPEN questions — do NOT invent answers
1. Game name (§2).
2. HP-vs-stat damage split (§10) — working default: attacks deal stat damage to the targeted part;
   HP only from penetrating/bypassing sources or overkill once a part bottoms out.
3. Shield-block mechanic (§6b) — flat-while-held vs. depleting/recharging; working default: flat.
4. Arms/legs equipment (§6) — working assumption: armor one-piece-per-group, weapons per-hand. Confirm.
5. Minion re-gating after WIS/CHA removal (§9).
6. Minion-type acquisition (§9).
7. "Action speed" capability — it had no home after the rework (was the torso/chest capability, now
   CON does HP/stun/block). Fold into a stat or drop? (§6/§8)
8. CON→HP timing (§6) — does chest damage lower MAX HP, or only the available pool?
9. Magic/charge resource (§10) — name; mechanic; POC presence.
10. Keystone taxonomy; budget math (≈55%/≈120%); gold buying low-tier runes (§11).
11. Map (§12) — city count; procgen vs. authored; Supplies cost scaling; how wide the route freedom is.
12. War-party forward pressure is now LOCKED (§12). Remaining knobs: arrival = instant loss (POC)
    vs. camp-defense last stand; and whether the party can be slowed beyond cracking the castle.
    Crafting from siege resources is still only floated.
13. Exact action-bar size (small, Core-set; POC ~5 + 1 bay).

## 18. DROPPED — must not resurface
- **The five-attribute model (STR/DEX/INT/WIS/CHA)** → now **four (STR/INT/DEX/CON)**; **WIS** merged
  into INT; **CHA** removed entirely.
- **"Unallocated CON passively mitigates"** → CON defense is **active reservation** (shield/Brace),
  §6b.
- **Binary-only part degradation** ("full output until destroyed") → graded **stat-capacity
  reduction**, §6.
- **Rallied support as an enemy-side repair/reinforcement** → it is the **player's** undamageable
  auto-fire **on the castle**, §12.
- **Healing magic/potions restoring HP** → potions restore **parts**; HP is out-of-combat only, §10.
- **The old direct part→capability map** (head→accuracy, torso→action-speed, hands→attack, feet→dodge)
  → capabilities now flow **through the part's stat**, §6/§8.
- **Fixed rune slots (1 Core / 6 Path / 4 Mark / 1 Keystone)** → **budget economy**, §11.
- **Branching skill-trees with forks** (and floated family names) → **simple trait lines**, §11.
- **"Spread"/divisor strength; stamina-per-swing** → **attribute reservation**, §5.
- **One-hand-at-a-time / serial player actions** → **parallel-by-allocation**, §5.
- **Poise / stagger bar** → dropped.
- **Separate muster/upkeep resource for minions** → cost is the attribute reservation, §9.
- **Weapon durability / maintenance** → covered by the disable layer, §8.
- **Party members / companions** → minions only, one main character, §9.
- **Summoning as an action-bar technique** → **bays with instant toggle**, §9.
- **Armor on the action bar** → **dedicated per-group slots**, §6/§13.
- **Verbs bound to weapons** → weapons grant zero verbs; techniques consult gear, §7.
- **Multiple consumable stockpiles** → one magic/charge resource + attribute-gated bays, §10.
- **C++/SDL, any editor-centric / ECS / Godot path** → **MonoGame DesktopGL, code-first, no ECS**, §15.
