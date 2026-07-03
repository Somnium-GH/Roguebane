// Roguebane — 9-SLICE FRAME generator (Tier-1 UI chrome, LAYOUT_CONTRACT §10).
// Persistent, deterministic — the "engine does heavy lifting" fidelity split: this paints the SMALL
// reusable ORNATE frame library the game nine-patch-blits under a panel/card at ANY size; shadows and
// gradients stay engine-drawn data (see style_tokens.js `shadows`), never baked into a PNG.
//
// NOT a captured screen atom — there is no on-screen "ornate frame" DOM node to capture (the screens
// today draw panels as flat rect+1px-border). Same category as ui_gen.js (button skins): a vector
// chrome asset with no DOM source, painted once, referenced everywhere by `style.frames.<token>`
// (ASSET_GEN_METHOD.md). To change the look, edit RB_FRAME_SPECS below and rerun — never hand-edit
// the PNG.
//
// Run via run_script:
//   eval(await readFile('proto/frame_gen.js'));
//   const out = buildFrames(createCanvas);
//   for (const [name, cv] of out) await saveFile('Content/ui/frame/' + name + '.png', cv);
//
// v3 (payload v4 flag-3 directive: "paint to the IDEAL — full tiled edges + painted 9-slice centers;
// the renderer conforms, design targets the goal"):
//   - EDGES carry a carved diagonal ROPE band whose period divides the edge span exactly, so edges
//     TILE (style.frames.<token>.repeat = 'tile'; CSS analogue border-image-repeat: round). No more
//     stretch-safe minimalism.
//   - The CENTER is PAINTED: a seamless (period = center-slice size) low-contrast parchment-dark
//     mottle on the panel fill tone — §10's "painted texture → the 9-slice center". centerFill: true.
//   - Single primary band (the box-in-a-box corrective stands, DEV_LOOP_MEMORY #13-era) + layered
//     corner medallions kept from v2; midpoint tick notches retired (the rope replaces them).
//   `slice` here MUST match `style_tokens.js` → `style.frames.<name>.slice` — resize both together.
//   GEOMETRY INVARIANT: edge span (size − 2·slice) MUST equal the center tile AND be an integer
//   multiple of 2·rope, or the tiling seams. panel: 64−32=32 = 4×8 ✓ · card: 48−24=24 = 4×6 ✓.
//
// v4 (2026-07-03 review: "borders oversized, won't produce a pixel-perfect design"): frames are now
// authored AT THEIR DRAW SIZE — 1:1, no downscale. Every screen draws border-image-width == slice
// (card 12px, panel 16px in 1920-space), so each source pixel lands on exactly one screen pixel.
const RB_FRAME_SPECS = {
  panel: { size: 64, slice: 16, accent: '#5a4636', accent2: '#3c2e20', stud: '#d9a441', center: '#1d150e', rope: 4 },
  card:  { size: 48, slice: 12, accent: '#4a3729', accent2: '#2e2216', stud: '#8d775c', center: '#1c140d', rope: 3 },
};

