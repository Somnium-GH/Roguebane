// Roguebane — UI ATOM CAPTURE (persistent, repeatable). The atoms are NOT redrawn anywhere: this
// reads the LIVE screen DOM (Encounter.dc.html / Equipment.dc.html), clones every node tagged
// data-atom="<asset/path>" at its NATIVE size into an on-page overlay, and reports each crop rect.
// A screenshot is sliced + nearest-neighbour-upscaled into Content/<path>.png. Because the source
// pixels are the screen's OWN rendered node, the assets can NEVER drift from the screens — there is
// no second copy of the markup. To add an asset: tag the real node with data-atom and re-run.
//
// IMPORTANT: do NOT scale the clone with CSS `transform` — html-to-image then fails to paint the
// element's background-color (borders still draw, fills go black). Capture at NATIVE size in normal
// flow (exactly how the screen renders the node) and upscale in the slice step instead.
//
// 100% REPEATABLE METHOD (run once per screen that owns atoms):
//   1. show_html("Encounter.dc.html")                       // the live source
//   2. eval_js: load this file with {cache:'no-store'} (query strings 403 the preview), then
//      RB_buildCaptureOverlay()  -> {vw, k, rects:[{name,x,y,w,h}]}  (clones rendered at k=4x geometry)
//   3. save_screenshot hq -> "design/_capN.png", with the overlay REBUILT inside the step `code`
//      (eval_js mutations are not reliably seen by a later screenshot — see gotchas).
//   4. run_script: scale = img.width / vw; for each rect crop
//        sx=round(x*scale)-1, sy=round(y*scale)-1, ex=round((x+w)*scale)+2, ey=round((y+h)*scale)+2
//      (the +2 right / +bottom margin captures the sub-pixel-thinned far border; ~1px dark overlay
//      margin is harmless). saveFile('Content/'+name+'.png'). No upscale — k=4 already gives crisp size.
//   5. eval_js: RB_clearCaptureOverlay(); delete the temp screenshot.
//   (repeat for Equipment.dc.html). Then rebuild Content.mgcb + asset-manifest.js + run the audit gate.

window.RB_buildCaptureOverlay = function (bg) {
  window.RB_clearCaptureOverlay();
  const K = 4; // render clones at K× EXPLICIT geometry (NOT transform — transform breaks html-to-image fills)
  const ov = document.createElement('div');
  ov.id = '__rbcap';
  // `bg` lets a caller do dual-background transparency recovery (ASSET_GEN_METHOD dual-bg recipe) for
  // an irregular/round atom — build once over '#000000', screenshot, rebuild over '#ffffff', screenshot.
  // Defaults to the original dark backdrop for existing single-background (opaque-box) callers.
  ov.style.cssText = 'position:fixed;left:0;top:0;z-index:2147483647;background:' + (bg || '#15100b') + ';margin:0;'
    + 'padding:6px;display:flex;flex-wrap:wrap;align-content:flex-start;align-items:flex-start;'
    + 'gap:10px;width:calc(100vw - 12px);';
  document.body.appendChild(ov);
  const scalePx = (s) => (s || '').replace(/([\d.]+)px/g, (m, n) => (parseFloat(n) * K) + 'px'); // scale every Npx in a value
  const nodes = [...document.querySelectorAll('[data-atom]')].filter(n => (n.getAttribute('data-atom') || '').trim());
  const items = [];
  nodes.forEach(n => {
    const w = n.offsetWidth, h = n.offsetHeight;
    if (!w || !h) return;
    const cs = getComputedStyle(n);
    const clone = n.cloneNode(true);
    clone.removeAttribute('data-atom');
    clone.style.boxSizing = 'border-box';
    clone.style.width = (w * K) + 'px'; clone.style.height = (h * K) + 'px';
    clone.style.flex = '0 0 auto'; clone.style.margin = '0'; clone.style.position = 'relative';
    clone.style.animation = 'none'; clone.style.transition = 'none'; clone.style.opacity = '1';
    clone.style.boxShadow = 'none';  // drop bevel/glow → flat Tier-1 asset (gradients dropped below too)
    clone.style.display = (cs.display === 'inline') ? 'block' : cs.display; // inline <span> pips ignore w/h
    // single-layer pieces html-to-image renders cleanly: solid colour on the clone
    clone.style.background = 'none';
    clone.style.backgroundColor = cs.backgroundColor;
    // scale fonts + borders across the WHOLE subtree so child glyphs/borders grow with K (do this
    // BEFORE inserting any hatch child, so the orig/clone node lists stay index-aligned)
    const oAll = [n, ...n.querySelectorAll('*')], cAll = [clone, ...clone.querySelectorAll('*')];
    oAll.forEach((o, i) => { const c2 = getComputedStyle(o), cl = cAll[i]; if (!cl) return;
      cl.style.fontSize = (parseFloat(c2.fontSize) * K) + 'px';
      cl.style.borderWidth = scalePx(c2.borderWidth);
    });
    // re-add ONLY hatch patterns (repeating-linear-gradient) as a child; decorative gloss
    // (radial-/linear-gradient highlights) is intentionally DROPPED → flat Tier-1 asset.
    if (cs.backgroundImage && cs.backgroundImage.includes('repeating-linear-gradient')) {
      const hatch = document.createElement('div');
      hatch.style.cssText = 'position:absolute;left:0;top:0;width:100%;height:100%;pointer-events:none;background-image:' + scalePx(cs.backgroundImage) + ';';
      clone.insertBefore(hatch, clone.firstChild);
    }
    ov.appendChild(clone);
    items.push({ name: n.getAttribute('data-atom').trim(), clone });
  });
  const rects = items.map(it => {
    const b = it.clone.getBoundingClientRect();
    return { name: it.name, x: Math.round(b.left), y: Math.round(b.top), w: Math.round(b.width), h: Math.round(b.height) };
  });
  return JSON.stringify({ vw: window.innerWidth, vh: window.innerHeight, k: K, rects });
};

