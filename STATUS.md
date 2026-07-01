# Status

## ⇒ AT A GLANCE (updated 2026-07-01) — read this first; detail is in the sections below
State: build GREEN (273 Core tests); all four screens smoke-clean; POC loop plays NewGame→Equipment→
Redeploy→fight→merchant. The self-contained, spec'd, Core-testable directive backlog is DONE.

SHIPPED this arc (detail inline below): shell rename; SINGLE-FOE canon + `Foes`/`CurrentTarget` shim
dropped; Charge = shield-pierce (#1-3,#5 audit); §8 DAMAGE model (part+HP together, no overkill/block)
+ dead-code sweep; foe SYMMETRY locked; global AUTO; **RACE split end-to-end** (Race type→cores carry no
attrs→Forge/BuildSession assemble from Race→CycleRace selection→NewGame race chips→HP-from-race);
fidelity ENGINE set (drop-shadow + gradient + 1080-class fonts via 2× supersample); rune-ladder test
retired; merchant per-HP heal price; comment hygiene.

OPEN — needs a HUMAN decision before I build (do NOT guess):
- **#4 Equipment between-fights MUTATION model** — read-side data exists (Expedition.Player/Equipment/
  Minions/Gold; gear-equip already works on the CityMap). Undecided: what a dedicated between-fights
  Equipment SCREEN may CHANGE (re-slot techniques? equip stash? rune-bag?). Unblocks the #3 button.
- **CASTLE thesis + ENEMY HEAL live-wiring** — mechanism proven (FoeSymmetryTests); the last off-model
  bit is `Encounter.BossRestoreTick` (free HP tick). Removing it needs the castle to gain PARTS + a
  RE-TUNED DPS-race (BalanceSimTests assert glass-loses / AllSix-wins). Balance numbers are yours.
- **PLATE armour role** — inert since §8 (flagged in Shops.cs): give it a role (shield source?) or retire.

