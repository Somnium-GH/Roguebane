// Roguebane — CORE-RUNE TOKEN capture (NewGame.dc.html), persistent + reproducible.
// Bakes DEV_LOOP_MEMORY #2 / DESIGN payload #1: the Core-rune identity icon (decagon + carved glyph)
// was inline SVG, so the extractor only caught the glyph text — the rune TOKEN (shape/fill/stroke) was
// lost. Per ASSET_GEN_METHOD (capture, never hand-canvas) + §11 (icons = textured PNGs), this captures
// the five NewGame core cards' decagon tokens straight off the live screen.
//
// DEDICATED overlay (not the shared proto/atom_capture.js one): that generic overlay only scales CSS
// border-width/font-size across the clone subtree, not an SVG's own width/height attributes — a cloned
// icon SVG stays at its native ~104px art size inside a much bigger K× box, capturing tiny + off-centre.
// Here we clone the <svg> itself and bump ITS width/height attributes by K (viewBox is untouched, so it
// scales as a crisp vector, no raster upscale) — then the wrapper is sized to the svg exactly, no dead
// space. These are FLAT pictographic icons (ASSET_MANIFEST Tier-1: "attr/rune/resource glyphs, pips
// stay flat") so no bevel/shadow preservation is needed here, unlike the node-token capture.
//
// RUN:
//   1. show_html("NewGame.dc.html")  — default state (race=human) shows all 5 cores UNLOCKED, i.e. in
//      their canonical accent colour — the correct state to capture (no need to force selection).
//   2. eval_js: fetch+eval this file {cache:'no-store'}, then
//        JSON.parse(RB_runeOverlay('#000000'))   -> {vw, k, rects}  (one entry per
//        data-atom="icons/rune/core_<id>" card, in DOM/render order) — stash `rects`/`vw`.
//   3. save_screenshot hq -> a FRESH "design/_capRuneBlack.png" (rebuild the overlay inline in the step).
//   4. eval_js: RB_runeOverlay('#ffffff')  (same rects, same order — DOM didn't change).
//   5. save_screenshot hq -> a FRESH "design/_capRuneWhite.png".
//   6. run_script: eval(await readFile('proto/rune_capture.js'));
//        await RB_runeSlice({ readImage, createCanvas, saveFile, log, vw, rects,
//                              black:'design/_capRuneBlack.png', white:'design/_capRuneWhite.png' });
//   7. eval_js: RB_clearRuneOverlay(); delete the two temp screenshots.
//
// Crop uses the -1 near / +2 far pad (ASSET_GEN_METHOD §5 fix) so the sub-pixel far border isn't
// shaved, then recovers true alpha+colour per pixel from the black/white pair (α = 1−(white−black)/255,
// colour = black/α) — opaque interior (fill + glyph + outline) comes back identical, only the decagon's
// concave corners go transparent.

// `only` (an exact data-atom value) renders a SINGLE icon at a fixed (6,6) origin — capturing one at a
// time keeps the K×-scaled box well inside any viewport, avoiding the multi-row layout that pushed
// later rows below the visible capture area (rows below the fold silently screenshot as blank).
window.RB_runeOverlay = function (bg, only) {
  var old = document.getElementById('__rbrune'); if (old) old.remove();
  var K = 4;
  var ov = document.createElement('div');
  ov.id = '__rbrune';
  ov.style.cssText = 'position:fixed;left:0;top:0;z-index:2147483647;background:' + (bg || '#000000')
    + ';margin:0;padding:6px;display:flex;flex-wrap:wrap;align-items:flex-start;gap:10px;width:calc(100vw - 12px);';
  document.body.appendChild(ov);
  var nodes = [].slice.call(document.querySelectorAll('[data-atom]')).filter(function (n) {
    var v = (n.getAttribute('data-atom') || '').trim();
    return v && (!only || v === only);
  });
  var items = [];
  nodes.forEach(function (n) {
    var svg = n.querySelector('svg');
    if (!svg) return;
    var w = parseFloat(svg.getAttribute('width')) || svg.getBoundingClientRect().width;
    var h = parseFloat(svg.getAttribute('height')) || svg.getBoundingClientRect().height;
    var clone = svg.cloneNode(true);
    clone.setAttribute('width', w * K);
    clone.setAttribute('height', h * K);
    clone.style.display = 'block';
    var wrap = document.createElement('div');
    wrap.style.cssText = 'flex:0 0 auto;position:relative;line-height:0;';
    wrap.appendChild(clone);
    ov.appendChild(wrap);
    items.push({ name: n.getAttribute('data-atom').trim(), wrap: wrap });
  });
  var rects = items.map(function (it) {
    var b = it.wrap.getBoundingClientRect();
    return { name: it.name, x: Math.round(b.left), y: Math.round(b.top), w: Math.round(b.width), h: Math.round(b.height) };
  });
  return JSON.stringify({ vw: window.innerWidth, k: K, rects: rects });
};

window.RB_clearRuneOverlay = function () {
  var o = document.getElementById('__rbrune');
  if (o) o.remove();
  return true;
};

async function RB_runeSlice(env) {
  const { readImage, createCanvas, saveFile, log, vw, rects, black, white } = env;
  const B = await readImage(black), W = await readImage(white);
  const scale = B.width / vw;
  const done = [];
  for (const r of rects) {
    const sx = Math.round(r.x * scale) - 1, sy = Math.round(r.y * scale) - 1;
    const ex = Math.round((r.x + r.w) * scale) + 2, ey = Math.round((r.y + r.h) * scale) + 2;
    const cw = ex - sx, ch = ey - sy;
    const cb = createCanvas(cw, ch); cb.getContext('2d').drawImage(B, sx, sy, cw, ch, 0, 0, cw, ch);
    const cw2 = createCanvas(cw, ch); cw2.getContext('2d').drawImage(W, sx, sy, cw, ch, 0, 0, cw, ch);
    const bd = cb.getContext('2d').getImageData(0, 0, cw, ch).data;
    const wd = cw2.getContext('2d').getImageData(0, 0, cw, ch).data;
    const out = createCanvas(cw, ch); const octx = out.getContext('2d'); const oid = octx.createImageData(cw, ch);
    for (let p = 0; p < cw * ch; p++) {
      const i = p * 4;
      const ar = 1 - (wd[i] - bd[i]) / 255, ag = 1 - (wd[i + 1] - bd[i + 1]) / 255, ab = 1 - (wd[i + 2] - bd[i + 2]) / 255;
      let a = (ar + ag + ab) / 3; if (a < 0) a = 0; if (a > 1) a = 1;
      if (a < 0.004) { oid.data[i + 3] = 0; continue; }
      oid.data[i] = Math.min(255, Math.max(0, Math.round(bd[i] / a)));
      oid.data[i + 1] = Math.min(255, Math.max(0, Math.round(bd[i + 1] / a)));
      oid.data[i + 2] = Math.min(255, Math.max(0, Math.round(bd[i + 2] / a)));
      oid.data[i + 3] = Math.round(a * 255);
    }
    octx.putImageData(oid, 0, 0);
    await saveFile('Content/' + r.name + '.png', out);
    done.push(r.name + ' ' + cw + 'x' + ch);
  }
  log('rune tokens rebuilt: ' + done.join(', '));
  return done;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_runeSlice };
