# Status

## ‼ HIGH PRIORITY (2026-07-05, Doug — interview answers, 4 small precise fixes)
1. ✅ DONE (2026-07-06, loop) — `Content/Races.cs`: Dwarf `Hp: 20→17`, HalfGiant `Hp: 17→20`, Halfling
   unchanged. New pinning test `RaceTests.DwarfAndHalfGiantHpMatchDougsConfirmedSwap`. 425/425 green.
   (Original text preserved below for the exact rationale.) Current: `Dwarf = ... Hp: 20`,
   `HalfGiant = ... Hp: 17` — backwards, since Dwarf is the CON-affinity race (CON converts to HP,
   RACES.md's own rule) and should read HIGHER than Half-Giant (STR-affinity), not lower. Fix: Dwarf
   → `Hp: 17`, HalfGiant → `Hp: 20`. Halfling (`Hp: 13`) unchanged. Still placeholder-blessed, not a
   final balance pass — canon note added to `design/systems/RACES.md`.
2. ✅ DONE (2026-07-06, loop) — `Minions.Shade` deleted outright; every reference removed/updated
   (`Content/CoreRunes.cs`, `Content/Paths.cs`, `Game1.cs` smoke-screen comment, `design/DESIGN_SPEC.md`).
   `Paths.BoundConclave` (the Conclave keystone) used to grant Shade as its minion reward — hit the
   documented ambiguity ("drop the grant or point it at a real minion — Doug's call"), so per the
   don't-invent rule this is now **Needs human**: BoundConclave currently grants NO minion (empty), a
   Cost-6 keystone with no reward. Doug needs to pick a real replacement minion or redesign the reward.
   Tests: `MinionTests` rewired its Shade-as-generic-3rd-minion usages onto `IronGolem`/`Hound` (values
   adjusted to match their real Reserve costs, not copy-pasted); `RuneGrantsTests
   .AMinionKeystoneExposesItsGrantedMinion` now exercises the GENERIC keystone-grant mechanism via a
   synthetic Mark, so the code path stays covered independent of Conclave's open decision. 425/425 green.
   Verified via clean `dotnet build Roguebane.Game --no-incremental` (0 errors) — Game1.cs changed.
3. ✅ DONE (2026-07-06, loop) — Sacrifice's heal formula (4/8 part-points, T1/T2) is APPROVED as the
   standing placeholder (RULES_SNAPSHOT.md already reflected this from the earlier Doug drop). Engine
   already matched (`4 * minion.Reserve` in `Caster.Discharge`) — the only stale thing was the
   `Techniques.Sacrifice` comment, which still called the numbers unconfirmed; reworded to point at the
   approval. Real gap found: **the formula had NO headless test** — added `SacrificeHealTests` (pins
   4x-Reserve heal amount, highest-Reserve minion consumed first, hold-fire with no minion fielded).
   428/428 green.
4. ✅ DONE (2026-07-06, loop) — Minion re-arm scope fixed. `Caster.RearmForEncounter()` now actually
   `Dismiss()`s every fielded minion (was only resetting `_minionCountdown`, never touching `_minions` —
   a minion fielded last fight silently stayed fielded into the next one for free, no fresh Summons
   charge). Techniques were already correct (persist across encounters, only the charge clock rewinds —
   no change needed there). **Wrinkle found + fixed:** a naive "always rearm on Enter" would have
   dismissed the chassis STARTING KIT (Summoner/Ranger's `DefaultMinions`, fielded at `Forge.Embark`
   assembly time, before any encounter) on the leg's very first fight — that's not a back-to-back
   carry-over. `Expedition.Enter()` now only calls `RearmForEncounter()` when `Battle` is already
   non-null (skips it on the leg's first encounter, applies it on every one after). Tests:
   `CasterFiringTests.RearmForEncounterDismissesEveryFieldedMinionAndFreesItsReservation` (replaces the
   old test that pinned the buggy timer-reset-only behavior) pins dismiss + freed stat reservation +
   re-summon re-paying Summons at the `Caster` level; `MinionTests.
   TheStartingMinionSurvivesTheFirstEncounterButNotTheSecond` pins the first-encounter exemption +
   second-encounter dismissal at the `Expedition` integration level. 429/429 green.

## ✅ CHUNK B item 1 DONE (2026-07-06, loop) — game-side mgcb mirrored, 1846 new entries, build green
`Roguebane.Game/Content/Content.mgcb` (the copy builds actually read) had ZERO of the new v6 race/core
body-figure, worn-set, and icon entries that `Roguebane.Content/Content.mgcb` (CD source) already had —
confirmed via grep: 0 vs 930 matches for `dwarf_|halfling_|half_giant_|human_barbarian|elf_barbarian`.
**Fix:** wrote a one-off mirror script (diffed every CD-source `#begin` block against the game-side file
by asset key, transformed each missing block's `/build:<path>` line to the established game-side
convention — `/build:../../Roguebane.Content/<path>;<output-name-without-extension>`, `#begin` name
stripped of its file extension to match game-side convention) and appended all 1846 missing blocks.
**Caught before commit:** the naive diff also picked up `#begin gear_catalog.json`/`#begin layout.json`
(CD-only `/copy:` directives, not `/build:` — my transform only rewrote `/build:` lines, so these two
would've copied through with an unrewritten relative path that doesn't exist on the game side).
`gear_catalog.json` is a CD-tooling-only artifact (grepped: zero references in `Roguebane.Game` code);
`layout.json` already has its own working mechanism (`Roguebane.Game.csproj`'s `None Include="..\
Roguebane.Content\layout.json" Link="Content\layout.json"` + `CopyToOutputDirectory`). Both blocks were
out of CHUNK B's stated scope (body dirs/worn sets/icons only) and were stripped before commit.
**Verification:** `dotnet build Roguebane.Game -o <scratch> --no-incremental` → 0 errors, 0 warnings —
all 1846 new textures compile to xnbs. Directive slashes double-checked against the 57cc8a6 precedent
(`/importer:`/`/processor:`/`/build:` — forward-slash, matches existing game-side entries exactly).
Core.Tests untouched by this change (asset-only): still 424/424 green.
CHUNK B items 2-4 (drop guards, `RB_SMOKE=1 RB_MF=all` figure/asset verification, final DoD) remain open.

## ✅ FIXED (2026-07-06, loop) — equip-time over-reservation gap: gear now refused cumulatively, not degraded after the fact
Root cause was `Body.Wield`/`Body.EquipRanged`/`Body.Equip` each gating only on `Capacity(stat) <
item's own Reserve` — raw capacity, never against what OTHER currently-equipped gear already
reserved on that same stat. A player could equip more gear than the shared pool held; nothing
blocked the equip action itself, only `DisabledGear`'s ongoing sustain cascade noticed afterward
and silently marked the lowest-priority piece(s) DISABLED. Named and confirmed by DESIGN_SPEC §7's
"Reservation timing" lock (2026-07-04) and Doug's exact prescribed fix in this file (2026-07-05).
**Fix:** added `Body.GearOnlyAvailable(stat, excludeArmorSlot)` — cumulative with other EQUIPPED
gear on the stat, but deliberately blind to `TechReserved` (same rule the existing `GearOnly*`
sustain reads already enforce, so a lingering active technique from a finished fight can't wrongly
block an unrelated equip — see `GearOnlyChecksIgnoreLingeringTechniqueReservation`). `Wield`/
`EquipRanged`/`Equip` now gate on this instead of raw `Capacity` — symmetric with how `Activate`
already refuses outright instead of degrading. `DisabledGear` gained an `excludeArmorSlot` param so
`Equip`'s same-slot SWAP (a piece replacing whatever already occupies that slot) doesn't count the
outgoing piece against the incoming one's own headroom check — without this, `GearingTests.
EquippingArmorDisplacesTheWornPieceBackToThePack` would wrongly refuse the swap.
The 3 existing `DisableCascade*` tests in `BodyTests.cs` previously triggered the cascade by
stacking gear PAST the pool at equip time (each piece fit "alone" against raw capacity) — that
pattern is now impossible by design, so all three were redesigned to start with enough headroom for
every piece to equip cumulatively, then shrink the pool via `Damage()` to trigger the identical
cascade-ranking assertions (cascade mechanism itself untouched). Redesigning surfaced a real
adjacent subtlety: Plate armor's PartMitigation soak (§6c, 2/tier) applies whenever a piece's SLOT
matches the damaged part's stat — two of the three tests originally picked slots that collided with
the arm being damaged, silently blunting the `Damage()` calls; fixed by choosing non-colliding slots
for the test armor pieces (comments left in place explaining why). Two new pinning tests added:
`WieldRefusesOutrightOnceOtherGearHasFilledThePool`, `EquipArmorRefusesOutrightOnceOtherGearHasFilledThePool`.
`dotnet test Roguebane.Core.Tests`: 424/424 green. Core-only change — no Game-side code calls
`Wield`/`EquipRanged`/`Equip` directly (all go through `Gearing.EquipWeapon`/`EquipArmor`), so no
Game rebuild was needed.

## ✅ FIXED (2026-07-06, loop) — inventory shows every technique in the game, not the core's kit
Root cause was `Content/Sessions.cs`'s `NewBuild()` seeding `BuildSession` with a static
`BuildPalette = Techniques.All.Concat(Armory's 5 weapon-verbs)` (19 items, every technique in the
game) regardless of the chosen core — `BuildSession.Palette` returned this list verbatim and the
UI's TECHNIQUES tab read it directly.
**Fix:** `BuildSession.Palette` is now a computed property — `CoreRune.Kit.Concat(_runes.
GrantedTechniques).ToList()` — instead of a fixed field, so it narrows to the current chassis's kit
plus whatever the taken rune Marks grant (`RuneLoadout.GrantedTechniques`, already existed), and
recomputes automatically across `CycleCoreRune`/`Climb`. The now-unused external-palette ctor param
was removed from `BuildSession` and `Sessions.NewBuild()` (`BuildPalette` field deleted — clean
removal, no back-compat shim); `SeedKit()`/`Equipment` read the same computed `Palette`. No Game-side
changes were needed — `Game1.cs:353,355,377` and `Game1.ManifestRenderer.cs:1239` all read
`_build.Palette` and pick up the narrower list automatically.
Updated `BuildSessionTests.cs`: `ToggleBuildsTheLoadoutInPaletteOrder` → renamed
`EquipmentOnlyEverIncludesPaletteTechniques` (proves off-palette toggles no longer leak into
Equipment — this was the bug, now asserted the other way); `LaunchMintsTheChosenBodyIntoARun`
adjusted (Lunge is no longer toggleable pre-grant, so the test drops a kit item instead). Added two
new tests per the DoD ask: `PaletteIsScopedToTheCurrentCoresKitForEveryChassis` (loops the whole core
roster, asserts `Palette.Count == CoreRune.Kit.Count` with no grants taken, not 19) and
`ClimbingAGrantKeystoneAddsItsTechniqueToThePalette` (constructs a `BuildSession` with
`Paths.TempestLadder` directly, climbs to the Eye of the Storm keystone, confirms `maelstrom` lands
on the Palette — proves the grant-union half of the fix, since the live NewBuild ladders (Vessel/
Resonance) don't currently grant any techniques themselves). `dotnet test Roguebane.Core.Tests`:
420/420 green. `dotnet build Roguebane.Game`: 0 errors/0 warnings.

## ‼ PRIORITY BACKLOG (2026-07-05, Doug) — Merchant Wares feature build. Ranks directly after all
## outstanding bug work above (the HIGH PRIORITY entries), ahead of CHUNK C/D and everything below.
Three sub-features, in the order Doug gave them. **AUDITED (2026-07-06, loop): 2 and 3 were already
shipped BEFORE this backlog note was written** (`cf638b3` "Merchant click-to-buy complete (P0-C.7)" +
the resource-buy commits before it) — Doug's ask reads as a checklist written without the current code
in front of him, not a from-scratch build. Only item 1 has real remaining work, and it's blocked:
1. **◻ NEEDS HUMAN — ware rotation + randomization.** Map-tier-appropriate gear by default, occasionally
   above-tier (a rare upgrade roll). Section presence (2-3 of 5 categories, weighted toward 3) is DONE —
   `MerchantStock.Roll()`'s locked 2026-07-03 weights already deliver that. What's missing is (a)
   TIER-AWARE item selection (confirmed: `MerchantStock.Pick<T>` is a tier-blind partial Fisher-Yates over
   the WHOLE pool passed in — no tier filtering anywhere) and (b) an "occasionally above-tier" bonus roll.
   **Blocker: there is no "map tier" signal anywhere to select against.** `Armor`/`Weapon` both carry a
   `Tier` field, but the MAP side has nothing — `CityMap`/`MapNode` carry no tier/depth/rank field, and
   §12's own Layer 1 (campaign map, cities → Capital) is explicitly `[OPEN]` in DESIGN_SPEC ("city count;
   procgen vs authored" undecided) — the POC currently runs exactly one leg (`Maps.StandardLeg`), so
   there is no notion yet of "which tier of the run this merchant sits in." Building tier-aware selection
   now means INVENTING that signal (leg index? node depth-from-camp? an explicit authored field?) —
   exactly the "no undesigned mechanics" line in CLAUDE.md. Doug needs to pick the source-of-truth for
   map tier before this can be built without guessing.
2. **✓ DONE (pre-dates this backlog) — gear/technique/rune sales.** `Expedition.BuyWeapon/BuyArmor/
   BuyTechnique/BuyMinion/BuyMark` all wired to `Stash`/`Gold`, real gold cost (`Expedition.Price(...)`
   overloads), stock cleared on purchase, landed in the run inventory per §12's receiving rule. Covered
   by `ExpeditionTests.cs` (`TheMerchantSells*IntoThe*` × 5, `WarePurchasesRejectWhenGoldRunsShort`).
   Presentation is still placeholder chrome (flagged, per CLAUDE.md's placeholder rule) — that's a CD ask,
   not engine work.
3. **✓ DONE (pre-dates this backlog) — Supply / Summons / Charge purchase.** `Expedition.BuySupplies/
   BuyCharge/BuySummons` all exist, gold cost per unit (seeded per-node, stable), capped at max, decrement
   their own per-visit stock. Covered by `ExpeditionTests.MerchantStocksSeededSuppliesAndCharge` +
   `MerchantRefillsSummonsUpToTheCap` (2026-07-06, loop — `BuySummons` had zero test coverage until now).
DoD per CLAUDE.md: headless Core tests for the buy/sell economy math (gold deducted, stock decremented,
resource capped at max) exist for 2/3. Item 1's tier-aware logic stays unbuilt until the map-tier
question above is answered — do not guess at a signal to unblock it.

## ‼ HIGH PRIORITY (2026-07-05, Doug, live screenshot) — the 4-zone pip build (previous entry, below) IS
## landing, but its container is too narrow — same bug CLASS as waresShelves/invItems, THIRD instance.
## This single miss cascades into 3 of Doug's 4 new reports; only 1 is a genuinely separate bug.
Doug's screenshot (Equipment "Attributes" panel, Human+Grunt): STR shows 5 hashed pips + reads "6/1";
INT/DEX show "6/6"; CON shows 2 empty + 3 filled, "6/4". **Root cause, confirmed by the numbers
themselves:** STR capacity is 6 (Human base 5 + Grunt's +1 bonus, v6). The pip container
(`attrs.cells`, `layout.json` rect width 326, `attrPip` template size `[53,9]` gap `2`) fits
`floor((326+2)/55) = 5` cells — but `PoolCells()` (just landed, see the 4-ZONE entry below) emits
`Capacity + Damaged` cells, which for undamaged STR is **6**. The 6th cell (source width needed:
`6×53 + 5×2 = 328`, container is `326` — 2px short) silently DROPS, same "list overflow hides the last
cell instead of partial-rendering" rule that bit `waresShelves` (1px) and `invItems` (1px) earlier this
session. **This is why:**
- **"Reservation doesn't show" / STR looks fully spent:** the dropped 6th cell IS the FREE pip. With it
  gone, all 5 VISIBLE pips read "gear-reserved" (hashed) and the bar looks 100% spent when 1 unit is
  actually still free (`Capacity 6, GearReserved 5` back-computes correctly to Grunt's DISCOUNTED total:
  4 plate pieces × (2 raw − 1 JoAT) + 1 Longsword × (2 raw − 1 JoAT) = 4+1 = 5 — the JoAT aggregate math
  checks out; nothing wrong with `Activate`/`EffectiveWeaponReserve` here, only the missing 6th pip cell).
- **"Jab shouldn't activate but does":** it's not a bug — Jab's reserve is 1 (RULES_SNAPSHOT), and 1 STR
  really is free (see above); the pip bar just never shows the free unit that makes that legal. Once the
  container fits all 6 cells this will visually resolve itself — don't chase `Body.Activate` further, it
  was already checked and confirmed correct last pass.
- ⇒ RE-DIAGNOSED (2026-07-06, loop) — not an engine bug, re-routed to CD as **B25**. Checked
  `ListLayout.Cells`'s horizontal-flow path (used for `attrs.cells`, non-grid): its overflow-drop is a
  second DELIBERATE pin, not an oversight — the "26 HP pips in a 12-pip region" comment at
  `ListLayout.cs:42-44` documents the same "never spill past the container edge" contract the
  `GridCapacity` pin covers for card grids, and it's shared by every horizontal pip strip (HP included) —
  changing it here would silently change HP's rendering too, an undesigned side effect for a container-
  geometry bug. The only real defect is `attrs.cells`'s authored width: `layout.json` rect is `326`, but
  6 `attrPip` cells (`53` wide, `2` gap) need `6×53 + 5×2 = 328` — 2px short, same shape as `waresShelves`/
  `invItems`. Logged as **B25** in `outputs/CLAUDE_DESIGN_issues.md` (widen `attrs.cells`'s rect to ≥328,
  330+ for margin); no engine change needed once it lands. The "general min-fit engine fix" idea from the
  first pass is NOT taken up here — it would touch the shared `Cells()` path (HP pips too) beyond what
  this bug needs; if Doug wants that as a deliberate design change, it's a separate ask. Parked on CD, not
  blocking.

**⇒ WEAPON HALF FIXED (2026-07-06, loop):** `Game1.ManifestRenderer.cs`'s `"invItems.badgeNum"` for a
`Weapon` now reads `Exp.Player.Body.EffectiveWeaponReserve(w)` (InRun) instead of `w.Reserve` raw.
`Body.EffectiveWeaponReserve` went `private` -> `public` (pure function, no new state) so the renderer can
call the SAME discount math the equip gate already used — no duplicated formula. `ResolveBind` dropped
`static` to reach the instance's `InRun`/`Exp`. New test `CoreEffectTests.
EffectiveWeaponReserveIsPubliclyReadableForCardDisplayAndMatchesTheEquipGate` pins the public contract
directly (WarlordMight -3 on a 2H claymore). 421/421 green; `Roguebane.Game` full clean rebuild (`--no-
incremental`, `scratch-build-stale-obj` memory) 0 errors.
**Technique half NOT done — separate, harder slice, still open:** `"invItems.badgeNum" =>
t.Reserve.ToString()` (technique card badge, same file ~line 1632) still prints raw. Reason it's not the
same fix: technique reservation isn't a flat per-item discount like weapons/armor — `Caster.Reservation`
(private, Caster.cs:196) branches on `Consults`: a `Primary`-consult technique (e.g. Jab) reserves **0** of
its own (the ONE weapon it swings already reserves as gear — baking `t.Reserve` in too would double-count),
while a `Both`-consult technique (Frenzy/Flurry) reserves its own `t.Reserve` minus Finesse/JoAT. Showing
the *right* badge needs that same branch, but `Caster` is a combat-time object (needs a target to
construct) — not something the pre-run Equipment/build screen has lying around the way `Exp.Player.Body`
already did for weapons. Needs a display-only accessor (Body- or Technique-side, not a full `Caster`) before
this half can land — a real design/API question, not a one-line wire-up. Left as-is, not touched this pass.

**Not yet root-caused, needs a retest once the above two land:** "removing the shield and re-equipping it
never reserves." Two live candidates, don't guess further without a fresh repro: (a) the still-open
GEAR-tab click-misrouting bug (logged two entries below, `GearTabItems()` reordering on every toggle) may
mean the click never actually hit the shield at all; (b) CON's own pip zone may hit the same container-
width drop as STR, hiding a real change. Retest after both fixes land before treating this as a third
distinct bug.

## ‼ HIGH PRIORITY (2026-07-05, Doug) — 3 more findings: pip-bar 4-zone build (art already exists!) —
## ✅ ALL THREE FIXED 2026-07-06 — pager-button skin bug (below), equipped-items-sort-to-top
## (also fixes bug #1 above), pip-bar 4-zone build (item 1 below)

**1. ✅ FIXED (2026-07-06, loop).** Attribute pip bar 4-zone build — see bug #4's entry below for the
complete fix writeup (`Body.GearReserved`/`TechReserved`/`Damaged`, `AttrRow`, 4-zone `PoolCells`). All 4
zone looks were confirmed already on disk (`pip_full`, `pip_reserved`, `pip_damage`, `pip_empty`) — pure
engine wiring, zero CD ask, exactly as diagnosed here.

**2. ✅ FIXED (2026-07-06, loop).** Pager prev/next buttons ("rotated and overscaled"). Root cause was
as diagnosed: `button_pager.png` (`Roguebane.Content/ui/button/`) is a small near-SQUARE frame purpose-
built for compact pager buttons; `button_normal/hover/down/disabled/on.png` are wide ~3:1 RECTANGULAR
bars built for full-width buttons like BEGIN THE RUN. `DrawStateSkin` (`Game1.ManifestRenderer.cs`) drew
EVERY `"family":"button"` element from `e.States[key]` only, ignoring `e.Image` entirely, so the 4 pager
buttons (`invPagePrev/Next`, `corePagePrev/Next` — authored with `"image":"Content/ui/button/
button_pager.png"` specifically for the compact frame) got the wide-bar skin's corners squeezed into a
~20x15px target, skewing the bevel/rivets. **Fix, both parts landed:** (a) `Roguebane.Game/Content/
Content.mgcb` now mirrors the `ui/button/button_pager` entry the CD-side mgcb already had (game-side had
zero registration — a separate confirmed gap, folds into CHUNK B's mgcb-mirror pass); (b) `DrawStateSkin`
now checks a new `FamilyOwnsImage(states, image)` helper before falling back to the state-key/9-slice
path — if `e.Image` names a texture that does NOT match any of the element's own `states` values, it's a
deliberate per-element override (not CD's usual same-family preview default, which DOES match a states
entry) and gets drawn untinted at the element's own rect instead of 9-sliced. Verified by surveying all
13 button-family elements' `image` vs `states` values: only the 4 pager buttons fail to match any state
value (the other 9, e.g. `autoAttackBtn` imaging `button_on.png`, correctly still take the state-driven
skin). **Verification:** clean `Roguebane.Game` build (0 errors/warnings), then `RB_SMOKE=1
RB_SCREEN=newgame` screenshot of `corePageNext` — renders as a proper square 9-sliced frame with corner
rivets and a centered, undistorted `▶` glyph (equipment screen's own pager was untestable this way, page
count is 1/1 for a fresh Human Grunt so the buttons don't render at all there).

**3. ✅ FIXED (2026-07-06, loop).** "Sort equipment so equipped items stick to the top." Bug #1's roster
fix (below) landed the STABLE base order but deliberately did NOT sort equipped-first — its own comment
said "order here never needs to reflect current equip state," since at the time only click-routing
stability was in scope. This is the separate, still-open half of that ask. **Fix:** `GearTabItems()`
(`Game1.ManifestRenderer.cs`) now applies a STABLE `OrderByDescending(item => InvCardState(item) ==
"equipped")` on top of the roster's fixed acquisition order — LINQ's `OrderBy`/`OrderByDescending` are
documented-stable, so ties (same equipped/not-equipped bucket) keep the roster's original order, meaning
equip/unequip visibly moves an item to/from the top cluster (the point of the ask) while both the render
list and the click hit-test (same shared `GearTabItems()` call, no divergence) stay in lockstep — no
regression on bug #1's click-routing fix. Reuses the exact live EQUIPPED read `InvCardState` already
computes for the card badge, no new state/derivation. Game-only change (no Core touched), verified via
clean `Roguebane.Game` build + logic review (stable-sort semantics are LINQ-documented, not assumed); not
re-screenshotted live this pass since the underlying roster/paging/click-routing were already verified
live in Task #2 and this only adds a stable sort on top.

## ⇒ CLARIFIED (2026-07-05, Doug) — active-technique reservation IS correctly gated in code; the risk
## was in the DOCS, not the mechanic. Plus one still-open, already-known mechanic gap.
Doug's rule, verbatim: "Equipment permanently reserves attr. A skill must reserve attr to be active and
charging or passively active. It cannot be active or passively active with insufficient attr. If
deactivated attr is returned to pool." **Checked `Body.cs` directly against this, line by line:**
- `Activate(Active active)` (line ~127): gates on `Capacity(stat) - TechReserved(stat) < active.Reserve`
  → returns `false` (refuses activation outright) when insufficient — matches "cannot be active... with
  insufficient attr" exactly. Confirmed correct, not reinterpreted.
- `Deactivate(Active active)` (line ~139): just removes it from `_actives`, and `Reserved()`/`Available()`
  sum over `_actives` live — so the freed reserve is immediate. Matches "deactivated attr returned to
  pool" exactly.
- `Reserved(stat) = TechReserved(stat) + DisabledGear(stat).EnabledTotal` (line ~78-82): gear's
  contribution comes from whatever's CURRENTLY in `_hands`/`_ranged`/`_armor` — unconditional, no
  separate "gear activation" gate exists — matches "Equipment permanently reserves attr" exactly.
**So the ACTIVATION rule is implemented correctly and wasn't reinterpreted.** The real risk Doug sensed
is real, just located in `design/systems/RULES_SNAPSHOT.md`, not the engine: its old "Reservation /
combat model" section said *"every ACTIVE thing reserves... worn armor + equipped weapons + active
techniques + active minions"* — one sentence that reads as if gear only reserves while some separate
"active" state holds, same as techniques. That's not what the code does (gear reserves unconditionally
at equip, no activation gate) and not what DESIGN_SPEC §7's fuller "Reservation timing" lock says either
— RULES_SNAPSHOT's compression just lost the distinction. Since STATUS.md's own RULES REFERENCE banner
tells the loop to trust RULES_SNAPSHOT over DESIGN_SPEC on any perceived conflict, that ambiguity was a
real latent risk of a future pass "fixing" gear reservation into an activation-gated model that doesn't
belong. **Fixed this pass:** RULES_SNAPSHOT.md's reservation section now spells out the two DIFFERENT
triggers explicitly (equip-time unconditional reserve for gear vs. activation-only reserve for
techniques/minions) and points back to DESIGN_SPEC §7 as the fuller lock.

**Equip-time over-reservation gap: FIXED (2026-07-06, loop)** — see the FIXED entry at the top of this
file. `Wield`/`EquipRanged`/`Equip` now gate cumulatively via `Body.GearOnlyAvailable`, not raw
`Capacity`.

## ‼ HIGH PRIORITY (2026-07-05, Doug) — Equipment/Inventory screen: 4 distinct root-caused bugs + 1 known item
Doug's live report bundled several symptoms under "Inventory." Read the actual code for each — these are
FOUR separate, independently-confirmed bugs, not one:

**1. ✅ FIXED (2026-07-06, loop).** Root-caused and closed — see below. Original report kept verbatim
for the record.

GEAR tab clicks resolve to the wrong item after every equip/unequip (the "clicking the same spot
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

**Fix landed exactly as prescribed above.** `Stash` gained a stable identity roster
(`WeaponRoster`/`ArmorRoster`, reference-identity keyed so structurally-equal `record` duplicates —
the seeded duplicate-armor case — stay distinct entries) fed by `TrackOwned`, called from
`Stash.AddWeapon`/`AddArmor` (catches merchant buys AND the RB_SMOKE dev-seed path, both of which
call these) and once more from `Expedition`'s constructor (seeds kit items that `Forge` equips
straight onto `Body`, which never touch `Stash`). `Game1.ManifestRenderer.GearTabItems()` now reads
`Exp.Stash.WeaponRoster.Concat(ArmorRoster)` — fixed acquisition order, immune to equip/unequip
churn — instead of concatenating live Body+Stash membership. `InvCardState`'s EQUIPPED/EQUIPPABLE/
DISABLED/LOCKED computation was already correct (per-item, not position-based) and untouched.
4 new headless tests (`StashTests`: roster order survives leaving the pack, equal-valued duplicates
stay distinct, `TrackOwned` is idempotent by reference; `ExpeditionTests`: roster seeds from the
starting kit and survives an equip/unequip round-trip) — 416/416 (412 + 4). Verified live via
RB_SMOKE `loadout`/`equipment`: GEAR tab renders 3 correctly-labeled EQUIPPED rows, no crash.log.
**`ui_gate.py` isolated per anti_block protocol** (`git stash` this fix → rebuild → re-run → near-
identical failure, e.g. equipment 70.7%→70.8%, everything else byte-for-byte the same, including
regressions on screens this fix never touches like campaignmap/citymap/merchant → `git stash pop`
restored) — confirmed the SAME pre-existing baseline-drift gate failure already logged under Task #2/
#3 above, not caused by this change. Parked, not blocking.

**2. ⇒ RE-DIAGNOSED (2026-07-06, loop) — not an engine bug, re-routed to CD as B24.** Re-checked
`ListLayout` against the original report: it does NOT ignore the authored `cols:2` hint out of
neglect — `ListLayoutTests.GridCapacityHonestlyReportsAOnePixelColumnShortfall` (landed in Task #2,
`99238ed`) is a DELIBERATE pin: `GridCapacity`/`Cells` compute column count from the region's real
width rather than trusting `cols`, specifically so a card can never render 1px past its container
edge. That decision is sound and stays. The only real defect is the container itself:
`invItems.size[0] = 403` is exactly 1px short of the `199×2+6 = 404` two columns need — pure manifest
geometry, same shape as the earlier `waresShelves` off-by-one, but this time in CD-owned
`layout.json` (regenerated externally per CLAUDE.md — not ours to hand-patch). Logged as **B24** in
`outputs/CLAUDE_DESIGN_issues.md` (widen `invItems.size[0]` to ≥404; renumbered from a stale B22 —
that slot collided with the unrelated merchant-sale-card-art ask already there); no engine change
needed once it lands. Parked on CD, not blocking.

**3. ⇒ RE-DIAGNOSED (2026-07-06, loop) — not an engine bug, re-routed to CD as B23.** Checked the
"will clip or run outside the button" claim against `TextPxWrapped` (`Game1.Canvas.cs:138`) directly:
a single-line label that overflows its rect already auto-shrinks to fit width instead of clipping or
spilling into neighboring chrome — confirmed working as intended, no engine gap. So "TECHNIQUES"/
"MINIONS" don't clip, they just render smaller than "GEAR", which reads as mislabeled/uneven. That,
plus the 128px-of-320px tab row leaving ~290px dead (no stretch/distribute concept exists anywhere in
the manifest schema — `Item`/`Template` have no such field, so this isn't a missing engine feature
either), is pure `layout.json` geometry: `invTab` template size, its item `size` on `invTabs`
(`layout.json:6394`/`10994`), and the label rect width. Logged as **B23** in
`outputs/CLAUDE_DESIGN_issues.md` (widen tabs to fill the container evenly, widen the label rect so
longer labels don't need heavy shrinking); no engine change needed once it lands. Parked on CD, not
blocking.

**4. ✅ FIXED (2026-07-06, loop).** Same root cause as originally reported here, but the real fix ended
up being the full DESIGN_SPEC-locked **4-zone** build (see item #1 at the top of this file, which this
same change also closes) rather than a 3-way approximation — the first pass through this cycle built a
3-way version before the 4-zone lock in item #1 was noticed mid-cycle; that intermediate 3-way state was
never committed.

Equipment reservation was visually present but easy to miss — not fully absent. `attrBar`
(`layout.json:10901`, bound to `"attrs"` on the Equipment screen, `layout.json:6321`) DOES read live
`Body` data and DOES render a pip strip (`attrs.cells`/`attrPip`) — but the pip fill logic
(`Game1.ManifestRenderer.cs:901-905`) only encoded TWO states: pip index `< Available` renders
colored/filled (free), everything else (gear reservation, technique reservation, AND any capacity lost
to damage, all three collapsed together) rendered as the same plain `"slot"` token. Also a separate
mislabel: the numeric readout binds `attrs.alloc` → `Item3` (actually `Available`, the FREE count) and
`attrs.available` → `Item4` (actually `Capacity`, the TOTAL) — swapped from what they actually carried.

**Fix.** `Body.cs`: exposed `TechReserved(stat)` publicly (was private) and added `GearReserved(stat)`
(`DisabledGear(stat).EnabledTotal`) so gear and technique reservation size their own zones instead of
only being visible as the combined `Reserved()` total; added `Damaged(stat)` — capacity lost to injury,
the gap between each part's undamaged `Capacity` and its live `Contribution`. `Game1.ManifestRenderer.cs`:
replaced the old 5-arity attr-row tuple with a proper `AttrRow(Key, Part, GearReserved, TechReserved,
Capacity, Damaged, Token)` record used by `AttrBars()`, the `attrs.cells`/`attrs.pip` dispatch,
`PoolCells`, and the bind-resolution switch. `PoolCells` now emits `Capacity + Damaged` cells total (bar
authored to MAX/undamaged capacity, so a damaged stat keeps its full width instead of shrinking) across 4
zones left→right, matching DESIGN_SPEC's lock exactly: gear-reserved → hashed `pip_reserved_<attr>`,
technique-reserved → solid non-hashed `pip_empty` (repurposed — no dedicated "tech" art exists, confirmed
by viewing all 4 PNGs directly: `pip_full`/`pip_reserved`/`pip_damage` are unambiguous, `pip_empty` is the
only remaining plain/solid look and reads correctly as "spent, but not permanently"), free → stat-tinted
`pip_full_<attr>`, damaged → hashed `pip_damage` (different tone from `pip_reserved`'s hash, already on
disk, never wired before this fix — zero new art). The dead (unreferenced in current `layout.json`)
`attrs.pip` solid-color-fill switch branch got matching zone math for parity, though it can't represent
hash-texture distinctions since it fills flat rects, not sprites. Fixed the `attrs.alloc`/`attrs.available`
swap by reading bind names by MEANING (added `attrs.gearReserved`/`attrs.techReserved` binds too). 2 new
headless tests (`BodyTests`: `DamagedIsZeroWhenNothingIsHurt`, `DamagedReflectsExactCapacityLostAndClears
OnRepair`) — 418/418. Both `Roguebane.Core` and `Roguebane.Game` build clean.

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

## ✅ TASK #3 COMPLETE (2026-07-06, loop) — pixel-perfection systemic-cause pass, 412/412
Five-part directive off the approved plan's Task #3: harden a fragile constant, root-cause two
FontPx-bypass literals feeding the "~4.4x ink-height mystery," sanity-check the recurring
`shift=(-3,-3)px` gate signature, wire `probes.py`/`geometry_diff.py` into the gate as report-only, and
write an aspect-fill design doc only if 2–3 actually surfaced a need for it (they didn't).

1. **`FullBarIds` hardened** (`Game1.ManifestRenderer.cs:52-53`) — new
   `LayoutManifestTests.FullBarStretchElementsExistInEveryScreenThatDeclaresThem` asserts `statusStrip`/
   `footer` exist as real elements in every screen that declares them, parsed from the actual on-disk
   `layout.json` (contract-level, no CD content pinned). 412/412 (411 + 1).

2. **Both FontPx-bypass literals resolved — different verdicts, NOT the same bug:**
   - `260,263-264` skinned-button-label hardcoded `7.0` → **FIXED**, now `e.FontPx ?? 7.0` (real
     per-button authored size; `7.0` kept only as the no-data fallback). Before/after gate diff: byte-
     identical everywhere except campaignmap's `equipmentBtn` fidelity 51.0%→52.2%. Does not clear the
     pre-existing button-label overflow (a separate box-vs-string-length issue) — not this literal's fault.
   - `955-959` `core.coreEffect`-binds eyebrow hardcoded `4.5` → **CONFIRMED correct, left as-is.** This
     branch draws ONLY the flattened "CORE EFFECT" eyebrow label (one string); the template's
     `coreCard.parts` entry for `binds:"core.coreEffect"` authors `fontPx:8, font:"display"` — CD
     flattened the wrong style onto it during extraction, exactly what the existing WHY comment claims.
     The real fix is a CD re-extraction, not an engine change; forcing `pp.FontPx` here would draw the
     eyebrow at the WRONG (8px display) size instead of the current, correct 4.5px mono.
   - **The actual collisions — `equipment: coreEffectLabelxcoreEffectName`, `coreEffectNamexcoreEffectDesc`
     (and NewGame's `previewNamexpreviewRole`, `previewCoreEffectLabelxpreviewCoreEffectName`,
     `previewCoreEffectNamexpreviewCoreEffectDesc`) — are a MANIFEST LAYOUT overlap, not a font-size bug.**
     Confirmed unchanged before/after the button fix. Three stacked text elements (label/name/desc) in the
     coreEffect identity block don't leave enough vertical room for each other at their authored
     sizes/positions. **This answers the Needs-Doug eyeball call below: P0-manifest-reflow, not a global
     font bug** — Needs-CD to re-space `coreEffectLabel`/`coreEffectName`/`coreEffectDesc` (and the
     NewGame preview mirrors).
   - Incidental, NOT part of either literal: the new `--probes` pass (item 4) flagged `coreEffectDesc`
     (equipment) at 3.20x "oversized" ink-height (drawn 16.0px / authored 5.0px) and
     `previewCoreEffectDesc` (newgame) at 1.50x (9.0px/6.0px). Checked `layout.json`: `coreEffectDesc` is
     `size:[124,16]` at `fontPx:5` mono holding a full sentence ("Every attribute cost you pay is reduced
     by 1.") — too wide for one line at that box width, wraps ~2 lines; 2 lines x ~8px design line-height
     = 16px ink height / 5px single-line authored fontPx = 3.2x, an exact match. **Very likely
     `probes.py` measuring a multi-line wrapped block's total ink height against a single-line-implied
     fontPx expectation, not a rendering defect** — the tool has no concept of wrapping. Caveat only, NOT
     logged as a bug; `previewCoreEffectDesc`'s smaller 1.5x (wider 198px box, little/no wrap) doesn't fit
     the same math and is left as an open, non-blocking curiosity — not chased further, out of this
     directive's scope.

3. **`shift=(-3,-3)px` signature: a tooling tie-break artifact, not a renderer bug.** Traced 3 simple
   screen-anchored elements (`suppliesTitle`, `castlePanelTitle`, `partLineChest`) by hand: anchor+offset
   -> `ScreenLayout.Resolve` output -> gate measurement vs reference PNG — all exact per-pixel matches at
   their resolved position. Root cause is `fidelity_diff.py`'s alignment search: it tries `dx=-3,dy=-3`
   FIRST and only updates `best` on strict `>`, so any element that scores `0.0` at every one of the 49
   candidate shifts (content mismatch, not offset) spuriously reports `shift=(-3,-3)` by tie-break — not
   because anything is actually offset. Confirmed: every recurring `(-3,-3)` entry in the gate's
   worst-lists (`autoAttackBtn`, `foeAimTags`, `heroShieldPips`, `heroShieldRegenFill`,
   `partLineChest/Head/Legs`, `runeBagTitle`, `runeBudgetFill`, ...) sits at 0.0% fidelity — exactly the
   class this artifact produces. Combined with stale v6-era reference PNGs (content changed under every
   screen per the Task #2 note above), this fully explains the pattern. **No engine offset bug. Do not
   chase this further; `tools/ui_baseline.json` was NOT touched.**

4. **`--probes` wired into `tools/ui_gate.py` (report-only, non-gating).** New optional flag runs
   `probes.py` + `geometry_diff.py` against the SAME shots/sidecars the gate run already produced (zero
   extra builds/smokes) and prints them under `-- REPORT ONLY (non-gating): ... --` banners inline per
   screen. Neither script's output touches `failures` or the exit code — verified by a live
   `python tools/ui_gate.py --probes` run: exit code identical (1, the same pre-existing baseline-drift
   failures logged under Task #2 above, unrelated to this wiring) with vs without `--probes`.

5. **Aspect-fill/stretch design doc: explicitly SKIPPED.** Neither #2 nor #3 surfaced a concrete
   aspect-fill gap — #2's issue is manifest spacing (Needs-CD), #3 is a diffing-tool artifact. Nothing to
   design yet; not writing a speculative doc.

Build/tests green at commit time: `dotnet build` clean, `dotnet test Roguebane.Core.Tests` 412/412.

## ✅ TASK #4 COMPLETE (2026-07-06, loop) — retrospective + indexed loop protocol docs
Docs only, no code. New `.claude/protocols/` (index + 5 docs, ~240 lines total, none inlined into
`loop.md`'s hot path — `loop.md` grew by ~10 lines of pointers only):
- `INDEX.md` — one table, doc → trigger-condition. `loop.md` and STATUS.md's Pointers both link here
  instead of duplicating content.
- `RETRO.md` — dated retrospective entries (append future ones, don't rewrite). Covers this week's
  Task #1 coupling surprise (slice boundary must match test boundary, not a size target), the 3
  identical bind-gap bugs (gate score ≠ content-correctness proof), the `-3,-3` tooling-artifact
  lesson (sanity-check a diff tool's own tie-break on the degenerate case), and fork-for-verification
  as the top process win (Task #3 forked ≈112min vs Task #2 inline 179min for the same class of work).
- `metrics.csv` + `metrics.md` — greppable `date,task,minutes,lines_changed,estimate_minutes,method`
  log, seeded with real numbers for Tasks #1-3 (340/179/112 min; 840/425/129 lines). `loop.md` step 5
  now appends one row per commit. All 3 seeded slices ran over the 60min target — #1 is one
  deliberately atomic coupled slice (not a slicing failure), #2/#3 show forking the verify phase is
  the lever, per RETRO.md.
- `drift_guardrails.md` — names the EXISTING "measurement is sacred" channel explicitly: gate-relaxing
  changes (baseline `--update`, threshold/mask edits, drive changes that dodge a divergence) go into
  STATUS's Needs Doug tagged **BASELINE-UPDATE-REQUEST**, land only as their own commit after Doug
  responds. No new mechanism invented — this session's 3 real M0 calls already followed this pattern.
- `anti_block.md` — formalizes isolate-then-park: `git stash`/rebuild/re-run to prove a failing gate
  pre-existing BEFORE diagnosing it (this session's `ui_gate.py`-red-everywhere case is the cited
  precedent), commit your own clean slice regardless, log enumerated numbers, keep working the queue.
  Distinguishes PARK (one item, proven, logged) from STOP (every remaining item proven blocked —
  enumerated list required, same bar `loop.md` already set for STARVED/BLOCKED).

This closes Doug's 4-part compound directive (race/core overhaul, UI paging, pixel-perfection,
retrospective+protocols). All 4 landed: `6578c53`, `99238ed`, `3a42287`, this commit.

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
1. ✅ DONE (2026-07-06, loop) — mirrored every new CD-source mgcb entry into `Roguebane.Game/Content/
   Content.mgcb` (the copy builds read); 1846 blocks, build green. See the FIXED banner at top of file.
2. ✅ DONE (2026-07-06, loop) — ran the 3 guards. `layout.json` parses clean. `drop_audit.py` found 4
   genuine, previously-untracked extraction gaps: `encounter` screen's `minionField`/`minionFieldLabel`
   elements + anon bind `encounter.minions.sprite` in `Encounter.dc.html` but absent from the manifest;
   `equipment` screen's `minionCard` template binds `minions.hotkey` in html but manifest template
   doesn't carry it. Likely fallout of the CHUNK A minion-content addition landing without a matching
   re-extraction. Key-set diff (screens/templates) across the last 2 CD-drop commits touching
   `layout.json` found ZERO lost screens/templates — no regression, net-new gap only. `layout.json` is
   CD-owned (never hand-edited by the loop) — flagging for CD, not fixing here.
3. ◐ PARTIAL (2026-07-06, loop) — `WornArmorBinding.SpriteKeys` now takes an optional `theme` (a
   core's own name, e.g. `"barbarian"`) and leads the candidate chain with the THEMED key
   (`sprites/gear/worn/<race>/<slot>/<core>/<type>_<tier>_<cond>.png`, confirmed present in the
   mgcb mirror for all 7 cores × all 5 races on every slot they grow gear in) ahead of the existing
   generic/bare rungs; omitting `theme` keeps the old generic-only chain byte-identical (back-compat,
   no caller migration forced). Race domain was ALREADY generic (race is just a string param, no
   hardcoded list) — confirmed with a themed-chain test parametrized over all 5 races
   (`dwarf`/`elf`/`half_giant`/`halfling`/`human`). 9 new tests in `WornArmorBindingTests.cs`, 438/438
   green. **Still open**: the live `RB_SMOKE=1 RB_MF=all` figure/asset probe this bullet also asks for
   can't run meaningfully yet — `SpriteKeys` isn't wired into the Game-side draw path at all (Debt
   below, "Worn-armor DRAW wiring" / CD_CLOSED_ITEMS #32), so no `theme` argument is ever passed live.
   That wiring is the actual gate for a real smoke pass; this cycle only widened what the resolver CAN
   return once it's called.
4. DoD: build green, probes 0 missing (bow/shield known gaps exempt), Core.Tests green.

### CHUNK C — SCREENS: selection, accents, per-core pixel lanes (after A+B; render only what the manifest authors)
1. ✅ DONE (2026-07-06, loop) — **Roster stamping.** MEASURE done headlessly: `coreCards` (476x404
   grid, 152x395 cells, gap 10) fits exactly 3/page (`GridCapacityMultipliesColsByRowsForNewGamesCoreGrid`,
   already pinned) — 7 cores overflow 3, so `CorePager` (page size derived live from
   `ManifestGridCapacity("newgame","cores")`, `Game1.ManifestRenderer.cs:1232`) was already wired in an
   earlier cycle (`Game1.cs:266-274,838-843`, Task #2) — the GENERIC pager primitive this bullet asked
   for already exists (`Pager.cs`) and is exactly what's in use, no new one needed. `raceCards` (209x423
   vertical, 79-tall cells, gap 7) seats all 5 races exactly (423 = 5*79+4*7) — no pager needed there,
   but nothing pinned that fact, so a future CD panel-shrink could silently drop a race card with no
   test catching it. Added `RaceCardsSeatAllFiveRacesWithNoSilentDrop` to close that gap. 442/442 green.
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
- **Engine: recursive `parent`-box resolution — ✅ DONE, this bullet was stale (2026-07-06, loop).**
  This was tracked here as a drop PRE-REQ, but it already SHIPPED — see the "✅ FIXED (2026-07-05, loop) —
  `"parent"` in layout.json now resolves" banner earlier in this file (`b6812bf`): `Element.Parent` and
  `ScreenLayout`'s recursive parent-chain resolve are both live (confirmed on disk: `LayoutManifest.cs:88`,
  `ScreenLayout.cs:13,30,33`). Leaving the old wording in place risked a future pass re-implementing
  already-shipped work. CD dev-memory #35/B21 (the drop that USES this) is otherwise unaffected.
  Two items from this bullet's old parenthetical are still genuinely open, not tracked elsewhere — kept
  here so they aren't silently dropped: **#30 glow/pulse** (no engine primitive yet, not started) and
  **#32 worn-draw composition**, which IS tracked, see Debt below ("Worn-armor DRAW wiring").
- **Merchant pager doesn't indicate page 2** — ROOT-CAUSED (2026-07-06): Pager/bind/click code is all
  correct (verified by hand + a live `RB_SMOKE=1 RB_SCREEN=citymap RB_MF=merchant RB_SHOT=...`
  screenshot at node "b": PAGE 1/1, 3 sections, no arrows — exactly right for `PageCount==1`). The real
  cause: `Expedition.Seed(nodeId)` is a pure function of the node id STRING with no leg-index/campaign
  salt, and `Maps.StandardLegNodes()` reuses the literal id `"b"` for the merchant on EVERY leg — so
  node "b" rolls the identical stock (weapons/armor/minions, never techniques/runes) every visit,
  every leg, every run. `SectionsPerPage` is 3, so page 2 is mechanically unreachable in live play —
  not a broken indicator. Pinned by `ExpeditionTests.MerchantNodeBNeverRollsMoreThanThreeSections` +
  `MerchantNodeBRollsIdenticalStockAcrossIndependentLegs`. **Needs Human**: should node/battle seeds
  fold in leg index (or another per-leg salt) for run-to-run variety? Same underlying gap as the
  Merchant Wares backlog's blocked map-tier signal above — no per-leg identity exists anywhere yet.
  Don't re-diagnose B13/B14 (unrelated, both already root-caused CD-side).
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
- **Baseline re-pin eyeball** (after A+C): one approved `--update`. The ink-box overflow/collide
  triage this bullet used to defer is DONE (see the TASK #3 banner above) — verdict is
  **P0-manifest-reflow, not a font bug**: `coreEffectLabel`/`coreEffectName`/`coreEffectDesc` (Equipment
  + the NewGame preview mirror) physically don't fit their authored vertical space. Needs-CD to re-space
  the coreEffect identity block; the `--update` eyeball itself still just waits on A+C.
- Standing design calls parked earlier: the free minion re-enable primitive (§8/§9 FTL-parity lock's
  second half — re-arm SCOPE itself is now locked, see DESIGN_SPEC §7); mid-run rune-bag Climb → Body
  reapply path (§11/§12); worn-armor DRAW composition (§17 #15) — B2-GO themed-half draw waits on it.
- **Conclave keystone (`Paths.BoundConclave`) grants no minion** (2026-07-06, loop): it used to hand
  out the now-deleted `Minions.Shade`. Needs a real replacement minion or a redesigned reward — the
  loop won't guess which.

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
- **⇒ FIXED (2026-07-06, loop):** `Kit.Count`-vs-`ActionSlots` mismatch, all 4 sites in
  `Game1.ManifestRenderer.cs` (`preview.techniques` ~L555, `loadout.slotLabel` ~L581, the technique
  `InvCardState` lock threshold ~L1005, `core.stats`'s `"actions"` row ~L1074) now read
  `CoreRune.ActionSlots` instead of `Kit.Count`. Audited the click path first (`UpdateBarDrag` in
  `Game1.cs` calls `toggle()` unconditionally — the "locked" chrome was cosmetic-only, no real
  functional cap existed anywhere in Core), so this was a display-correctness fix, not an unblock:
  5/7 cores (all but Summoner/Barbarian, both `ActionSlots == Kit.Count`) were showing/locking one
  slot short of the real action-bar capacity. Added
  `BuildSessionTests.EveryCoreRunesActionSlotsCoverItsOwnStartingKit` (Core.Tests) pinning the data
  invariant these reads depend on (`ActionSlots >= Kit.Count` for every roster core) — 422/422 green;
  `Roguebane.Game` clean rebuild (`--no-incremental`) 0 errors.
- Barbarian's CORE EFFECT card text visually cuts off (Warlord's Might string vs box size, likely) —
  seen in a screenshot pass, not yet root-caused. Cosmetic, one card, not blocking.
- **⇒ FIXED (2026-07-06, loop):** multi-word content ids showed with a literal underscore on cards —
  `"iron_golem"` rendered as `"Iron_golem"` (caught live in a merchant-stall smoke screenshot this
  session; `"aimed_shot"`'s technique-facing DisplayName reads had the same defect). Root cause:
  `Game1.ManifestRenderer.DisplayName(id)` only capitalised the id's first character, never touched
  underscores. Fixed to split on `_`, capitalise each word, and rejoin with spaces — `"Iron Golem"`,
  `"Aimed Shot"`. Verified via clean `dotnet build Roguebane.Game --no-incremental` (0 errors) + a
  `RB_SMOKE=1 RB_SCREEN=citymap RB_MF=merchant RB_SHOT=...` screenshot confirming the Iron Golem ware
  card now reads correctly. Game-side only (no Game.Tests project exists) — Core.Tests unaffected,
  441/441 green.

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
- Loop process detail (gate-approval channel, park-vs-stop, metrics, retro): `.claude/protocols/INDEX.md`
  — not hot-path, read on trigger only. `loop.md` stays the terse per-run checklist.
- Shipped history/rationale: `git log`. This file = current state only.

## Backlog (not prioritized; don't pull ahead)
- String/i18n externalization posture (route strings through content/binds; no hardcoding growth).
- Race↔core restriction matrix (POC allows all 35). Campaign graph model (§12 Layer 1) + city roster.
