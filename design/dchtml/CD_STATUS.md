# CD_STATUS — open layout/engine gaps (content-drop relay status)

Canonical, deduplicated STATUS of everything the **layout structure** or the **game engine** can't yet
represent — cases where the design does something real that does **not** survive the manifest extraction
(§9) or has no engine implementation (§11). This is the single source of truth for "what is still open"
on the CD↔engine relay.

**Two artifacts, distinct functions (do not duplicate one into the other):**
- **`CD_STATUS.md`** (this file) — the LIVING list of OPEN gaps. Forward-looking state: what the engine/
  extractor still owes, why it matters, how to verify. Numbered entries are stable ids other docs cite.
- **`DROP_AUDIT.md`** — the per-pass CHANGELOG (what files changed in each drop: adds/removes/manual
  edits + the drop-audit checklist). It **references** `CD_STATUS #N` for open items instead of restating
  their consequences. If you're about to paste engine-consequence prose into the audit, it belongs here
  as a CD_STATUS entry (new or updated) and the audit just cites the number.

**Ships in every drop.** Both `CD_STATUS.md` and `DROP_AUDIT.md` are copied into `drop/design/dchtml/`
with the sources, so the engine side always has the current open-gap status alongside the changelog.

**Protocol**
- ADD an entry the moment a design feature can't be faithfully carried to `layout.json` / the engine,
  and FLAG it in the turn summary (never silently ship a lossy export).
- Once an entry is CLOSED or ROUTED into the development loop, it is **DELETED outright** — no
  tombstones, no "closed" lists. Git history is the archive.
- Therefore: **everything in this file is currently open.** An empty OPEN section = nothing outstanding.
- Each entry: what · where it shows up · why it matters · status.

---

## OPEN

### 40. `border.colorBind` — accent binds now target the BORDER draw site (B20 / Doug #7); engine apply pending
- **What (2026-07-12):** the coreCard Core-Effect block's `colorBind:"core.accent"` was engine-drawn as a
  FULL-RECT fill behind the effect text — a contrast violation Doug reported. The accent is a LEFT-BORDER
  trim sliver, so the extractor now supports `data-color-bind-target="border"`: the bind is emitted INSIDE
  the border spec (`border.colorBind`) instead of element-level `colorBind`. Authored on the NewGame
  coreCard effect block + previewCoreEffect AND Equipment's `coreEffectBlock` (previously a STATIC token —
  same bug class, it never re-tinted per core).
- **Consequences (engine, NOT yet done):** apply `border.colorBind` when drawing an element/part border
  (all sides listed in `border.sides`); element-level `colorBind` keeps its current fill/text semantics.
- **Related:** `style.coreAccents` is now published in `style_tokens.js` / `layout.json.style` (B20.2)
  — the per-core accent tokens (grunt…barbarian, lockstep with core-kits.js `CORES.<id>.accent`); read
  these instead of the engine's flagged stopgap palette.
- **Status:** OPEN until the engine tints bound borders + reads `style.coreAccents`.