function buildFrames(createCanvas) {
  const INK = '#0a0807';
  const hexrgb = h => [parseInt(h.slice(1, 3), 16), parseInt(h.slice(3, 5), 16), parseInt(h.slice(5, 7), 16)];

  // deterministic integer hash -> [0,1)
  function hash(x, y) {
    let h = (x * 374761393 + y * 668265263) ^ 0x5bf03635;
    h = Math.imul(h ^ (h >>> 13), 1274126177);
    h ^= h >>> 16;
    return (h >>> 0) % 1000 / 1000;
  }

  function frame(spec) {
    const { size: S, slice: M, accent, accent2, stud, center, rope } = spec;
    const cv = createCanvas(S, S);
    const ctx = cv.getContext('2d');
    ctx.imageSmoothingEnabled = false;

    const OB = Math.max(2, Math.round(M * 0.10));      // outer black border thickness (thin frames: 2px min)
    const bandW = Math.round(M * 0.30);                // carved rope band thickness
    const T = S - 2 * M;                               // center tile size == edge span (tiling period)
    const P = rope * 2;                                // rope stripe period along the x+y diagonal
    const [ar, ag, ab] = hexrgb(accent);
    const [dr, dg, db] = hexrgb(accent2);
    const [cr, cg, cb] = hexrgb(center);
    const cell = Math.max(4, Math.round(T / 20));      // mottle cell size (blocky EGA grain)

    // ---- per-pixel base: black ring / rope band / painted mottle everywhere else ----
    const id = ctx.createImageData(S, S);
    const inBand = (x, y) =>
      x >= OB && y >= OB && x < S - OB && y < S - OB &&
      (x < OB + bandW || y < OB + bandW || x >= S - OB - bandW || y >= S - OB - bandW);
    for (let y = 0; y < S; y++) for (let x = 0; x < S; x++) {
      const i = (y * S + x) * 4;
      let r, g, b;
      if (x < OB || y < OB || x >= S - OB || y >= S - OB) {           // outer black frame ring
        r = 10; g = 8; b = 7;
      } else if (inBand(x, y)) {                                       // carved rope band (tiles at P)
        const s = ((x + y) % P) < rope;
        r = s ? ar : dr; g = s ? ag : dg; b = s ? ab : db;
      } else {                                                         // painted center/parchment mottle
        // seamless: sample the mottle on coords wrapped at T so every slice region tiles
        const wx = ((x % T) + T) % T, wy = ((y % T) + T) % T;
        const n = hash(Math.floor(wx / cell), Math.floor(wy / cell));
        const d = 1 + (n - 0.5) * 0.14;                                // ±7% tonal mottle
        const fleck = hash(wx, wy) > 0.988 ? 0.82 : 1;                 // rare darker fleck
        r = cr * d * fleck; g = cg * d * fleck; b = cb * d * fleck;
      }
      id.data[i] = Math.max(0, Math.min(255, Math.round(r)));
      id.data[i + 1] = Math.max(0, Math.min(255, Math.round(g)));
      id.data[i + 2] = Math.max(0, Math.min(255, Math.round(b)));
      id.data[i + 3] = 255;
    }
    ctx.putImageData(id, 0, 0);

    // ---- carved depth: light hairline at the band's outer edge, dark + ink delimiter at its inner ----
    function ring1(inset, color) {
      ctx.fillStyle = color;
      ctx.fillRect(inset, inset, S - inset * 2, 1);
      ctx.fillRect(inset, S - inset - 1, S - inset * 2, 1);
      ctx.fillRect(inset, inset, 1, S - inset * 2);
      ctx.fillRect(S - inset - 1, inset, 1, S - inset * 2);
    }
    ring1(OB, 'rgba(255,246,222,.18)');                 // raised outer lip
    ring1(OB + bandW - 2, 'rgba(0,0,0,.35)');           // engraved shadow inside the band
    ring1(OB + bandW - 1, INK);                         // crisp inner delimiter

    // ---- corner medallion — layered rivet: outer ring, inner diamond, center glint (v2, kept) ----
    const c0 = Math.round(M * 0.5);
    const rOuter = Math.max(3, Math.round(M * 0.28));
    const rInner = Math.max(1, Math.round(rOuter * 0.5));
    const IB = Math.max(1, Math.round(M * 0.07));
    function medallion(cx, cy) {
      ctx.fillStyle = INK; ctx.beginPath(); ctx.arc(cx, cy, rOuter, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = stud; ctx.beginPath(); ctx.arc(cx, cy, rOuter - IB, 0, Math.PI * 2); ctx.fill();
      ctx.fillStyle = INK;
      ctx.beginPath();
      ctx.moveTo(cx, cy - rInner); ctx.lineTo(cx + rInner, cy); ctx.lineTo(cx, cy + rInner); ctx.lineTo(cx - rInner, cy);
      ctx.closePath(); ctx.fill();
      ctx.fillStyle = 'rgba(255,246,222,.35)';
      ctx.beginPath(); ctx.arc(cx - rOuter * 0.3, cy - rOuter * 0.3, Math.max(1, rOuter * 0.18), 0, Math.PI * 2); ctx.fill();
    }
    [[c0, c0], [S - c0, c0], [c0, S - c0], [S - c0, S - c0]].forEach(([cx, cy]) => medallion(cx, cy));

    return cv;
  }

  return Object.entries(RB_FRAME_SPECS).map(([name, spec]) => [name, frame(spec)]);
}
if (typeof module !== 'undefined') module.exports = { buildFrames, RB_FRAME_SPECS };
