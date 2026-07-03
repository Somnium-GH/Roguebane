# Claude Design payload — engine-side findings awaiting a manifest/asset drop
_Compiled 2026-07-02 (evening render arc). Each item names the screen, the manifest gap, and what
the engine already does about it. The engine renders everything else in these areas live; these are
the specific blockers to pixel-close._

## Manifest semantics
1. **z conventions are mixed.** The 07-02 drop authors `*.scene` backdrops at `z:0` meaning BACKMOST
   while container panels keep the depth convention (high z behind, leaf `z:1` in front — e.g.
   encounter `statusStrip z:6` with a fill CONTAINING `z:1` chips). The engine special-cases z=0 as
   the scene layer; please normalize on one convention. Side effect today: `encounter/attrPool`'s
   right-edge divider is wholly overpainted by `hudFooter`'s fill (the smoke reports it OCCLUDED).
2. **`doomEta` (citymap) is a bindless content literal** — `"1 WAYPOINT AWAY FROM CAMP"` mock text.
   Give it `binds: enemy.advance` (the engine resolves the live count) and DROP that bind from the
   `doomBar` container, or the mock shows beside the live readout.

## Missing template parts (design shows it; the manifest can't express it)
3. **Pip strips as parts, everywhere the designs show them:** `poolRow` (encounter attribute pool),
   `attrBar` (equipment — currently flat fill parts where design/02 shows textured `ui/pip/*`
   states), the citymap `supplies`/`musteredSupport` gauges (values+captions render live; the pip
   rows can't), and the under-figure name+segmented-HP bars on design/01 (heroHp/foeHp resolve text
   only). `ui/pip/*` assets already exist.
4. **Card/tile chrome parts:** race/core cards + attr/budget tiles (newgame), techCard/minionBay
   frames + footer state lines (encounter action bar), runeCard/runeGroup chrome (equipment) all
   render chrome-less — the extraction dropped their bg/border parts. Also newgame's SELECT buttons
   and boxed stat tiles.
5. **`invTab` parts carry no label bind** — all three equipment tabs stamp the "GEAR" sample. The
   engine feeds `inventory.tabs` labels; the part needs `binds: tab.label`.
6. **coreStatRow ordering/labels:** the engine now feeds live (bays/actions/budget) rows; design/02
   shows a gear count too — if wanted, say what "gear" counts (wielded? worn? packed?).

## Screen gaps
7. **Equipment has no backdrop scene element** while design/02 paints one — add the `*.scene`
   element + asset like encounter/merchant/campaignmap have.
8. **CityMap un-homed elements:** design/03 shows NO gear bar / PACK chips, EQUIPMENT button,
   castle panel, or campaign spine — the engine keeps them as flagged hand-drawn overlays. They
   need a designed home (or explicit removal) before the legacy overlay code can die.
9. **Top-right collisions:** equipment `coreLabel`/`marchState` overlap the `resourceStrip`;
   citymap legend rows overlap their panel's top edge (item pad); citymap node[3]'s label runs
   under the castle-panel overlay once that panel has a home.
10. **`encounter.label` place names:** the engine resolves the live node TYPE ("CASTLE"); the
    sample's "· the high pass" flavor needs a naming model (design-open §17) before it can be live.

## Confirm-to-close (engine landed; CD can verify + close its side)
- Frame-v3 tile/repeat + centerFill draw; button + panel/card nine-slice corners at native
  proportion (`dstCornerScale`); per-side borders at authored design-px weight.
- Bundled fonts live: IM Fell English (display) + JetBrains Mono (mono); single-line labels
  shrink-to-fit their authored boxes (IM Fell runs wider than the extraction font — chip labels
  like AUTO-ATTACK sized for the old font now fit by shrinking; resize chips if that reads small).
- All 6 screens render from the manifest with live binds (encounter 15/21, equipment 21/26,
  citymap 7/7, campaignmap 4/4, newgame 12/18, merchant 10/17 — residues are containers or
  state-gated); per-element paint coverage + fidelity scoring gate every pass.