window.RB_clearCaptureOverlay = function () {
  const o = document.getElementById('__rbcap');
  if (o) o.remove();
  return true;
};

// SYNTHETIC technique-chip overlay — for a technique the locked Encounter layout does NOT show
// (e.g. `shot`, the bow's shield-piercing shot: the approved action row has exactly 5 cards and
// adding a 6th would be layout drift, LAYOUT_CONTRACT §9 "NO LAYOUT DRIFT"). Reconstructs the chip
// from the SAME spec the screen's techCard template uses — 30×30 box, 2px solid #0a0807 border,
// 15px/700 JetBrains Mono glyph in #0a0807 — at K=4 explicit geometry. Same precedent as
// node_capture.js's castle reconstruction. MUST run on a screen that has the fonts loaded
// (Encounter.dc.html). Slice with atom_slice.js RB_buildTechChips({ techs, y: 6, … }).
window.RB_buildChipOverlay = function (specs, bg) {
  window.RB_clearCaptureOverlay();
  const K = 4;
  const ov = document.createElement('div');
  ov.id = '__rbcap';
  ov.style.cssText = 'position:fixed;left:0;top:0;z-index:2147483647;background:' + (bg || '#15100b') + ';margin:0;'
    + 'padding:6px;display:flex;flex-wrap:wrap;align-content:flex-start;align-items:flex-start;gap:10px;width:calc(100vw - 12px);';
  document.body.appendChild(ov);
  const rects = specs.map(s => {
    const chip = document.createElement('span');
    chip.style.cssText = 'box-sizing:border-box;flex:0 0 auto;width:' + (30 * K) + 'px;height:' + (30 * K) + 'px;'
      + 'border:' + (2 * K) + 'px solid #0a0807;background:' + s.glyphBg + ';display:flex;align-items:center;'
      + "justify-content:center;font-family:'JetBrains Mono',monospace;font-size:" + (15 * K) + 'px;color:#0a0807;font-weight:700;';
    chip.textContent = s.glyph;
    ov.appendChild(chip);
    const b = chip.getBoundingClientRect();
    return { name: s.name, x: Math.round(b.left), y: Math.round(b.top), w: Math.round(b.width), h: Math.round(b.height) };
  });
  return JSON.stringify({ vw: window.innerWidth, k: K, rects });
};
