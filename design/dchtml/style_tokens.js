// Roguebane — STYLE TOKENS (single source of truth for palette + fonts).
// Per LAYOUT_CONTRACT §8/§9.3: the .dc.html screens AND the screen extractor
// (proto/screen_extract.js) both read THIS file, so the style frame and the
// emitted layout.json.style stay in lockstep. Plain data — no IP, no deps.
//
// Usage:
//   browser/extractor:  eval(await readFile('style_tokens.js'))  -> window.RB_STYLE
//   game:               the same table is mirrored into Content/layout.json.style
//
// The extractor maps each rendered computed colour to the NEAREST palette token
// (RGB distance) so screens reference TOKENS, never raw hex (§8).

(function (root) {
  const RB_STYLE = {

    // ---- PALETTE (token -> hex). Grouped by role; values are the real hexes the
    //      screens already paint, so nearest-token matching is exact for the mains. ----
    palette: {
      // base ink / parchment text
      ink:        '#ece0cb',  // primary body text
      inkBright:  '#f0dcb4',  // panel titles / emphasis
      inkWarm:    '#e9d6b0',  // logo / serif headers on dark
      parch:      '#cdbfa8',  // italic flavour text
      muted:      '#9a8468',  // captions / mono labels
      mutedDim:   '#8d775c',  // sub-captions / part names
      mutedWarm:  '#b09a82',  // secondary mono

      // surfaces
      ground:     '#0c0810',  // screen backdrop
      panel:      '#1d150e',  // raised band
      panelDeep:  '#15100b',  // inset panel
      panelCard:  '#1c140d',  // card fill
      panelTab:   '#17110b',  // inventory body
      slot:       '#241b14',  // empty pip / bar trough

      // structure
      border:     '#5a4636',  // bright panel border / card border
      borderMid:  '#4a3729',  // band top border
      borderDim:  '#382a1d',  // column divider
      borderFaint:'#2e2218',  // inner hairline
      outline:    '#0a0807',  // 1px sprite/pip black border

      // accents
      amber:      '#d9a441',  // budget / gold numbers / HELD
      gold:       '#cf9a44',  // CON / brace
      blood:      '#b23b32',  // damage hatch / danger
      flame:      '#e8913a',  // firebolt number
      hit:        '#e0654b',  // damage number / targeting

      // attributes (STR/INT/DEX/CON one-part-one-stat)
      str:        '#c2553f',  // Hands  · attack
      int:        '#6f8fc4',  // Head   · casting
      dex:        '#82a85e',  // Legs   · evasion
      con:        '#cf9a44',  // Chest  · HP

      // states
      good:       '#7fa05a',  // equipped / ready / active (green)
      mintActive: '#7fc4b0',  // minion active
      teal:       '#4f9a8a',  // minion bay border
      lockRed:    '#b0473a',  // dropped / locked / starved
      lockText:   '#d0744a',  // locked gate text

      // rarity tiers (gear)
      rareCommon: '#9a8c7a',
      rareMagic:  '#6f9fd6',
      rareRare:   '#c66fb4',
      rareEpic:   '#e0a23c',
    },

    // ---- FONTS (role -> family + intent). Sizes are DESIGN-SPACE px (dc.html px ÷ 2). ----
    fonts: {
      display: { family: "'IM Fell English', Georgia, serif", role: 'world & names (storybook serif)' },
      mono:    { family: "'JetBrains Mono', monospace",        role: 'numbers / state / captions' },
      sizes:   { title: 12, header: 11.5, body: 8.25, label: 7, caption: 5.5, num: 11, numBig: 13 },
    },

    // ---- PART STATE GRAMMAR (canonical condition -> sprite suffix, §1/§8). ----
    // The figure parts (sprites/body/<figure>/<part>_<state>.png) bind to these.
    partStates: {
      ok:        'healthy',
      damaged:   'damaged',
      broken:    'broken',
      bareOk:    'barehealthy',   // armour force-unequipped: show flesh
      bareDmg:   'baredamaged',
      bareBroke: 'barebroken',
      hidden:    'hidden',        // disabled/unequipped: paint nothing
      focus:     'ui/reticle/focus',
      aiming:    'ui/reticle/aiming',
      gearSocket:'hatch',
    },

    // ---- POOL / PIP RULES (the reactor-block readout, §8). ----
    pip: { wrapAt: 10, shrink: true, fixedFootprint: true, anchor: 'monoNumber', max: 20 },

    // ---- FIDELITY PRIMITIVES (§10 LAYOUT_CONTRACT — the "engine does heavy lifting" split).
    // Shadows are ENGINE-DRAWN (cheap, resolution-independent) — never baked into a PNG. A screen
    // references one of these by `data-shadow="<token>"`; the extractor resolves it into a concrete
    // {dx,dy,blur,color,opacity} spec in layout.json. Adding a shadow to a screen is a MARKUP change
    // (attribute), never a new asset.
    shadows: {
      soft:  { dx: 0, dy: 3, blur: 4,  color: 'outline', opacity: 0.45 },  // small icon/token lift (map markers, chips)
      panel: { dx: 0, dy: 4, blur: 10, color: 'outline', opacity: 0.5  },  // panel/card lift off the backdrop
      title: { dx: 0, dy: 2, blur: 3,  color: 'outline', opacity: 0.65 },  // text-shadow analogue for headers/labels
    },

    // 9-slice FRAME library — the small reusable set of ORNATE painted frame assets (proto/frame_gen.js).
    // A screen references one by `data-frame="<token>"`; the extractor emits {asset, slice} so the
    // engine nine-patch-blits it at any element size. `slice` = [L,T,R,B] inset px INTO the source PNG
    // (must match RB_FRAME_SPECS in proto/frame_gen.js — if you resize a frame, update both together).
    // Reserve these for STATIC/neutral chrome only — state-coloured borders (ready/targeting/locked,
    // danger banners) stay engine-drawn flat borders so their colour can change per state.
    frames: {
      panel: { asset: 'ui/frame/panel', slice: [16, 16, 16, 16], repeat: 'tile', centerFill: true },
      card:  { asset: 'ui/frame/card',  slice: [12, 12, 12, 12], repeat: 'tile', centerFill: true },
    },

    // ---- INTERACTION STATES (hover/pressed/selected/disabled etc, DESIGN payload v3 "spec them across
    // all interactive elements"). Consolidates patterns the screens ALREADY use ad-hoc into one table so
    // new screens/elements reuse the same grammar instead of re-inventing per-element colour logic.
    // Three families, by what kind of "state" an element has:
    interactionStates: {
      // A) MOUSE/INPUT state (hover/pressed/disabled/toggled-on) — the ui/button/* skin set IS this
      // family already (one 9-sliceable PNG per state; never re-derive it from tokens at runtime).
      button: { normal: 'ui/button/button_normal', hover: 'ui/button/button_hover', down: 'ui/button/button_down',
                disabled: 'ui/button/button_disabled', on: 'ui/button/button_on' },
      // B) GAMEPLAY state on a selectable card/tile (technique cards, minion bays) — border colour +
      // fill + an optional border-style/pattern swap, NEVER a texture. Exact tokens as already painted
      // on Encounter's technique row (READY=Swing, LOCKED=Frenzy, TARGETING=Firebolt, COOLDOWN=Disarm,
      // HELD=Brace) — reuse these on every other action/ability card in the game, same five states:
      actionCard: {
        ready:      { border: 'good',    fill: 'panelCard', opacity: 1 },
        locked:     { border: 'borderDim', borderStyle: 'dashed', fill: 'panelCard', opacity: 0.82, pattern: 'diagonalHatch' }, // desaturated, deliberately NOT red
        targeting:  { border: 'str',      pulse: true,     fill: 'panelDeep', opacity: 1 },   // actively aiming — matches the reticle red
        cooldown:   { border: 'borderDim', fill: 'panelCard', opacity: 1 },
        held:       { border: 'gold',     fill: 'panelDeep', opacity: 1 },
      },
      // C) SELECTION state on a picker card (race/core/inventory-row) — border + fill + label swap;
      // `locked` = the SB-style restriction matrix (race can't bear this core), distinct from `disabled`.
      pickerCard: {
        idle:       { border: 'borderDim', fill: 'panelDeep' },
        hover:      { border: 'border',    fill: 'panelCard' },     // lightens one step, no texture change
        selected:   { border: 'amber',     fill: 'panelCard', badge: '\u2713 CHOSEN' },
        locked:     { border: 'borderFaint', fill: 'panelDeep', opacity: 0.55, badge: 'LOCKED' },
      },
      // D) ROW/LIST hover-disabled (inventory rows, rune-bag rows, city nodes) — overlay TINT only,
      // never a texture (UI_ASSET_MAP.md "Inventory rows, hover / selected / disabled highlights").
      row: {
        hover:      { overlay: 'rgba(255,255,255,.04)' },
        selected:   { border: 'good' },
        disabled:   { opacity: 0.5 },
      },
      // E) EQUIPMENT INVENTORY card state — locked vocabulary per DESIGN_SPEC §6e (2026-07-03 states
      // session), payload B5/B6a/B6b. Renames the old ad-hoc equipped/dropped/ready/neutral to the
      // canon EQUIPPED / DISABLED / EQUIPPABLE / LOCKED read; hover is an OVERLAY TINT (row-family
      // style) so it never fights the state border colour underneath it.
      invCard: {
        equipped:   { border: 'good',      fill: 'panelDeep' },  // active: wielded/worn/slotted/assigned
        disabled:   { border: 'lockRed',   fill: 'panelDeep' },  // assigned but unsustainable right now
        equippable: { border: 'border',    fill: 'panelCard' },  // unequipped, requirements met
        locked:     { border: 'borderDim', fill: 'panelCard', opacity: 0.6 },  // unequipped, requirements NOT met
        hover:      { overlay: 'rgba(255,255,255,.06)' },
      },
      // loadout-bar / minion-bay slot state (Equipment action-bar + bay cards) — slotted vs. empty,
      // same hover tint as invCard.
      loadoutCard: {
        slotted: { border: 'border', fill: 'panelCard' },
        empty:   { border: 'border', borderStyle: 'dashed', fill: 'panelDeep', opacity: 0.8 },
        hover:   { overlay: 'rgba(255,255,255,.06)' },
      },
      // Equipment's GEAR/TECHNIQUES/MINIONS tab strip — same idle/active pair it already had, now with
      // a hover step (raceCard-style: brighten one notch) per payload B6b.
      invTab: {
        idle:   { border: 'borderDim', fill: 'panelDeep', color: 'mutedDim' },
        hover:  { border: 'border',    fill: 'panelTab',  color: 'ink' },
        active: { border: 'amber',     fill: 'panel',     color: 'inkBright' },
      },
      // CityMap beacon-graph nodes (payload B8) — same current/hover language as CampaignMap's
      // cityNode, so both screens share one grammar. `glow` reuses the SAME fixed-tick pulse primitive
      // as actionCard.targeting's `pulse` flag (see the glow note in DEV_LOOP_MEMORY) — not a separate
      // effect to build.
      beaconNode: {
        hover:   { border: 'parch' },
        current: { border: 'amber', glow: true },
      },
    },
  };

  root.RB_STYLE = RB_STYLE;
  if (typeof module !== 'undefined' && module.exports) module.exports = RB_STYLE;
})(typeof window !== 'undefined' ? window : this);
