# Status

## ✅ BUILD-BREAKING BUG FIXED (2026-07-03, post-commit 57cc8a6): mgcb crashed on launch
`dotnet run` failed content build (MGCB exited -532462766 / 0xE0434352 — unhandled CLR exception,
not a normal build error). Root cause: the 57cc8a6 mgcb-mirror step wrote the 9 new entries
(8 technique icons + doom_stripe) into `Roguebane.Game/Content/Content.mgcb` (the copy the build
actually reads) with `\directive:` backslashes instead of MGCB's required `/directive:` — 81 lines,
that file only. The CD-source copy (`Roguebane.Content/Content.mgcb`) was fine. Fixed in place
(mechanical slash swap, no content change, verified by diff). **BUILD-VERIFIED (2026-07-03, loop):**
`dotnet build Roguebane.Game` green; all 9 new xnbs produced (technique icons + doom_stripe). If a
future drop mirrors mgcb entries again, verify the directive slash direction (`/`, not `\`) before commit.

## ✅ DROP APPLIED + COMMITTED (2026-07-03; applied by Cowork, committed 57cc8a6)
The `.drop/` staging is applied and deleted; the drop-commit first-pass directive is SATISFIED
(guards re-verified at commit: parse 6 screens, drop_audit 0 gaps, 313 tests green).
**What landed (all VERIFIED):** per-state chip labels (`states.<state>.label` — SELECT/LOCKED/
✓ CORE SET, CHOOSE/✓ CHOSEN); `core.badge` bound (engine adds the display datum: Grunt STARTER,
Warden BULWARK, Adept CASTER, Summoner/Reaver/Ranger SPECIALIST); **NEW SCHEMA `element.parts[]`**
(named value/label sub-parts with real fonts/px/margins on the loadout tiles + named chrome boxes);
`previewStage` panel (the purple figure backdrop); citymap gauge internals as real elements
(titles/counts/notes — retire the one-text-run stopgap header); `doomFillStripes` via NEW pattern
imageBind semantics (STATIC `ui/pattern/*` path = TILE the PNG across the element rect;
`ui/pattern/doom_stripe.png` shipped); skinned-button labels re-extracted (mono/7px/ground-dark);
uniform 94px resource chips (SUMMONS seats, nothing clips); `ShieldPool.count`+`ShieldPool.regenPct`
now in the manifest (wire the existing shell renders to them); 8 technique icons added
(bandage/block/cleave/drain/ember/jab/lunge/stoneskin — game mgcb MIRRORED, 558 entries, done by
Cowork); NewGame "1"/"2" step markers removed from headers (Doug's manual edit, intentional).
GUARDS RUN: layout.json parses (6 screens); `drop_audit.py` = 0 gaps, all screens clean; changed
refs (01/03/05) exactly 1920×1080; target_tag stays deleted.
**⚠ DROP REPAIR (flag, already done — do not redo):** the drop's layout.json had LOST
`screens.campaignmap` + `templates.cityNode` entirely (CD extraction miss; their audit claimed "04
untouched"). Cowork RESTORED both VERBATIM from the previous manifest (no invention; drop_audit
confirms the restored screen matches CampaignMap.dc.html: 9 els / 2 tpls). Logged to CD in the
payload — until CD's extractor includes campaignmap again, EVERY future drop must re-check the
key-set diff (screens/templates lost vs previous — add that diff to `drop_audit.py` as a standing
guard: it currently checks html→manifest, not manifest→previous-manifest).
**NEW ENGINE WORK from this drop (fold into the M1 newgame batch + adjacent screens; M0 STILL
FIRST):** `element.parts[]` draw support (schema model + contract test + renderer: parts carry the
text, never double-draw the element sample); per-state chip labels draw; pattern-tile imageBind;
`CoreRune.Badge` datum; gauge-header stopgap retire; shield count/regen manifest wires; verify
resource-strip seating; LAYOUT_CONTRACT fold now includes `parts[]`, `part`, `states.<state>.label`,
pattern imageBind. Canon core-effect copy in `CoreRunes.cs` is STILL pending (M1).
**PRIORITY ORDER post-drop (standing rule, CLAUDE.md §Working):** failing gates / measurement
integrity (M0) FIRST → drop-unblocked work (the list above + M1) → only then contingency/refactor.
A drop RESOLVING a blocker is authoritative here — don't re-verify what the re-arm block says
landed; spend the pass building on it.

## ✅ layout.json RESTORED (2026-07-01 pm) — the PNG-clobber is fixed by a clean re-drop
Valid JSON (5690 lines) carrying the newest CD work: `imageBind` (beaconNode → node icons as PNGs, incl.
skirmish, via `icons/node/{node.type}` — so "skirmish" isn't a literal), frame-v3 `repeat`+`centerFill`,
`cityNode`, the instrumentation. The BUILD GAP is CLOSED (2026-07-02): the three new icons are mirrored
into the GAME-side `Roguebane.Game/Content/Content.mgcb` (the one the build actually reads — CD's
`Roguebane.Content/Content.mgcb` copy is not wired; unify later) and skirmish renders on the map.

*Lean by design. SHIPPED-work history + rationale live in `git log` (detailed commits) and old STATUS
revisions (`git show <rev>:STATUS.md`) — recoverable, so not duplicated here. Locked design lives in
`design/DESIGN_SPEC.md`. This file = CURRENT state only. (Whittled 2026-07-01 from ~900 lines; nothing
current dropped.)*

## ✅ CD DROP LANDED (2026-07-03) — payload #11–18 delivered; REVIEWED (Cowork session), COMMITTED
Drop committed 2026-07-03 with the new drop-time guard: **`tools/drop_audit.py` BUILT (P0-A.7)** —
parses every `design/dchtml/*.dc.html` (data-el/-binds/-template/-tpl/-container/-bind-gate/-states/
-image-bind/-color-bind/-frames, text content, data-design÷2) and diffs that inventory against
layout.json; exit 1 on any extraction gap. First run: 6 screens audited, **2 real gaps found**
(encounter `ShieldPool.count` + `ShieldPool.regenPct` bound in html, absent from manifest — engine
already renders both; logged Needs-CD #4). Run it on every drop alongside the parse guard.
**GATE: GREEN (2026-07-03, post P0-C.2).** The post-drop 48-element BLANK class was the OLD z
convention overpainting them (equipment backdrop covered its whole screen) — the paint-ordinal
switch zeroed both the ELEM-BLANK and OCCLUDED classes; baselines re-pinned on the fixed render.
VERIFIED: all `design/NN` refs exactly **1920×1080** (#11);
targeting redesign complete in 01+08 (hotkey chips 1–6, number aim-tags w/ stacking, "no boxes ever
drawn", pulse assets `ui/reticle/focus_p0–p2` + AIMING=red cursor) (#12); merchant fits shelves w/ real
wareCard chrome + literal pager/LEAVE labels (#13); role chips per-core (#14); beginBtn/HELD labels ship
(#15); technique icons are imageBinds + `youAreHere`/doomTitle homed (#16); Elf blurb canon'd (#17);
**`design/dchtml/` landed (#18)** — all screens as .dc.html at native 1920×1080 with full extraction
markup (`data-el/-binds/-template/-container/-bind-gate/-states/-image-bind`, `sc-for`) + `proto/`
scripts + CD's own `DROP_AUDIT.md`; citymap homes ALL four ex-overlays (castlePanel+fortRows w/ per-part
states, campaignStrip, packChips, equipmentBtn); equipment gains backdrop + ✕ CLOSE; **z is now ONE
convention (paint ordinal, back→front; find the scene by its `*.scene` bind, not z==0)**; layout.json
structurally complete (9260 lines, new binds present — full parse = first loop guard).
**DROP GAPS:** (a)+(b)+(c)+(f) ~~mgcb mirror / target_tag delete / LAYOUT_CONTRACT fold / gitignore~~
ALL DONE (2026-07-03 — see P0-C.1); (d) ~~v4 frames 1:1~~ DONE (P0-C.3);
(e) `reference/screens/` never arrived (CD-side only — fine).

## ⇒ NEW LOCKS (2026-07-03 pm, Doug): core-effect roster = CANON (adopted from design/05 v2 → SPEC §5:
Called Shot renames Piercing Focus; copy may describe unbuilt mechanics until the effect-model pass —
flagged, acceptable; Summoner's stays the only BUILT effect). STAT BLOCKS in design/05 v2 are NOT
adopted — Doug wants a LIVE TUNING session; until then the roster keeps build values and the fidelity
diff MASKS stat-digit regions (tolerated placeholder zones). §13 ASPECT-FILL: BUILD IT (the old
letterbox scope-confirm is resolved — bg scale-to-cover, HUD anchored to real edges, no bars).
MERCHANT receiving LOCKED (§12): ALL wares click-to-buy tiles; technique→palette, minion→minion
inventory, rune→rune bag; slotting stays Equipment's; techniques/minions/runes become BUYABLE.

## ‼‼ HUMAN DIRECTIVES — 2026-07-03 LATE (COURSE-CORRECTION; WINS over everything below.
## Doug reviewed the live build vs design/05: newgame is visibly wrong yet marked "at floor".)

**M0 — MEASUREMENT INTEGRITY (the ruler got bent; fix it BEFORE any more fidelity passes):**
_M0.1–3 DONE (2026-07-03 late, same pass the directive landed): blur scoring REVERTED → unblurred
alignment search (±3px, shift REPORTED as a number in the ranked list); the illegal identity mask
DELETED; the newgame smoke now cycles the build to the ref's GRUNT state for its shot and restores
after (drive aligned, not masked). M0.5 DONE same day: SMOKE ASSETS probes every
authored image/imageBind path against the real content build (placeholders enumerate their domains;
unknown placeholders report unverifiable, never skip silent) — first run found the race.headImage
class exactly as Doug called it: `race.id` had NO ResolveBind case, so the head imageBind resolved
to an empty path and died silently. Fixed (+ part-image path normalize); HEADS NOW BLIT on both
race cards (eyeballed). Current probe truth: 6 technique icons missing (bandage/cleave/drain/ember/
jab/lunge — real Needs-CD), 2 placeholders unverifiable (legend.type/lot.id — domains to add).
M0.4 DONE same day: the smoke emits a
textgeom sidecar (element, drawn bbox, string, font family per text draw) and
`tools/geometry_diff.py <dc.html> <rects> <textgeom>` prints the numeric per-element table —
authored(source/2) vs manifest vs drawn: pos/size deltas, fontPx, family, string, span-FLATTENED
detection. First newgame table (9 findings) CONFIRMS Doug's list numerically: the four preview
tiles draw the VALUE only vs the authored value+label spans; coreHeader/raceCards/raceHeader flag
FLATTENED; all measured fontPx match. (beginBtn shows NO-TEXT-DRAWN — a recording gap: the styled
button draws via TextPx directly, not through the recorded path; extend recording with the M1 pass.)
Newgame floor claim stays REVOKED until the M1 re-walk._
1. **REVERT the blur-tolerant element scoring (4b6a705).** The 1.5px Gaussian before element scoring
   makes the tool blind to ±2–3 design-px errors — EXACTLY the current bug class (padding/margins/
   label offsets Doug can see by eye). Replace with an **ALIGNMENT SEARCH**: score each element crop
   at integer shifts within ±3px, take the best, and REPORT THE SHIFT as a position delta in the
   ranked list (a real offset becomes a NUMBER to fix, not noise to tolerate). AA tolerance comes
   from the alignment, not from blurring; if residual AA noise still floors small text, cap any
   smoothing at 0.5px AND print both blurred/unblurred scores. NEVER let a smoothed score be the
   one a "done/floor" claim cites.
2. **RULE (also added to loop.md): never modify the MEASUREMENT to improve a score.** Any change to
   scoring/masks/thresholds/drives that RAISES numbers requires a STATUS-logged human approval line
   BEFORE commit. Fix the render, not the ruler.
3. **MASK AUDIT:** the ONLY Doug-approved mask is the newgame stat-digit zones (tuning session
   pending). The b61bbb7 identity-block mask (previewName/Role/CoreEffect*) is ILLEGAL — the drive
   picked Summoner while the ref shows Grunt: **ALIGN THE DRIVE TO THE REF STATE** (pick Grunt) and
   DELETE that mask. Sweep for other unapproved masks.
4. **GEOMETRY DIFF (the real fix for text layout, immune to AA):** the smoke sidecar already carries
   rect/fontPx/borderW — extend it with each element's DRAWN TEXT BBOX + the string drawn; build
   `tools/geometry_diff.py` comparing per element against the dc.html AUTHORED geometry (box, font
   family+px, casing, alignment, sibling spacing — parse the source spans like drop_audit does).
   Output a NUMERIC per-element table: pos-delta, size-delta, font mismatch, missing-string. Pixels
   judge ART; geometry judges LAYOUT. Gate on geometry-clean before any screen is called done.
5. **ASSET-EXISTS PROBE:** every `imageBind`/static image path in manifest + dc.html must resolve to
   a GAME-mgcb texture; unresolved paths print per screen. FINDING to reverse: b61bbb7 tagged
   raceCards low score "Needs-CD head sprites" — WRONG: `sprites/body/human_grunt/head_healthy.png`
   EXISTS and the manifest authors `race.headImage` imageBind; the ENGINE isn't blitting it. Un-tag
   Needs-CD, fix the resolve. (This is the "missing assets not detected" class Doug called out.)
6. **NEWGAME "at floor" is REVOKED.** After M0.1–5, re-walk newgame with unblurred scores + the
   geometry table; the M1 list below is the starting bug set (visible by eye today).

**M1 — NEWGAME REAL BUGS (Doug's eyeball + manifest facts, 2026-07-03 late):**
- Canon core-effect COPY never landed in data: `CoreRunes.cs` still ships "Piercing Focus" + the old
  descs — update CoreEffectName/Desc to the §5 CANON roster (pure data, quick; also shrinks the
  drive-vs-ref content divergence class).
- `race.headImage` imageBind unresolved (M0.5) — heads render as empty boxes.
- TILE value+label layout broken on all three columns — and in the LOADOUT column the labels are
  MISSING OUTRIGHT: every box (STR/INT/DEX/CON, BASE HP, RUNE BUDGET, ACTIONS, MINION BAYS) renders
  a bare corner number and nothing else (Doug, 2026-07-03 late). Mechanism: dc.html authors VALUE
  (mono, larger) over LABEL (mono 8.5px, +4px margin) as separate spans; the manifest flattened the
  tile to ONE text ("20 RUNE BUDGET") and the engine draws that run at one size in the DISPLAY face
  (serif "1" reads as "I") — it overflows the tile and everything past the value CLIPS AWAY. Engine
  meanwhile: split value/label from the flattened run ('\n'-aware path), render MONO per source,
  centred, label under value. Manifest fix queued (payload A3, proper two-part tiles). Same class on
  core-card BUDGET/ACTIONS/BAYS + race-card stat boxes (labels clip under values).
  **DETECTOR GAP this exposes: a drawn string whose visible portion ≠ the full string (truncated /
  clipped outside its rect) must FAIL the collision/overflow detector — "label missing from every
  box on the screen" cannot score 77+ again.**
- STATE CHIPS missing: unchosen race shows no CHOOSE chip; unchosen cores show no SELECT/LOCKED
  button (dc.html computes selLabel per state; extraction only captured the chosen sample —
  payload addendum). Shell-side flagged stopgap OK meanwhile (input already exists).
- roleChip stamps "STARTER" on all six cores — bindless sample in the manifest (BULWARK/CASTER/
  SPECIALIST appear ZERO times in layout.json; the PNG shows them but extraction dropped the datum).
  Payload addendum: bind `core.badge`. Engine: add the display datum when it lands.
- previewFigure's purple backdrop panel doesn't draw (design/05 shows it; check the element's fill).
- **previewFigure MISSING PARTS per core (Doug, 2026-07-03 late):** Warden renders EMPTY OUTLINE
  BOXES for arms+legs; Elf Ranger likewise (partial). NOT an art gap — `sprites/body/human_warden/`
  has all 21 part PNGs, the game mgcb lists them (84 entries), the figure def is in layout.json.
  It's an engine part-resolve/compose failure for non-grunt figures (state-key or part-name lookup
  misses → the slot rect draws as an outline box, which the paint-coverage gate counts as painted —
  invisible to it). BUILD THE PROBE with M0.5: for EVERY figure def, attempt-resolve every z-list
  part texture at every state, headless; report failures per figure. Then fix the lookup. Check the
  same failure on the Equipment paper-doll + encounter figures (same compose path).
- CORE EFFECT block spacing (eyebrow/name/desc run together, desc to card edge) — geometry-diff
  will quantify; fix paddings per source.
**Fix M1 as ONE batched newgame pass (same-class items share causes), verified by the M0 tools.**
_M1 BATCH LANDED (2026-07-03 late): canon §5 core-effect copy in CoreRunes.cs (all six, Called Shot
renamed); preview tiles draw VALUE-over-LABEL mono centred per source (geometry STRING findings
gone); unchosen cards wear FLAGGED-stopgap CHOOSE/SELECT chips (dim border, muted label — per-state
labels remain payload A2); styled/skinned button labels now RECORD textgeom (beginBtn
NO-TEXT-DRAWN cleared). geometry_diff newgame findings 9→4; the rest are CD-side: 3 FLATTENED
span flags (A3) + the coreCards container heuristic. previewFigure backdrop CONFIRMED an
extraction gap (element fill=null in layout.json — A4 stands). roleChip awaits the core.badge
bind (A1). Core-effect block spacing: geometry shows ≤3px shifts — re-judge after the A3 tile
re-extraction lands._

**M2 — WORKLOAD BATCHING (Doug: more per pass):** a fidelity pass = one SCREEN, but fix the WHOLE
ranked geometry-diff table for that screen in the pass — same-class deltas (font/pad/label) share a
cause; batch them, one commit. The gate prints the ranked list + geometry table at pass END so the
NEXT pass starts with a plan instead of re-discovery.

**M3 — BASELINE RATCHET PLAN (the path to baselines that mean something):** (1) NO re-pin until M0
lands — pinning now freezes the lies. (2) After M0: ONE `--update` — scores will DROP (newgame's
78.7 was blur+mask-flattered); that pin is the first honest floor. (3) Each batched screen pass ends
with `--update` — the baseline only climbs. (4) Once M1 lands, overflow/collision baselines flip
from "may not rise" to **pinned ZERO, absolute** — the grandfathering that let loadout clipping pass
is abolished; masks shrink to the stat digits only (dies at the tuning session). (5) A screen
graduates from regression-floor to ABSOLUTE bar when geometry-diff is clean: done = geometry clean +
UNBLURRED fidelity ≥ the agreed threshold + every residual enumerated and tagged CD/art/state.

## ⇒ HUMAN DIRECTIVES — 2026-07-03 (P0 — do this block TOP-DOWN before anything else)

**‼ P0-A — PIXEL-TRUTH SYSTEM (root cause found: measurement itself was broken; build FIRST — it
multiplies every later pass):** every `design/NN-*.png` is **924×540** but design space is **960×540**
(aspect 1.711 vs 1.778), and `fidelity_diff.py` LANCZOS-stretches BOTH images to 960×540 — so every
score ever taken compared against a reference warped ~4% horizontally (≈18 px mid-screen), with the
live window's letterbox bars (2560×1528 client = 1.675) baked into the shot, all at a 1× 540p working
res where border weight + text metrics are unmeasurable. Prior "walked clean / zero renderer deltas"
claims were made against warped refs — treat them as UNVERIFIED; re-walk with v2. Build, in order:
1. **Reference contract + guard — ✅ BUILT (2026-07-03):** `ui_gate.py` step 0 hard-fails any
   `design/NN-*.png` screen ref not exactly 960K×540K (`00-assets-*` sheets exempt — not screens).
   Baseline re-pin still WAITS on v2 (step 3) + the newgame stat-digit MASK (fold into v2's
   per-element crops — see NEW LOCKS).
2. **Render-at-reference-resolution — ✅ BUILT (2026-07-03):** `RB_SIZE=WxH` pins the backbuffer
   (scene aspect-fits to exact ref res, shots save the scene = no letterbox/chrome); the gate runs
   all smokes at `RB_SIZE=1920x1080`; `fidelity_diff` now compares SAME-SIZE pairs 1:1 (zero
   resample, mode-tagged in output; mismatched sizes keep the legacy 960×540 fallback). First 1:1
   numbers (informational, not pinned): enc 80.5 eq 80.4 city 84.7 camp 94.7 ng 77.5 mer 83.0.
3. **fidelity_diff v2 — ✅ BUILT (2026-07-03):** the smoke emits a rects SIDECAR per shot (every
   element's resolved design rect via ScreenLayout — no anchor math duplicated tool-side);
   `--elements` scores each element's crop → RANKED worst-first list (id, score, design rect);
   `--mask` neutralizes tolerated placeholder zones (newgame stat tiles/rows masked in the gate —
   race/core-card digits live INSIDE templates, accepted depression until per-part masking is
   warranted). Tile grid stays as the whole-frame score + heatmap visual. **BASELINES RE-PINNED**
   on v2 numbers (1:1 + mask): binds enc 16 eq 22 camp 4 ng 12 city 9 mer 10; fidelity enc 80.5
   eq 80.4 city 84.7 camp 94.7 ng 78.1 mer 83.0. The dead-baseline warning is CLEARED.
4. **Numeric probes — ✅ BUILT (2026-07-03):** `tools/probes.py <shot> <rects.sidecar>` measures the
   SHOT's pixels vs the manifest numbers: TEXT-HEIGHT (ink bbox in each text element ÷ authored
   fontPx; skinned/image elements excluded via sidecar-v2 flags) + BORDER-STROKE (edge-midpoint ink
   run vs authored w). FIRST NUMBERS: borders ALL 1.00× (the BorderPx fix verified by measurement);
   labels 1.0–1.1×; worst text 1.4× (part labels — serif ascender span vs fontPx), multi-line descs
   read as line-count multiples (probe caveat). **Doug's "1.5–2× oversized" is NOT present in the
   current build's numbers** — if it still LOOKS big live, re-shot through the probe (stale-build
   suspicion, same as the 07-02 font case). Standalone tool; run on gate shots ad hoc.
5. **Collision/overflow detector — ✅ BUILT (2026-07-03):** the smoke's full render records every
   drawn text footprint (element-owned); `SMOKE TEXTGEOM` reports per screen OVERFLOW (bbox outside
   its element rect ±2px) and COLLIDE (footprints of two non-nesting elements intersect); the gate
   BASELINES both counts and fails any RISE. First measurement (baselined): overflows enc 11 eq 10
   city 4 camp 2 ng 6 mer 7-10; collisions eq 3 camp 1 ng 2 — catching the known family
   (heroShield=SHIELD-over-bar, currentCoreName×currentCoreRole=doubled identity, eyebrow×title).
   Walk the lists down as slices; counts may only fall. FIRST BURN-DOWN (same day): the doubled
   identity text DIED ("core" block resolves null — the panel header was printing the name a 2nd
   time; eq binds 23→22, correct) and SHIELD-over-bar DIED (bound-panel headers now confine to the
   band ABOVE their contained children + height-fit their font; enc overflows 11→9, heroShield/
   foeHp clean). Both were Doug's live-screenshot bugs (P0-C.10 items 4+5).
6. **Hand-shot normalizer — ✅ BUILT (2026-07-03):** `tools/normalize_shot.py <shot> [--trim-top N]
   [--client x,y,w,h] [--design ref.png]` — auto-trims uniform margins to the client rect (an OS
   title bar needs --trim-top; a warning fires on non-16:9 clients since §13 pins HUD to real
   edges), rescales to 1920×1080, optionally chains fidelity_diff. ROUND-TRIP VALIDATED: a
   synthesized window shot (scene scaled to 2560×1440 + title bar + desktop margin) recovers the
   client exactly and scores 77.7 vs the gate's own 77.8 on the same build. **P0-A IS COMPLETE.**
7. **DROP AUDIT — ✅ BUILT (2026-07-03):** `tools/drop_audit.py` parses each screen's dc.html and
   diffs its inventory against `layout.json`; exit 1 on gaps. First run caught 2 (ShieldPool.count/
   regenPct — Needs-CD #4). dc.html stays READ-ONLY CD source (never edit/"fix" it; PNG refs stay
   the pixel bar). Run on every drop alongside the parse guard. Optional backstop still open:
   pinned headless capture at 960K×540K.
Wire ALL of it into `tools/ui_gate.py` (stays the ONE command); run every pass per loop.md.

**‼ P0-B — TARGETING build [design LANDED (01+08 v2); DESIGN_SPEC §8] — CORE DONE (2026-07-03):**
- ~~boxes~~ DELETED: whole-foe hover border, hovered-part border, band strips, no-frame fallback box
  — zero box affordances (band strips remain the CLICK hit-test, undrawn).
- ~~Cursor is reticle~~ DONE: OS cursor hides while a technique targets (IsMouseVisible in Update);
  red `aiming` rides the cursor, centring on the hovered limb band; right-click cancel already wired.
- ~~Locked mount~~ DONE: manifest `frames` (Element.Frames parses, schema-tested) cycle focus_p0→p1→p2
  on the fixed `_animTick` (render-only counter); size = part-rect larger side ×1.5 clamped 64–136
  scene px; while ANOTHER module is picking, kept targets draw the faint SECONDARY (0.55 alpha).
  Static foeReticle element gates off (bound icons suppress when their bind is unresolved — its
  authored image is a mock-position stand-in; the mock box is gone from the shot, eyeballed).
- ~~AIM TAG stack~~ DONE (2026-07-03): aims group per part; each part's reticle wears a centred tag
  ROW above it — one `templates.aimTag` chip per kept module, reading its HOTKEY number (eyeballed:
  "1" above the locked head). Static foeAimTags element stays gated (mock position).
- ~~hotkey chips~~ DONE (2026-07-03): `technique.hotkey`/`bay.hotkey` parts resolve positionally —
  techniques 1..T then bays T+1.. (the D1–D6 order); eyeballed 1–4 on the castle drive's cards.
- **P0-B residual:** pulse cycling + hidden cursor are Update-side — verify LIVE (headless smoke is
  single-frame); bay-lane keys beyond techniques don't PRESS yet (chips number them; input still
  technique-only — wire when bay pressing is designed). Flag any repro to the targeting-FSM debt line.

**‼ P0-C — POST-DROP ENGINE QUEUE (the 07-03 walk's CD-tagged items LANDED in the drop; what remains
is ALL engine-side). Fix with P0-A numbers (before/after per-element scores):**
1. ~~Drop wiring, mechanical~~ **✅ DONE (2026-07-03):** parse guard = drop_audit + contract tests
   (P0-A.7); game mgcb mirrors focus_p0/p1/p2 (asset probe pins them, 14/14), target_tag entry +
   PNG deleted (CD's mgcb was already clean; zero code refs); scratch .gitignore predated the drop;
   DROP_AUDIT schema notes folded into LAYOUT_CONTRACT §11 (ref-export contract) + NEW §12
   (z=paint-ordinal, frames animation, bind-gate, data-part, item.pad); CLAUDE.md line arrived
   WITH the drop.
2. ~~z paint-ordinal switch~~ **✅ DONE (2026-07-03) — GATE WENT GREEN.** Renderer draws ascending z
   (one convention); scene layer found by `*.scene` bind (`IsSceneElement`); smoke leave-one-out +
   backdrop baseline follow the bind, not z==0. BIG un-break: the old descending order painted
   equipment's `backdrop` (z=1) over nearly the whole screen — ALL 48 ELEM-BLANK elements and ALL 6
   OCCLUDED elements were overpaint casualties, and both classes went to ZERO with the switch
   (beginBtn/heldBadge/titles/buttons all render now; attrPool divider un-broke as predicted).
   Fidelity re-pinned (enc 77.7 eq 76.5 city 85.3 camp 94.7 ng 77.0 mer 82.0) — enc/eq DIPPED vs
   the pre-switch pins because those pins scored backdrop-covered blanks whose dark fill
   coincidentally matched the ref (metric artifact, visually verified: full content now renders).
3. ~~v4 frames 1:1~~ **✅ DONE (2026-07-03):** `DrawFrameTex` scales by `1/SS` (not the fixed
   `1/ChromeBake`) — border-image-width == slice in SCENE px at ANY scene scale (identical at SS=2,
   native-crisp above); NineSlice tile STEPS now carry dstCornerScale too so edges tile at scaled
   native density (Core-tested, 305). Button skins verified: still ChromeBake=2 art (unchanged in
   the drop), their path keeps 1/ChromeBake.
4. **New template families:** ~~heroHpPip/foeHpPip~~ DONE (2026-07-03: segmented HP strips live —
   one point.live pip per max-HP point through the shieldPip leaf path, + hpLabel eyebrows
   ("HUMAN SUMMONER - HP", foe id + n/m; ASCII dash, font lacks U+00B7); encounter binds 16→20);
   ~~poolPip/attrPip/supplyPip/supportPip/healPip~~ DONE (2026-07-03: leaf templates carry
   `imageBind` — textured `ui/pip/*` PNGs replace fill chrome; template parts carry a NESTED
   `list` (PlacedPart.List) so poolRow/attrBar stamp per-stat full/reserved cells from the row
   datum; supplies.points/support.points resolve live [city binds 12]; healPips rides
   Body.hp.points; eyeballed: design/01 pool strips exact); ~~`data-bind-gate`~~ DONE
   (2026-07-03: content+binds = literal gated by the bind, WHOLE element suppresses on a closed gate
   — heldBadge only shows paused; nav.close/nav.equipment/begin resolver gates added, binds eq 23
   city 10 ng 13; smoke classifies gated literals as legit-silent); ~~`item.pad`~~ DONE (2026-07-03:
   [T,R,B,L] container inner padding in ListLayout, Core-tested 306 — citymap legend + equipment
   invTabs consume it); `frames` animation cycling (fixed tick) — OPEN, with the P0-B reticle.
5. **New binds resolve live:** ~~technique.hotkey/bay.hotkey~~ ~~targeting.tags~~ ~~Body.hp.points/
   hpLabel + foe.hp.points~~ ~~pool.attr.cells/attrs.cells~~ ~~supplies.points/support.points~~
   ~~Body.gear rows~~ (packChips live: Dagger/Plate chips + stat swatches, city binds 13)
   ~~nav.equipment/nav.close~~ ~~icons/technique/{id} + {loadout.id} imageBinds~~ (Brace blits its
   PNG; missing icon ids fall back to the tinted tile, logged Needs-CD #4) — ALL DONE 2026-07-03.
   ~~map.current~~ ("YOU ARE HERE"; U+25BC not in font) ~~campaign.cities/city.status~~ (spineCity
   chips taken/current/future via template ROOT chrome + states — a parts-template now styles its
   own cell; pickerCard family follows the chosen index: selected=amber ring, idle=dim, which also
   landed the race/core card chrome) ~~city.castle.parts~~ (fortRows read the castle foe's parts +
   INTACT/DAMAGED/BROKEN from live contributions — rows exist only while the castle encounter is
   live; NO persistent fort-damage model invented, §17 open) — DONE 2026-07-03, city binds 15–16/17.
   REMAINING: castlePanel container (resolves when its own datum model lands); scene backdrops
   verify (equipment's renders).
6. **P0-B targeting build** (block above).
7. ~~Merchant buys complete~~ **✅ DONE (2026-07-03, per the click-to-buy LOCK):** Stash gains
   Techniques/Minions/Marks inventories; `BuyTechnique/BuyMinion/BuyMark` mirror the gear buys
   (placeholder prices flagged to the economy tune); ALL five ware categories show price + BUY and
   click-to-buy dispatches by item type; bought techniques/minions join the Equipment tabs' pools
   (slotting stays Equipment's). Core-tested end-to-end (312: buy per category on deterministic
   stocking nodes + broke rejection). BONUS un-break: ListLayout linear flows now CLIP overflowing
   cells (26 HP pips were spilling out of healPips' 12-pip strip across the shelves) — merchant
   fidelity 81.6→82.7. NEEDS-HUMAN (new): how the RUNE BAG displays/uses a BOUGHT Mark — the bag
   renders ladder groups (climb-by-budget); a purchased Mark sits in Stash.Marks with no display
   home yet. Decide: bought rung auto-owns? shows as a bag row? (§17 rune taxonomy adjacent).
8. ~~§13 aspect-fill~~ **✅ DONE (2026-07-03):** scene target = the FULL backbuffer (blit 1:1 at
   origin, letterbox clear gone); design space EXTENDS on the loose axis (`ManifestUi.DesignW/H`,
   set on resize) so anchors pin to REAL window edges; `*.scene` backdrops SCALE-TO-COVER the
   extended space (source-cropped to the viewport aspect, nothing stretches). 16:9 resolves to the
   authored 960×540 exactly (gate byte-stable); verified 1600×1000 eyeball: zero bars, full-width
   status strip, buttons at true top-right, footer at true bottom. Legacy fixed-coord overlays
   (cleared/lost) still center on 960×540 — cosmetic on exotic aspects, noted.
9. **Legacy deletions — citymap HALF DONE (2026-07-03):** DrawSpine/DrawCastlePanel/DrawGearBar/
   EQUIPMENT[E] button + PackChipRect/PackItem/PackCount/EquipOpenRect DELETED — the manifest
   ex-overlays render live and INPUT rides their geometry (equipmentBtn click via nav.equipment;
   pack click-to-equip via the packChips cells, wielded chips no-op). Game1.CityMap.cs 119→33
   lines. ~~encounter card chrome~~ DONE (2026-07-03): techCards wear the actionCard state family —
   the frame reads the live FSM (targeting=str pulse-color, held=gold, ready=good, cooldown=dim,
   DRY/unpowered=locked); the mock damage-highlight digit (`technique.amount`, positioned for the
   mock's wrap) resolves EMPTY (the live description carries {power}); `technique.attr` resolves
   live (Ember reads INT, not the STR sample). **THE WHOLE P0 QUEUE (A/B/C) IS COMPLETE.**
10. **Engine-bug residue from the 07-03 walk:** ~~RUNE BAG regression~~ FIXED (2026-07-03: the drop
   restructured runeGroup into a bound header [runeGroups.type → live PATH name; taxonomy stays
   OPEN §17] + a NESTED runeCard list [g.runes] — the engine still ran the old flat-row path.
   Nested stamping wired through RuneRow/RuneBind; eyeballed: VESSEL/RESONANCE groups with rung
   names, effect lines, EQUIPPABLE/LOCKED state colors, discounted costs. One new minor overflow
   from live text on tight authored rects rode the fix — baselined);
   ~~doubled identity~~ ~~SHIELD label-over-bar~~ FIXED (see P0-A.5 burn-down);
   ~~CASTLE FORTIFICATIONS run-on~~ RETIRED (fortRows live);
   ~~node label oversize + chart's doubled "you are here"~~ FIXED (2026-07-03: hand label deleted —
   the youAreHere ELEMENT carries it; option labels draw at the design's 5.5px caption size instead
   of base-size Text; ring tightened to +2. citymap 85.5→86.3);
   ~~resource-strip~~ MEASURED: the region (~197px) can't seat 4 authored chips — engine clips
   (SUMMONS drops on encounter), extents logged Needs-CD #5. RESIDUE COMPLETE — remaining overflow
   burn-down items are the measured logo/title-width class (CD-authored tight rects, baselined).
**⇒ P0 QUEUE COMPLETE (2026-07-03, ~20 loop passes):** A.1–7 (pixel-truth system), B (targeting),
C.1–10 (drop wiring → z → frames → templates/binds → merchant buys → aspect-fill → legacy deletions
→ residue). Gate green. NEXT ARC: per-screen pixel-walks toward the design bar using the new
tooling (ranked per-element lists + probes + overflow burn-down), and the parked Needs-human/
Needs-CD items as they unblock.

## ✅ CD DROP LANDED (2026-07-02 pm) — verified + committed
layout.json (+4786 lines, parses, all 5 smokes green): NEW `merchant` screen; `states` (button skin
families + template state styles), `border.sides` (per-side borders), `colorBind` (element + part),
`shieldPip` self-styled leaf template (pips + state styles); `apex*` binds GONE from the manifest (the
rename completed CD-side). Assets: `icons/resource/summons.png` (the missing Summons icon),
`bg/merchant_stall.png` — both mirrored into the GAME-side mgcb. Designs: refreshed 01–05, NEW
`design/07-merchant.png` + `design/08-reticle-mounts.png` (the how-it-mounts reference). Core schema
model + contract tests updated (self-styled leaf templates + static imageBind paths are legal).
ENGINE TODO (unblocked, in order): ~~`states` draw~~ DONE (button-family skins nine-slice under the
label; disabled/on/down/hover/normal picked by bind + pointer; route-style states on chart/cityGraph are
style HINTS the graph drawers already cover semantically — revisit only on pixel-compare; merchant
buttons light up with the consumer), ~~`border.sides` draw~~ DONE (element + template-part
borders honour named edges + manifest width; contract test pins side names), ~~`colorBind` resolve~~
DONE (part fills tint from the bound datum's stat — technique/loadout/inv/bay — and element borders
take a core accent; `CoreRune.Accent` is the data hook, per-core VALUES await design [Needs-human];
ware.* resolves with the merchant consumer), ~~shield-pip instancing + regen fill~~ DONE (variable-N
pips via the self-styled shieldPip leaf template, live/spent states, SHIELD n/m header, regen track
fills toward the next pip from Body.ShieldRegenProgress — Core-tested; dashed spent-borders draw solid
pending the pixel pass), ~~summons icon~~ DONE, merchant-screen consumer PART 1 DONE (2026-07-02):
the design/07 stall renders FROM THE MANIFEST at merchant nodes — arrival opens it, LEAVE/Esc returns to
the map; stall backdrop via the new *.scene handler; heal offers + provision lots are live-priced list
rows with row-click + keyboard buys; run.resources strip is manifest data; PURSE readout. Part 2 DONE
(2026-07-02): WARES shelves render (nested wareCard stamping inside each shopSection's region, geometry
shared with the click hit-test), pager pages 3 sections at a time, weapons/armor BUY off the shelf into
the stash (techniques/minions/runes display un-buyable — receiving models stay design-open; their tag/
buy slots suppress rather than show mock samples), and the CITYMAP MERCHANT POPOVER STOPGAP IS RETIRED
(H re-opens the stall on the node). The whole engine-TODO queue from the 07-02 drop is now CLEAR —
remaining merchant work is design-gated (ware pricing/rarity models, pixel-compare vs design/07).

## ⇒ HUMAN DIRECTIVES — 2026-07-02 (revisions WIN; fold into the render arc / after the current slice)

**‼ P0 (2026-07-02 pm) — regressions/bugs first:**
- ~~COMBAT SCREEN REGRESSION~~ **FIXED (2026-07-02):** root cause = the CD drop authors full-screen
  `*.scene` backdrops at **z=0 meaning BACKMOST**, but the renderer's depth ordering (high z first)
  painted z=0 LAST — the backdrop covered every element on encounter/merchant/campaignmap (all three
  z=0-backdrop screens were blank; merchant/campaignmap had silently regressed too). Fix: z=0 draws
  as the scene layer behind everything; depth ordering unchanged above it. GUARD BUILT: `RB_SMOKE=1
  RB_MF=all` renders EVERY manifest screen headless, diffs each against its backdrop-only baseline,
  prints per-screen painted-% and **exits non-zero if any screen is backdrop-blank** (asset probes
  couldn't see this — the blank build reported probes 13/13). Run it every pass. All 6 screens
  currently paint 8.8–74.5%. NOTE Needs-CD: manifest z now mixes two conventions (z=0 backmost vs
  depth-descending panels z=6 over leaf z=1) — normalize in a future drop; renderer handles both.
- ~~SAMPLE-over-LIVE text bug~~ **FIXED (2026-07-02):** not the fallback logic (`?? sample` was already
  fallback-only) — the resolver mapped `core.coreEffect`/`preview.coreEffect` (the bordered BLOCK
  containers; label/name/desc are their own elements/parts) to CoreEffectDesc, painting the desc a
  SECOND time across the block. Blocks now resolve to nothing (chrome + sample eyebrow only). Bonus:
  the Equipment identity block's `core.role`/`core.coreEffectName`/`core.coreEffectDesc` screen binds
  never got resolver mappings (elements landed in the 07-02 CD drop) — mapped to the build's core
  (fixed for the run), so design/02's bottom-left block reads live. Verified RB_MF=all + eyeball:
  no doubled copy on NewGame cards or Equipment; identity block live. Residual bindless mock text
  ("gear 4" rows on Equipment = coreStats block extraction gap) tracked in Needs-CD.
- ~~FONT SWAP~~ **DONE (2026-07-02):** IM Fell English Regular (display) + JetBrains Mono Regular (mono)
  BUNDLED — TTFs from the official google/fonts + JetBrains repos (OFL texts alongside) live in
  `Roguebane.Content/fonts/` (CD source of truth) mirrored to `Roguebane.Game/Content/fonts/` (what the
  build reads); both `.spritefont`s point at the TTF paths (char regions were already widened ①②③✓✚◉).
  Content build green, RB_MF=all green, ✓ chips render real glyphs, card copy reads in the design serif.
  Georgia/Consolas gone. (Old "awaits download approval" gate: superseded by this P0's "do it now".)
- ~~RENDER AT NATIVE RES~~ **DONE (2026-07-02):** the scene RenderTarget is now sized to the NATIVE
  backbuffer aspect-fit every frame (recreated on fullscreen/resize; `EnsureSceneMatchesBackbuffer`)
  and blits 1:1 — the fixed 1080 cap + soft upscale are gone. `SS` became the float design→scene scale;
  the two densities it conflated are split out: `FontBake=3` (SpriteFonts rebuilt at 3x design px —
  display 60 / mono 42 — so text stays a DOWNSCALE up to ~1620p-class scenes) and `ChromeBake=2` (the
  2x-painted button/frame skins; nine-slice corners keep their shipped proportion). Border weights
  pinned to the shipped look (`BorderPx`: authored w=1 → 2 design px) independent of scene scale.
  Verified RB_MF=all at a 1600×900 backbuffer → 1600×900 shots, all screens paint, layout identical.
  Pixel-art figures still scale through the common path (nearest-neighbor via PointClamp).
- ~~LETTERBOX vs §13 aspect-fill~~ **SCOPE CONFIRMED (Doug 2026-07-03): BUILD §13 fill** — folded into
  the P0-C queue (#8). Bars die; bg scale-to-cover; HUD anchors to real edges.
- **RENDER-ACCURACY FLOOR (drive this BEFORE ever claiming "starved"):** a LOT matches the design better
  WITHOUT CD or systems — PURGE the outdated box/frame TEXTURE (old chrome still in use); apply the
  manifest v3 frames/chrome to every panel; fix the box treatments. Stream-1 fidelity isn't at its floor
  yet; get it there first. FIRST CUT DONE (2026-07-02): the legacy `Panel()` stretch-blit path is gone —
  legacy panels (live citymap chrome, button fallback) now draw through the SAME manifest `style.frames`
  (v3 tile + centerFill) the element renderer uses; `AssetRegistry.Frame` + the local stretch nine-slice
  deleted. Verified live-citymap smoke: THE CASTLE panel wears the tiled hazard frame. REMAINING for the
  floor: per-screen pixel-walks vs design PNGs to find leftover wrong box treatments (fold into the
  SYSTEMIC fidelity-diff work below).
- ~~OVERSIZED frames/borders/corner-bolts~~ **FIXED (2026-07-02):** `DrawFrameTex` now nine-slices with
  the same `dstCornerScale: 1/ChromeBake` the button path uses — every manifest frame + the legacy
  Panel route through it, so panel/card corners render at native proportion (citymap gauges/legend
  visibly slimmed to design weight). Gate green.
- ~~`e` doesn't exit Equipment~~ **FIXED (2026-07-02):** E toggles — the key that opens Equipment now
  closes it (alongside Esc), returning to the caller.
- ~~WATCH — Windows DPI scaling~~ **CLOSED (2026-07-02, no code change):** the game's `app.manifest`
  already declares per-monitor-v2 DPI awareness (MonoGame template), so fullscreen gets TRUE native
  pixels (2560×1600 confirmed via Win32_VideoController). Doug's 1707×1067 reading came from his
  PowerShell probe, which is itself a DPI-UNAWARE process seeing the 150%-scaled desktop — the probe
  lies, the game doesn't. If fullscreen still looks soft on a FRESH build, reopen with a screenshot.

**‼ SYSTEMIC — build UI VALIDATION / proof-of-correctness (the ROOT CAUSE of "starved before pixel-perfect"):**
the loop has NO deterministic way to know how well a screen matches its design PNG — so it can't measure
the remaining gap, can't justify "done" or "starved," and regressions slip (combat 95%-blank; the font
bug). The manifest gives correct LAYOUT/DATA, NOT visual correctness (not a silver bullet for a visual
game). Build, in order:
- **Coverage + content validation (deterministic, headless, ALL 5 screens every pass):** assert EVERY
  manifest element renders NON-BLANK at its expected rect, and each BOUND element shows LIVE data (not its
  `sample`). This alone catches the combat regression + empty/overlapping labels the instant they happen.
  BIND-RESOLUTION REPORT DONE (2026-07-02): `RB_MF=all` also reports per screen which BOUND elements
  resolve LIVE data (`SMOKE BINDS: resolved=M/N unresolved=[...]`); drive a run first
  (`RB_SCREEN=encounter RB_MF=all`) to validate the in-run screens (encounter resolves 14/21 driven vs
  4/21 cold; the residue = container binds + state-gated ones). `encounter.label` now resolves to the
  live node type (place names stay design-open); `pool.legend`'s sample IS the design copy (its pips
  are Needs-CD, no live datum exists). First catch: the encounter smoke drive had silently rotted
  (cleared-fight-HOLDS made post-Resolve Enter() hops no-op — the "castle" shot was really the a1
  hold); each hop redeploys now. A bind that silently goes dead shows the pass it happens.
  NOT yet an exit-gate (pin a baseline first).
  PER-ELEMENT COVERAGE DONE (2026-07-02): `RB_MF=all` now leave-one-out renders EVERY element and
  measures its actual pixel contribution — an element with unconditional chrome/content (fill/frame/
  content/image/button) contributing ZERO pixels fails the run (exit 1); bind-only/list elements empty
  pre-run report SILENT (state-gated, ok); border-only elements overpainted by the mixed-z container
  fills report OCCLUDED (encounter/attrPool's divider — Needs-CD z normalization). First catch: citymap
  `doomHost` (a bindless text element whose static `image` was never blitted) — fixed, the enemy-host
  icon renders. REMAINING: bound-shows-LIVE-not-sample assertions (needs driven run state in the smoke)
  + the fidelity diff below.
- **Fidelity diff vs `design/NN.png`:** a region/perceptual image compare → an objective match score +
  per-region delta map (meaningful once fonts/chrome land; tolerate known placeholder-data regions).
  BUILT (2026-07-02): `tools/fidelity_diff.py <shot> <design> [--map heat.png] [--worst N]` — 24×14
  tile grid in design space, per-tile color+edge distance → overall % + the worst tiles as design-space
  rects (+ optional heatmap). BASELINES (RB_MF pre-run shots, so placeholder-data regions depress the
  numbers): encounter 71.1 / equipment 66.9 / citymap 74.2 / campaignmap 85.1 / newgame 66.4 /
  merchant 76.7. REFINEMENTS open: drive run state for live-data shots; a placeholder-region mask.
  WORST-TILE WALKS (2026-07-02, the enumerated-delta discipline): NEWGAME's 0% tiles = the known
  Needs-CD extraction gaps (card bg/chrome parts, boxed stat tiles, SELECT buttons, race head art);
  EQUIPMENT's = missing screen backdrop art (design/02 shows one; the manifest has no scene element
  — Needs-CD), textured `ui/pip/*` attr-bar pips (Needs-CD, already logged), runeCard chrome
  (Needs-CD). Both screens' remaining fidelity floors are CD-gated; no renderer-side deltas found.
  ENCOUNTER walked (2026-07-02 pm): 0% tiles = poolRow pip strips (Needs-CD, known), action-bar CARD
  chrome/frames + footer state lines (Needs-CD techCard parts), hero/minion/foe figure art +
  positions (placeholder art), under-figure name+segmented-HP bars (heroHp/foeHp resolve text; the
  segments need pip parts — Needs-CD). MERCHANT walked: its low tiles are a STATE artifact (the
  gate's drive ends at the castle so merchant lists are legitimately empty) + the same chrome family;
  a true merchant walk needs a merchant-state drive — DONE (2026-07-02 pm): the gate now runs TWO
  driven passes (encounter drive owns encounter/equipment/campaignmap/newgame; the citymap drive
  stops AT THE MERCHANT and owns citymap/merchant), so merchant validates with live stock: binds
  10/17 (residue = containers + the single-page pager's legit-null prev/next). All walked screens'
  floors are CD/art/design-gated — zero renderer-side deltas remain from the walks.
- **GATES — PINNED (2026-07-02):** `python tools/ui_gate.py` = the ONE regression command: scratch
  build → driven all-screen smoke (blank-screen + blank-element failures are the engine's own exit
  code) → per-screen bind-resolution counts vs `tools/ui_baseline.json` (a DROP = a bind went dead =
  fail) → per-screen fidelity score vs baseline (drop past 2pts = fail). `--update` re-pins after a
  slice that legitimately improves things — run the gate EVERY pass before commit. Baselines (driven):
  binds enc 15/21 eq 11/26 city 2/7 camp 4/4 ng 12/18 mer 5/17; fidelity 66.9/65.8/69.4/84.1/65.8/76.4.
- **GATES (design bar):** a screen is "DONE" only when coverage+content pass AND the fidelity diff is under threshold.
  The loop may claim "blocked/starved" ONLY by emitting the ENUMERATED per-element remaining-delta list,
  each tagged **CD / system / human** with a reason. No backed-up list ⇒ NOT starved — keep perfecting
  what's achievable. **Missing systems/content must NOT block pixel-perfecting the elements that DO exist.**

**⚡ NOT STARVED — DROP-INDEPENDENT WORK (do NOW; the pixel-perfect render POLISH is drop-gated on CD's
states/sides/colorBind/merchant manifest, but plenty needs NO drop):**
- Finish the flagged CODE fixes below (war-party fill R→L; Bandage target-side {enemy|self} + shield
  sources flagged PASSIVE + the SHIELD-BAR wiring; the Encounter/CampaignMap Equipment buttons shell-side
  if doable; the `apex`→`Core Effect` CODE rename `ApexName/Desc`→`CoreEffectName/Desc`).
- Build the ENGINE DRAW routines against the SCHEMA (data arrives with the drop; code is testable now):
  `states` resolution (draw by the bound datum's state key), `border.sides` (per-side borders — else a
  single accent edge draws as a FULL box), **`colorBind` [APPROVED]** (bind a color field e.g.
  `core.accent` → a visual prop), shield-pip instancing + regen fill, the merchant-screen consumer,
  **nested-list stamping** (#23 — a template/list whose items contain their OWN list: merchant shelves,
  runeGroup rows) + **merchant paging** (#23; same family as #4 overflow).
- **CONTINGENCY GREEN-LIT** (render polish is drop-gated = the exhaustion trigger is met): the **`Game1.cs`
  SRP refactor** — split the uber-class by responsibility. High-value, fully drop-independent.
- **If after all this everything left is TRULY blocked (needs a drop / a human decision): STOP looping**
  — say so in one line; do NOT spin, invent busywork, or grind low-value churn (see loop.md).

FLAGGED FIXES (from live screenshots):
- ~~Enter still passes the OLD build screen~~ DONE (2026-07-02): BEGIN now marches straight to the
  CityMap (`Redeploy` + Run screen) — no build gate; the fixed kit keeps the bar non-empty.
- ~~Equipment must be the FULL screen, not the LOADOUT popover~~ MOSTLY DONE (2026-07-02): popover
  DELETED; E / the EQUIPMENT button opens the real Equipment screen; Esc returns to the caller. In a
  run the screen reads the LIVE state (run body/reservations, run bar, run minions) and card clicks
  power/unpower on the campaign; pre-run they slot/unslot the build; rune Climb stays pre-run (mid-run
  rune mutation is design-open). RESIDUAL: an Encounter button (disabled in combat) + a CampaignMap
  button need manifest elements (Needs-CD); a drawn BACK affordance likewise (Esc carries it).
- (drop renamed `combat.flee`→`combat.retreat` — resolver + input geometry flipped same pass)
- **AUTO-ATTACK button isn't wired** — believed ALREADY WIRED at the Encounter cut-over (slice 14:
  the combat.autoAttack element click toggles the one global AUTO, and its label reads
  "AUTO-ATTACK ON" when lit). If the screenshot predates that build, re-test; else report repro.
- ~~Resource-count readout top-right~~ DONE (2026-07-02) for supplies / gold / charge (the drop's new
  charge icon renders) on Encounter + CityMap + Equipment, InRun-gated. SUMMONS joins when its §9
  resource model lands (not yet built).
- ~~War-party indicator (fill direction re-open)~~ DONE (2026-07-02): the covered-ground fill now loads
  RIGHT→LEFT IN TANDEM with the host — its leading (left) edge tracks the host token; camp LEFT / castle
  RIGHT unchanged (citymap smoke green).
- ~~Targeting reticles don't sit on the foe's body parts~~ ENGINE DONE (2026-07-02): reticles anchor
  on the figure's ACTUAL limb rects via the manifest transform (`FigureBinding.StatOf` + a screen-rect
  union, both arms as one group) — AIMING centres on the hovered part while picking, FOCUS marks each
  locked part-aim (verified on the aimed head). Band strips remain the click hit-test. REMAINING
  Needs-CD: the demonstrative "how-it-mounts" design screens to pixel-match against.
- ~~SHIELD BAR + active-shield BUG~~ DONE (2026-07-02): passivity landed earlier (IsPassive, CardPress
  toggles); the drop's bar is WIRED — variable-N pips (one per layer, filled-first), live/spent state
  styles, and the per-pip regen track (Core-tested 304). Dashed spent-borders solid pending pixel pass.
- ~~CityMap start node = CAMP~~ DONE (2026-07-02): `NodeType.Camp` end-to-end — the origin authors as
  Camp, fog always shows it (your own origin), re-entering it spawns NO fight (safe ground, like the
  merchant), and the chart blits the camp token. Core-tested (297 green).
- ~~Friendly/self vs enemy TARGET-SIDE bug~~ DONE (2026-07-02): `Technique.Side` {Enemy|Self} added
  (Bandage declares Self — its heal already auto-picks the caster's most-damaged part at discharge);
  `IsPassive` derives from ShieldLayers>0 so a source can't be authored active. CardPress now: a powered
  SELF tech never enters targeting; a powered PASSIVE source toggles OFF (§6b) — never the FSM.
  Core-tested (296 green). The SHIELD BAR UI waits on CD's bar design.

RENAME **apex → Core Effect** — COMPLETE (2026-07-02): code side done earlier; today's CD drop renamed
the manifest binds (`preview.coreEffect*` / `core.coreEffect*`, zero `apex` left in layout.json) and the
Game resolver keys flipped in the same pass (keystone "apex-tier" untouched). [DESIGN_SPEC §17 #14]

MINION RESOURCE = **Summons** [LOCKED §9/§14] — BUILT (2026-07-02): Caster carries MaxSummons/
SummonsLeft (Forge sizes runs at bays+2, placeholder; bare test casters stay unlimited); a FRESH summon
spends 1 (uniform across gates) + its gate; a drained gate stat now IDLES the minion (stays summoned,
silent, holds its bay) and it re-raises FREE on recovery — the old drain-dismiss cascade + its test are
superseded. Merchant stocks summons (seeded x1-2, M key) + the in-run readout shows summons N/M (icon
landed 07-02). Core-tested (300 green). REMAINING: minion DEATH (no HP model yet — killed
re-pay untestable) + the Summoner Core Effect refund (next).

MERCHANT: mechanics SPEC'd (§12). PARTIAL (2026-07-02): resource stock (seeded per node — supplies 1-3,
charge 1-2, capped top-ups, placeholder prices flagged to the economy tune) + the 1-HP buy + the premium
FULL-heal are BUILT + Core-tested (298 green) + wired into the stopgap popover (H/F/S/C). REMAINING:
Summons stock (needs the §9 resource model). The 5-CATEGORY gear stock MODEL is BUILT (2026-07-02):
`MerchantStock.Roll` — seeded/reproducible, ≤4 of 5 sections, 3 picks per section (pool-capped),
techniques always-5-when-present + 2nd-rarest, rank-2 runes rarer, keystones NEVER; section weights
placeholder (shuffle algorithm OPEN §17). Shape Core-tested over a 400-seed sweep (300 green).
WIRED (2026-07-02): Expedition rolls the stock ONCE per merchant node from its seed (GearSalt) —
weapons/armor buy from the roll (purchases consume it; the static Shops lists are retired from the
merchant path), and techniques/minions/runes are OFFERED (exposed lists) but not yet buyable — their
receiving models (mid-run palette/bay/rune mutation) stay the design-open gate. The standard map's
"b" merchant deterministically stocks dagger+plate so the existing buy/equip tests hold unchanged. The SCREEN LAYOUT waits on a CD design PNG (popover = flagged stopgap).

**AFTER HiFi completes/blocks + all outstanding identified bugs resolved:**
- ~~Summoner CoreRune — real Core Effect~~ BUILT (2026-07-02): `CoreRune.CoreEffectRefundsSummons`
  (effect-as-DATA, one interpreter) threads Forge→Campaign→Expedition; Redeploy refunds 1 Summons per
  SURVIVING minion (idle counts — still summoned), capped at max; Summoner's card copy updated to the
  real effect (CD reconciles the design/05 Legion label). Core-tested over a live cleared fight
  (301 green). NOTE surfaced: the Shade (INT 3) fails its gate at assembly on a base Human (INT 3,
  Skeleton already reserving 2) — only the Skeleton fields from the off; balance touchpoint.
- **CONTINGENCY (HiFi is CD-blocked → active):** refactor the uber `Game1.cs` — split by responsibility
  (SRP), codify SOLID where it sensibly applies. STARTED (2026-07-02): the manifest RENDERER half
  (generic element renderer, list/graph stamping, bind resolvers, fidelity primitives — 640 lines)
  lives in `Game1.ManifestRenderer.cs`; the legacy CityMap screen (chart/panels/gear bar/merchant
  stopgap/spine — 289 lines, retires wholesale at the citymap cut-over) lives in `Game1.CityMap.cs`.
  The palette + raw draw primitives (rect/border/line/sprite/glyph-safe text/panel/bar/button — 205
  lines, no game knowledge) live in `Game1.Canvas.cs`. Partial splits, behavior identical; Game1.cs
  1867→780 (loop/input/state + encounter shell). Splits are DONE for now — true class extraction
  (canvas as a real type) only if a concrete need shows; don't refactor for its own sake.

## ⇒ HOW TO WORK THIS ARC — read EVERY pass (pixel-perfect · no drift · no premature "done")
ONE screen per pass. The goal is: every screen renders 100% from `layout.json` and matches its
`design/NN-*.png` **pixel-close**.

1. **Render FROM the manifest, never hand-draw.** Draw via the shared renderer (`DrawManifestScreen` /
   `DrawManifestElement`). If a screen needs something the renderer can't do yet, **extend the SHARED
   renderer** (one path for all screens) — never special-case a screen or fall back to hardcoded rects/
   frames. Hardcoded layout is how the old drift happened.
2. **Verify by COMPARISON, every time.** Build to a scratch dir, `RB_MF=<screen>` to render it, then open
   BOTH the shot AND `design/<screen>.png` and walk it element-by-element, listing every delta (position,
   size, colour token, text, font size, chrome/shadow/frame, missing/extra elements, stray borders). Fix
   until the delta list is EMPTY. "Looks like something" is not the bar — matching the reference is.
3. **Drive the STATE, don't trust the picture.** For data-driven screens, drive input→state (pick race/
   core, equip, aim a foe part, redeploy) and confirm LIVE data renders — binds resolve to real values,
   not the template `sample`. A control that looks right but does nothing is not done.
4. **NO DRIFT — manifest + design PNG are the ONLY source of truth.** Don't invent layout, don't add
   borders/boxes/padding not in the design, don't "improve" it. If the manifest looks wrong, FLAG it
   (Needs-CD/human) — never diverge by hand to "fix" it (that re-introduces drift).
5. **"DONE" means ALL of:** renders 100% from the manifest (zero hardcoded layout for that screen);
   delta-list vs the design PNG is EMPTY (you actually read both images); live state drives it; build
   GREEN; `crash.log` clean. Less than that is NOT done — write the exact residual as a checklist line,
   don't check the screen off.
6. **Blocked?** Write the SPECIFIC missing capability (what the design needs that the manifest/renderer
   can't express) as a concrete Needs-CD/human item. NEVER claim "exhausted" or silently approximate.
7. One screen per commit; small semantic slice; pull-before / push-after; update this file (check a screen
   only if truly done per #5, else note residual). Then STOP.

## Current target — the manifest-driven screen RENDERER (close the design delta)
The shared render path is the ONE path: `DrawManifestScreen`/`DrawManifestElement` + `ManifestUi` /
`CardTemplate` / `ListLayout` / `GraphLayout` / `StageComposer` / `NineSlice`.

FOUNDATION DONE (slices 1-7 — see git): Element `Shadow`/`Frame`/`Fill`/`Border` model + generic element
renderer (§10 order: shadow → frame|fill → border → content); LIST rendering (item template stamped per
cell, grid flow); LIVE bind resolver (`race.*`/`core.*` → live data, else `sample`); per-attr tiles;
`fontPx` scaling; display data (`Race.Tag/Blurb`, `CoreRune.ApexName/Desc` — DISPLAY-ONLY, no apex EFFECTS).

SLICES (one screen/pass, pixel-verify vs its design PNG):
1. **NewGame** — CUT OVER: the live screen now renders via `DrawManifestScreen("newgame")`; the old
   5-card + `RACE [tab]` screen is DELETED. Input reads manifest geometry BY BINDS (races/cores list
   cells + begin rect — never CD's renameable element ids). `.selection` parts draw only on the chosen
   card (CHOSEN / CORE SET follow clicks; other state binds still gated). Verified LIVE + state-driven:
   RB_CHASSIS=2 moves CORE SET to Adept and the whole preview column follows (figure/role/budget/apex).
   Renderer foundation from this arc: z back-to-front, fill+frame, text-shadow-as-glyph-copy,
   height-capped word-wrap, template-part fill/border.
   NOT design-done — remaining deltas are ALL manifest-expressiveness gaps (Needs-CD below): card/tile
   bg+chrome parts, tile value+label split, `align`, button/chip labels+fills, core-rune icons +
   ①②③/✚ glyph font, head-image assets, hi-fi bg. Re-walk the full delta list when CD's drop lands.
   Race-column pixel-walk (2026-07-01, live vs design/05): positions/values MATCH the manifest; the
   residual deltas are (a) text ~1.5x wider than the mock — the SYSTEM display font is wider than the
   design font at the same fontPx (fixed by the bundled-font swap, tracked in Verify mechanics), and
   (b) the known Needs-CD chrome: no card bg/panel part, no whole-card CHOSEN ring (only the chip is
   in the template), race.headImage draws its gradient slot but head sprites don't exist, ✓ folds.
   No renderer-side bugs found in the walk.
2. **Equipment** — CUT OVER: live screen renders via `DrawManifestScreen("equipment")`; the hand-drawn
   build screen (core selector/core block/palette/loadout strip/anatomy tags) is DELETED. Core switching
   is REMOVED from this screen per design/02 (core is fixed between fights — that choice is NewGame's).
   Input by binds: tab strip picks GEAR/TECHNIQUES/MINIONS (`_invTab`, render-side state); TECHNIQUES
   tab lists the palette as inv cards (badge stat+reserve, name) — click slots/unslots; clicking a
   slotted action-bar card unslots; keys 1-9 still toggle; Enter marches (the design's READY TO MARCH
   chip was flattened into the status strip by extraction — Needs-CD; Enter carries the march).
   Live binds: paperDoll figure, attr bars (§6 part labels, free/cap, positional pips), loadout/minion
   cards, core identity, rune budget. BIND-RENAME RECONCILED (2026-07-02): the CD drop renamed the
   inventory binds (`invItems`→`inventory.activeTab.items`, `tabs`→`inventory.tabs`) and neither render
   nor input was chased — the GEAR tab had been stamping "Sword" SAMPLES instead of live gear and tab
   clicks were dead (the bind validator surfaced it). Chased clean; PLUS the drop's new equipment binds
   mapped live: `core.name`, `run.state` (real expedition state in-run), `loadout.slotLabel` /
   `minions.slotLabel`, `core.stats` identity rows (bays/actions/budget), `runes.budgetPct` (spent-
   fraction bar; datum-driven fills are exempt from the blank-element gate at zero). Equipment binds
   21/26 driven — the 5 left are pure containers. NOTE minor CD collision: coreLabel/marchState overlap
   the resourceStrip top-right. RUNE BAG LIVE: one group per PATH ladder (MARKS/PATHS/KEYSTONES
   taxonomy stays OPEN §17 — the model's ladders render); each group shows held-rung + next with
   `Mark.Name` display titles (Vessel Seal I/II, Hollow Vessel, ...), a data-derived effect line
   (sockets/grants — can't drift), live EQUIPPED/EQUIPPABLE/LOCKED state + discounted cost; clicking a
   group card Climbs the ladder (budget-gated in Core). Group header label + row icon/stack are
   bindless in the template (Needs-CD). GEAR tab now lists the RUN's gear (wielded + packed weapons,
   armor; EMPTY pre-run — gear only exists once marching); rarity chip stays gated (no rarity model).
   Not design-done: chrome/label gaps in Needs-CD.
3. **Encounter** — CUT OVER: the fighting screen renders `DrawManifestScreen("encounter")` over the
   battlefield backdrop; only the cleared/lost overlay stays legacy (not part of design/01). Combat
   input reads manifest geometry by binds: card cells press/right-press, the foe hit-box + limb bands
   ARE the foeFigure element, AUTO-ATTACK/FLEE/HELD chips click (labels authored in the shell — the
   design chips were flattened by extraction, Needs-CD). Fielded minions draw as sprites in the
   minionField element. Deleted: DrawFighter/AttributePool/Pips/Foe/Bays/Support/ActionBar + hot/
   toggle buttons + hand rects (~10 helpers). LOST at cut-over (manifest lacks the element — Needs-CD/
   design): per-card AIM TAG (F1:H read), banked/rallied SUPPORT pips (rally lane already in Debt).
   Live binds (all verified over a live castle fight): hero/foe figures + HP, pool rows w/ live
   reservations, technique/bay cards (stat glyph tiles, costs), FSM state chips (DRY/HELD/READY/
   COOLDOWN, idle = no chip), cooldown labels, charge-progress fill widths, bay ACTIVE.
   AIMING state done: the card the targeting FSM is picking for reads an AIMING chip + "locking on"
   label (design/01's Firebolt card), verified live via the encounter smoke's targeting card.
   Gaps: techCard's bindless sample parts (the cost NUMBER + the mid-description damage digit) stamp
   their sample on every card (Needs-CD: bind them).
   Pixel-walk vs the NEW hi-res design/01 (2026-07-02): positions/values match the manifest; deltas are
   the known chrome family PLUS two fresh Needs-CD items — `poolRow` has NO pip parts (the design's
   per-stat pip strips can't render; `ui/pip/*` assets exist), and the re-dropped manifest LOST the
   "Attribute Pool"/"Action Bar" panel titles that design/01 shows (they were elements before the
   re-drop). No renderer-side bugs found. Equipment walked vs the new design/02 too (2026-07-02):
   clean — same known gaps; one family addition: attrBar pips are FLAT FILL parts where the design
   shows the textured `ui/pip/*` states (hatch/reserved) — pips-as-imageBind would close both screens.
   DISPLAY COPY DONE: `Technique.Desc`/`Minion.Desc` (+`DescText`) ship card copy for all 8 techniques
   + both minions; `{power}` resolves from the data at render so copy can't contradict tuning. Bound
   via technique.description/bay.description; Core-tested (290 green).
4. **CityMap** (in progress) — `design/03`. The manifest `chart` graph element renders LIVE
   via `DrawManifestGraph` (GraphLayout spread over the element region): fog-aware beacon icons,
   charted solid / uncharted dashed links, the current node ringed "you are here", reachable
   deployments numbered. Verified RB_MF=citymap at a live merchant node. PANELS LIVE (2026-07-02):
   all 7 citymap binds resolve — SUPPLIES/MUSTERED SUPPORT gauges read live counts + flavor (bound
   PANELS now draw their resolved header inset over the chrome — same path lights encounter's SHIELD
   header), the war-party bar fills covered-ground RIGHT→LEFT live (`enemy.advancePct`; full-rect fill
   bypassed — the width IS the datum), the chart legend list stamps its 4 icon+label rows through
   NodeToken. Inner pip STRIPS stay flattened-extraction Needs-CD. FRESH Needs-CD: `doomEta` is a
   BINDLESS content literal ("1 WAYPOINT AWAY FROM CAMP" mock) — bind it (enemy.advance) and drop the
   bind from the doomBar container, else the mock shows beside the live count; legend rows overlap
   their panel's top edge (item pad).
   **VISUAL CUT-OVER DONE (2026-07-02):** the LIVE citymap renders `DrawManifestScreen("citymap")` —
   legacy chart/supply-panels/war-party/legend/gold-readout DELETED; node input reads the MANIFEST
   chart geometry (NodeRect locates the chart by its `map` bind, so clicks land exactly where the
   graph draws; legacy region is only a no-manifest fallback). STILL HAND-DRAWN, flagged un-homed
   overlays (design/03 shows none of them — Needs-CD/human home before they can die): gear bar +
   PACK chips, EQUIPMENT [E] button, castle panel, campaign spine (parked bottom-left). Known
   overlaps: node[3] label under the castle panel, legend rows over their panel edge (both CD-side).
   Verified live smoke + gate green.
   REMAINING for cut-over: map input on manifest geometry (node clicks); a home for the merchant panel,
   gear bar + EQUIPMENT button (design/03 shows none of them — surface where they live, Needs-CD/human,
   before deleting the legacy citymap screen).
5. **CampaignMap** (render started) — `design/04`. The manifest screen renders: header/eyebrow,
   `campaign.taken` ("N / M" from the live campaign), and `cityGraph` draws one marker per campaign
   LEG spread across the region (taken = solid good links, current framed, onward dotted) with
   "Tier N - TAKEN/CURRENT" labels. City NAMES stay undrawn — §12/§17 leave the city roster OPEN
   (count, procgen-vs-authored), so no names are invented; the design's castle icons are absent from
   the cityNode template (Needs-CD). MODEL gap: §12 Layer 1 locks a forward-biased city GRAPH but
   Campaign is still a linear leg list — graph model + campaign-level supplies/WAIT is its own Core
   pass (surface before building). No screen-flow entry yet (not reachable in game; RB_MF only).
FLOW [FIX — still wrong today]: (1) BUG — Enter still passes through the OLD build screen; NewGame must go
STRAIGHT to CityMap, no build gate / no Enter-through. (2) Equipment must open as a **FULL SCREEN**
(design/02) — KILL the redundant "LOADOUT" popover; route ALL access to the real screen. (3) Hotkey **`e`**
(was `i`) + an "open Equipment" BUTTON on Encounter (DISABLED in combat), CityMap, CampaignMap; a
**BACK/close** on Equipment returns to the CALLER (CityMap already shows EQUIPMENT [E]; add Encounter +
CampaignMap). (4) MERCHANT is an IMPROVISED un-designed POPOVER stopgap — the heal+gear-shop MECHANIC is
designed (§12/§14), the SCREEN is not; spec with Doug + a CD design PNG before building it (design-open,
DESIGN_SPEC §17). Do NOT expand the popover as if it were the design. See DESIGN_SPEC §12.
(core.icon bind + mgcb icon entries: DONE 2026-07-02. FONT task: DONE 2026-07-02 — see the P0 block.)

ENGINE TODOs reconciled from CD's gap list (2026-07-01) — NOT already covered above:
- **`imageBind`** — DONE (2026-07-02, CD #15): `TemplatePart.ImageBind` parses (contract-tested), the
  list renderer resolves `{bind}` placeholders per datum generically, and the map graph blits the
  fog-aware node token through it — skirmish ⚔ renders live; the "unknown ?" stopgap mapping retired.
- **Button 9-slice corners** — DONE (2026-07-02, CD #11): DrawButton nine-slices the 320×88 skins
  (was stretch-scaled); source margins 12px, destination corners land at 12 TARGET px via NineSlice's
  new `dstCornerScale` (1/SS for 2x art) so rivets stay native and a 25px-tall button keeps its face
  (Core-tested). If CD meant 12 DESIGN px corners instead, flip the scale to 1 — one constant.
- **Container overflow/scroll** (CD #4): DEFERRED (2 races / 6 cores fit today) — add a scroll/page
  primitive when a list actually overflows; tracked here so it's not lost.
- **Confirm-to-close (lets Doug clear CD's entries):** on the relevant cutovers, confirm `cityNode` reads
  `[8,8]` (CampaignMap, CD #12) and the 5 full-bleed HUD bars ship WITHOUT a `frame` (CD #14 regression fix).
- NOTE: CD #1+7 (shadow/frame/gradient DRAW) is STALE — the foundation renderer already draws them (§10
  order). Frame-v3 tile/repeat + centerFill (CD #16) — DONE 2026-07-02 (NineSlice tiles edge/centre
  patches at native scale, trailing chunk crops 1:1; centerFill:false leaves the middle open;
  Core-tested). Only a gradient-interpolation check remains; CD can close #1+7's basic-draw.

## Verify mechanics
- `RB_MF=<screenId>` renders a screen STRAIGHT from the manifest (safe — live screens untouched): the
  cut-over dev hook. `RB_SMOKE=1 RB_SHOT=x.png RB_SCREEN=<newgame|equipment|encounter|citymap> dotnet …`
  renders a LIVE screen to a PNG headless.
- A RUNNING game locks the default build output → build to a SCRATCH dir and run THAT instance.
- On ANY crash READ `bin/Debug/net9.0/crash.log` (Program.cs writes the full exception + stack).
- Fonts are BUNDLED TTFs (IM Fell English display / JetBrains Mono mono; regions incl. ①②③ ✓ ✚ ◉,
  DefaultCharacter "?"). `Core.GlyphSafe.Sanitize` still guards anything outside the regions.
- FIDELITY: `python tools/fidelity_diff.py <shot> design/NN-*.png --map heat.png` scores a shot vs its
  design (see the SYSTEMIC block for baselines). Run after RB_MF=all; walk the worst tiles it lists.
  **GATE on it — a screen is NOT done until its score is under threshold; run every pass, all 5 screens.**
  ~~dead-baseline warning~~ RESOLVED 2026-07-03: baselines re-pinned on fidelity v2 (1:1 @ 1920×1080,
  per-element ranked lists, newgame stat mask) — see P0-A.3.
  RE Doug's ~2×-oversize note — TRIAGED (2026-07-02 pm): (1) BORDERS were genuinely 2× — `BorderPx`
  had pinned the old fixed-SS=2 weight (w=1 → 2 design px); now draws the AUTHORED design px. FIXED.
  (2) Run-together gauge text FIXED — the wrap is '\n'-aware and the gauge resolvers stack
  "SUPPLIES n/m" over its caption line. (3) **"wrong font" = a STALE RUNNING BUILD**: the game
  instance that's been holding the exe lock all evening predates the font swap AND every P0 fix —
  RESTART THE GAME to see IM Fell/JetBrains, native-res, slim corners, hairline borders. (4) Supplies
  PIPS: the manifest supplies/support panels carry NO pip parts (flattened extraction — Needs-CD,
  logged); the engine renders values+captions live meanwhile. No design-px/SS element-scale confusion
  found beyond (1) — element rects come straight from manifest design px through the scene transform.

## Debt (active — with reconcile trigger)
- Equipment: no inventory tabs (GEAR/TECH/MINIONS) + drag-to-equip + equipped-gear-on-anatomy + real
  rune-bag cards yet — blocked on gear/minion equip + mid-run gear/rune mutation (design-open). MODEL now
  refined (DESIGN_SPEC §7): a Core rune gives STARTING gear (does NOT lock it), gear is swappable;
  MULTI-SLOT pieces (robe = all slots); figures MORPH (human base + race + core + equipped-gear parts) —
  author morph layers, not per-combo art. Exact morph/slot mechanics OPEN (§17); feeds this when built.
- Bow.png not mounted on the figure. (~~`shot` icon~~ RESOLVED by the 07-03 late drop —
  `icons/technique/shot.png` ships + is mirrored; drop the swing fallback when touching that code.)
- The FLAT thumbnail dir `sprites/char/chassis/*` + its `Content.mgcb` entries still use the old name.
  (The `layout.json` FIGURE keys are ALREADY `human_`/`elf_`, and CD has ALREADY renamed the screen ids
  to newgame/equipment/citymap/campaignmap/encounter — both done.) Retire the flat `chassis/` dir once
  cards compose from modular parts (the retire-bare directive).
- Gradient fill draws vertical/horizontal only; diagonal deferred (PointClamp sampler).
- Minion `AltCost` summon is an UN-COSTED placeholder — wire the real HP/stat spend when an alt-cost minion ships.
- Shields (Stoneskin) wired + available but not yet in starting kits; SpellWard deferred (no spell model).
- No rallied-support lane in combat yet. Mouse is click+hover only (no drag-to-equip/tooltips/rebinding).
- G1: skirmish foe-part-aim numbers placeholder — tune in play (campaign verified winnable).
- Targeting-FSM bug (no clean repro yet): firing after a weapon charges while UNTARGETED misbehaves —
  watch/fix as the targeting+firing FSM is refined.
- Code ORGANIZATION (LOW-PRI — do NOT prioritize over HiFi): the `Game1.cs` uber-class was split (good),
  but the resulting classes lack a sensible FOLDER structure — organize into responsibility-based dirs
  (e.g. `Roguebane.Game/{Rendering,Screens,Input,Assets,...}`, `Roguebane.Core` by domain). Pure housekeeping;
  reconcile whenever render/HiFi work isn't the priority.
- FTL-ism **"jump"** (the beacon-chart verb) — canonical terms are **deployment** (the move/unit) +
  **Redeploy** (the action), **Retreat** (bail a fight). STOP THE BLEEDING: introduce NO new "jump" in any
  text/identifier. UI strings + the two canon docs are fixed; residual "jump" lives only in CODE COMMENTS
  + test prose — LOW-PRIORITY sweep, DEFERRED until the hi-fi render arc is confirmed complete. ("beacon"
  is the other FTL-ism — embedded in the `beaconNode` manifest template + comments; flagged for a
  decision, not auto-changed. "FTL" in comments is an allowed private design ref, leave it.)

## Needs human (loop skips)
- BOUGHT RUNE display/consumption (2026-07-03): merchant rune buys land in `Stash.Marks` (per the
  §12 receiving LOCK) but the rune bag renders ladder GROUPS (climb-by-budget) — a purchased Mark
  has no display home or consumption rule yet. Decide: bought rung auto-owned? separate bag row?
  (§17 rune-taxonomy adjacent; wire the moment it's called.)
- Balance/feel tuning (all placeholder-sane, tune in PLAY): tick 10/s; cooldowns + damage; DEX haste
  2%/pt cap 28%; CON→HP 1:2 +base8; evasion %; shield amounts/regen; armed-foe + castle HP/strike/cadence;
  budgets/spoils/prices; supplies vs march length; race/core stat blocks (design/05).
- Plate armour role: give it a role or retire the kind (flagged in `Shops.cs`).
- Apex EFFECTS: the 5 core signature effects are DISPLAY-ONLY placeholders — design them (+ the effect
  model) in a dedicated pass; NO undesigned mechanics in code meanwhile (CLAUDE.md guardrail).
- Part→stat friction (legs = accuracy, arms = STR) — low-pri, revisit only if it nags.

## Asset gaps (Needs Claude Design) — art missing/wrong, not composable from primitives
- Ranger figure (`human_ranger`/`elf_ranger`) — placeholder card until it lands.
- ~~Glyph coverage~~ RESOLVED (2026-07-02): bundled fonts + widened regions ship; ✓ renders live.
  RESIDUAL WATCH: if a bundled font truly lacks a used glyph (①②③ in IM Fell is unverified — MGCB
  built green but may substitute "?"), bake that glyph as an icon; check when a screen actually
  draws one.
- Skirmish node icon — CD adding it (the ⚔ marker on b1 + a legend row, ~8px taller: an APPROVED intended
  CityMap delta, not drift). Deliver as a node ICON PNG (`icons/node/skirmish`), NOT a font glyph.
- wraith + gargoyle: ART-QUALITY completion ONLY — NOT a uniform 21-part scheme. Robe figures legitimately
  have ~12 parts (§1 omits torso/legs/boots; adept/summoner are also 12); gargoyle's `back` is contract-
  legal. The renderer composes each figure from its OWN z-list — never assume 21 (fix any consumer/test
  that hardcodes it).
- ~~HI-FI CHROME pass~~ **LANDED 2026-07-03** (v4 frames at 1:1, wareCard/techCard/race-core card
  chrome, tile parts). Engine catches up per P0-C.3.
- ~~Manifest-extraction gaps~~ **LANDED 2026-07-03** (labels, chips, tabs, identity block, backdrops —
  the 07-03 drop closed the family). Any NEW gap gets caught by `tools/drop_audit.py` at drop time.

## Pointers
- Design canon: `design/DESIGN_SPEC.md`. Capture/layout contract: `design/LAYOUT_CONTRACT.md`.
- FUTURE / open DESIGN questions (healless-compensation archetype, shield numbers + shield-wall formula,
  race/core roster + the race↔core matrix, CON-as-minion-resource, camp-defense last-stand, keystone
  taxonomy, apex-effect design) live in **DESIGN_SPEC §17 OPEN** — not lost, canonical there by design.
- Visual truth / the pixel bar: `design/NN-*.png` + `design/SCREENS.md`.
- Shipped-work history + rationale: `git log` (detailed commits) — intentionally not duplicated here.
- Pending Claude Design payload: `outputs/CLAUDE_DESIGN_issues.md` (relay when ready; CD-resolved items
  await loop-wire + Doug's confirm-to-close — tracked in the space's designer-notes memory).

## Backlog (NOT prioritized — later; do not pull ahead of the render arc)
- **String localization / i18n:** externalize all user-facing strings behind IDs; route every drawn
  string through the renderer's `content`/`binds` text path (build toward it, don't hardcode literals) so
  a locale/string-table layer slots in later at ~zero cost; fonts need target-script glyph coverage
  (today ASCII-only). NOT now — just don't architect against it.
