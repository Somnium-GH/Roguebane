# Status

**How to work this file (every pass):** read CLAUDE.md + `.claude/loop.md` first. Top of this file =
priority; human revisions WIN. One task/run, tests green before commit, update this file, stop. History
lives in `git log` + old STATUS revisions (`git show <rev>:STATUS.md`) — it is NOT re-listed here.
(Whittled 2026-07-05 from ~1850 lines; every ✅ DONE/FIXED entry moved to git history.)

## ⇒ DROP RE-ARM (2026-07-05, Cowork) — the v6/roster drop is APPLIED (straight overwrite, no `.drop/`).
**FIRST LOOP PASS COMMITS THE DROP + this STATUS/docs/FOES batch as one commit before any engine work.**
What landed (all verified on the real tree):
- **5 races × 7 cores of body figures** (`sprites/body/<race>_<core>/…`, new: dwarf, halfling, half_giant
  everywhere + barbarian for everyone) and **worn-armor sets for all 5 races** incl. `<core>/` themed
  subdirs (barbarian theme = str, `layout.json` `worn.themes`).
- **`layout.json` regenerated** (figures/gear/worn sections; screens/templates PRESERVED from the 07-03
  extraction — the generator merge guards key-loss). Structure verified from Cowork: all 6 screens +
  templates present, worn block lists 5 races, file closes clean. **Guard residue for the loop (sandbox
  couldn't full-parse — stale mount): run the real parse guard + `python tools/drop_audit.py` on your
  first pass; they must be green before the drop commit.**
- **14 per-core reference PNGs** `design/01-encounter-<core>.png` + `design/02-equipment-<core>.png`
  (all verified exactly 1920×1080) + `design/00-assets-4-armor.png` (assets sheet, exempt).
- **v6 systems docs** (`design/systems/`): RACES / CORE_RUNES / TECHNIQUES / WEAPONS / ARMOR — now the
  operative canon for stats + content numbers (headers say so). NEW: `design/systems/FOES.md` (foe canon
  + symmetry prototypes — CHUNK D's spec). CORE_RUNES/RACES updated 2026-07-05 by Cowork with Doug's
  locks: **Barbarian** (budget 14 · actions 3 · minions 1) and **Half-Giant** (6/4/4/4 STR).
- **CD-side `Roguebane.Content/Content.mgcb` updated; the GAME-side `Roguebane.Game/Content/Content.mgcb`
  (the one builds READ) has ZERO of the new entries — CHUNK B mirrors it.**
- Icons: only `frenzy.png` + `skeleton.png` arrived beyond the old 8 — B18 still open (flurry, aimed
  shot, siphon, barkskin, sacrifice, bind, parry, suture, steel, golem, hound); tinted-tile fallback
  covers meanwhile.
What this drop RESOLVES (swept everywhere below): B17 (dwarf+halfling figures — EXCEEDED: half_giant +
barbarian came too); the race/core roster gap (§17 #4 mostly); the "Techniques/minion numbers drift"
flag (now a sanctioned sync, CHUNK A); core-effect design (v6 effects have rules text = build them);
the Warden CON-substitution question (now CANON as Fortified); B12's barbarian-theme extension (shipped
unasked). **NOT resolved / do not hand-draw:** the per-core refs show NEW screen elements (stat-bonus
chips, action-card rules text, minions-vocab labels) that the manifest does NOT author yet — that's the
B20 extraction ask (`outputs/CLAUDE_DESIGN_issues.md`); render only what the manifest authors (NO DRIFT).

## ‼ WORK QUEUE (2026-07-05, Doug) — big strategic chunks, in order. Nothing outranks A–C.
Chunks are sized for real progress: finish a WHOLE chunk per pass where possible (M2 batching rule —
same-class items share causes; one commit per coherent slice inside a chunk is fine, don't atomize).

### CHUNK A — v6 DATA LAYER (pure Core + tests; no MonoGame; fully unblocked; do FIRST)
Sync every content table to `design/systems/*.md` (they are canon; mock text in the ref PNGs is
NON-canon where it disagrees — e.g. mock Claymore "6 dmg/1.4×" vs WEAPONS.md 7/1.3×; docs win).
1. **Races** (`Content/Races.cs`, `Race.cs`): re-block to v6 — Human 5/5/5/5 hp20 · Elf 4/6/4/4 hp14 ·
   NEW Dwarf 4/4/4/6 · Halfling 4/4/6/4 · Half-Giant 6/4/4/4 (id `half_giant` — must match the figure/
   worn sprite keys). HP for the new three: FLAGGED placeholders — Dwarf 22, Halfling 16, Half-Giant 24
   (Doug reviews; RACES.md leaves HP open). Tags/blurbs: FLAGGED placeholders in the same register as
   Human/Elf (B17 notes Doug supplies finals).
2. **Core stat bonuses — NEW MECHANIC** (`CoreRune.cs`, `Forge.cs`): a core now grants an ADDITIVE
   per-stat bonus on top of race base (breaks the old "a core carries NO attrs" comment — update it):
   Grunt +1 all · Warden +5 CON · Adept +5 INT · Summoner +3 INT/+2 CON · Reaver +5 DEX ·
   Ranger +4 DEX/+1 CON · Barbarian +4 STR/+1 CON. Effective = race + core, everywhere bodies mint.
3. **Core layouts + kits** (`Content/CoreRunes.cs`): budgets/actions/caps per CORE_RUNES.md's new
   Layout-numbers table (Grunt 20/4/2 · Warden 18/4/1 · Adept 16/4/1 · Summoner 17/3/3 · Reaver 19/4/0 ·
   Ranger 18/4/2 · **Barbarian 14/3/1**). Kits per CORE_RUNES.md §Default loadouts (Warden's Cleave→Jab;
   Adept: Ember/Siphon/Stoneskin, staff, robe, NO minion; Summoner: Ember/Sacrifice/Barkskin, Skeleton
   only in kit; Reaver: Frenzy/Flurry ONLY, no heal; Ranger: Aimed Shot/Lunge/Bandage, Iron DAGGER +
   Short Bow; NEW Barbarian: Cleave/Bind/Bandage, Iron Claymore 2H, iron plate ×4). Archetypes/badges/
   flavor from the docs+refs (Barbarian "THE WARLORD"/SPECIALIST) — this REPLACES the old Flavor
   strings wholesale, which also settles the flagged "bay" copy in Warden/Summoner Flavor (last pass's
   judgment call: superseded, no separate decision needed). `RuneDiscount`: set 0 all cores (flagged
   working assumption in CORE_RUNES.md — JoAT is attribute costs, not rune prices).
4. **Core Effects — REPLACE the roster, BUILD the mechanics** (data + ONE interpreter, no per-core
   classes): Grunt *Jack of All Trades* (every attribute cost −1) · Warden *Fortified* (plate paid in
   CON at tier−1) · Adept *Resonance* (each landed TARGETED spell part-hit → that spell's next charge
   −2%, stacks ×5) · Summoner *Conscription* (fielding spends no Summons; replaces Legion/refund) ·
   Reaver *Finesse* (techniques requiring two weapons reserve −1) · Ranger *Fletcher's Luck* (bow fires
   20% no-Charge off the SEEDED sim RNG; bows equip −1/tier) · Barbarian *Warlord's Might* (2H swords
   equip −2 STR). Shared rule: on-hit boons need a landed PART-hit — never shield-absorbed, never a
   broken part. Names/desc strings from CORE_RUNES.md verbatim.
5. **Techniques** (`Content/Techniques.cs`): sync to TECHNIQUES.md — base 8.0s anchor model. Renames:
   Drain→**Siphon** (2 dmg/6.0s, r2 INT, lifesteal = heals YOUR attribute damage equal to dealt;
   part-hit rule applies) · old `stoneskin`→**Barkskin** (T1 INT ward, pool 3, +1/3.0s, r1). NEW:
   **Stoneskin T2** (pool 6, **+2 pips/3.0s**, r2 INT — Doug 2026-07-05) · **Steel** (T2 CON, pool 8,
   +1/1.5s, r3) · **Suture** (T2 CON heal 2/8.0s, r3) · **Bind** (T1 STR ward, pool 2, +1/2.5s, r2) ·
   **Parry** (T1 DEX ward, pool 1, +1/2.0s, r2) · **Frenzy** (both blades, 1.0× avg speed, r2 DEX,
   needs two weapons) · **Flurry** (both ×0.5, 0.5×, r1 DEX, needs two) · **Aimed Shot** (bow ×2.0,
   2.0×, r1 DEX + 1 Charge, pierces) · **Sacrifice** (Summoner heal: consume 1 fielded minion → restore
   4 part-points most-damaged-first — FLAGGED placeholder amount, TECHNIQUES.md marks it needs-design).
   Bandage r1→**r2**. Frenzy/Flurry gate DEX (doc TBD says so; refs agree). Weapon-verb dmg/speed mults
   per the doc table (Jab .5/.5 · Cleave 1.5/1.5 · Lunge .75/.6 · Shot 1.0/1.0 +1 CHG).
6. **Weapons** (`Content/Armory.cs`): sync the T1 table — Dagger 0.6×/1dmg/1DEX · Rapier 0.7×/2/2 ·
   Short Sword 0.8×/3/3 · Axe 0.9×/3/1STR · Longsword 1.0×/4/2 · Mace 1.1×/5/3 · Battleaxe 1.2×/6/4STR ·
   Claymore 1.3×/7/5 · Warhammer 1.4×/8/5 · Staff 2dmg/2INT, +0.2× SPELL dmg/tier (2× a tome) · Wand
   2/2INT shield-subtraction · Charm/Tome +0.1×/tier, 1 INT. Higher tiers scale as already established
   (blessed-initial). Bow/sling dmg stay OPEN-flagged (§17 #9) — don't invent.
7. **Minions** (`Content/Minions.cs`): Skeleton T1 INT r1, Timer 30 (3.0s), Power 1 · **Iron Golem**
   (rename Golem) T2 INT r2, Timer 50, Power 3 · Hound T1 DEX r1, Timer 40, Power 1 + **+5% accuracy
   while fielded** (new small hook; +5%/tier for descendants later). Shade stays out of `Minions.All`
   (still the Conclave keystone reward; full deletion still needs Doug).
8. **Tests (DoD for A):** every table above asserted (economy math, not assumed); all 35 race×core
   combos embark and win per the established CoreCampaignTests pattern; effect mechanics each get a
   focused test (Resonance stack cap, Conscription zero-spend, Fletcher seeded-RNG band, Fortified
   CON-payment + Warden clearance CON 10 on Dwarf, JoAT −1 sweep, Finesse reserve). **EXEMPT Barbarian
   from "activates its whole default kit"** — its STR 15 over-demand is the design (CORE_RUNES.md
   note); assert instead: barbarian fights and wins with its cascade-sustained subset. Keep the
   no-CD-content-pinning rule (schema/fixtures only).
(Bay→minion C# rename: DONE last pass, 391/391 — only the bind-key literal half remains, paired to
CD's B19 landing; the literals list lives in git history + B19 itself. Don't rename literals early.)

### CHUNK B — ASSET WIRING (mechanical; do right after A or interleave freely)
1. Mirror every new CD-source mgcb entry into `Roguebane.Game/Content/Content.mgcb` (the copy builds
   read) with the established path/output transform — new body dirs (all `dwarf_*`, `halfling_*`,
   `half_giant_*` + `human_barbarian`, `elf_barbarian`), all new worn sets/themed subdirs, new icons.
   Verify `/directive:` slashes (the 57cc8a6 lesson). `dotnet build` must produce the xnbs.
2. Run the drop guards this same pass: layout.json parse + `drop_audit.py` + the manifest→previous
   key-set diff (screens/templates lost = repair verbatim + flag CD).
3. `RB_SMOKE=1 RB_MF=all` + `SMOKE FIGURES`/`SMOKE ASSETS`: every figure × z-part × state resolves for
   ALL 35 player figures; worn resolution covers the 3 new races + barbarian themed (str) via the §12a
   fallback chain (`WornArmorBinding` already handles the chain — extend its race domain + tests).
4. DoD: build green, probes 0 missing (bow/shield known gaps exempt), Core.Tests green.

### CHUNK C — SCREENS: selection, accents, per-core pixel lanes (after A+B; render only what the manifest authors)
1. **Roster stamping:** NewGame lists must seat 5 races × 7 cores. MEASURE first (headless
   `ListLayout.Cells` against the authored `raceCards`/`coreCards` containers). If cells overflow (the
   silent-drop rule), build the GENERIC container overflow/pager primitive (CD #4's deferred ask — the
   trigger has arrived; same interaction family as the merchant pager) as a FLAGGED interim, and B20
   asks CD to re-author the screen properly. No silently-hidden cards, ever.
2. **Per-core tile colors (Doug's ask):** each NewGame core tile highlights with its core's BG color +
   Core-Effect TRIM color. The manifest already carries `colorBind: core.accent` (newgame preview +
   equipment identity) — supply `CoreRune.Accent` (+ a bg variant if the bind set needs it) as engine
   data. VALUES = FLAGGED placeholders keyed to each core's worn-theme line (`worn.themes`): str cores
   → `str` #c2553f (Grunt overrides to amber #d9a441 as the generalist; Warden gold #cf9a44 to split
   from Barbarian), int → `int` #6f8fc4 (Summoner teal #4f9a8a to split from Adept), dex → `dex`
   #82a85e (Ranger keeps dex-green, Reaver a darker cut if needed). B20 asks CD for canonical tokens;
   these are stopgaps, flag them in the commit.
3. **Identity binds:** budgets/actions/minions/base-hp/effect name+desc all flow through EXISTING binds
   (`core.stats`, `core.coreEffect*`) — verify per-core against the 02 refs. The stat-bonus CHIPS,
   action-card rules text, and "minions" labels are B20/B19 manifest work — flag, don't hand-draw.
4. **Pixel-perfect lanes:** keep the 6 classic gate lanes (old refs) as the REGRESSION bar. Add the 14
   per-core refs as REPORT-ONLY lanes (drive race=human, core=<each> via the existing core-cycling
   drive) — they become the failing bar only when B20's extraction lands. After A+C land, the
   long-pending baseline re-pin happens ONCE with Doug's eyeball (the standing "don't `--update`
   blind" rule) — content changed under every screen, the old numbers are noise now.
5. DoD: 35 combos selectable and correct end-to-end (NewGame → Equipment → march), accents live, gate
   green on the classic lanes, per-core lane report attached to the pass notes.

### CHUNK D — SYMMETRICAL FOES (after A–C; the "loop caught up" follow-up Doug queued)
Build the FOES.md symmetry model so existing foes get tougher + T1–T2 balanced:
1. Engine: foes get real gear — `Foe`/`Foes.cs` grow weapon/armor wiring through the SAME `Body`/
   consult/timer/cascade paths the player uses (frame parts already shared); arsenal entries use real
   `Technique` records at TECHNIQUES.md numbers; **Foe Effects as DATA + one interpreter** (exactly the
   Core-Effect pattern; FOES.md "design rules" constrain the vocabulary).
2. Content: the six built foes at FOES.md's T1/T2 specs (Skeleton/Bandit/Wraith/Ogre/Troll/Gargoyle,
   Dire variants, effects incl. *Brittle*/*Plunder*/*Insubstantial*/*Overwhelm*/*Regenerative Flesh*/
   *Stoneform*). Numbers are Cowork placeholder-blessed — build them, flag them, Doug tunes. Castle
   keeps its current proven shape (reconcile onto the model, don't retune in the same pass).
3. Encounter tables: keep today's node→foe mapping shape (`Maps.cs`/`Sieges.cs`) but pull from the new
   roster (skirmish = T1 pool, resource-hold = tougher T1/T2, castle unchanged). Which-foe-where stays
   design-open — use a seeded pick over the T1 pool, FLAGGED.
4. DoD: headless economy asserts per foe (kill-time vs player T1 kit inside FOES.md's envelope; foe DPS
   inside the band; arm-break actually cascades the arsenal off); campaign still winnable for all 35
   combos; **everything in FOES.md's IDEAS section stays unbuilt** (it's marked, believe it).

### Then: the standing bug queue (loop-actionable, in order)
- **Merchant pager doesn't indicate page 2** (Doug 2026-07-05, not root-caused): needs a live repro
  with a >3-section stock — check `MerchantSections()` count vs the `>` arrow's render/bind, and
  whether B13's 1px row-drop masks page 2. Don't re-diagnose B13/B14 (both root-caused, CD-side,
  logged); chase the landing instead.
- **Targeting-FSM watch** (no clean repro): unaimed-charged misbehavior — Core FSM proven correct
  headlessly; if it reproduces live it's shell-side (card-state chip / stale Targeting cursor).

## Needs Doug (live checks + review queue — loop skips, keep visible)
- **Launch fresh + verify:** statusStrip/footer full-width stretch on maximize (fix committed, built
  clean, not yet eyeballed live); merchant wareCard borders (code path verified correct; last report
  was very likely a stale EXE — DLL-lock evidence in git history); P1 arm/chest hit-test (headless-
  proven correct for every figure via `FigureHitTestTests`; if a live click still resolves chest,
  capture foe + click pos — `wraith`'s 56px overlap is the lead).
- **Review the flagged placeholders from tonight:** new-race HP (22/16/24) + tags/blurbs; per-core
  accent hexes; Sacrifice's heal amount (4 part-points); RuneDiscount retiring to 0; FOES.md T1/T2
  numbers + foe effects (prototypes for your review — they ARE being built per CHUNK D).
- **Baseline re-pin eyeball** (after A+C): one approved `--update`; also triage the ink-box
  overflow/collide lists then (the ~4.4x ink-height mystery on `previewCoreEffect*` rides it —
  Equipment shows the same three-collision shape, so it's one shared mechanism; eyeball decides
  P0-global-font-bug vs tooling-only).
- Standing design calls parked earlier: per-encounter re-arm scope for techniques + the free minion
  re-enable primitive (§8/§9 FTL-parity lock's second half); mid-run rune-bag Climb → Body reapply
  path (§11/§12); worn-armor DRAW composition (§17 #15) — B2-GO themed-half draw waits on it; Shade
  record deletion.

## Debt (active)
- Worn-armor DRAW wiring: `WornArmorBinding` resolves keys (all races after CHUNK B) but isn't wired
  into `Game1` figure compose — blocked on the §17 #15 composition call above.
- Bow not mounted on figures (back-mount layer = B2-GO art + §17 #22 assumed-not-drawn).
- Flat `sprites/char/chassis/*` legacy dir + mgcb entries — retire when cards compose from parts.
- Gradient fills: vertical/horizontal only. Minion `AltCost` summon un-costed placeholder.
- No rallied-support lane in combat. G1 skirmish aim numbers placeholder.
- `Game1` folder organization (Rendering/Screens/Input/...) — housekeeping, never over HiFi.
- "jump"/"beacon" FTL-isms in comments/test prose — low-pri sweep post-HiFi ("FTL" itself allowed).
- Rarity chips (COMMON/MAGIC/RARE) + rune-bag MARKS/PATHS/KEYSTONES sample runes appear in the refs —
  NO rarity/taxonomy model is designed; engine keeps gating those binds silent. Design-open (§17 #3),
  don't build from mock copy.

## Asset gaps (Needs Claude Design) — see outputs/CLAUDE_DESIGN_issues.md for the payload versions
- B20 (NEW): re-extraction for the per-core refs (stat-bonus chips, action-card rules text, minions
  vocab, NewGame re-author for 5×7 + accent tokens, 05/03/07 refs regenerated with v6 data).
- B18 (updated): technique/minion icons for the new content. B19: bay→minion manifest vocabulary.
- B13 (waresShelves 1px) · B14 (buildMinions zero-cell sizing) · B4 (Equipment buttons on Encounter/
  CampaignMap) · B7 (raceCard head imageBind) · B8 (citymap node states + glow primitive) · B10 (catalog
  display names) · B11 (bow sprites) · B1b (CD-side key-set diff) · B2-GO residue (weapon sprites etc.).
- Shield-object sprites; ranger figure quality; wraith/gargoyle art-quality; skirmish node icon.

## Verify mechanics (how to prove anything here)
- `RB_MF=<screenId>` renders a screen from the manifest; `RB_SMOKE=1 RB_MF=all [RB_SCREEN=…]
  [RB_SIZE=WxH] RB_SHOT=x.png` = headless shots + SMOKE BINDS/ASSETS/FIGURES/TEXTGEOM/SCENECOVER.
- `python tools/ui_gate.py` = the ONE gate (scratch build → driven smokes at 1920×1080 + 1600×1000 →
  binds/fidelity/textgeom vs `tools/ui_baseline.json`; refs must be exactly 1920×1080). `--update`
  only with a STATUS-logged human approval (M0 rule: never bend the ruler to raise a score).
- `tools/drop_audit.py` (dc.html↔manifest diff), `tools/geometry_diff.py`, `tools/probes.py`,
  `tools/fidelity_diff.py`, `tools/normalize_shot.py` — per loop.md.
- A RUNNING game locks the build output → build to a scratch dir. Crashes: read
  `bin/Debug/net9.0/crash.log`. Fonts are bundled TTFs; `GlyphSafe.Sanitize` guards regions.
- dc.html is READ-ONLY CD source. Never edit it; never hand-patch layout.json (regenerates externally).

## Pointers
- Design canon: `design/DESIGN_SPEC.md` (+ `design/systems/*.md` = operative content tables; FOES.md
  new). Layout contract: `design/LAYOUT_CONTRACT.md`. Pixel bar: `design/NN-*.png` + `design/SCREENS.md`.
- CD outbox: `outputs/CLAUDE_DESIGN_issues.md` (OPEN items only; loop appends Needs-CD finds per pass).
- Shipped history/rationale: `git log`. This file = current state only.

## Backlog (not prioritized; don't pull ahead)
- String/i18n externalization posture (route strings through content/binds; no hardcoding growth).
- Race↔core restriction matrix (POC allows all 35). Campaign graph model (§12 Layer 1) + city roster.
