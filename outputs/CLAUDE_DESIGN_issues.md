# Claude Design payload

This file holds only what CD still needs to act on. Open asks are alphabetized by topic under
**## Open**, each its own heading — scan the headings to find something, don't read top to bottom.
Sending an item to CD is not the close signal; it clears only once verified landed in the repo, and
moves to **## Confirm-to-Close** (one line, nothing to do but clear it from memory).

## Open

### core-kits.js → fetch shared cores.json (NEW, 2026-07-12 Doug, LOCKED architecture)
Race stats + core budget/actions/minionCap/statBonus/CoreEffect/starting-kit are moving out of three
hand-maintained copies (your `core-kits.js`, our `CoreRunes.cs`, `CORE_RUNES.md`) into one shared
`design/systems/cores.json` — this is the fix for the exact drift this session kept catching (the
Adept/Summoner kit patch we just hand-applied to your file, and today's Adept MinionCap mismatch).
Once `design/systems/cores.json` lands (engine-side, tracked in `STATUS.md`), switch `core-kits.js`'s
per-core `budget`/`effect`/`gear`/`techniques`/`bayCap` fields to a `fetch()` of that file at module
load (same pattern you already use for the dynamic `import()`), instead of hand-typed object literals.
Keep everything display-only as-is — accent hex, blurb, scenario copy, figure key, `finds` — none of
that is drifting and none of it is in the JSON's scope. We'll ping you when the file lands with its
exact shape; no action needed until then, just flagging so it's not a surprise in a future drop.

### CityMap Retreat/Redeploy — Progress-to-Available UX (NEW, 2026-07-12 Doug)
New mechanic landing engine-side: Retreat/Redeploy now takes time to become available (DEX-timed, starts
on arrival, placeholder 30s base). The button currently has no way to show progress toward that — needs
a treatment (fill-bar sweep across the button, a radial, a numeric countdown, whatever reads best against
`retreatBtn`'s existing states) that's hidden once the button is actually available (so it doesn't clutter
the normal case). No numbers needed from you — the engine will feed a 0..1 progress fraction, just needs
somewhere to draw it.

### Figure + Gear Asset Regen Batch (B2-GO)
Bow sprites landed (see Confirm-to-Close) — the rest of the batch is still open:
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
The SALE card grammar itself landed (gold-trimmed `priceStrip` footer on `wareCard` — BUY/short-of-coin
chip + spoils coin + price, the visual signature an `invCard` never has) — that structural half is done,
see Confirm-to-Close for the next batch scope. Still open: **per-kind chip ART** (technique glyph / rune
polygon on the sale face) — deliberately deferred on your side pending the real sale catalog (sample
names don't all map to captured glyph ids yet), no rush, doesn't block our mechanic work.

### Re-Extraction for the v6 Roster (B20)
The coreCard accent contrast fix + accent tokens landed (see Confirm-to-Close) — the rest of the
re-extraction is still open, the engine renders only what the manifest authors:
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
- **Adept + Summoner Starting Kits** — **hand-patched directly in `design/dchtml/core-kits.js` by
  Cowork, 2026-07-12, at Doug's explicit request (skipping the usual CD round-trip).** NOT your work,
  but flagging clearly so your NEXT drop doesn't silently clobber it: Adept's `staff_wooden` is now
  `'STR'` + `jab` added to `techniques`; Summoner's `gear` swaps `charm_wooden` → `SH('shield_wooden',
  'Wooden Shield', 1, 'handR', 't1', 'COMMON')`, `techniques` is now `[ember, blast, sacrifice, brace]`,
  `techCap` is 4. A new `blast` entry was added to the `T` catalog (INT, cost 1, `needs:'wand'`) since it
  didn't exist yet. If your own generation pipeline re-derives `core-kits.js` from a different source of
  truth, fold these same changes in there too so a future drop doesn't revert them.
- **B0 · B0b · B1a · B3** — evening 2026-07-03 drop, all verified landed.
- **B1b** — key-set diff guard now runs automatically in `extract_merge.js` on every merge.
- **B4** — "open Equipment" button elements on Encounter + CampaignMap, landed.
- **B5 · B6** — `invCard`/`loadoutCard`/`invTab` `states.family` + hover overlays, landed.
- **B7** — raceCard head-portrait imageBind moved to the correct (square) element, landed.
- **B8** — CityMap beaconNode hover/current states authored, `glow` spec'd (`style.pulse`, steady
  1.8s ease-in-out breathe). Building the pulse primitive + wiring it is ours now, not a CD ask.
- **B9** — folded into Figure + Gear Asset Regen (B2-GO), no separate item.
- **B10** — gear catalog display-name drift vs. §6c canon, corrected.
- **B11** — bow sprites `bow_{short,long,compound,elven}` (+ `*_back`) + catalog rows, landed.
- **B12** — worn-armor part set (race-first, full-part convention): 744 files landed clean. Canon in
  LAYOUT_CONTRACT §12a / DESIGN_SPEC §7a.
- **B13 · B14** — `waresShelves`/`buildMinions` sizing fixes, landed.
- **B17** — Dwarf + Halfling figure batch, landed and exceeded (Half-Giant + Barbarian too).
- **B18** — every technique/minion icon now shipped: Flurry/Aimed Shot/Siphon/Barkskin/Sacrifice/Bind/
  Frenzy-Flurry split-fill, plus this pass's Parry/Steel/Suture + Iron Golem/Hound. Nothing open.
- **B19** — "bay(s)" retired as the minion-slot term everywhere, landed.
- **B20 (partial)** — coreCard Core-Effect accent contrast (Doug #7) fixed: `border.colorBind` now
  tints the LEFT-BORDER trim, never a full-rect fill, on NewGame coreCard/previewCoreEffect AND
  Equipment's coreEffectBlock. `style.coreAccents` published (all 7 cores). Rest of B20 still open
  above.
- **B21** — no-absolute-positioning: `parent`-field re-extraction landed (160 keys, 6 screens), and
  the standing invariant is now permanent in `CLAUDE.md`/`LAYOUT_CONTRACT.md` §3. Remaining piece
  (engine recursive parent-box resolution) is ours, not a CD ask.
- **B23** — Equipment tab buttons (`invTab`) widened to fill their row, three labels readable.
- **B24** — `inventory.invItems` widened to fit its authored 2-column grid.
- **B25** — `attrs.cells` pip strip widened so a 6-capacity stat's 6th (free) pip renders.
- **B26** — `CD_STATUS.md` #34 armor-reservation wording corrected (armor consumes pool, same as
  weapons/techniques) — stale sub-items cleared too.
- **B27** — minion column reflow: new declarative `countWidth` field (`{bind,item,gap,pad,hideAtZero}`)
  on Equipment `minionColumn` + Encounter `minionGroup`. Reading the field is ours now, not a CD ask.
- **B28** — attrBar reorder/rename/color, closed directly by Doug.
- **B29** — Encounter shell hosts quest/camp/nothing-here + CityMap retreat→redeploy: all three CD-side
  asks delivered (`questPanel`+children gated on `encounter.quest`, `campMarker` gated on
  `encounter.camp`, "nothing here" = bare shell needing no new elements, `retreatBtn` two states with
  new `label`/`asset` per-state fields). Remaining work (engine element-gating, foe-cluster unmount,
  per-state label/asset swap) is ours, not a CD ask.
- **B30** — both technique action-bars (Equipment `loadoutList`, Encounter `techList`) now seat a 4th
  card; region-width fix, same idiom as B23-B25.
- **B31** — Encounter `poolRows` grown to `[309,82]`; the CON row renders.
- **B32** — `attrPip` dropped its `max-width:106px` cap; verified `[54,9]` in the fixed `layout.json` —
  pip rows now stretch to a common width regardless of live count.
- **B33** — `resourceItem`'s value rect widened to fit `"current/max"`; verified `[14,1,22,9]` with
  sample `"6/8"` in the fixed `layout.json` (was `[14,1,4,9]`/`"6"`).
- **B34** — `combatMinionCard` header divider grown to `[1,1,76,37]` (verified), matching the sibling
  `minionCard`'s vertical layout — no more name/description overlap.
- **Contextual Encounter Backgrounds** — 8 new backdrops shipped (`enc_{camp,forest,mountain,river,
  meadow,quarry,lumber,city_gates}`), `campMarker` retired entirely, Encounter's backdrop element now
  authors `binds:"encounter.scene"` + `imageBind:"bg/{encounter.scene}"` — verified both directly in the
  fixed `layout.json` (`campMarker` absent, `encounter.scene` present and wired). Per-node scene PICK is
  engine-side work (CD_STATUS #41), tracked in `STATUS.md`, not a CD ask.

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