CD LANDING (2026-07-01, mid-loop external drop — now committed): a big Claude-Design payload arrived —
`ui/frame/` NINE-SLICE assets, `sprites/gear/bow.png`, `sprites/body/{human,elf}_ranger/` figures,
`icons/rune/core_*.png`, refreshed backgrounds + button skins, new `design/01-05*.png`, and a new
`layout.json` carrying **gradient `fill` OBJECTS**. Reconciled: LayoutManifest now parses `fill` as a
string-or-gradient union (`Fill` + `FillConverter`); ListLayout falls back to the TEMPLATE size when a
terse item omits its own `size` (CD's `buildMinions`); manifest schema-tests relaxed to the resolvable
contract. 274 Core green; all screens smoke-clean with the new assets.

STILL BLOCKED / TODO on the Claude Design side:
- NINE-SLICE FRAMES DONE (§10 fidelity now COMPLETE: shadow + gradient + 1080 fonts + frames): added the
  two `ui/frame/{panel,card}` entries to the game-side mgcb, an `AssetRegistry.Frame` loader, and
  `DrawFrame` (blits `NineSlice.Patches` Src->Dst). `Panel()` uses the carved panel frame for LARGE panels
  (w>=220 & h>=170 — its 60px corners need room) and keeps the gradient chrome for small cards + thin bars.
  Screenshot-verified on build/combat. TODO next: apply the smaller `card` frame to cards via the manifest
  per-element `frame` (Panel is hand-called, not manifest-driven yet).
- RANGER FIGURE + BOW mgcb SYNC DONE + BOW RENDER CONFIRMED: added the `human_ranger`/`elf_ranger` body
  sprites (42 entries) and `sprites/gear/bow` to the GAME-side mgcb (they landed in CD's Roguebane.Content
  but the game builds its own mgcb). The Ranger figure renders (green archer, RB_CHASSIS=5) and the BOW
  mounts correctly at the human_ranger handR socket (gear.bow pivot [36,132]) — confirmed via a red-tint
  diagnostic: the bow draws as a tall sprite on the figure's right arm. It blended into the body at first,
  so added a SPRITE-SHAPED DROP SHADOW under mounted weapons (the weapon's own alpha, offset + darkened,
  drawn under it) — now the bow (and any wielded weapon) reads against the figure. Screenshot-confirmed.
- MGCB DIVERGENCE now has a GUARD: `tools/check_mgcb_coverage.py` reports any Roguebane.Content png NOT
  built by the game-side mgcb (run after a CD asset drop; exit 1 lists gaps). Ran it -> synced the 8
  orphans it found: the 6 `icons/rune/core_*` glyphs (now DRAWN in the Equipment CURRENT CORE panel) + the
  2 `icons/map/enemy_host*` war-party icons (pending a map feature). Guard now GREEN. Real fix (a single
  mgcb source of truth) still wants a CD/human call, but the guard stops silent blank-asset regressions.
- Still Game TODO: a `shot` technique icon (none exists — falls to `swing`/crossed-swords; CD asset need);
  consume the manifest `fill:{gradient}` + a per-element `shadow` field; wire the enemy_host war-party icon.
- Manifest screen-id renames (newrun/build/runmap→newgame/equipment/citymap); per-PART `binds`
  (manifest-drive arc blocker); the TWO divergent `Content.mgcb` still need a single source of truth.

DONE this loop: between-fights **Equipment view** (#3/#4 flow slice) — EQUIPMENT [E] button on the
CityMap opens a LOADOUT overlay reading the RUN state (figure, HP/gold, attrs, minions) with technique
RE-SLOTTING via the existing Toggle; mid-run gear/rune mutation still deferred (design-open). Fixed a
pre-existing `map` smoke bug (missing Redeploy left it stuck at a cleared fight).

## ⇒ HUMAN DIRECTIVES — 2026-06-30 (do these FIRST; they revise shipped work and WIN)
Rationale now in canon: DESIGN_SPEC (damage/symmetry/heal/flow/nomenclature §8/§10/§12/§13) +
LAYOUT_CONTRACT §10-11 (fidelity primitives + 1080). Priority order:

1. **RENAME everywhere** (Screen enum + titles + labels; keep in sync with `layout.json` ids — Claude
   Design renames the manifest side in parallel): NewRun→**NewGame**, Build→**Equipment**,
   RunMap→**CityMap**, Campaign→**CampaignMap**, Combat→**Encounter**; Flee→**Retreat**, March→**Redeploy**.
   Also make FIGURE keys uniform **`human_<core>`** (like `elf_<core>`) — retire bare `<core>`, no
   "unprefixed = human" special case. Do it ADDITIVELY (add `human_` PNGs/keys, repoint FigureId refs,
   then drop the bare ones); coordinate with Claude Design (asset + manifest side).
   [DONE for the shell: Screen enum {NewGame, Equipment, Run}; screen Draw/Update methods + rects renamed
   (DrawEncounterScreen/DrawCityMapScreen/DrawNewGameScreen/DrawEquipmentScreen, RetreatRect/RedeployRect);
   display titles/verbs EQUIPMENT / REDEPLOY / BEGIN / RETREAT; smoke-verified. Core API (BuildSession.
   March, CampaignState.Marching, Expedition.Flee) left as-is. Manifest LOOKUP ids still "newrun"/"build"/
   "runmap" — flip to new ids WHEN Claude Design renames the manifest. RB_SCREEN dev values unchanged.]
2. **EVERY SCREEN off the manifest.** You did New Run then wandered to shield work — systematically render
   ALL screens (Equipment, CityMap, CampaignMap especially), ONE per pass, verified vs its design PNG,
   BEFORE new gameplay features.
3. **FLOW.** Encounter must NOT auto-return to the map — add an explicit **Redeploy** transition.
   **Retreat** = only during an active fight (bail it); **Redeploy** = only out of combat (advance).
   **Equipment reachable between fights** — a button on Encounter (enabled only when NOT fighting) + on
   CityMap + CampaignMap (generic button OK). Redeploy timing/DEX-lockout is DESIGN-SPEC'd but NOT built
   yet — **flow only** now.
   [REDEPLOY TRANSITION DONE: new ExpeditionState.Cleared — a won fight banks its hold then HOLDS at
   Cleared (no silent auto-return); Expedition.Redeploy() (Cleared->Choosing) + Campaign.Redeploy();
   tested (can't jump until redeployed). Shell keeps the Encounter view on Cleared with a "NODE CLEARED /
   REDEPLOY" overlay (SPACE or click). Retreat (Flee) still only during a fight. STILL TODO: the
   Equipment-reachable-between-fights button — coupled to #4 (Equipment must become a between-fights
   screen reading the RUN state, not the pre-run BuildSession); do it with the #4 redo.]
4. **EQUIPMENT screen** (was Build): full redo off `screens.equipment` (design/02). **REMOVE the
   rune-ladder TEST** ("Q vessel / W resonance") — throwaway rune-test, retire it. Between-fights screen,
   not a post-NewGame gate.
   [RUNE-LADDER TEST REMOVED: dropped the on-screen ladder UI from the Equipment screen — PathKeys (Q/W)
   input, the ladder click-to-climb, DrawLadders + LadderRowRect, and the "click a rune rung" hint. The
   Core rune economy (BuildSession.Paths/Climb, RuneLoadout, Marks) is UNTOUCHED — only the placeholder
   screen harness went; the real rune-bag cards (design/02) are the deferred replacement. Screen still
   shows figure/attrs/CURRENT-CORE/techniques/action-bar/bays; build+newgame+combat smoke clean, 272 Core
   green. STILL TODO for #4: make Equipment read the RUN state (not pre-run BuildSession) as a between-
   fights screen + the design/02 rune-bag + inventory tabs — unlocks the #3 Equipment-reachable button.]
5. **FIDELITY** (LAYOUT_CONTRACT §10-11): add renderer support for `shadow` (engine-drawn), `frame`/
   nineSlice (blit CD frame assets), gradient fill; build SpriteFonts at **1080-class** density. Frame
   ASSETS come from Claude Design (blocked until delivered — do shadow/gradient + fonts meanwhile).
   [SHADOW DONE: engine `DrawShadow(x,y,w,h,dx,dy,blur,opacity)` — the silhouette offset by (dx,dy),
   softened by `blur` concentric decaying-alpha rings, UNDER the element (no baked art, resolution-
   independent per §10). Applied a subtle default in the `Panel()` helper (dx2/dy3/blur3/op.40) so all
   chrome + cards gain depth; screenshot-verified across build/combat/newgame/citymap (reads as depth,
   not muddy). Game-only.]
   [GRADIENT DONE: engine `DrawGradient(x,y,w,h,from,to,dir)` — interpolates `from`->`to` in 1px strips
   (PointClamp sampler rules out a stretched-texture lerp; vertical/horizontal, diagonal deferred).
   Panel() now fades PanelTop (a touch lighter) -> PanelBot (Panel0) vertically under the border, so the
   flat chrome reads as soft-lit depth alongside the shadow. Screenshot-verified. Reusable for a manifest
   `fill:{type:gradient}` field when CD authors one.]
   [1080 FONTS DONE (§11 density): the scene RenderTarget is now SS=2x design (1920x1080), painted via a
   `Matrix.CreateScale(SS)` on the scene SpriteBatch.Begin — design COORDS unchanged (960x540). Fonts
   rebuilt at 2x (mono 14->28, display 20->40); Text() draws at 1/SS so on-screen size matches design
   while glyphs rasterize at 1080 density (crisp). All 7 MeasureString sites route through a single
   `MeasureText` helper (/SS) so centring/wrapping stay in design space. INPUT UNAFFECTED — the
   mouse->design transform derives from the W,H design constants + _viewScale, decoupled from the scene
   resolution (verified). Smoke receipts now 1920x1080; screenshot-verified crisp text + intact layout on
   build + combat. Game-only. STILL TODO: per-element manifest `shadow`/`fill` fields. FRAMES blocked on
   CD assets. -> the §10-11 ENGINE fidelity work (shadow + gradient + 1080 fonts) is now COMPLETE.]
6. **BUG BATCH:**
   - CityMap supplies label "spent per jump" → **"per deployment"** (drop the FTL wording). [DONE]
   - CityMap movement **ANY-DIRECTION**: any move (incl. back to a merchant) costs 1 supply AND advances
     the war party — remove any forward-only restriction. [DONE: RunMap edges are now traversable both
     ways (undirected Adjacent()); MoveTo still spends a supply + advances the war party; tested.]
   - Merchant = **HP HEALING only** (gold → HP at 1 HP per randomized cost, loot-bounded). No potion
     purchases (potions are heal-body-part techniques). [DONE (removal): the whole potion ITEM economy is
     gone — Stash.Potions/AddPotion/TryUsePotion, Expedition.BuyPotion/UsePotion/Potions, the merchant
     P/U buttons + the potion readout; merchant keeps BuyHeal (H, gold->HP). Tests reconciled (279 Core).
     TUNING TODO: the "1 HP per randomized cost, loot-bounded" incremental buy — BuyHeal is still flat
     gold->full-HP at cost 3; needs a per-HP randomized price + a run rng.]
     [BUYHEAL TUNED (§10): merchant now charges PER HP. `HealPricePerHp` = 1..2 gold/HP, rolled from the
     merchant NODE seed (reuses the deterministic `Seed(nodeId)` + Rng, XOR a heal salt) so it's stable
     per merchant + reproducible — no new rng plumbing. BuyHeal() buys as much HP as the gold affords at
     that price, capped at the missing HP (spends healed*price). Tests: MerchantHpServiceChargesPerHpAnd-
     TopsUp (per-HP charge + top-up), MerchantHealPriceIsStablePerNode. Shell button shows "({price}/hp)".
     273 Core green; citymap smoke clean. Price RANGE (1..2) is placeholder — tune vs spoils (2..10/node)
     in play.]
   - **AUTO-attack is GLOBAL, not per-weapon**: on = a fired weapon re-fires on its next charge at the
     kept target. Fix the per-weapon coupling. [DONE: production AUTO was already the one global toggle
     (Expedition/Campaign.SetAuto -> Caster.SetAutoAll(keepTargets); PlayerTargetingFsmTests.GlobalAuto-
     GovernsEveryModule proves on=keep+refire, off=one-shot-then-clear across every module). The vestigial
     per-weapon coupling -- Caster.SetAuto(Technique,bool) + IsAuto(Technique), used by ONE engine test --
     is removed, so there is no per-technique AUTO surface left. Run.Auto stays as the engine-only cadence
     primitive (unattended foes/minions; always on for the player, whose holding comes from requireAim).
     Stale comments corrected. Removed the redundant CasterFiringTests toggle test. 273 green; Game builds.]
   - **DAMAGE**: every hit applies part damage AND hp damage simultaneously; only a shield block or full
     evade mitigates. Ensure the impl applies both. [DONE (§8 LOCKED): Caster.Hit now does full evade ->
     shield absorb -> then BOTH raw part-erode (Frame.Damage, no plate blunt, no overkill maths) AND HP
     (target.Damage), same power. The flat CON block + plate protection are GONE from the hit path. Brace
     RE-CAST as the CON SHIELD SOURCE (ShieldLayers 4 / regen 15) — the "replace flat block with shields"
     step — so kits keep a mitigation and the campaign stays winnable under the deadlier model (verified:
     CampaignTests win). Tests reconciled: PartAim/FoeOffense/FoeArming -> part+HP-no-overkill; Mitigation
     block-test -> shield-test. 277 Core green; combat smoke clean.]
     [DEAD-CODE SWEPT (follow-up): removed Body.BlockMitigation / Body.AbsorbPartHit / Body.Protection,
     Fighter.DamagePart, Foe.DamagePart, and DamagePart from ICombatTarget (Hit is now the sole damage
     path). Deleted ArmorTests.cs (all plate-protection) + the BodyTests block test; leather-evasion
     coverage stays in MitigationTests. 271 Core green; Game builds. Plate armour now INERT -> flagged
     NEEDS HUMAN in Shops.cs (give plate a role or retire the kind).]
   - **ENEMY HEAL**: must run on a real tuned technique (same system as the player), can't out-tick the
     player's healing — not a fast free regen. [MECHANISM READY: a heal in a foe's Arsenal is run by its
     own offense caster and repairs the foe's PARTS (§10), proven in FoeSymmetryTests. STILL LIVE + off-
     model: Encounter.BossRestoreTick is a free HP tick (Sieges.Castle/ArmedCastle restoreAmount/Every) —
     removing it + moving the castle to a real part-heal reshapes the DPS-race the BALANCE SIM asserts
     (BalanceSimTests: glass build must lose the castle race). NEEDS HUMAN: the castle needs PARTS to heal
     + re-tuned thesis numbers. Not guessed here.]
   - **SYMMETRY**: enemies act through the SAME technique/attribute/shield/heal framework as the player
     (shared sim). Refactor toward this; exceptions few + obvious. [PROVEN + LOCKED: FoeSymmetryTests show
     a foe's own offense caster runs the §10 part-heal AND the §6b shield on the foe body, mid-Battle,
     identical to the player — no code change, the framework already is symmetric. Only remaining
     asymmetry = the castle free-restore tick above (flagged needs-human).]
   - BUG (no clean repro yet): firing after a weapon charges while UNTARGETED misbehaves — watch as the
     targeting FSM is refined.
7. Carry-over review fixes (already tracked below): coreCard per-core figure bind, add `✦ ◉ ✓` glyphs,
   normalize element `type`. (#3 truncated subtitles still parked.)

**RESOLVED 2026-06-30 — SINGLE-FOE is canon.** Human picked single enemy (body-part aim is already
enough focus; the FTL lesson). DESIGN_SPEC §13 reverted to single-foe; §8/§18 already said single.
DIRECTIVE: revert the shipped MULTI-foe Encounter to **SINGLE-foe** (design/01 layout — one structured,
possibly multi-part enemy; the prominent bottom attribute pool returns). Do NOT maintain a multi-foe
branch; KEEP the multi-foe capability latent ONLY if it stays neat (else drop it).
[CORE MODEL DONE: Encounter now holds ONE `Foe Enemy` (multi-foe list dropped); `Foes`/`CurrentTarget`
kept as a thin 1-element compat surface so Battle/shell/drivers are untouched. Sieges folds each old
multi-layer encounter into ONE tankier foe (skirmish = one raider; castle = one hp-40 restoring boss).
Tests reconciled (SupportTests/RunSiegeTests/SiegeFigureTests dropped front-rotation/variety asserts;
FoeOffense/PartAim Solo helpers single-foe). 282 Core green. SHELL design/01 single-foe LAYOUT DONE:
DrawFoe draws the ONE enemy LARGE on the right (name tag + HP + limb bands); the prominent bottom-left
ATTRIBUTE POOL panel is back (pips moved out of the YOU panel); the ACTION BAR moved bottom-right with
adaptive card pitch (ActionCardRect fits N cards beside the pool, before the AUTO/PAUSE/RETREAT verbs).
Smoke-verified vs design/01. Multi-foe capability fully dropped.]

**CHARGE = the shield-pierce resource (2026-06-30).** Human kept + NAMED the resource **Charge** and
REDEFINED it (see DESIGN_SPEC §6b/§10/§14/§17/§18). Directives:
1. **Charge fuels SHIELD-IGNORING techniques only** — drop the old "magic-tier verb costs charge" rule.
   A technique that ignores the shield pool requires + spends Charge per use (dry → holds the pierce). Add
   a `ShieldPiercing`/`IgnoresShield` concept to Technique; wire the Charge spend to THAT, not to "magic."
   Shield-piercing damage bypasses the shield pool entirely.
   [DONE: Technique.ShieldPiercing added; Caster spends Charge (ChargeCost, min 1) ONLY for piercing
   techniques and its Hit skips AbsorbShields when piercing; ChargeDry indicator + heal branch re-tied
   (heals no longer cost charge). Tested (bypass pool, spend/hold-dry/recharge, non-piercing ignores
   charge). NOTE: no LIVE content is piercing yet — bows (#4) are the intended user; the player's granted
   Charge capacity is dormant until a piercing technique/weapon ships.]
2. **Fix `Paths.Maelstrom`** — it carries `ChargeCost:1` as a "magic-tier verb" (old rule). Remove the
   charge cost unless it is actually shield-piercing; drop the "draws the finite charge" comment. [DONE:
   ChargeCost removed (Maelstrom isn't piercing); comment updated.]
3. **`Minion.AltCost` paying Charge** is off-definition (Charge = shield-pierce, not summon fuel). §9's
   alt-cost example is HP — reconcile alt-cost summons to a DESIGNED cost (HP or a stat), not Charge.
   [DONE: Caster.Summon no longer spends Charge for a MinionGate.AltCost summon; comment + tests updated
   (AltCostSummonDoesNotSpendCharge). No alt-cost minion is authored + HP isn't reachable in Caster, so
   the summon is an UN-COSTED placeholder for now — wire the real HP/stat spend when an alt-cost minion
   ships. 278 Core green.]
4. **BOWS (new, shield-ignoring):** spec + implement a bow weapon type (DEX, §6) whose attacks bypass the
   shield pool and cost Charge; add tests. At least ONE starting Core rune ships a bow in its default
   loadout [WHICH core = needs-human; suggest the DEX Reaver or a ranged identity]. Bow ASSET (sprite +
   hand-mount pivot) is a Claude Design need — mark BLOCKED on art, use a placeholder meanwhile.
   [DONE (art landed 2026-07-01): `Armory.Bow` (DEX stat-stick, reserve 2, power 2) + `Armory.Shot`
   (Timered cd3, Consults primary DEX weapon, ShieldPiercing, ChargeCost 1). CoreRune gained a
   `DefaultWeapons` kit wielded in NewBody; the **RANGER** ships the bow (its identity IS the marksman —
   no needs-human guess) with Shot+Brace+Bandage on the bar. This makes the player's Charge pool LIVE (was
   dormant). Tests: BowTests (Shot bypasses shields + spends charge using bow power; Ranger assembles
   wielding the bow). DESIGN_SPEC §6 bow note updated (no longer "don't exist yet"). 276 Core green;
   newgame/combat smoke clean. Numbers placeholder. TODO (Game): mount the bow.png on the figure + a
   `shot` technique icon.]
5. **AUDIT — no undesigned mechanics (new CLAUDE.md + loop guardrail):** sweep code + sample/test content
   for invented mechanics/effects/resources/conditions absent from DESIGN_SPEC. Known: the Charge misuse
   above; the aether-referencing Hollow Vessel effect (spec §11 effect is OPEN — code grants placeholder
   +CON; leave as neutral sample, do NOT invent 'regenerating aether'); the Vessel/Resonance/Tempest/
   Conclave sample NAMES (placeholder — mechanically fine, rename only if cheap). Neutralize/flag, don't
   invent.
   [DONE (sweep): CLEAN — no undesigned mechanics found. `aether` = ZERO refs (design cleaned). Rune
   Grants only socket chassis-extension PARTS (vessel-core +CON, resonant-core +INT) + unlock techniques/
   minions — all DESIGNED (§11); the Hollow-Vessel grant is a NEUTRAL placeholder (+CON part), no
   regenerating-aether. No status/condition system invented (§8 disable/stun/silence are design-only, not
   in code; `PruneSilenced` is a method, not a status). Resources = only Charge(§6b)/HP/stats/shields —
   no mana/rage/etc. Only fix: stale "magic resource/charge" COMMENTS on Caster (Charge is the shield-
   pierce resource now) -> corrected. Vessel/Resonance/Tempest sample NAMES left (mechanically fine).]
   [COMMENT HYGIENE (follow-up): swept comments that still described RETIRED mechanics after this session's
   refactors — the §8 change killed "overkill spill / §10 part-vs-HP split" (Fighter/Foe now say part+HP
   together); the shield revamp killed "the CON block" (Techniques/Caster now say §6b shield source);
   potions are gone (Campaign/Shops); removed the dead `Caster.BlockCap` const (unused since the §8 sweep).
   Comment-only + one dead const; 273 Core green. NOT swept: "chassis" as descriptive prose in comments
   (the TYPE is CoreRune; prose left — lower urgency, larger churn).]
**STRESS TEST — add the RANGER Core rune (2026-06-30).** Human wants to exercise the "content is DATA,
not code" invariant by adding a 6th core. Add **Ranger** as DATA ONLY — a new `Chassrium` entry + append
to `Roster`; NO new classes. **REPORT in STATUS whether anything beyond data was required** — any code or
test-shape ripple is the finding (it tells us how leak-free the extensibility really is).
[DONE. FINDING: adding Ranger required ZERO production-code changes — pure data (Chassrium entry +
Roster append). Loadout uses Lunge+Brace+Bandage (bow not built yet). Ripples: exactly ONE test hardcoded
"five" (RosterHasFiveDistinctChassis -> RosterCoresAreDistinct, count-agnostic); NewGame grid overflows
(sized for 5 cards, the 6th clips off-right) — ACCEPTED by human (redesign coming), not fixed. Verdict:
content-is-DATA holds cleanly; the only leak is a hardcoded count in a test + a fixed-width shell grid.
Figure art human_ranger/elf_ranger still a Claude Design need (placeholder card until it lands).]
- Spec (placeholder stats, tune later): id `ranger`, Archetype "THE MARKSMAN", `StandardBody` str4/int3/
  dex8/con4, RuneBudget 12, RuneDiscount 0, Bays 0. Flavor ~ "Strikes from range with a shield-piercing
  bow; high DEX, thin armour — answers before the wall matters."
- LOADOUT: Ranger's primary is the **bow** (shield-ignoring, Charge — the bow work above). SEQUENCE: land
  the bow first, then Ranger's default loadout = bow Shot + Brace + Bandage. If Ranger is added before the
  bow exists, use existing techniques so it compiles, then swap the bow in.
- Race-gating: allow Human + Elf (both plausible archers); the race↔core matrix is OPEN (§17) — don't
  over-build gating if it isn't wired yet.
- FIGURE ART: Ranger needs a figure sprite (`human_ranger`, `elf_ranger`) — a Claude Design need; NewGame
  will show a fallback/placeholder card until it lands (expected, not a bug).

**TERMINOLOGY RENAME — clean, NO backwards-compat (2026-06-30).** New rule (CLAUDE.md + loop.md): renames
update ALL usages; NO aliases/mapping/compat shims unless a feature flag is explicitly requested. Apply
across the 30-file audit:
- **Chassis → CoreRune** everywhere: record `Chassis`→`CoreRune`, `Chassis.cs`→`CoreRune.cs`,
  `Content/Chassrium.cs`→`Content/CoreRunes.cs`; ~254 refs across ~30 files (Forge/BuildSession/Fighter/
  Expedition/Minion/Mark/RuneLoadout + tests: Chassis{Thesis,Roster,Body}Tests, FigureIdTests,
  RunStartTests, MinionTests…); `layout.json` `chassis/*` figure keys; `Content.mgcb` `sprites/char/
  chassis/` dir + entries; design docs. Figure keys also go `human_<core>` (earlier directive).
  [CODE DONE: `s/Chassis/CoreRune/g` + `Chassrium->CoreRunes` across Core+Game+tests (80 refs); files
  Chassis.cs->CoreRune.cs, Chassrium.cs->CoreRunes.cs, Chassis{Body,Roster,Thesis}Tests->CoreRune*.
  Case-sensitive, so the LOWERCASE content asset keys are PRESERVED (`_assets.CoreRuneFigure` still loads
  `sprites/char/chassis/<id>`; the RB_SMOKE `"chassis/grunt"` probe label unchanged). 278 Core green,
  build/newrun smoke clean. NOT renamed (CD-coordinated content, deferred): the `chassis/*` ASSET dir +
  keys in mgcb/layout.json — flip with CD. `BuildSession.CoreRune`/`CoreRuneIndex`/`CycleCoreRune` etc.
  all renamed. Race axis + Chassrium->Races-split still not built (needs the Race type).]
- **RunMap → CityMap** (`RunMap.cs`→`CityMap.cs`, RunMap*Tests, all refs). [DONE: class RunMap->CityMap,
  RunMapOutcome->CityMapOutcome, files RunMap.cs/RunMapTests/RunMapSupplyTests -> CityMap*; all refs
  (Core + tests) renamed; no Game refs needed it (shell uses Exp.Map). CityMapOutcome.Marching kept (the
  map's in-progress outcome — only CampaignState.Marching became Redeploying). 280 Core green.]
- Finish the Core-API renames the shell pass left as-is (no compat): **Expedition.Flee → Retreat**,
  **BuildSession.March → Redeploy**, **CampaignState.Marching → Redeploying**. [DONE: renamed everywhere
  (Battle/Session/Expedition.Flee -> Retreat; BuildSession.March -> Redeploy; CampaignState.Marching ->
  Redeploying) across Core + Game1 + tests, incl. residual test labels; RunMapOutcome.Marching left
  (that's the RunMap->CityMap rename's scope). 280 Core green, smoke clean.]
- **NewRun residuals → NewGame**; manifest LOOKUP ids `newrun`/`build`/`runmap` → `newgame`/`equipment`/
  `citymap` (game side now; Claude Design renames the manifest ids in sync — CD payload).
- **Drop the compat surfaces** earlier passes kept: the `Foes`/`CurrentTarget` 1-element shim (single-foe
  is canon) and the bare `<core>` figure keys (→ `human_<core>`).
  [FOES/CURRENTTARGET DONE: removed `Encounter.Foes` + `Encounter.CurrentTarget`; callers read `Enemy` (the
  one foe) and gate on `Enemy.Down` for a live target. `Expedition.Foes`/`Campaign.Foes` -> `Enemy` (Foe?,
  null between fights); Battle builds foe-offense from the single `Enemy` + retargets on `Enemy is { Down:
  false }`; Sessions/Forge use `run.Current.Enemy`. Game1's 3 foe-list sites collapsed to the single Enemy
  (FoeIndexOf -> 0/-1). Tests repointed `.Foes[0]`->`.Enemy!`, `Single(.Foes)`/`CurrentTarget`->`.Enemy`,
  `Empty(exp.Foes)`->`Null(exp.Enemy)`. 271 green; combat/citymap smoke clean. FigureKey bare->human_ was
  already handled by the Race rewire's `FigureKey(Race)`.]
- **aether**: confirm ZERO remaining refs (design cleaned).
- VOCAB LOCKED: **Race** (attrs+HP), **CoreRune** (layout, was Chassis), **Loadout** = Race+CoreRune (the
  assembled identity — the freed-up term; retire "Core" as a label), **Equipment** = the installed-things
  layer (weapons/armor/runes/techniques/bays; WAS called "Loadout"), configured on the Equipment screen.
  Apply: `DefaultLoadout` → `DefaultEquipment`; the plain "Loadout"-as-gear term → Equipment; reserve
  "Loadout" for Race+CoreRune (`BuildSession` produces a Loadout — rename as fits). `RuneLoadout` is the
  rune bag — keep Rune-prefixed (or → `RuneBag`), NOT plain Loadout. Keep tests green throughout.
  [DONE (gear-term pass): `DefaultLoadout`->`DefaultEquipment` and the standalone `Loadout` property/field/
  param -> `Equipment` across Core + Game + tests (word-boundary sed, so `RuneLoadout` + the manifest
  `loadoutCard`/`loadoutList` ids + `DrawLoadoutStrip` are untouched). 280 Core green. NOT yet: reserving
  "Loadout" for the Race+CoreRune combo (that type doesn't exist until the CoreRune split); `RuneLoadout`
  kept as-is; `DrawLoadoutStrip` (internal render name) left.]

**ASSETS LANDED — race + New Run (2026-06-30).** Claude Design delivered: modular figures for BOTH races
× all 5 cores (`elf_*`/`human_*`, complete part×state sets), their `figures.*` manifest entries, and the
NewGame manifest now carries a `raceCard` template + `races` bind + a `preview.fig`. UNBLOCK:
- Build the **two-step Race→Core NewGame** off the manifest (raceCard + coreCard) — turn the race path ON
  (retire the single-core-only flag / "race behind the flag"). [UX + race stats + race↔core matrix are
  PENDING HUMAN — being interviewed now; don't invent them.]
- The `human_<core>` rename is now actionable (assets + manifest present): repoint FigureId refs
  bare→`human_`/`elf_`, then RETIRE the bare body dirs + flat `chassis/*` thumbnails (keep mgcb in sync).
- coreCard per-core figure bind (review fix): now doable (figures + `preview.fig` exist).
STILL BLOCKED (NOT landed — the CD issues doc is mid-WIP, only caught up to race+NewRun): fidelity 9-slice
FRAMES; the Ranger core figure + bow asset; the CityMap node framing/shadow fix; interaction states —
keep those gated. (binds: 24 element-level binds present incl. `preview.fig`/`races`; verify per-PART
template binds when wiring the coreCard render.)

**NEW RUN DESIGN LANDED + decisions (2026-06-30).** design/05 is now the SINGLE-screen NewGame: three
columns — **Race** (head sprites + attr/HP card; Human/Elf) | **Core Rune** (rune icon + budget/actions/
bays + APEX card; the 5 cores) | **Loadout** (assembled Race+CoreRune: composed figure + combined stats +
apex) | BEGIN THE RUN. Build NewGame off this design + the manifest (raceCard + coreCard + `preview.fig`).
Decisions:
- **RACE DATA SPLIT:** attrs+HP come from RACE ONLY (cores add none). Extract a `Race` (attrs+HP); the
  CoreRune keeps budget/actions/bays/apex/default-equipment. Placeholder race blocks: Human 3/3/3/3 HP20;
  Elf 2/3/4/2 HP14. (Retires the per-core `StandardBody` attr blocks — attrs are race-only now, §7.)
  [SLICE 1 DONE: `Race` type (Race.cs — attrs + Hp + NewBody laying the standard Head/Chest/Arms x2/Legs
  x2 anatomy) + `Content.Races` (Human 3/3/3/3 HP20, Elf 2/3/4/2 HP14, Roster) + RaceTests. 276 green.
  Isolated keystone data.
  [SLICE 2 DONE (the rewire): CoreRune carries NO attrs — dropped `BodyParts`/`StandardBody` from CoreRune
  + all 6 cores; body now minted from the RACE (`CoreRune.NewBody(Race, RuneLoadout)` = race anatomy +
  rune-grant parts). Forge.Assemble/Embark/EmbarkCampaign take a `Race`; `BuildSession.Race` (default
  Human, settable) threads it; `CoreRune.FigureKey(Race)` => `<race>_<core>` (hardcoded `human_` gone).
  Game core-grid + build previews render off `_build.Race`. Tests: deleted CoreRuneBodyTests + the two
  stat-identity RosterTests cases (core stat-identity is retired); the thesis "never built for it" is now
  a BUDGET gap not a stat gap. 270 Core green; newgame/build/combat smoke clean. The balance sim was
  UNAFFECTED (Sessions.Demo uses a bespoke DemoBody, not a core).
  STILL DEFERRED: (a) HP-from-race — [RESOLVED + DONE, see SLICE 5 below]. (b) Race is not user-selectable
  yet [DONE, slice 4]. (c) figure repoint bare->human_/elf_ + retire bare dirs (CD-coordinated).]
  [SLICE 3 DONE (race selection MODEL): BuildSession now holds the race roster + `RaceIndex`/`RaceCount`/
  `RaceRoster`/`Race` + `CycleRace(dir)` (mirrors the CoreRune cycle). A race swap changes body attrs ONLY
  — core budget + slotted kit untouched (all combos allowed, design/05). Sessions.NewBuild feeds
  Races.Roster; ctor now takes races first. Tested (CyclingRaceSwapsBodyAttrsAndKeepsTheCoreBudget: Elf
  con != Human con, budget unchanged, wraps). 271 green; Game builds.
  [SLICE 4 DONE (race axis ON in the shell): NewGame now has a RACE selector — bottom-left chips
  (HUMAN hp20 / ELF hp14, clear of the manifest core-card grid + BEGIN) + input (Tab cycles, click a chip)
  -> `_build.CycleRace`. Every core card's attr block + composed `<race>_<core>` figure already read
  `_build.Race`, so the pick propagates through the grid and threads into the run (BuildSession.Race ->
  Forge). Smoke + screenshot verified (Human 3/3/3/3, elf assets present). Game-only (no headless test);
  the FULL design/05 three-column Race|Core|Loadout redesign + the manifest `raceCard` template are the
  remaining polish — this is the minimal strip that turns the axis on.]
  [SLICE 5 DONE (HP-from-race): HUMAN DECISION (2026-07-01) — race.Hp is the natural BASE; CON is a bonus
  ON TOP (1 CON = 2 HP). Chest damage shrinks the bonus (HP caps down); a chest HEAL does NOT refund lost
  HP — HP lost in a fight is PERMANENT, restored only by the vendor / post-fight recovery (active heals +
  potions repair PARTS only). The existing Fighter already models exactly this (dynamic MaxHp = base +
  2*CON, CapToMax on damage, no auto-refund on repair — already locked by ConHpTests.RepairingConDoes-
  NotRefundHpAlreadyLost). So the only change: PlayerFighter now takes the Race and uses race.Hp as the
  base (Forge.Assemble/Embark/EmbarkCampaign). Human MaxHp = 20 + 2*3 = 26; Elf = 14 + 2*2 = 18. Test:
  AssembledPlayerHpIsTheRaceBasePlusConBonus. The bespoke DemoBody sim path keeps the baseHp:8 overload.
  272 Core green; Game builds.]
- **ALL race×core combos allowed** (no gating this pass).
- **Retire the bare asset set:** compose NewGame/Equipment/Loadout figures from the MODULAR race parts
  (`preview.fig`); drop the bare body dirs + flat `chassis/*` thumbnails (mgcb in sync).
- Apex effect TEXT (Hollow Vessel/Unbroken Aegis/Overchannel/Legion/Bloodrush) may be DISPLAYED from the
  manifest, but do NOT implement the apex EFFECTS — they aren't in DESIGN_SPEC yet (pending human; show
  text only — no-undesigned-mechanics guardrail).

## ⇒ Claude Design manifest+asset LANDING — MIGRATED (2026-07-01)
DONE: 1) the 12 manifest-schema tests re-pointed to assert the CONTRACT (quantify over "every figure /
screen / template / item" — CD renames never break a test again; only a real schema violation does).
2) game player-figure refs -> `human_<core>` via new `Chassis.FigureKey` ("human_"+Id; thread Race here
when it lands); Forge/Expedition/Campaign/Game1/FigureIdTests updated. 3) CONTENT-PIPELINE gap FOUND +
FIXED: two divergent mgcb exist — the game builds `Roguebane.Game/Content/Content.mgcb` (was 291 bare-key
entries) but CD edited `Roguebane.Content/Content.mgcb` (386, human_/elf_). Added the 198 human_/elf_
entries to the ACTIVE game mgcb (game-side build wiring for CD's sprites; bare kept for now) -> human_grunt
sprites build + the figure renders (hi-fi). 278 Core green; build/newrun/combat smoke clean.
FLAG for CD/human: the two mgcb DIVERGE (game one is hand-synced) — pick a single source of truth or a
sync step. Ranger has NO human_ranger/elf_ranger figure yet (CD need) — its card falls back. Manifest
screen ids STILL old (combat/build/newrun/runmap/campaign) + per-part `binds` still absent (arc blocker).

## OLD next-slice note (superseded by the MIGRATED block above)
CD dropped a big payload mid-turn (2026-07-01): a NEW `layout.json` (figures renamed bare `<core>` ->
`human_<core>`/`elf_<core>`, screens restructured — combat lost its old `backdrop` element), new
`sprites/body/human_*` + `elf_*` PNGs (UNTRACKED), `Content.mgcb` + `ASSET_MANIFEST.md` + node/map icons,
+ refreshed `design/*.png`. This RED-lights 12 manifest-schema tests (they assert CD's OLD literal keys).
The Loadout->Equipment rename was committed IN ISOLATION (436a68c, green) by stashing this payload; it is
now popped back into the tree. MIGRATION PLAN (human-directed): 1) re-point the 12 manifest tests to
assert the CONTRACT/SCHEMA (or a test-owned fixture), NOT CD's literal keys — CD owns layout.json's
contents (so a figure/screen/template rename never breaks a test again). 2) repoint game FigureId refs
bare `<core>` -> `human_<core>`. 3) VERIFY the content pipeline builds + smokes with the new sprites/mgcb.
4) commit CD's asset landing + the schema tests together, green. Manifest LOOKUP screen ids are STILL old
(`combat`/`build`/`newrun`/`runmap`/`campaign`) — CD hasn't renamed those yet (their TODO); per-part
`binds` also NOT in this manifest yet (still the arc blocker).

**FIDELITY DRAW = PRIORITY + CD-review follow-ups (2026-06-30).** Hi-fi chrome is the human's top priority
and it's GATED on the engine draw — bump the fidelity primitives to the FRONT of the queue:
- **Implement the engine DRAW for `shadow` + 9-slice `frame` + gradient `fill`** (LAYOUT_CONTRACT §10). CD
  has wired shadow + frame on the pipeline side (data present in the manifest); gradient capture is
  coming. Until the engine draws these, ALL hi-fi chrome renders flat — this is the critical path.
- **#8 consumer safety-net:** the template renderer must de-dupe overlapping parts — when two share ~the
  same rect, prefer the `binds` entry, ignore the sample-only twin. (CD also de-dupes at source.)
- **binds LANDED** on all 5 screens' templates → manifest-drive coreCard/preview render unblocked (when
  content lands). But the NewGame **Loadout panel internals + section headers are NOT in the manifest yet**
  — CD is re-instrumenting them (LAYOUT_CONTRACT §9 completeness). Build the Race/Core columns now; leave
  the Loadout panel a placeholder until CD's re-instrumented drop — do NOT hand-fake its internals.

**CD PIPELINE DONE — engine DRAW is now the SOLE hi-fi blocker (2026-06-30 pm drop).** CD closed almost
the entire pipeline side. Loop actions once the drop LANDS locally (add the new rune PNGs to `mgcb`; let
the contract-based manifest tests absorb the re-extract — do NOT re-pin content):
- **ENGINE DRAW = TOP PRIORITY, now the ONLY thing between us and hi-fi chrome.** Implement: offset+blur
  **shadow** draw, nine-patch **frame** blit (read the updated slice = 60/36 + the v2 button skins),
  corner-interpolated **gradient** fill (LAYOUT_CONTRACT §10). CD wired shadow+frame+gradient CAPTURE on
  all 5 screens + a richer v2 frame/button/background set + an `interactionStates` table — until the
  engine draws these, the chrome renders flat. This is the whole ballgame now.
- **Wire the baked rune icons** `icons/rune/core_<core.id>` (grunt/warden/adept/summoner/reaver) into the
  NewGame core cards — same convention as `icons/node/<type>`. (#2 done by CD.)
- **Build the FULL NewGame Loadout panel** from the re-extracted `screens.newgame` — now properly
  instrumented (previewAttrRow container + attr/HP/layout tiles + apex + section headers; header copy is
  "Choose your Loadout"). DROP the placeholder — render it for real. (#5 done by CD.)
- **CONFIRM on wire** (so Doug can tell CD to close the entries): rune icons in use (#2); Loadout panel
  renders full (#5); templates read clean, no doubled parts (#8 — CD dedupes at source; keep the consumer
  safety-net as belt-and-suspenders); cityNode reads [8,8] (#12); recaptured `castle.png` in use (#6).
- Still OPEN: the DRAW above; container overflow/scroll (#4, deferred); literal 80-char truncation (#9,
  parked).

## Current target
**RE-OPEN RESOLVED (both threads cleared) — POC functionally complete again.**
1) RUN-START CRASH: fixed + LIVE-draw verified (detail below). 2) SCREENS MATCH DESIGN: all four live
screens done + smoke-verified vs the committed 06-30 renders — 01 combat (locked s13 layout), 02 build
(matches modulo the deferred inventory-tabs / rune-bag cards), 03 map (rebuilt: supply/support panels,
spread beacon chart, castle panel), 05 new-run (dedicated Choose-Your-Core grid). Flow: NewRun -> Build
-> March. REMAINING (deliberate, not blockers): the design/02 INVENTORY TABS (GEAR/TECH/MINIONS) +
RUNE-BAG cards (input-coupled — need drag-equip + mid-run stash), and the Phase-3 arc (see below). ARC
HISTORY: human picked MANIFEST-DRIVE (New Run grid + centred header driven off layout.json; then gated
on Claude Design authoring per-part `binds`) -> pivoted to Phase-3 primitives (part-heal, shields) ->
human picked GO FOE-PART-AIM LIVE: DONE this pass (skirmishes erode parts, kits carry Bandage, campaign
winnable; see G1 in Debt). NEXT candidates: tune the skirmish numbers in play; kit a shield source;
replace flat block with shields; the big refactors (#1 single-enemy, #2 Race+CoreRune); or resume
manifest-drive when binds land.
(older manifest-arc detail kept below: ListLayout added; the New Run coreCards grid is the first container driven off
layout.json).
Original re-open detail (kept as record):
- TOP — RUN-START CRASH: FIXED (confirmed via crash.log). Root cause: `DrawGearBar` drew an em-dash `—`
  into the ASCII-only mono font (SpriteFont THROWS on unknown glyphs) on the MAP screen. Fixed
  CENTRALLY — `Game1.Text` now runs `Safe()` (maps common typographic chars → ASCII, replaces anything
  else the font lacks), so NO drawn string can crash again; the em-dash literal is also gone.
  `Program.cs` keeps writing `crash.log`. The fold-to-ASCII guard is now `Core.GlyphSafe.Sanitize`
  (pure, engine-agnostic) with 7 headless tests (245 Core tests); `Game1.Safe` is a thin caller that
  feeds it the font's glyph set — the crash-class is regression-covered headlessly. Non-ASCII drawn-
  literal sweep CLEAN: the only remaining drawn non-ASCII was DrawLoadoutStrip's "-" placeholder (was an
  em-dash) -> fixed; every other non-ASCII in source is comment prose (never drawn). Driven run-start
  STATE regression added (RunStartTests: NewBuild -> March -> assert the map-screen render contract at
  camp/Choosing -- state, options, player HP, resource readouts, bay/loadout/gear sources; 247 Core
  tests). LIVE-DRAW VERIFIED (crash item DONE): a running Roguebane.Game locks the default build output,
  so build to a SCRATCH dir and run that instance instead --
  `dotnet build Roguebane.Game -o <scratch>` then from <scratch> `RB_SMOKE=1 RB_SCREEN=<map|build|combat>
  RB_SHOT=x.png dotnet Roguebane.Game.dll`. All three render clean, exit 0 ("SMOKE OK") -- the run-start
  crash is gone in the live draw, not just headless.
- Fresh design renders landed (`design/01–06`, 06-30) and are now COMMITTED as the rebuild reference.
  Audited each live shot vs its PNG (scratch-dir smoke). Punch list:
  * 03 MAP: DONE (all design/03 elements present + smoke-verified) -- top-left SUPPLIES + MUSTERED-
    SUPPORT panels (pip bars + flavor); BIGGER CHART (nodes SPREAD to fill an inset region via
    `Core.Layout.GraphLayout`, viewport-independent, camp left -> castle right); right-side "THE CASTLE /
    the exit" panel. FOLLOW-UP (not a fidelity gap): swap the hand-set ChartRegion + panel positions for
    the manifest regions once the whole map screen is manifest-driven.
  * 02 BUILD: matches except the already-known DEFERRED items — INVENTORY tabs (GEAR/TECH/MINIONS) +
    rarity item cards, and a RUNE BAG of MARKS/PATHS/KEYSTONES cards (current screen shows rune LADDERS).
    Both input-coupled (need input wiring + mid-run stash).
  * 01 COMBAT: divergence from design/01 is a LOCKED decision (s13 multi-foe layout) — NOT a defect.
  * 05 NEW RUN: DONE + smoke-verified. Dedicated Screen.NewRun "Choose Your Core" 5-card grid -- each
    card draws its OWN core figure + Title + Archetype + stat block + wrapped Flavor; the current core is
    ringed SELECTED; BEGIN THE MARCH -> the loadout (build) screen. Flow is now NewRun -> Build -> March
    (shell starts on NewRun; RB_SCREEN=newrun smokes it). Input: arrows / card-click select, Enter/click
    Begin. Single-core (race step still behind the flag). NOTE: this screen is HAND-CODED (draws figures
    directly by core id), so it sidesteps the manifest coreCard template's hardcoded-grunt image -- that
    review-fix only matters if/when the screen is re-based on the manifest coreCard.
  * PALETTE: NOT a uniform shift — 05 reads warm-dusk, 02/03 read cooler/navy; renders vary, so leave
    the palette as-is (warm-muted-dusk, DESIGN_SPEC §13) until a palette decision actually locks.
- MANIFEST validated COMPLETE (human review of the full 3305-line `layout.json`; parses clean — an
  earlier "truncated" read was a stale mount, ignore). runmap/campaign/newrun are now RICHLY spec'd
  (templates coreCard/legendRow/beaconNode/cityNode, `type:"graph"` containers + `anchor:"nodePoint"`,
  bundled open fonts) — rebuild those 3 screens FROM the manifest; they're no longer stub-blocked.
  Consumer must learn `type:"graph"` (place nodes from map/campaign data in the region) + `nodePoint`
  (node-relative parts) + IMAGE PARTS (done: `TemplatePart`/`PlacedPart` now carry an optional `Image`,
  so a card part can be a figure sprite instead of text; CardTemplate schema test accepts sample-OR-image).
  MODEL HALF DONE: `Element` now parses `content` (literal text) + `item` ({template, flow, gap, size} for
  list/graph containers) -- pinned by LayoutManifestTests against the real manifest (chart=graph/beaconNode,
  build action bar=horizontal/techCard, newrun=coreCard). RENDER half remains: the Game consumer must
  iterate map/campaign nodes (graph) + bound lists and STAMP the item template at each cell (CardTemplate
  already places a template at an origin; wire data -> positions). GraphLayout.Cell now does the graph
  NODE positioning (spread grid coords to fill a region) -- the runmap chart uses it; campaign/newrun
  can reuse it for their `type:"graph"` containers. ListLayout.Cells does the LIST positioning
  (horizontal/vertical, cell+gap); ManifestUi.ListCells resolves a container's region + item -> cell
  rects (tolerant, null-fallback). ManifestUi.ElementContent returns an element's literal `content`.
  NEW RUN is now POSITION-manifest-driven: coreCards grid + the centred header (eyebrow "A NEW RUN
  BEGINS" + serif title "Choose Your Core") read content + rects from layout.json (dropped the hand top
  bar -> matches design/05's centred header). SKIPPED (manifest quirks): the newrun SUBTITLE content is
  truncated in layout.json (kept hand copy, manifest position); the beginBtn is only 120px in the
  manifest -> clips "BEGIN THE MARCH", kept the wider hand button. PER-PART BINDS MODEL DONE: `TemplatePart`/`PlacedPart` now
  carry an optional `Binds` (the live datum vs the design `sample`); CardTemplate.Place threads it;
  unit-tested. NEXT (template render): a consumer that stamps a template's PlacedParts -- image parts ->
  sprite from Binds, text parts -> resolve Binds to live data (else the sample) -- driven off the
  manifest. BLOCKED on Claude Design authoring per-part `binds` in the templates (they ship only
  `sample` today; see Asset gaps). Once bound, render coreCard/legendRow/etc. from the manifest instead
  of hand-code. Separately: position-drive a whole screen as a unit (build is dense -> wholesale, not
  piecemeal, to avoid mixed coord systems).
  ARC STATUS: clean position-drive wins delivered (New Run grid + centred header). Remaining steps are
  design-data-GATED (per-part binds) or a risky wholesale rewrite of screens that already match design.
  Arc PAUSED pending Claude Design binds; loop pivoted to unblocked roadmap work (Phase 3 #4 part-heals).
  cityNode is labels-only; castle stays the generic node icon (procedural castle parked — not
  mission-critical). REVIEW FIXES:
  * coreCard figure is HARDCODED (`image: …/chassis/grunt.png` sample) — BIND each card's image to its
    OWN core's figure, else all 5 New-Run cards show Grunt.
  * Decorative glyphs `✦ ◉ ✓` render as `?` (GlyphSafe maps `· — →` but not these). ADD the glyphs the
    design uses to the SpriteFont character regions so they render; keep GlyphSafe as the safety net.
  * Normalize element `type` (some images/panels are typed `"text"`, e.g. combat `backdrop`) — or ensure
    the consumer keys off image/fill/item presence.
  * PARKED (revisit later): two literal `content` subtitles (campaign/newrun) are truncated in the
    manifest — the text itself may be outdated, so not worth an extractor fix yet.

## Prior integration record (the "DONE" claim below is RETRACTED per the re-open above)
**Shell wired to `layout.json` — integration DONE; combat layout RESOLVED (locked, see s13). POC functionally complete; only the low-value Equipment inventory-tabs polish remains (deferred).**
Core manifest toolkit COMPLETE + pinned: `Layout/` LayoutManifest (parse), StageComposer + FigureBinding
(figure assembly), ScreenLayout (anchor->rect), PaletteColor, CardTemplate. Game consumes it via
`LayoutRegistry` + `ManifestUi` (tolerant ElementRect/Color). Figures (player + foes) compose from the
manifest; chassis figure key threaded through assembly (Expedition/Campaign.FigureId). Clean content
build certified from scratch (226 Core tests, all 5 screens smoke clean, no stale-.xnb).
- Four stale-asset bugs fixed (foe sprite, icon maps, pips, rune path); full asset-string sweep clean.
- design/02 build: attribute-readout bars + anatomy stat callouts. Gear-on-figure draws the actual
  wielded weapon. design/03 map: node legend + supplies remaining/max + support banked/holds.
- Combat: foe figure variety (ogre/troll/bandit/skeleton) + name tags; minion sprites in the bay lane;
  header titled by node type (SIEGE/SKIRMISH/RESOURCE HOLD).

VERIFY loop: `RB_SMOKE=1 RB_SHOT=x.png RB_SCREEN=<build|combat|map> dotnet run --project Roguebane.Game`
renders a PNG + exits (headless); add `RB_CHASSIS=<0-4>` to pick a chassis on the build screen. Smoke
every visual change. GOTCHA: spritefonts are ASCII-only — a non-ASCII glyph in Text() THROWS at draw.
All 5 chassis figures + 3 live screens verified clean (no overlaps/clipping after the layout sweep).
All usable foe figures confirmed too: ogre/troll rendered in castle combat; bandit/skeleton render by
structural equivalence (identical 7-part/21-file layout). wraith/gargoyle stay excluded (incomplete art).

REMAINING (deliberate / not safe 1-min fragments):
- COMBAT exact design/01 bottom-panel rebuild: SUPERSEDED by a locked decision (DESIGN_SPEC s13) — for
  1-3 foes, large foes (clear limb-band PART-aim, the core mechanic) beat a prominent bottom attribute
  pool; combat keeps its working hand-placed vertical-spread layout (foes large, pool in the YOU panel),
  which is the canonical MULTI-FOE layout. design/01's bottom-dominant pool is the single-foe ideal.
  So combat is DONE for multi-foe; the only open combat item is a future 1-foe mode (would revisit) and
  the battlefield minionField figure (still parked — its manifest slot overlaps the working lanes).
- EQUIPMENT screen (design/02): no screen state yet (only Build/Run). Inventory tabs (GEAR/TECH/MINIONS) +
  item cards, Rune Bag, click/drag-equip — INPUT-COUPLED features needing input wiring + mid-run stash.
- Asset gaps (see section): skirmish node icon, wraith/gargoyle figure art, torso bare-variant (plate
  invisible on the figure), bundled open fonts.

FEATURE-FLAG asset-gated work — `Features` toggle DEFAULT OFF: build FULLY but render NOTHING when off;
flip when art lands. Cases: WAR-PARTY advance UI (war-party token + camp marker); RACE + CORE-RUNE
two-step NEW RUN (race art). Keep the current single-core New Run until race art exists.


## Recently shipped (one-liners; full detail in git log)
Combat thesis loop is whole and tested (226 Core tests). Highlights — all pinned + RB_SMOKE-verified:
- Targeting/firing FSM (requireAim, target-driven fire, one global AUTO; no fire button); foe PART-aim
  via limb bands; shell-input mapping extracted to Core (CombatTargeting).
- Minions fight (auto-summoned to bays); combat lanes (bay lane, rallied-support "RALLIED +N").
- Gear END-TO-END (Core): merchant buy -> Stash pack -> equip honoring gates; leather evasion; Body.Unequip.
- Build attribute readout + gate markers; bank-on-clear; campaign spine strip (linear).
- THIS PHASE (layout-manifest integration): see "Current target" above.

## Locked decisions
- Enemy threat: LIGHT/winnable for now (goal = combat dwell, not difficulty); full envelope = later balance.
- Pacing: fixed ~10 tick/sec clock; cooldowns in seconds (weak 4-6s, strong 12-15s); small dmg; 30s+ fights.
- Randomness: seeded deterministic PRNG (same seed = same run).
- CON→HP: CON = BONUS HP on a natural base, 1 CON = 2 HP; chest dmg drops CON → bonus shrinks → MAX HP
  drops + current caps down.
- DEX = haste: base cooldown from technique/weapon; DEX shortens ~1.5-2%/pt, capped ~28%.
- Minion gating: data {stat | none | alt-cost}; default INT; a chassis/minion may override (ungated
  retinue; HP-cost summon).
- TEMPO/PERIL header: DROPPED.
- Default loadout: FIXED per-chassis kit (data), grown by finds; NO build-time pick gate.
- Targeting/firing: the FSM in Current target.
- Fidelity review: the LOOP self-reviews each RB_SMOKE shot vs the design PNG + design/SCREENS.md
  (automatic every pass); human review ON REQUEST only.
- Per-technique aim. Degradation = GRADED via direct stat-capacity reduction.
- Rallied support: player-allied, UNDAMAGEABLE, intermittent auto-fire ON the castle (NOT enemy repair).
  Castle = boss; HP permanent in-encounter; it may restore its own systems.
- Healing split: potions/magic restore PARTS (never HP); HP only out-of-combat (shop/quest).
- War party: marches on CAMP 1 step/move; crack the castle disbands it; reach camp = supplies cut = lose.
  Instant-loss now; camp last-stand later. (DESIGN_SPEC §12)
- Roguebane.Content is TRACKED.

## Attribute model (one part = one stat; integer-only)
- STR (Arms): attack power 1.0x. Gates STR weapons; equips shields (heavy = STR).
- INT (Head): spell power; keeps spell actives + passives up. Absorbs old WIS.
- DEX (Legs): evasion; accuracy; +0.25x attack (DEX/4, int); HASTE (cooldown −%/DEX). Gates DEX weapons.
- CON (Chest): bonus HP (1:2); stun resist; the DEFENSIVE stat — powers the CON "block" source and scales
  SHIELD-POINT REGEN (all sources). STR still wields the heavy shield OBJECT; CON powers its block. Shield
  model = the Phase 3 SHIELDS revamp (passive stat-reserving technique with regenerating 1-dmg points),
  NOT a flat cap. Gates chassis-extending runes.
- CHA dropped; WIS → INT (5 → 4).
- ARMOR = light effect layer, NOT attribute gear: plate → flat 1-4 protection vs the stat-dmg to its part;
  leather (DEX) → evasion; head spell-armor → spell/blind protection. Rides the part's condition (break
  part → effect gone). Weapons/shields gate on their stat to wield.
- Parts: Head x1, Chest x1, Arms x2, Legs x2. Paired parts take damage independently, each carries a SHARE
  of the stat (one arm = ½ STR). Armor one piece per part-group; weapons in hands. Lose a limb → lose its
  stat share → gear can fall off.
- SCALE: low ints; ~20 = huge; damage/heal in 1-3; a hit subtracts 1-3 from the targeted part's stat.
- CORE MECHANIC: part damage subtracts that part's stat from the live pool until repaired — unifies graded
  degradation, equip/ability fall-off, and the allocation economy.

## Needs human (loop skips these)
- Balance/feel tuning (placeholder-sane, tune in play): tick 10/s; cooldowns + damage; DEX haste 2%/pt
  cap 28%; CON→HP 1:2 + base 8; evasion %; shield amounts/regen; armed-foe HP/strike; castle cadences;
  budgets/spoils/prices; supplies vs march length.
- Five chassis stat blocks (design/05) are placeholder — tune later.
- Part→stat friction (legs = accuracy, arms = STR) — low-pri revisit only if it nags.
- Fonts: SpriteFonts use system Consolas/Georgia — swap to bundled open fonts before distribution.

## Locked this round (were Needs-human)
- MULTI-FOE COMBAT LAYOUT — LOCKED (DESIGN_SPEC s13). Large foes (clear limb-band PART-aim) over a
  prominent bottom attribute pool; current vertical-spread layout is canonical for 1-3 foes. design/01's
  bottom-dominant pool = single-foe ideal. Loop-decided given the part-aim tradeoff; revisit for a 1-foe mode.
- FOE→PLAYER PART aim — SHIPS. Foes erode player PARTS (HP-vs-stat default accepted: a hit eats the
  targeted part's stat; HP only via penetrate/overkill). WHICH limb = a per-foe TARGETING PERSONALITY,
  as data: SMART (best for its build) | RANDOM | INEPT (botches a good pick). Localize CON-block/evasion
  onto part hits here.
- WAR-PARTY indicator — a TOP-edge track, castle (right) → camp (left), advancing each move (try top first).
- Armor one-per-group + weapons per-hand — LOCKED. Fog reveal rules — LOCKED.
- Campaign topology + shields — revamped this round; see Phase 3.

## Asset gaps (Needs Claude Design)
*Loop logs here when a screen needs ART that's missing/wrong in Roguebane.Content and can't be composed
from primitives. Route each to Claude Design. (Art direction: DESIGN_SPEC §13.)*
- Skirmish node icon: removed by the drop with no replacement; map renders the `unknown` "?" as a
  stopgap (label disambiguates). Needs a dedicated combat-node icon.
- Foe creature variety: DONE for ogre/troll/bandit/skeleton (per-encounter assignment in Sieges —
  raiders bandit/skeleton, castle ogre/troll). STILL UNUSED: wraith (PARTIAL art — only 12 of the
  21 part files, renders with gaps) and gargoyle (24 files, nonstandard part layout) — both need art
  completion/normalization before wiring.
- MANIFEST per-part `binds` (blocks manifest template-render): the templates (coreCard/legendRow/…) ship
  parts with only a `sample` placeholder, no per-part `binds` key — so the consumer can't map each part
  to live data. Author `binds` per part (figure/title/archetype/str/…) so cards render from the manifest.
  Model side is ready (`TemplatePart.Binds`). Also: newrun + campaign SUBTITLE `content` is truncated in
  layout.json (shell keeps a hand copy); the newrun `beginBtn` is 120px — clips "BEGIN THE MARCH" (shell
  keeps a wider hand button). Widen/repair when convenient.

## Debt (active — with reconcile trigger)
- BUILD screen attribute readout + gate markers DONE. Still lacks inventory tabs (gear/tech/minions) +
  drag-to-equip and equipped-gear on the anatomy. Blocked on gear/minion equip (G2/G7).
- Combat surface: PART-level aim UI DONE (limb bands + part-aim); minion-bay lane DONE. Still no
  rallied-support lane.
- Part-group EVASION already localizes on PART hits (Caster.Hit reads EvasionPercent(part)); it is live
  wherever foe part-aim is on. CON block is still WHOLE-HP only — localizing it on PART hits reconciles
  with the Phase 3 SHIELDS revamp (current code is still the old flat block).
- INT beams are fast Timered bolts (Sustained=every-tick was a firehose at 10/s); for a true channel, add a
  per-tick damage-scaled sustained kind. (Speculative — defer until a channel weapon is authored.)
- Leather armor evasion now FUNCTIONAL + content (Shops.Hide) + tested. SpellWard still deferred (no spell model).
- Mouse is click + hover only — no drag-to-equip, tooltips, rebinding; PAUSE/FLEE are plain rects (U6).
- Rune grants = chassis-extension PARTS only; add more data-driven Mark effect kinds when a non-extension
  keystone is authored.
- G1: foe->player PART aim is now LIVE on SKIRMISHES (human signed off the difficulty shift). ArmedPoint
  encounters set FoePartAim=true -> field raiders erode the player's PARTS (Inept/Random personalities);
  the ArmedCastle stays a whole-HP DPS race (boss thesis intact). Every chassis kit + the palette now
  carry `Bandage` (part-heal) so builds survive; a build that drops it pays the intended penalty.
  Campaign verified WINNABLE (CampaignTests green); FoeArming/BuildSession/Heal/Campaign tests reconciled
  to the new behavior; the build palette re-laid to fit 7 techniques. This LEAVES the old "LIGHT/winnable
  without precautions" envelope by design -- skirmishes now demand a defensive source. REMAINING: tune
  the numbers (foe cadence/power vs Bandage rate — placeholder-sane); optionally flip SMART castle part-
  aim or armed real-content foes later. Shields (Stoneskin) are wired + available but not yet in kits.

## Roadmap
- Phase 1 [DONE]: Core skeleton, rune economy, chassis, techniques + deterministic combat tick,
  enemies+castle, the attribute rework, chassis→body wiring, combat migration onto Body, balance-sim.
- Phase 2 [ACTIVE]: UI U1-U6 built (combat/build/map/spine/chrome; fidelity continuing via self-review).
  Gameplay G1-G8 built or partial (multi-part foes, gear/weapons + plate armor, 5 chassis, node-map run,
  war-party, campaign spine, data-driven runes/minions/magic, economy). Remaining actionable work = the
  Debt above + the targeting FSM fix. Visual truth = design/ PNGs + design/SCREENS.md; look = DESIGN_SPEC
  §13. Keep "FTL" out of shipped UI text.

## Phase 3 — combat depth + Race/CoreRune rename [SCOPED, not started]
Big slice after the current combat polish. Do it in SMALL /loop slices. Reconcile DESIGN_SPEC sections
DURING the phase with SURGICAL edits (do NOT rewrite/clobber). LOCKED below; OPEN parked at the foot.

LOCKED:
- SINGLE-ENEMY combat: always vs ONE enemy (it may be multi-PART: a human foe, an atypical creature, the
  castle, or a special resource fight). DROP multi-foe / multiple targets entirely — the only targeting is
  PART aim within the one enemy. (Simplifies the front-target machinery.)
- RENAME "Chassis" -> RACE + CORE RUNE (the real names; like FTL ship + layout):
  * RACE = starting attributes + base HP. Start with Human + Elf (more later).
  * CORE RUNE = LAYOUT (rune budget, # techniques, # minion bays) + apex effects/bonuses (stronger than a
    keystone). RACE GATES which core runes it may take (SB-style restriction matrix).
  * New Run = pick RACE -> pick CORE RUNE (race-allowed). Today's 5 "chassis"
    (Grunt/Warden/Adept/Summoner/Reaver) BECOME core runes; Race is the new orthogonal axis.
  * Code: rename Chassis -> CoreRune everywhere; add Race; split Chassrium into Races + CoreRunes.
- LONG COMBAT by design. Most builds carry a DEFENSIVE SOURCE (shield / stoneskin / DEX-evasion); dropping
  it = high hit odds. A build with NONE must compensate (cheap/near-instant part-heals, or high damage +
  evade/CON + frequent heals).
- ENEMY HP high but SCALES with tier so early play keeps pace; ~1/4–1/3 damage taken is typical when unspecialized.
- HEALS standard — they repair PARTS, never HP ("repair systems"): Potion (item; strong ones rare),
  Bandage (CON ability), Cure Wounds (INT spell). Starting healless < starting shieldless in penalty.
- SHIELDS (revamped): a shield SOURCE is a PASSIVE TECHNIQUE that RESERVES its stat in the bar and is ON
  by default (most builds carry one for survivability); toggleable off in combat to free the stat. It
  maintains a POOL of SHIELD POINTS (FTL layers, no hard cap) — each absorbs 1 damage and is CONSUMED on
  hit; points REGENERATE on a timer scaled by CON (+ rune effects). The SOURCE sets amount/regen + its
  stat: block (CON), stoneskin/barkskin/steelskin/diamondskin (INT), parry (DEX, low cap), bind (STR, low
  cap), shield-wall (CON, scales with troops). Every class should have a viable block source. (Supersedes
  the old "flat while held" CON-block.)
- CAMPAIGN MAP: a forward-biased city GRAPH (NOT a fixed linear list). Movement is free-ish but generally
  forward, GATED by SUPPLIES (deplete as you move). Run dry → you must WAIT; each waiting turn has a CHANCE
  of an encounter (may resupply — affordable, free, or useless). The enemy war-party advances the whole
  time toward your camp + capital; reaching them = LOSE. Unblocks the design/04 branching screen.

OPEN (park; decide as the phase starts):
- Healless compensation archetype(s): heal-spam vs damage-tank — define.
- "Ready to block Nx faster" mechanic (the healless trade) — design it.
- Shield damage caps per source + actual numbers (relative only so far) — balance.
- CON as a MINION resource (CON funds minions -> a no-INT cleric-caster) — floated; ties to minion alt-cost gating.
- Shield Wall troops->layers formula (floor(troops/4) is illustrative) — balance.
- Race roster beyond Human/Elf; full Core-rune roster; the race<->core-rune restriction matrix.

Suggested order: (1) single-enemy + part-aim simplification; (2) Chassis->Race+CoreRune rename + race-gated
New Run; (3) shield-levels system [WIRED (dormant): `ShieldPool` layers live ON the body
(Body.RaiseShield/DropShield/TickShields/AbsorbShields); Caster raises a pool per active shield SOURCE,
ticks it each Step (CON-scaled regen), sheds it when the source's stat is smashed; Caster.Hit absorbs
through shields FIRST (outermost, before armor/block/parts, for whole-HP AND part hits). Content
`Techniques.Stoneskin` (INT, 3 layers) is opt-in (NOT in `All`) so no current balance shifts. Tested.
REMAINING (disruptive, human balance eyes): REPLACE the flat CON block (Brace) with a CON shield source
+ retire Body.BlockMitigation + place shield sources in starting kits + tune the placeholder numbers
(layers/regen — OPEN #8)]; (4) part-repair heals [DONE + LIVE:
in-combat `Heals` technique + Techniques.Bandage, now in every chassis kit; foe part-aim flipped on for
skirmishes]; (5) defensive-source defaults in starting kits [PARTIAL: heal (Bandage) is in all kits; a
shield source (Stoneskin) exists but is not yet kitted]; (6) enemy-HP scaling. Touch points: §4/§6/§7/§10/§11/§16.

## Layout & assembly system (manifest-driven) [FOUNDATION — do before the fidelity rebuild]
Fixes the EXPLODED figure and makes ALL layout pixel-perfect + viewport-independent by consuming the
generator's emitted coords (contract: `design/LAYOUT_CONTRACT.md`). The generator already computes every
part rect + hand socket; it will now EMIT them (`Content/layout.json` + modular part PNGs) instead of
flattening them away. Gated on Claude Design shipping that; until then build the consumer against the
schema with a small hand-stub manifest, then swap to the real one.
- LayoutRegistry: load `Content/layout.json` (figures + gear + screens).
- Stage composer: draw a figure by blitting its part sprites at manifest `rect` in `z` order, swapping
  part STATE (healthy/damaged/broken) by Core part condition, mounting gear pivots on hand sockets;
  uniform- (ideally integer-) scale the whole figure into its slot, pinned by `pivot`. RETIRE Game1's
  hard-coded `DrawHumanoid` offsets (the cause of the explosion).
- UI from manifest: place every element by anchor+offset+size; RETIRE magic-number rects.
- Viewport (aspect-independent, no bars): background SCALE-TO-COVER; HUD anchored to real screen edges
  (fills any aspect); pixel stage integer-scaled + centered. Only the final transform reads the backbuffer.
- Determinism: position = f(manifest, figure->slot scale, screen). A screenshot matches the mockup BY
  CONSTRUCTION — the loop's visual review becomes confirmation, not guess-and-nudge.
