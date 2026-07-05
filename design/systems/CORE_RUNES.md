# Core Runes — canon (prototyped)

> **Authoritative design for the six Core Runes.** Effect names + rules text are canon (this prototype set
> REPLACES the older §11 roster — Hollow Vessel / Unbroken Aegis / etc.). Kits/numbers are blessed-initial.
> Shared mechanics: `../DESIGN_SPEC.md` §7 / §11. See also `TECHNIQUES.md`, `WEAPONS.md`, `ARMOR.md`, `RACES.md`.

## Purpose
A Core Rune is the LAYOUT a Race's body sockets into. It carries NO attributes (those are the Race's) — only a
rune budget + per-rung discount, a bay count, a fixed starting kit (techniques / minions / weapons / armor), and
a signature **Core Effect** (stronger than a keystone). Design principles: **~4 techniques is the ceiling, not
the fill** (leave free slots); **bays can start empty** (minions acquired); **each core heals differently**.

## Entry pattern (one core)
- **Core Effect** — name + full rules text (the signature effect).
- **Archetype / flavor** — the card pitch.
- **Budget / discount / bays** — the build economy knobs.
- **Default techniques** (start) + free slots · **Default minions** (start) / bay cap · **Weapons** · **Armor**.

## Content

### Grunt — *Jack of All Trades*
**Effect:** Every attribute cost you pay is reduced by 1.
**Archetype:** THE GENERALIST · badge STARTER. No edge, no hole; a fat budget of cheap runes climbs into any keystone you pay for.
**Kit:** Jab, Brace, Bandage (T1 heal) + 1 free slot · bays 0 / **2** (empty) · Iron Longsword + Wooden Shield · Iron plate (all 4).

### Warden — *Fortified*
**Effect:** Armor's equip cost is paid in CON instead of its usual attribute.
**Archetype:** THE WALL · badge BULWARK. Soaks blows and holds the line; deliberately CON-heavy (armor + shield + heal all CON) as a rune sink.
**Kit:** T2 CON Shield, Cleave, T2 CON Heal + 1 free slot · bays 0 / **1** (empty) · Iron Longsword + Iron Buckler · Iron plate (all 4, paid in CON).

### Adept — *Resonance*
**Effect:** Each targeted spell that hits reduces its next charge time by 2%, stacking up to 5 times.
**Archetype:** THE SCHOLAR · badge CASTER. Frail chest, deep INT head, widest action bar. All-INT kit — no CON tax.
**Kit:** Ember, Siphon, Stoneskin + 1 free slot · bays 0 / **1** (empty) · Wooden Staff · Cotton Robe + Cloth Cap.

### Summoner — *Conscription*
**Effect:** Minions do not consume Summons when activated.
**Archetype:** THE BINDER · badge SPECIALIST. Fights through a war-party of summons while staying back.
**Kit:** Ember, Sacrifice + 1 free slot · bays **1 (one T2 minion) / 3** · Adept Wand + Wooden Charm · Cotton Robe + Cloth Cap.

### Reaver — *Finesse*
**Effect:** Techniques requiring two weapons cost 1 less to activate.
**Archetype:** THE DUELIST · badge SPECIALIST. Glass-cannon twin blades; ends parts before they answer. **No heal** (glass-cannon test).
**Kit:** Frenzy, Flurry, Lunge + 1 free slot · bays — / **0** · twin Iron Daggers · leather (all 4).

### Ranger — *Fletcher's Luck*
**Effect:** Bow techniques have a 20% chance to consume no charge when fired.
**Archetype:** THE MARKSMAN · badge SPECIALIST. Strikes from range with a shield-piercing bow; answers first.
**Kit:** Shot, Aimed Shot, Lunge, Bandage (T1 heal) · bays **1 (Hound) / 2** · Iron Short Sword + Short Bow · leather (all 4).

## Shared rules
- **Healing map:** Grunt & Ranger → Bandage (T1, flat baseline); Warden → T2 CON heal; Adept → Siphon (lifesteal);
  Summoner → Sacrifice; Reaver → none. Flat baselines are deliberate — they pull players toward acquiring better heals.
- A Core Effect outranks a keystone rune. Most non-Summoner effects were display-only historically; these are live.

## Open / TBD
- Budget / discount / bay numbers are placeholder (tune with the rune economy). **Sacrifice** + **T2 minion**
  need their systems built. This set diverges from the locked §11 roster — reconcile DESIGN_SPEC when it locks.