### 39. Encounter FOELESS arrivals (quest / camp / nothing-here) + CityMap retreat→redeploy states (B29) — engine gating/draw pending
- **What (2026-07-12):** the Encounter shell now hosts non-combat arrivals. (a) `questPanel` (+
  questTitle/questPrompt/questAccept/questDecline children) — the quest prompt card INSIDE the shell
  (card-frame chrome, ACCEPT gold / DECLINE neutral), gated on `encounter.quest` (`data-bind-gate`);
  title/prompt are bound SAMPLES (quest copy/catalog is Doug's separate pass). (b) `campMarker`
  (icon + CAMP + note), gated on `encounter.camp` — Camp is a foeless encounter so techniques can be
  pre-activated (HELD/CHARGING still reserve; a TARGETING intent relaxes to READY foeless). (c)
  "nothing here" = the shell with NO marker — no new elements; the engine resolves neither
  `encounter.foe` nor quest/camp. The foe cluster (glow, stream, foeFigure, foeHp, foeLabel, reticle,
  aim tags) must UNMOUNT when `encounter.foe` is unresolved. (d) CityMap gains `retreatBtn` with two
  authored states: `retreat` (neutral) / `redeploy` (gold, relabelled) — replacing the engine-only
  overlay. ⚠ the states carry NEW fields: `label` (per-state text swap) and `asset` (per-state
  ui/button skin swap) — no engine support exists for either yet.
- **Consequences (engine, NOT yet done):** foe-cluster unmount on unresolved `encounter.foe`;
  quest/camp element gating; per-state `label`/`asset` swap on `retreatBtn`. The DEX-gated
  retreat/redeploy TIMER is undecided on the engine side — deliberately NOT designed against (per B29).
- **Renders:** quest + camp ship as their own refs (`design/01-encounter-quest.png` /
  `01-encounter-camp.png`) — mutually-exclusive whole-screen states, same rule as the cores.
- **Status:** OPEN until the engine hosts all three foeless arrivals + the two-state button.

### 38. `countWidth` (data-driven group width + hide-at-zero, B27) — NEW manifest field, engine draw pending
- **What (2026-07-12):** Equipment `minionColumn` + Encounter `minionGroup` now emit
  `countWidth: {bind, item, gap, pad, hideAtZero}` — engine width = count×item + (count−1)×gap + pad
  (design px, bind = `minions.cap` / `loadout.minionCap`), hidden entirely at count 0. This is the
  declarative form of the design-side reflow (the dc.html computes the same width from bayCap; at
  MinionCap 0 the sc-if drops the whole column). The static `size` stays the design-sample width, so an
  engine without the field draws exactly as today (the B27 "always-full-width" cosmetic).
- **Status:** OPEN until the engine resolves `countWidth` (closes payload B27; supersedes the fixed
  [170,99] read).

### 36. `either` (dual-attr technique cost — pay in STR **or** DEX) — NEW manifest concept, no engine draw
- **What (2026-07-05, Doug):** Frenzy + Flurry are now payable from EITHER pool. `core-kits.js` technique
  defs carry `either: ['STR','DEX']` (order = STR top / DEX bottom); the resolver picks the pool that can
  afford it (else the one with the most room, for the lock shortfall) and reserves there — `payAttr` is
  returned per technique. New shared helpers: `glyphFill(t)` (solid stat colour, OR a hard 50/50
  top/bottom split for `either` — NO black seam, Doug: the seam interfered with the glyph), `costSplit(t)`
  (two `{attr,cost,color}` rows), `costLabel` → `"STR/DEX N"`. Rendered on THREE surfaces: the technique
  glyph chip (split fill) + a two-row STR-red/DEX-green cost readout on the Encounter action-bar card, the
  Equipment loadout card, and the Equipment inventory badge (split box).
- **Consequences (engine/extraction, NOT yet done):** `layout.json` technique templates have no
  `either`/`payAttr`/split-fill field — the extractor + engine must carry a dual-attr cost (which pool the
  live reserve draws from is a RUNTIME decision the engine must make, mirroring the resolver's
  can-afford-else-most-room rule) and draw the split glyph + two-row cost. The engine-blit
  `icons/technique/{frenzy,flurry}.png` chips ARE now the split fill (re-captured pass 9), so the blit
  is correct; the gap is the manifest field + the two-row cost draw, not the glyph art.
- **Status:** OPEN — design landed + rendered (Reaver core), split glyph PNGs shipped. Pending: extend the
  template extractor for `either`, engine dual-pool reserve + split-cost draw. Ride the next drop-audit.

### 35. `parent` (parent-relative offsets) — NEW manifest field, engine child-box resolution pending
- **What (2026-07-05, Doug's no-absolute-positioning directive):** `proto/screen_extract.js` now emits a
  **`parent`** field on any screen element that nests inside another `[data-el]`, and its `offset` is
  measured against that PARENT's design box instead of the screen — so a grouped cluster (HP readout + its
  label/pips/value, a panel + its contents, the action-bar minion column + its cards, the top-right control
  cluster + its buttons) reflows as ONE unit. Anchor math generalized to any box (`anchorOffsetBox`). All
  six screens re-anchored so no right/bottom/center element bakes a `TopLeft` absolute; re-extracted
  `layout.json` (verified: 0 baked-absolute suspects, 6/10–45/52 elements per screen now parented).
- **Consequences (engine, NOT yet done):** the engine's manifest interpreter must resolve an element's box
  RECURSIVELY — resolve `parent` first, then place the child by (anchor, offset) INSIDE the parent's
  resolved box — instead of anchoring every element to the screen. Elements are z-sorted and children rank
  after parents in paint order, so a single forward pass with an id→box cache works. Until this lands, a
  child with a `parent` will mis-place (its offset is now parent-relative, typically small, not screen-px).
- **Related but SEPARATE (engine aspect-fill, §13 / #27):** how freed horizontal space is filled at
  off-16:9 aspect — a fixed-width center/side panel (Equipment center column, Merchant `waresPanel`, the
  Encounter action-bar fill) stretching to consume the gap between fixed neighbours — is the engine's
  aspect-fill call, not an authoring defect. The anchors correctly glue each panel to its edge; whether the
  interpreter grows the middle is §13 WIP. Full-bleed bars (Top/Bottom strips, Left/Right full-height
  panels) DO stretch to fill their cross-axis under any sensible interpreter.
- **Self-check tool:** `proto/resolve_check.html?screen=<id>` re-composes a screen from the manifest at
  four sizes (1920×1080 / 2560×1440 / 2560×1080 / 1440×1080) using the scale-by-height + edge-glue model,
  so a drift/detach regression is visible without the engine.
- **Status:** OPEN until the engine resolves child boxes against `parent`. Ride the next drop-audit.

### 33. PROTOTYPE Core Effects + core-parameterised screens — diverge from §11; extraction/renders stale
- **What:** Encounter / Equipment / NewGame now show the CD-payload PROTOTYPE Core Effects (Jack of
  All Trades / Fortified / Resonance / Conscription / Finesse / Fletcher's Luck) + default-kit slot
  config (tech-cap / bay-cap = 4/2, 4/1, 4/1, 3/3, 4/0, 4/2) — a CONSCIOUS replacement for the §11
  canon roster (Hollow Vessel / Unbroken Aegis / Overchannel / Legion / Bloodrush / Called Shot),
  NOT drift. This SUPERSEDES entry #27's "05 v2 Core Effect roster IS canon" line. Encounter +
  Equipment are core-parameterised via a `core` enum prop, driven by new **`core-kits.js`**; the
  action bar reflows off the counts (minion group collapses at 0 bays, width scales with count).
- **Consequences (engine/extraction, NOT yet done):** `Content/layout.json` `screens.encounter` /
  `.equipment` / `.newgame`, the `design/0N-*.png`, and `reference/screens/*` renders are STALE
  against these edits. Re-extraction must decide how to carry a core-PARAMETERISED action bar (one
  template stamped from a `cores` data block vs six baked screens) + the new `minionCard` template +
  the bays-collapse `sc-if`. Budgets stay parked (#27). The payload's "Needs human — not for CD"
  flags (Ranger seeded RNG, Adept stack nuance, Warden CON-armor cascade, Grunt −1 scope, Summoner
  Conscription vs Legion) are engine/design calls, not CD asks.
- **Status:** OPEN — floated for a UI + play pass; not locked into DESIGN_SPEC §11. Ride the next
  drop-audit; re-extract `layout.json` + re-render 01/02/05 in the same PR.

### 34. Deterministic per-core gear + reservation model (2026-07-04) — engine consumer pending
- **What:** `core-kits.js` is the ONE deterministic source for the Encounter/Equipment/NewGame screens.
  It carries per-core STARTER GEAR, a per-core run SCENARIO, and a `resolve(core, race)` pass computing
  standing gear reservations, the §6e disable CASCADE, §6d weapon-gates, and combat technique/minion
  states that MATCH the reservation. The paper-doll HIDES disabled/broken-arm weapons (`hidemounts`).
- **ARMOR RESERVATION SEMANTICS — CORRECTED (B26, Doug 2026-07-12: "armor consumes pool; eradicate
  incorrect design documentation in that regard").** Worn armor is a STANDING RESERVATION against the
  shared per-stat pool, exactly like a wielded weapon or an active technique — a full plate kit CAN
  visibly crowd out a technique activation on the same stat. This is what `core-kits.js` has implemented
  since the v6 rewrite (pass 6) and what DESIGN_SPEC's SUSTAIN MODEL + `Body.cs` already do. The old
  sub-entry here claiming "armor is threshold-gated only (no pool pips)" was WRONG — do not rebuild
  from it.
- **Consequences (engine, remaining):** consume the resolved reservation model fields (LOCKED/IDLE
  states, per-tab inventory PAGINATION idiom — `ui/button/button_pager.png` is in the manifest+mgcb).
  Re-extraction/renders + glyph capture from the original entry are DONE (passes 6–11).
- **Status:** OPEN until the engine consumer lands.

### 32. Worn-armor part set (B12 corrected) + `worn` inventory block — assets/data SHIPPED, engine draw pending
- **What:** payload B12 CORRECTED (2026-07-04): 744 FULL part sprites under
  `sprites/gear/worn/<race>/<slot>/[<core>/]<type>_<tier>_<condition>.png` (+ `bare_<condition>`
  terminals) + a top-level `worn` inventory block in `layout.json` (races/slots-per-type/tiers/
  conditions/themes/fallback). The engine picks the whole part sprite by (race, slot, wear-state) —
  fallback chain themed → generic → generic healthy → bare → bare healthy — and draws back-mounts
  (`bow_<tier>_back`/`sling_<tier>_back` → `sockets.back`) behind `legL`. None of that draw code
  exists yet. The earlier mis-built per-figure cross-product set (1344 files under `sprites/body/`)
  is REMOVED and never reached the repo.
- **Composition question (THEIR side, §7/§17 #15):** worn parts are core-agnostic (race base body
  plan) — how they compose with per-core figure geometry (warden bulk, robe silhouettes) is the
  engine/morph question; art convention proceeds regardless.
- **BINDING-SPEC DOC GAP (B2-GO readiness audit, 2026-07-05):** the `back` socket + the ranged
  back-mount layer are BUILT (8 `*_back` PNGs; `sockets.back` on all 41 figures; manifest+mgcb) and
  the *render* is flagged above — but the AUTHORITATIVE repo specs never DEFINE them, so "the engine
  has a spec to build against" isn't true yet. Next PR must add: (a) `back` to LAYOUT_CONTRACT §1's
  socket list/example (generator uses `[mid, tTop+2]` / robe `[mid, rTop+1]`); (b) the back-mount
  layer to §2 gear-mounting (`bow_<tier>_back`/`sling_<tier>_back` mount pivot→`sockets.back`, drawn
  behind `legL` when melee hands are full); (c) resolve DESIGN_SPEC §6e/§17 #22 from "assumed NOT
  drawn until a back-mount layer exists" → "draw at `sockets.back` while melee hands are full" (Doug's
  call). No art change — spec sync only. Full trace: `B2-GO_READINESS.md` §GAPS G1.
- **Status:** OPEN until the engine implements worn-part selection + back-mount render, AND the
  binding specs above define the `back` socket / back-mount layer.

### 27. Design locks from engine reconcile (2026-07-03 pm) — remember before re-rendering
- design/05 (NewGame) **stat blocks are NOT canon** — Doug wants a live tuning session first. If a
  future 05 re-render can read stats from a handed sample set, ask him for the tuned numbers then.
  (The Core Effect roster from 05 v2 was canon as of 2026-07-03 — now SUPERSEDED for the live screens
  by the PROTOTYPE relay in #33; Called Shot is replaced by Fletcher's Luck.)
- §13 aspect-fill is being built engine-side (letterbox dies) — no CD action.
- Status: OPEN as a standing reminder; fold into DESIGN_SPEC knowledge once the tuning session happens.

### 4. No GENERIC container overflow / scroll / virtualization model
- **What:** per-screen pagination SHIPPED (Merchant 3-per-page, Equipment inventory per-tab PER_PAGE=6)
  and pip-row WRAP shipped (ShieldPool pips instance + wrap on width). What's still missing is a GENERIC
  manifest concept for overflow — no `PER_PAGE`/scroll/virtualization FIELD a container can declare; each
  paged region is bespoke engine code today. Fine until a new list needs paging with no engine support.
- **Status:** OPEN — only the generic/declarative overflow model remains; the concrete cases are done.

### 10. Chrome/backgrounds vs. the ART_RULES hi-fi target — ONGOING elevation pass (CD-side)
- **What:** screen CHROME and BACKGROUNDS trail the figures' fidelity. Backgrounds stay PROCEDURAL
  (Doug's call — no hand-painted scene art); all five backdrops incl. `bg/merchant_stall` are
  procedural stand-ins, `status: placeholder` in ASSET_MANIFEST by design.
- **Status:** OPEN — CD's standing art priority; not an engine ask.

### 30. `glow`/`pulse` — ONE fixed-tick pulse primitive; design spec SHIPPED (2026-07-04), engine draw pending
- **What:** LOCKED by Doug (2026-07-04, "yes it's that glow"): one fixed-tick pulse primitive drives
  both flags — `pulse: true` (actionCard.targeting) breathes the state BORDER colour's alpha;
  `glow: true` (cityNode/beaconNode `current`) breathes an OUTER amber ring + halo. Design side now
  SHIPPED: `style_tokens.js` `pulse` block (periodMs 1800, easeInOut, fixedTick clock; border alpha
  .45→1; glow ring 1.5dpx alpha .4→.73 + halo blur 11dpx alpha 0→.33; colour = the state's border
  token), mirrored into `Content/layout.json.style.pulse` and added to `RB_styleBlock`
  (screen_extract.js) so future extracts carry it. Third mode added same day (Doug): `pulse: "self"`
  breathes the WHOLE element's alpha (`style.pulse.self`, .45→1) — used by `spineCity.current`, whose
  amber marker box was dropped (neutral borderDim chrome; the chip icon pulses instead).
  CSS mocks unified on the reference feel
  (1.8s ease-in-out): CityMap's went green/1.6s→amber, keeps the node bevel inside the keyframes,
  and the current node's border now reads amber per `beaconNode.current`; Encounter's TARGETING card
  border now breathes (`rb-pulse`) instead of authoring a dead `anim:'none'`; its stale unused gold
  `rb-glow` keyframes were removed.
- **Status:** OPEN until the engine implements the primitive — nothing engine-side draws
  `style.pulse` / `pulse` / `glow` yet. Ride the next drop-audit.

### 26. Rich inline text runs flatten to one colour (accepted, low priority)
- **What:** a text element with styled inline spans (citymap `supportNote`'s amber "2 resource
  holds") extracts as ONE text run in the wrapper's colour — the manifest has no per-span text
  styling. Engine reconcile (2026-07-03 pm residual #6) accepted the one-run caption; noting so the
  flattening isn't rediscovered as a bug.
- **Status:** OPEN (structural; revisit only if a screen needs an inline accent that actually matters).

