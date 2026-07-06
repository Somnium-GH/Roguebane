# Status

## ‼ HIGH PRIORITY (2026-07-05, Doug) — Equipment/Inventory screen: 4 distinct root-caused bugs + 1 known item
Doug's live report bundled several symptoms under "Inventory." Read the actual code for each — these are
FOUR separate, independently-confirmed bugs, not one:

**1. GEAR tab clicks resolve to the wrong item after every equip/unequip (the "clicking the same spot
keeps toggling something else" / "clicked first repeatedly unequipped everything, clicked last on the
last page equipped everything" reports) — HIGH PRIORITY, this can strip a player's whole loadout by
accident.** Root cause: `GearTabItems()` (`Game1.ManifestRenderer.cs:1005`) builds the GEAR tab's list by
CONCATENATING the currently-equipped items pulled live off the body (`Body.Hands`, `.Ranged`,
`.ArmorOn(stat)`) with the currently-unequipped pool (`Exp.Stash.Weapons`/`.Armor`). Equipping/unequipping
doesn't just flip a state badge on a stable card — it physically MOVES the item between these two
source collections, so the concatenated list's membership and ORDER change after every single click.
Game1.cs's GEAR click loop (`UpdateEquipment`, ~line 386) maps click index `i` straight to `gear[i]`
computed FRESH that frame — so a click on a fixed screen slot resolves to whatever item the reshuffled
list put there THIS frame, not the item the player was looking at. Repeatedly clicking one screen
position drains/fills the list front-to-back exactly as described. **Contrast: TECHNIQUES and MINIONS
tabs do NOT have this bug** — their palettes (`_build.Palette`+`Stash.Techniques`;
`MinionKit+GrantedMinions+Stash.Minions`) are STABLE lists separate from the equipped/fielded state
(`Exp.Equipment`/`Exp.Minions`), so toggling never reorders them. **Fix: make GEAR's list the same
shape — a single stable owned-item roster (all owned weapons+armor, equipped or not, in a fixed order
keyed by identity/acquisition, not by which collection currently holds it), with EQUIPPED/EQUIPPABLE/
DISABLED/LOCKED computed as a per-item property at render/click time.** This also happens to be what
DESIGN_SPEC §6e already locks ("ONE state family... items don't move, the state badge changes") — GEAR
is the one tab not following its own spec.

**2. Only 1 column renders instead of 2 (`invItems` grid).** Two things stacked: (a) the manifest
authors `"cols": 2` on the `invItems` list item (`layout.json:6449`), but `ListLayout`'s grid flow
(confirmed earlier this session) computes column count from `region.W / stepX` and never reads the
authored `cols` hint at all — dead data, same class of bug as the merchant `waresShelves` miss. (b) Even
if it did, the container is 1px too narrow to fit 2 columns: `invItems.size[0] = 403`, item size `[199,44]`
gap `6` → 2 columns need `199×2+6 = 404`. Same shape of off-by-one as the earlier `waresShelves` fix.
Fix both: widen `invItems` to ≥404 (410+ for margin), and make grid flow actually respect an authored
`cols` when present instead of only back-computing from width (the width-based fallback should stay for
manifests that don't author `cols`).

**3. GEAR/TECHNIQUES/MINIONS tab buttons mislabeled/mis-sized/mis-spaced.** The `invTab` template
(`layout.json:10994`) is a fixed `40×18` box with its label text in a hardcoded `[12,5,16,8]` rect (16px
wide) — sized and positioned around the 4-character word "GEAR" specifically (matches the template's own
`sample: "GEAR"`), never adjusted for "TECHNIQUES" (10 chars) or "MINIONS" (7 chars), which will clip or
run outside the 40px button at the same font size. Separately, the 3 tabs (3×40 + 2×4 gap = 128px) sit in
a 419px-wide `invTabs` container (`layout.json:6394`) with no stretch/distribute rule, leaving ~290px of
dead space — reads as "not properly spaced." Fix: either widen the template box (and re-check text-rect
centering) or shrink the font per label length; distribute/stretch the 3 tabs across the container's real
width instead of left-packing them.

**4. Equipment reservation is visually present but easy to miss — not fully absent.** `attrBar`
(`layout.json:10901`, bound to `"attrs"` on the Equipment screen, `layout.json:6321`) DOES read live
`Body.Available`/`Body.Capacity` data and DOES render a pip strip (`attrs.cells`/`attrPip`) — but the pip
fill logic (`Game1.ManifestRenderer.cs:901-905`) only encodes TWO states: pip index `< Available` renders
colored/filled (free), everything else (both genuinely RESERVED capacity and any capacity lost to damage)
renders as the same plain `"slot"` token. DESIGN_SPEC's own "ATTRIBUTE-POOL PIP WIDGET" calls for THREE
distinguishable states (free/reserved/damaged) — today reserved and damaged are visually identical to
each other and to generic "unfilled," so equipping something that eats STR doesn't visibly change
anything distinct from how the bar already looked. Separate, smaller mislabel found alongside this:
the numeric readout binds `attrs.alloc` → `Item3` (which is actually `Available`, the FREE count) and
`attrs.available` → `Item4` (which is actually `Capacity`, the TOTAL) — the bind names are swapped from
what they actually carry (`Game1.ManifestRenderer.cs:1505-1508`); harmless today only because the template
doesn't literally print the word "alloc"/"available" next to them, but worth fixing before anyone reads
those bind names as documentation. Fix: give `PoolCells`/the pip-fill switch a real 3-way state (add a
`Reserved(stat)` tier between free and damaged — `Body.Reserved(stat)` already exists and is currently
NEVER called anywhere in `Roguebane.Game`, confirmed by grep), and swap the `alloc`/`available` bind names
to match what they actually hold.

**5. Paper-doll missing sprites (NewGame heads + "almost every other paper doll") — ALREADY TRACKED, not
a new bug.** This is CHUNK B item 1 below: the last CD drop updated `Roguebane.Content/Content.mgcb` but
the GAME-side `Roguebane.Game/Content/Content.mgcb` (the one the build actually reads/compiles) still has
ZERO of the new entries, so none of the new race/core figure art exists in the compiled content regardless
of what's on disk. Doug's report is a live confirmation this is real and visible now, not new information —
CHUNK B is already next in the work queue; no separate re-diagnosis needed.

## ✅ CHUNK A COMPLETE (2026-07-05, loop) — v6 race/core overhaul landed as ONE coupled slice, 405/405
Items 1–8 all done together (proven coupled — see the cont.#2 finding below): `Content/Races.cs` v6
(Human/Elf/Dwarf/Halfling/Half-Giant), `Content/CoreRunes.cs` full 7-core rewrite (budgets/actions/
minion-caps/stat-bonuses/kits/effects per CORE_RUNES.md, Barbarian added to `Roster`), the
`CoreEffectKind` interpreter (`Body.cs` equip-time discounts shared with `DisabledGear`'s sustain
cascade; `Caster.cs` reservation/discharge-time hooks) covering all 7 effects, `CoreRuneRosterTests`/
`StartingKitTests` rewritten for the new kits/names, `CoreEffectRefundsSummons` retired end-to-end
(dead mechanic, replaced by Conscription's `CoreEffectFreeSummons`), the SUSTAIN MODEL fix in
`Body.Activate`, and the Sacrifice mechanic (consume-a-minion heal). NEW this pass:
`CoreEffectTests.cs` — focused headless coverage for JackOfAllTrades (4 equip-time checkpoints incl.
DisabledGear-consistency), Fortified (CON-plate governance reassignment + tier discount + sustain-
cascade proof), Resonance (stack-and-decay-on-`RearmForEncounter`), Finesse (dual-wield technique
reservation −1, isolated to `Caster.Reservation` — confirmed no Body-side branch), FletcherLuck (bow
tier discount + a seeded maxCharge:0/luckyFree charge-skip proof), WarlordMight (2H STR-weapon −3 +
flat STR-plate −1). Conscription already covered by `CoreRuneRosterTests`. Full suite 405/405 (395
baseline + 10 new).
**FLAGGED placeholder needing Doug's confirm:** Sacrifice's heal formula (`4 × consumed minion's
Reserve` → 4/tier1, 8/tier2) is implemented and tested but the numbers/formula shape are unconfirmed
per RULES_SNAPSHOT.md's OPEN item — do not treat as final until Doug reviews.
**Next target: CHUNK B/C (Task #2 — UI paging, NewGame roster + Inventory screen).**

## ✅ TASK #2 COMPLETE (2026-07-06, loop) — UI paging (NewGame 7-core roster + Inventory), 3 bind-gap bugs fixed
`Pager`/`ListLayout.GridCapacity` (Core, headless-tested: `PagerTests`, `ListLayoutTests`) wired to
both the NewGame `corePager` and Equipment `invPager` manifest clusters — CHUNK C item 1 (roster
paging) is satisfied by this same work. Verified live: NewGame 7 cores page correctly; Equipment
GEAR tab pages 8 items / 3 pages (p2/p3 screenshotted, item data correct incl. seeded duplicate-
armor edge case); TECHNIQUES tab pages correctly; MINIONS tab's 0-item empty state renders clean
("PAGE 1/1", no crash) — both edge states (empty + full multi-page) driven per the plan.
**3 real pre-existing bind bugs found+fixed while verifying** (same bug class each time: an
unhandled key in `ResolveBind`'s per-type `switch`, `Game1.ManifestRenderer.cs`, silently fell back
to the manifest's static SAMPLE text instead of the real datum):
1. `CoreRune` kit display (`core.kitWeapon`/`core.kitArmor`) — overflow, needed a real formatter.
2. `Weapon`/`Armor` `invItems.effect` — every gear card showed the same placeholder stat line.
3. `Technique` `invItems.effect` — every technique card showed the same placeholder stat line
   (`"technique.description" or "invItems.effect" => t.DescText,` — reuses the existing populated
   field, no new derivation).
All 3 fixes verified via rebuild + RB_SMOKE screenshots showing distinct, correct per-item text.
Uncommitted in the working tree pending this STATUS write, landing as one commit next.

**`python tools/ui_gate.py` is currently RED across every screen — confirmed PRE-EXISTING, NOT
caused by this work.** Isolated via `git stash` (removes these 3 fixes) → rebuild → re-run gate →
near-identical failure at HEAD (equipment 69.8% vs this branch's 70.7%, newgame 75.9% vs 76.2% —
this work's real text is marginally BETTER than the placeholder baseline it's compared against,
never worse) → `git stash pop` restored. This matches what CHUNK C item 4 (above) already predicted
in writing: *"content changed under every screen, the old [gate] numbers are noise now... the
long-pending baseline re-pin happens ONCE with Doug's eyeball... after A+C land."* Task #1 (v6
races/cores) landed since the baseline was last pinned (`tools/ui_baseline.json` git history: last
touched by 2249948/8512334/c3d2c1f/cf81242/01de711, all pre-v6) — this IS that predicted noise, not
a new regression. Per the M0 rule, NOT bending the ruler: no `--update` without a STATUS-logged
Doug approval, which per CHUNK C's own plan waits for the roster/pixel-lane work below to finish
first. Numbers for the record (this branch, with the 3 fixes): encounter 77.5%, equipment 70.7%,
citymap 86.6%, campaignmap 94.5%, newgame 76.2%, merchant 82.9%; recurring `shift=(-3,-3)px` on
many unrelated elements across encounter/equipment/citymap/newgame is consistent with stale-ref
noise (old design PNGs captured before v6 content), not yet root-caused as a renderer bug — Task #3
should sanity-check this signature before the baseline re-pin, in case it IS hiding a real offset.
**Flagged, not yet logged as Debt until now — 2 more open items surfaced this pass:**
- Barbarian's CORE EFFECT card text visually cuts off (seen in an earlier screenshot pass) — not
  yet root-caused (font/box-size mismatch vs Warlord's Might's string length, likely). Needs-human
  triage or a quick measure-and-fix pass; not blocking, cosmetic on one card.
- `Kit.Count`-vs-`ActionSlots` mismatch at 4 more sites in `Game1.ManifestRenderer.cs` (~lines 551,
  577, 994, 1057) beyond the one Task #1 already fixed in `Forge.cs` — worst case is line ~994's
  technique-equip gate, which may silently cap equip at `Kit.Count` (3) instead of the real
  `ActionSlots` (4 for 5 of 7 cores), blocking players from using a rune-granted 4th slot. Needs a
  dedicated pass: audit all 4 sites, swap to `ActionSlots` where the check is gameplay-gating (not
  just display), headless-test the equip-cap per core.

## 📐 RULES REFERENCE (2026-07-05, Cowork — STANDING; consult on ANY design conflict/ambiguity)
The core/race/effect/kit/number design changed a lot this week. On any conflict or ambiguity about races,
cores, Core Effects, stat bonuses, default kits, technique/gear reserves, or the reservation model,
**`design/systems/RULES_SNAPSHOT.md` is the current source of truth** — a clean consolidated snapshot that
SUPERSEDES DESIGN_SPEC §11 (old Core-Effect roster) and §7 (old 2-race set) and any in-code placeholder
stats. The per-system `design/systems/*.md` docs hold the detail but carry historical reconciliation notes;
when they and the snapshot seem to disagree, trust the snapshot's clean current state. Numbers are
prototype/placeholder-blessed; snapshot items marked **OPEN** are NOT settled — don't hardcode them as final.
(Temporary — remove when this design folds into DESIGN_SPEC.)

## ✅ COWORK PASS LANDED (2026-07-05) — display strings + Reaver heal + Fortified unblock (verify tests, then absorb)
A Cowork/Doug hand-edit pass is COMPLETE on the working tree. All DISPLAY/data + one kit heal — it does NOT
touch the coupled CHUNK A stat/kit/effect economy (that stays yours). Absorb it into your next commit; the
one thing I can't do in the sandbox is BUILD — run Core.Tests and confirm green before committing.
- **`Content/Techniques.cs` + `Content/Armory.cs`:** technique `Desc` strings reconciled to TECHNIQUES.md
  canon rules text (they were loop-invented and drifting from the rules — Doug's complaint); Frenzy/Flurry/
  Swing/Shot/AimedShot given real card copy (were empty). `{power}` tokens preserved; shield/heal Descs bake
  the canon numbers (pool/regen) that match today's data — when a tuning pass changes those, update the Desc
  (or route through a token). Frenzy/Flurry MECHANICS were already correct (Reserve 3/2, `Consults.Both`,
  `AltStat.Dex`) — this pass only added their missing text.
- **`Content/CoreRunes.cs`:** **Reaver kit += `Techniques.Bandage`** (Doug + balance spreadsheet Kits/Demand
  tabs; CON 2 demand). Reaver was never meant heal-less — the "no heal glass cannon" interim was a loop
  artifact of the pre-Bandage healing map. Test-checked safe: `StartingKitTests.ReaverWieldsTwinDaggers…`
  asserts weapons/armor only; no test pins Reaver's kit list or "no heal"; Bandage is CON r2, every race has
  the CON headroom.
- **Canon:** `CORE_RUNES.md` healing map Reaver→Bandage (+ req `DEX 9 · CON 2`); `ARMOR.md` sanctions the
  **Fortified CON-plate override (Warden Core Effect)** — the item-3/4 blocker is cleared (see Needs-Human
  RESOLVED below). `outputs/CLAUDE_DESIGN_issues.md` reconciled vs CD_STATUS (B18 glyphs + B21 authoring
  closed). (Rules-text-from-canon PROCESS fix is a SEPARATE agent's `design/systems/RULES_SNAPSHOT.md` — see
  the RULES REFERENCE block above; I did not touch CLAUDE.md.)
- **For CHUNK A item 3 (your slice):** the spreadsheet confirms `CORE_RUNES.md` §Default-loadouts ALREADY
  matches Doug's model for every core (only Reaver's heal had drifted, now fixed) — so item 3's kit target is
  CORE_RUNES.md **verbatim**, no re-derivation. Confirmed engine-vs-canon drift still to reconcile in your
  tested slice: Warden Cleave→**Jab** (Doug: nothing too strong to start), Adept →Stoneskin + drop the
  Skeleton kit-minion, Summoner →Sacrifice/Barkskin + Skeleton-only (drop IronGolem/Brace/Bandage), Ranger
  Shot→**Aimed Shot** + ShortSword→**Dagger** (drop Brace), **Barbarian** core add, Core-Effect roster rename+
  mechanics (still pins `CoreRuneRosterTests` line ~32 Grunt "Hollow Vessel" + the Summoner RefundsSummons
  test — update those), budgets + v6 race stats. All now UNBLOCKED.

## ✅ FIXED (2026-07-05, loop) — `"parent"` in layout.json now resolves; was the HIFI-HIGH-PRIORITY
## overlapping-panel bug from Doug's NewGame screenshot
`Element` (`LayoutManifest.cs`) gained `public string? Parent { get; init; }`. `ScreenLayout` gained a
screen-aware `Resolve(int designW, int designH, Screen screen, Element e)` that recurses through the
parent chain (cycle/missing-id guarded, falls back to viewport-anchor same as before) and resolves the
child's anchor+offset against the PARENT's resolved rect instead of the screen's flat design size.
`ManifestUi.Resolve` now passes `screen` through so every `Rect`/`ElementRect`/`ListCells` call site (and
`Game1.cs`'s smoke-probe sidecar, which calls `ScreenLayout.Resolve(def, x)` directly) gets the fix for
free — no call-site changes needed beyond the one `ManifestUi.Resolve` line. New headless tests in
`ScreenLayoutTests.cs`: parented child resolves against a non-origin parent rect, missing parent id
degrades to viewport, cyclic parent chain degrades to viewport instead of stack-overflowing. 395/395 green.

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
- Icons (RECONCILED 2026-07-05 vs CD_STATUS #34/#36, verified on the tree): the pass-8/9 glyphs LANDED —
  `flurry` `aimed_shot` `siphon` `barkskin` `sacrifice` `bind` present + `frenzy`/`flurry` re-captured as
  the STR/DEX split two-badge. B18 now open for ONLY `parry` `steel` `suture` + minion `iron_golem` `hound`;
  tinted-tile fallback covers those meanwhile.
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

### CHUNK A — v6 DATA LAYER — ✅ DONE 2026-07-05 (see the banner at the top of this file for the
landed slice + the Sacrifice-heal placeholder flag). All 8 items + the new `CoreEffectTests.cs`
coverage landed together in one coupled commit (races/cores/kits/effects cannot split — a full-kit
CoreCampaignTests run is the chunk's own DoD). Blow-by-blow progress log moved to `git log`
(STATUS.md whittling rule, line ~54) — see commits around 2026-07-05 for the coupling investigation,
the Reaver/Sacrifice/Barbarian Needs-Human resolutions, and the RuneDiscount/Stoneskin/Wand-req
crumbs landed along the way.

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
- **Engine: recursive `parent`-box resolution — DROP PRE-REQ (2026-07-05, verified ABSENT).** CD's next
  drop re-anchors every screen and adds a `parent` field (CD dev-memory #35, = the no-absolute-positioning
  payoff, outbox B21): a child's `offset` becomes RELATIVE to its named parent element's box instead of the
  screen, so grouped clusters (HP readout+pips, panel+contents, top-right controls, action-bar minion
  column) reflow as ONE unit. Today `ScreenLayout.Resolve(designW,designH,Element e)`
  (`Roguebane.Core/Layout/ScreenLayout.cs:16`) resolves anchor+offset+size against the SCREEN only and
  `Element` has no `parent`. Add: parse `parent`; resolve an element's rect RECURSIVELY (resolve the
  parent's box first, then place the child by anchor+offset INSIDE it) — single forward pass, id→box cache,
  children rank after parents in z. MUST land BEFORE/WITH that drop or every parented child mis-places
  (offsets are now small parent-relative, not screen-px). SEPARATE: off-16:9 aspect-fill (grow the middle
  panel) is §13 WIP, not part of this. (Verified 2026-07-05: the OTHER dev-memory "engine-pending" items —
  merchant/states/border.sides/shield-pips/core.label/pagination — already SHIPPED; logged in
  `CD_CLOSED_ITEMS.md`. Only #35, #30 glow/pulse, and #32 worn-draw remain, #32 already in Debt below.)
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
- `Kit.Count`-vs-`ActionSlots` mismatch, 4 sites in `Game1.ManifestRenderer.cs` (~551/577/994/1057) —
  line ~994 may gameplay-cap technique-equip at 3 instead of the real `ActionSlots` (4 for 5/7 cores).
  See the TASK #2 COMPLETE banner above for detail. Needs an audit pass + per-core headless equip-cap
  test, not yet started.
- Barbarian's CORE EFFECT card text visually cuts off (Warlord's Might string vs box size, likely) —
  seen in a screenshot pass, not yet root-caused. Cosmetic, one card, not blocking.

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
