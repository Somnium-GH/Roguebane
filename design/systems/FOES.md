# Foes — canon (prototyped)

> **Authoritative design for foes.** The SIX BUILT figures below are specced for T1–T2 and are the
> symmetry prototypes — numbers are Cowork-proposed PLACEHOLDERS awaiting Doug's review, but the loop
> USES them to build the symmetrical-foe system (flag, don't stall). Everything in the IDEAS section is
> **[IDEA] — do NOT build** unless promoted here. Shared mechanics: `../DESIGN_SPEC.md` §4 / §6 / §8;
> siblings `CORE_RUNES.md`, `TECHNIQUES.md`, `WEAPONS.md`, `ARMOR.md`, `RACES.md`.

## Purpose
Combat is always ONE foe (§8). A foe is the same kind of thing a player loadout is — **one combat
grammar everywhere**: a body of targetable PARTS whose stats power its techniques, gear its verbs
consult, and a signature **Foe Effect** (the Core-Effect analogue). Smash the part → its reserve
collapses → the behavior it powers cascades off. Foes never get free ticks: everything a foe does is a
real technique on its own reserve pool (§8 symmetry — the castle's mend already works this way).

## The symmetry model (prototype — build target for the loop)
Parity with the player's loadout, axis by axis:

| Player side | Foe side |
|---|---|
| Race (base attrs + HP) | **Frame** — parts carry the stats; HP is the encounter life total |
| Core Rune (layout + Core Effect) | **Role** (brute / caster / skirmisher / …) + **Foe Effect** |
| Weapons (techniques consult) | Foe weapons — same `Weapon` data, same consult/timer math |
| Armor (§6c lines, sustain gates) | Foe armor — same lines; disable cascade applies to foes too |
| Techniques (reserve + charge) | **Arsenal** — same `Technique` records at TECHNIQUES.md numbers |
| Minions / Summons | [IDEA] only — no foe minions in T1–T2 |
| Aim (player picks parts) | `FoeAim` personality: Random / Smart (+ ideas below) |

**Tier naming:** T2 of a base foe wears the **"Dire"** prefix (per the 01-encounter refs: DIRE OGRE) —
prefix convention placeholder-blessed, tune with content. T2 ≈ +50–100% HP, +1 weapon/armor tier, one
extra arsenal verb or the effect upgraded.

**Balance envelope (T1–T2, placeholder-blessed):** player T1 kit ≈ 0.4–0.7 DPS, HP 20, Bandage ≈ 0.125
HP/s + shield pools 2–4. So: T1 foe HP 8–16, offense 0.2–0.4 DPS (1–2 dmg per 4–6 s); T2 foe HP 16–24,
offense 0.4–0.6 DPS. Foe part stats 2–5 so part-aim genuinely turns fights (arm-break ends offense).
Every arsenal verb reserves against the owning part's stat, exactly like the player.

## Built foes — T1/T2 specs (symmetry prototypes; numbers = placeholder for Doug's review)
Parts are (STR arm · INT head · DEX legs · CON chest); the STR arm powers strikes unless noted.
Weapons/armor name real `WEAPONS.md`/`ARMOR.md` records — the consult/timer/gate math is the shared one.

### Skeleton — *the fodder chassis* (figure `skeleton`)
- **T1 "Skeleton"** — HP 8 · parts 2/1/2/1 · weapon Iron Dagger · no armor ·
  arsenal: Jab (dagger consult ≈ 0.5 dmg/2.4s ≈ 0.2 DPS) · aim Random.
- **T2 "Dire Skeleton"** — HP 14 · parts 3/1/3/2 · Steel Dagger · DEX Leather Cap ·
  adds Lunge · aim Random.
- **Foe Effect: *Brittle*** — the first arm-break it suffers refunds YOUR aimed technique's charge
  (rewards part-aim on fodder); **Dire: *Reassemble*** — the first time HP would hit 0, it stands back
  up at half HP with every part at half health (once).

### Bandit — *the mirror-match human* (figure `bandit`)
- **T1 "Bandit"** — HP 12 · parts 3/2/3/2 · Iron Axe + Wooden Shield · Leather chest ·
  arsenal: Swing + **Brace** (real shield source, pool 2 — the first shielded foe) · aim Random.
- **T2 "Dire Bandit"** — HP 18 · parts 4/2/4/3 · Steel Axe + Iron Buckler · Leather chest+legs ·
  arsenal: Swing, Brace (pool 3), Bandage (the §8-symmetric mend) · aim Smart.
- **Foe Effect: *Plunder*** — every part-hit it LANDS on you drains 1 Charge AND 1 Summons (recovered
  on victory); Dire: drains 2 of each. Neither refills mid-fight, so Plunder pressures both fragile
  pools at once — the pierce economy Brace (its own shield) forces you to spend into, and the minion
  economy if you've fielded one. The mirror-match human punishes shield-piercing and summoning alike.

### Wraith — *the caster / INT lane* (figure `wraith`)
The head, not the arm, powers its arsenal — the anti-caster lesson: aim the HEAD.
- **T1 "Wraith"** — HP 10 · parts 1/4/2/2 · no weapon · no armor ·
  arsenal: Ember (INT-innate 1 dmg/3s) · aim Random.
- **T2 "Dire Wraith"** — HP 16 · parts 1/5/3/3 · Adept Wand (shield-SUBTRACTION damage, §6d) ·
  arsenal: Ember + Siphon (its landed part-hits heal its own parts — lifesteal per TECHNIQUES.md) · aim Smart.
- **Foe Effect: *Insubstantial*** — while its head is undamaged, every hit against it deals 1 less HP
  damage (min 1); breaks with the first head part-damage. (Teaches head-aim; never stacks with shields —
  it has none.)

### Ogre — *the brute* (figure `ogre`; today's `Foes.Armed/ArmedHealing` default)
- **T1 "Ogre"** — HP 14 · parts 4/1/2/3 · Club ≈ Iron Mace · no armor ·
  arsenal: Swing (mace consult, ~0.35 DPS) · aim Random.
- **T2 "Dire Ogre"** (the 01-encounter ref foe) — HP 20 · parts 5/1/2/4 · Iron Warhammer ·
  STR Breastplate · arsenal: Swing + Cleave · aim Smart.
- **Foe Effect: *Overwhelm*** — its hits knock 1 point off your ACTIVE shield pool even when evaded;
  **Dire: *Rampage*** — each part YOU lose speeds its next charge by 25%.

### Troll — *the sustain check* (figure `troll`)
The DPS-race teacher: out-damage the mend or lose the long game. (The castle's ArmedHealing shape,
promoted to a field foe.)
- **T1 "Troll"** — HP 16 · parts 4/1/2/4 · Iron Axe · no armor ·
  arsenal: Swing + Bandage (chest-powered, mends 1 part-point/8s) · aim Random.
- **T2 "Dire Troll"** — HP 24 · parts 5/2/2/5 · Steel Axe · STR chest+arms ·
  arsenal: Swing, Cleave, **Suture** (mends 2 part-points/8s) · aim Smart.
- **Foe Effect: *Regenerative Flesh*** — its mend restores DOUBLE the part-points (2 T1 / 4 Dire) to
  its most-damaged PART instead of the base 1/2 — parts only, HP is never restored in combat (§10).
  Break the CHEST FIRST (the part powering the mend) or the buffed regen outpaces your DPS.

### Gargoyle — *the armor lesson* (figure `gargoyle`)
- **T1 "Gargoyle"** — HP 12 · parts 3/2/1/4 · no weapon (stone fists ≈ Iron Axe profile) ·
  STR plate chest+arms · arsenal: Jab · aim Random.
- **T2 "Dire Gargoyle"** — HP 18 · parts 4/2/1/5 · fists at Steel-Axe profile · STR plate ×4 ·
  arsenal: Jab + **Bind** (STR ward, pool 2) · aim Smart.
- **Foe Effect: *Stoneform*** — part damage against it is reduced by 1 (min 1) while its chest holds;
  HP damage lands full (the inverse of STR plate — teaches "ignore parts, race the HP" as a valid line).

### Castle (boss role — keeps its current shape)
`Foes.ArmedHealing` stays the campaign boss: HP 40 · arm 4 · BossStrike (3/2.5s) + Bandage · aim Smart.
Reconcile it onto the model above when foes gain gear (its strike becomes a real weapon consult);
numbers already proven winnable — don't retune in the same pass as the field roster.

## Foe-effect design rules (for the roster + ideas)
Effects use ONLY designed mechanics (shields §6b, regen, charge/cooldown, reserves, part/HP damage §8,
evade, gold, Charge §10). On-hit effects obey the shared rule: a LANDED PART-hit, never shield-absorbed,
never a broken part. No new resources, no auras, no multi-foe anything (§8). One effect per foe; Dire
may upgrade or swap it.

## IDEAS — [IDEA] every line; do NOT build any of these unless promoted above
Roles cover the four stat lanes + mixed. Tier is a suggested first home. "FX:" is the Foe Effect sketch.

**STR / brute lane**
1. **Brigand Marauder** (T1) — dual Iron Axes, Frenzy. FX *Reckless*: +1 damage dealt AND taken.
2. **Pit Fighter** (T2) — Claymore, Cleave. FX *Warlord's Echo*: 2H swings cost its arm 1 less (Warlord's Might mirror).
3. **Iron Golem** (T2) — the minion, foe-sized: slow Warhammer, huge arm stat. FX *Ponderous*: charges 2× slow, hits 2× hard.
4. **Berserker** (T2) — Battleaxe. FX *Blood Frenzy*: below half HP its charge time halves.
5. **Ogre Chieftain** (T3 idea parked) — Warhammer + Bind. FX *Warcry*: your charging techniques slow 10%.
6. **Stone Colossus** (T3) — plate ×4, fists. FX *Bulwark*: immune to part damage while chest > half.

**DEX / skirmisher lane**
7. **Wolf** (T1) — bite ≈ dagger, legs-powered. FX *Pack Instinct*: +5% accuracy per your broken part.
8. **Highwayman** (T1) — Rapier + leather. FX *Riposte*: your MISSES speed his next charge 20%.
9. **Assassin** (T2) — twin daggers, Flurry. FX *Exposed Seam*: always aims your most-damaged part (Smart+).
10. **Sling Skirmisher** (T2) — Shepherd's Sling: shield-bypassing pot-shots that spend ITS Charge pool (3).
    FX *Skirmish Step*: +4% evade while legs healthy.
11. **Duelist** (T2) — Rapier. FX *Parry Stance*: holds a 1-point ward that refills every 2s (Parry mirror).
12. **Hound Pack Alpha** (T2) — the Hound minion foe-sized. FX *Harry*: your heals mend 1 less (min 1).

**INT / caster lane**
13. **Cultist** (T1) — Ember only. FX *Fervor*: Ember speeds up 2% per cast, resets on its head-damage (Resonance mirror).
14. **Necromancer** (T2) — wand + robe. FX *Grave Tithe*: gains 1 HP whenever a shield absorbs its bolt.
15. **Storm Adept** (T2) — staff. FX *Static*: every 4th bolt ignores shields (pierce without Charge — foe-only).
16. **Illusionist** (T2) — no weapon. FX *Mirage*: first aimed shot each 10s auto-misses; breaks with head damage.
17. **Bog Witch** (T2) — wand. FX *Hex*: her landed part-hits also slow that part's technique charge 10%.
18. **Lich Acolyte** (T3) — wand + tome. FX *Phylactery Sliver*: revives once at 25% (Reassemble, caster-flavored).

**CON / bulwark lane**
19. **Shield Bearer** (T1) — Longsword + Wooden Shield, Brace. FX *Shield Wall*: his pool regens 2× while chest healthy (Unbroken Aegis mirror).
20. **Tower Knight** (T2) — Mace + Tower Shield, Brace pool 4. FX *Fortified*: plate paid in CON (Fortified mirror — his plate rides the chest, so chest-aim strips it).
21. **Bone Bulwark** (T2) — skeleton + plate. FX *Deadbolt*: shields can't be pierced by Charge (bow counter-lesson; sling/wand still work).
22. **Toad Behemoth** (T2) — no weapon, body slam. FX *Guts*: heals 2 HP whenever a fight-pause (HELD) ends (anti-turtle).

**Mixed / oddballs**
23. **Rust Creeper** (T1) — weak bites. FX *Corrode*: its landed hits damage your ARMOR's slot part by +1 (gear-hate).
24. **Thieving Sprite** (T1) — dagger. FX *Filch*: steals 1 Charge on a landed hit (max 2/fight).
25. **Mimic Chest** (T1) — ambush at loot nodes. FX *Ambush*: starts with your techniques all at zero charge.
26. **Plague Rat Swarm-as-one** (T1) — ONE foe, swarm-figure. FX *Gnaw*: ignores armor part-mitigation.
27. **Sand Stalker** (T2) — legs-heavy frame. FX *Burrow*: untargetable 1s after each of its strikes (aim between).
28. **Gravewind Banshee** (T2) — caster. FX *Wail*: your PASSIVE sources (shields) tick their regen at half rate.
29. **Ettin** (T2) — TWO arm parts, each powering its own Swing (still one foe — §8-legal multi-arm).
    FX *Two Minds*: breaking one arm doesn't slow the other.
30. **Chained Ghoul** (T2) — high HP, no arsenal until "unchained" at half HP. FX *Unchained*: gains Frenzy below half.
31. **Warden's Shade** (T2) — plate + Brace + Suture. FX *Echo of the Wall* — the player-core mirror-boss idea (one per core, 7 total, parked).
32. **Alpha Gargoyle Roost-Lord** (T3) — gargoyle king. FX *Petrify*: his landed hits reserve 1 of your DEX until fight end (cap 3).
33. **Siegebreaker Ram-Crew** (T2, siege flavor) — battering-ram frame, chest-heavy. FX *Momentum*: damage grows +1 per 10s alive.
34. **The Collector** (T2, merchant-adjacent) — dagger. FX *Toll*: fleeing (Retreat) from him costs 5 gold.
35. **Molten Whelp** (T1) — ember-spitter. FX *Cinder Coat*: melee part-hits against it cost the striking arm 1 stat (thorns, part-side only).
36. **Frost Revenant** (T2) — wand. FX *Chill*: your DEX-haste bonus is halved while its head holds.

## Open / TBD
- All built-foe numbers above = Cowork placeholder-blessed for the symmetry build; Doug reviews/tunes.
- Foe ARMOR + weapon-consult require the symmetry engine slice (STATUS queue) — today's `Foes.cs`
  hardcodes Strike/BossStrike without gear.
- Foe effects = data on `Foe` (one interpreter, like Core Effects) — never per-foe classes.
- Which foes spawn per node type / campaign tier (encounter tables) — design-open (§12/§17).
- Foe minions, multi-foe anything: out of scope by design (§8 single-foe is LOCKED).
