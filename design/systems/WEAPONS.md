# Weapons — canon (prototyped)

> **Authoritative design for weapons.** T1 numbers are blessed-initial (tune later); rules/structure are canon.
> Shared engine mechanics: `../DESIGN_SPEC.md` §6d / §10.

## Purpose
Weapons are stat-sticks AND technique gates: a technique REQUIRES its matching weapon present to be usable at
all, and CONSULTS the weapon for damage + timer. The weapon never carries its own timer entity — the technique
is the timer; the weapon multiplies it and supplies the damage.

## Entry pattern (one weapon)
- **Type · family** — stat (STR / DEX / INT), hands (1H / 2H / off-hand / ranged).
- **Timer multiplier** — multiplies the consulting technique's charge (below 1.0× = faster).
- **Per-tier damage** + **per-tier equip requirement** — 4 material/quality rungs.
- **Resolution** — how its damage lands (normal / shield-piercing + Charge / shield-subtraction / blockable).

## Content (T1 = Iron / first rung)
DPS at the base 8.0s technique speed with a plain 1.0× verb: **DPS = dmg ÷ (8 × timer)**.

| Type | Hands · stat | Timer | Dmg | Req | DPS | Resolution |
|---|---|:--:|:--:|:--:|:--:|---|
| Dagger | 1H DEX | 0.6× | 1 | 1 DEX | 0.21 (0.42 dual) | normal |
| Rapier | 1H DEX | 0.7× | 2 | 2 DEX | 0.36 | normal |
| Short Sword | 1H DEX | 0.8× | 3 | 3 DEX | 0.47 | normal |
| Axe | 1H STR | 0.9× | 3 | 1 STR | 0.42 | normal |
| Longsword | 1H STR | 1.0× | 4 | 2 STR | 0.50 | normal |
| Mace | 1H STR | 1.1× | 5 | 3 STR | 0.57 | normal |
| Battleaxe | 2H STR | 1.2× | 6 | 4 STR | 0.63 | normal |
| Claymore | 2H STR | 1.3× | 7 | 5 STR | 0.67 | normal |
| Warhammer | 2H STR | 1.4× | 8 | 5 STR | 0.71 | normal |
| Bow (Short Bow) | 2H DEX, ranged slot | 1.0× | **OPEN** | 2 DEX | — | bypasses shields, spends Charge; needs both arms |
| Sling (Shepherd's) | 1H DEX, ranged slot | **OPEN** | **OPEN** (< bow) | 1 DEX | — | bypasses shields, spends Charge; needs one arm |
| Wand (Adept) | 1H INT, dual-OK | **OPEN** | 2 | 2 INT | — | shield-SUBTRACTION (reduced by standing shields, not consumed, no Charge) |
| Staff (Wooden) | 2H INT | **OPEN** | 2 | 2 INT | — | plain BLOCKABLE melee; blocks the ranged slot; **+0.2× SPELL damage / tier (2× a tome)** |
| Charm (Wooden) | off-hand INT | — | — | 1 INT | — | +0.1× MINION damage / tier |
| Tome (Old Worn) | off-hand INT | — | — | 1 INT | — | +0.1× SPELL damage / tier |

**Material ladders:** melee Iron → Steel → Mithral → Dwarven Steel; bow Short → Long → Compound → Elven;
sling Shepherd's → Braided → Sinew → Giantsbane; wand Adept → Twisted → Gemstone → Glowing; staff Wooden →
Twisted → Ornate → Humming; charm Wooden → Bone → Ornate → Humming; tome Old Worn → Leather → Ornate → Glowing.

## Mechanics
- **Timer** multiplies the technique's base charge; **DEX haste** shortens it further at cast (≤28%).
- **Dual-wield:** damage from BOTH weapons per the technique's text; timer = the AVERAGE of the two.
- **Two equip layers (§6d):** a MELEE hand-config (main + off-hand) and one separate RANGED slot. A shield
  object lives in the off-hand; a bow/wand can't coexist with a held shield (both need free hands). Broken arm
  removes a hand-slot; a bow needs both arms, a sling one.
- DPS climbs with STR investment (bigger req, slower swing, more damage); DEX weapons stay cheap / fast / low.

## Open / TBD
- Bow + sling per-tier damage (§17 #9); wand / staff timer multipliers; how STR / INT scale damage beyond
  gating + DEX haste ("modest, runes are stronger").
