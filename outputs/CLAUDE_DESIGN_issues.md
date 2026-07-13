# Claude Design payload

This file holds only what CD still needs to act on. Open asks are alphabetized by topic under
**## Open**, each its own heading — scan the headings to find something, don't read top to bottom.
An item moves to **## Confirm-to-Close** once verified landed in the repo, and drops out of this file
entirely once that pass is behind us — `STATUS.md`'s re-arm history is the permanent record, this file
is a to-do list, not an archive. Keep entries self-contained; don't lean on cross-references into
Confirm-to-Close, since that section gets pruned regularly.

## Open

### CityMap Retreat/Redeploy — Progress-to-Available UX (NEW, 2026-07-12 Doug)
New mechanic landing engine-side: Retreat/Redeploy now takes time to become available (DEX-timed, starts
on arrival, placeholder 30s base). The button currently has no way to show progress toward that — needs
a treatment (fill-bar sweep across the button, a radial, a numeric countdown, whatever reads best against
`retreatBtn`'s existing states) that's hidden once the button is actually available (so it doesn't clutter
the normal case). No numbers needed from you — the engine will feed a 0..1 progress fraction, just needs
somewhere to draw it.

### core-kits.js → fetch shared cores.json (NEW, 2026-07-12 Doug, LOCKED architecture)
Race stats + core budget/actions/minionCap/statBonus/CoreEffect/starting-kit are moving out of three
hand-maintained copies (your `core-kits.js`, our `CoreRunes.cs`, `CORE_RUNES.md`) into one shared
`design/systems/cores.json` — the fix for the drift this session kept catching. Once
`design/systems/cores.json` lands (engine-side, tracked in `STATUS.md`), switch `core-kits.js`'s
per-core `budget`/`effect`/`gear`/`techniques`/`bayCap` fields to a `fetch()` of that file at module
load, instead of hand-typed object literals. Keep display-only fields as-is — accent hex, blurb,
scenario copy, figure key, `finds` — none of that is in the JSON's scope. We'll ping you with the
exact shape when it lands; no action needed yet.
(`core-kits.js` currently carries a Cowork hand-patch to Adept's weapon/techniques and Summoner's full
kit, applied 2026-07-12 to unblock testing ahead of this round-trip — if your pipeline regenerates the
file from a different source before `cores.json` lands, diff against the live file first so you don't
revert it.)

### Figure + Gear Asset Regen Batch (B2-GO)
Bow sprites already shipped. Still open:
1. **Weapon sprites** — ONE silhouette per type, FOUR material palettes (Iron → Steel → Mithral →
   Dwarven Steel), hand-socket mounts per LAYOUT_CONTRACT: Longsword · Axe · Mace · Claymore ·
   Battleaxe · Warhammer · Dagger · Rapier · Short Sword.
2. **New gear families:** Sling (1H, pairs with shield) · Staff (2H) · Charm + Tome as OFFHAND
   hand-socket mounts · wands are HAND items now (hand-socket mount; dual = one per hand; never
   alongside bow/sling) · a ranged BACK-MOUNT layer (bow/sling) so an equipped ranged weapon renders
   while melee hands are full.
3. **Race × core figure regen** on the established part/z-list contract (robe figures stay
   legitimately ~12-part); fix the elf_ranger chest-accent neckline in this batch (strap sits too
   high, reads as fused to the head).
4. **Ship with:** updated figure defs + asset inventory in `layout.json`, mgcb source updates (we
   mirror game-side), refreshed 00-assets sheets. Our smoke-figures + asset-exists probes verify
   completeness on landing — an inventory list in the drop notes helps us confirm fast.

Note: figure-MORPH mechanics residuals (§7/§17 #15) are our composition questions, not art blockers —
proceed on the current contract; propose contract changes in drop notes, don't block.

### Merchant Sale Card Art (B22)
Sale card grammar (gold-trimmed `priceStrip` footer on `wareCard`) already shipped. Still open:
**per-kind chip ART** (technique glyph / rune polygon on the sale face) — deliberately deferred on
your side pending the real sale catalog (sample names don't all map to captured glyph ids yet), no
rush, doesn't block our mechanic work.

### Re-Extraction for the v6 Roster (B20)
coreCard accent contrast fix + accent tokens already shipped. Still open, the engine renders only
what the manifest authors:
1. Per-core STAT-BONUS chips (`core.statBonus` list on the NewGame coreCard + Equipment identity
   block; one colored chip per non-zero stat: Grunt +1 all · Warden +5 CON · Adept +5 INT · Summoner
   +3 INT/+2 CON · Reaver +5 DEX · Ranger +4 DEX/+1 CON · Barbarian +4 STR/+1 CON), action-bar cards
   with rules text (name + cost + italic description + footer state line), and the "minions" label
   vocabulary. New Core-Effect copy is in `design/systems/CORE_RUNES.md` — some rules text runs long
   (Ranger's compound sentence), size the coreEffect rects for it.
2. NewGame re-author for the grown roster: 5 races × 7 cores (raceCards/coreCards currently seat
   2×6 — cells past the container silently drop, so the screen can't show the new picks).
3. Refresh the stale refs to v6 data: 05-newgame (new roster + tile colors), 03/07 if their strips
   show core identity; a `01/02-*-<core>` set for any future core additions. Race-card art/portraits +
   final race blurbs/tags for Dwarf/Halfling/Half-Giant (Doug supplies copy) ride here too.
4. FYI: mock numbers in the refs that disagree with the systems docs (e.g. Claymore "6 dmg · 1.4×" vs
   WEAPONS.md's 7 dmg · 1.3×) are non-canon — docs win, no action needed unless you'd rather
   regenerate the copy.


## Confirm-to-Close (landed and verified — nothing to do, just clear these from memory)
(none currently pending — everything through B0-B34 and the Contextual Encounter Backgrounds batch has
been verified landed and cleared from this file; STATUS.md's re-arm history has the permanent record if
you need to trace one back)

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
- **Drops apply via a stop/apply/re-arm handshake:** stage in `.drop/` (or, this pass, direct-unpacked
  into the working tree — Cowork applied it either way), loop halted, guards run before the tree
  resumes.
