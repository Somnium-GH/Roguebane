# Armor — canon (prototyped)

> **Authoritative design for armor.** Numbers are blessed-initial (tune in a balance pass); the rules/structure
> are canon. Shared engine mechanics: `../DESIGN_SPEC.md` §6 / §6b / §6c / §6e.

## Purpose
A LIGHT effect layer worn one piece per part-GROUP slot (Head / Chest / Arms / Legs), each line keyed to the
attribute whose part it protects. Armor does NOT grant or gate the attribute pool — it needs its governing
attribute (what's left after active reservations) above a threshold to stay active, and rides the part's
condition: damage a part past that and its armor drops to **DISABLED (shown RED)** but stays ASSIGNED, then
re-enables when the attribute heals (§6e cascade). Armor never stops HP damage — only a shield block or a full
evade does; STR plate reduces the covered part's own stat loss, not HP.

## Entry pattern (one armor line)
- **Line** — governing attribute (STR / DEX / INT / CON).
- **Slots** — body-group slots covered.
- **Tier ladder** — 4 material/quality rungs (T1→T4), read as palette.
- **Equip gate** — governing-attribute cost per tier (remaining stat ≥ this to stay worn).
- **Per-tier effect** — the bonus each tier adds.

## Content

| Line | Slots | Tier ladder (T1 → T4) | Gate / tier | Per-tier effect |
|---|---|---|:--:|---|
| **STR plate** | Head / Chest / Arms / Legs | Iron → Steel → Mithral → Dwarven Steel — pieces: Helm / Breastplate / Vambraces / Greaves | 2 STR | −2 to the covered part's OWN damage (part-stat mitigation, never HP) |
| **DEX leather** | Head / Chest / Arms / Legs | Leather → Hardened → Studded → Reinforced — pieces: Cap / Padded Armor / Bracers / Leggings | 1 DEX | +2% evasion per worn piece (global, stacks across pieces) |
| **INT robe** | Chest + Head ONLY | Cotton/Cloth → Silk → Ornate → Humming — pieces: Robe / Cap→Circlet | 2 INT robe · 1 INT cap | +2 spell damage per piece (2-piece cap: robe + hat) |
| **CON shield** (object) | one off-hand slot | Wooden Shield → Iron Buckler → Kite Shield → Tower Shield | 1 CON | +2% shield-pool recharge; REQUIRED to run the CON block source (§6b) |

## Mechanics
- **Risk mirrors payoff.** Plate soaks part damage but rides the arms-break cascade (STR is one pool shared
  across both arms — a build leaning on it goes RED across the board when both arms break). Leather's evade is
  **hard-zeroed if a leg breaks** (§6). Robe caps at two pieces so its spell bonus tops out fast. The shield
  object is the gateway to the strongest block source.
- **Disable cascade** — highest-requirement-first, ties last-equipped-first; recovery re-enables cheapest-first.

## Open / TBD
- All numbers are §6c blessed-initial — tune in a balance pass.
- Effect scaling at higher tiers (parked with the tier ladder; tier raises the effect and its cost).
