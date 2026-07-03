// Roguebane — SCREEN CAPTURE PREP (persistent, capture-pipeline side).
// Contract (payload 2026-07-03 #11): every design/0N-*.png and reference/screens/*.png must be
// EXACTLY 1920×1080 — the native size of the [data-screen] roots (data-design="1920,1080"),
// i.e. 2× the 960×540 design space. No pane-fit scaling, no warping, no scrollbar loss.
//
// RUN (inside the capture eval, before save_screenshot):
//   eval(await fetch('proto/screen_capture_prep.js',{cache:'no-store'}).then(r=>r.text()));
//   await RB_prepScreenCapture();   // returns {w,h} of the root it prepared
//
// WHAT it does:
//  1. Loads + applies the frame render shim (html-to-image drops border-image — see
//     proto/frame_render_shim.js) unless already applied.
//  2. Finds the [data-screen] root, clears ANY transform on it and every ancestor up to <body>
//     (the DC host may fit-scale the page to the preview pane — that scale is what produced the
//     old warped 924×540 refs; it must never reach a reference render).
//  3. Pins <html>/<body> to exactly the root's design size (from data-design, fallback to the
//     root's own width/height style), margin 0, overflow hidden, so the captured document node
//     is exactly design-sized with nothing clipped by the pane viewport.
//  4. Freezes animations (animation:none on a global style tag) so pulse/torch/glow states
//     capture at their CANONICAL frame, not a random mid-tween one.
//
// NOTE: capture with save_screenshot hq:true to a FRESH path (the tool caches by save_path).
//
// ⚠ THE PANE IS SMALLER THAN THE SCREEN (measured 924×540 on 2026-07-03): a single pane capture
// can NEVER be 1920×1080 — that is exactly how the old warped refs happened. So references are
// captured as TILES and stitched (proto/screen_stitch.md documents the run recipe):
//   RB_prepScreenCapture() once, then per step i: RB_tile(i) → capture → stitcher crops each
//   tile to TILE_W×TILE_H (640×360, safe for any pane ≥ that) and blits at its grid offset onto
//   a 1920×1080 canvas. Grid: 3 cols × 3 rows, x∈{0,640,1280}, y∈{0,360,720}, index i = row*3+col.

window.RB_prepScreenCapture = async function RB_prepScreenCapture() {
  // 1. frame shim
  if (!window.RB_applyFrameShim) {
    eval(await fetch('proto/frame_render_shim.js', { cache: 'no-store' }).then(r => r.text()));
  }
  await window.RB_applyFrameShim();

  // 2. root + de-scale
  const root = document.querySelector('[data-screen]') || document.querySelector('[data-screen-label]');
  if (!root) throw new Error('RB_prepScreenCapture: no [data-screen] / [data-screen-label] root found');
  let el = root;
  while (el && el !== document.documentElement) {
    if (el.style) { el.style.transform = 'none'; el.style.zoom = '1'; }
    el = el.parentElement;
  }

  // 3. pin geometry
  const dd = (root.getAttribute('data-design') || '').split(',').map(n => parseInt(n, 10));
  const w = dd[0] || root.offsetWidth, h = dd[1] || root.offsetHeight;
  for (const node of [document.documentElement, document.body]) {
    node.style.margin = '0'; node.style.padding = '0';
    node.style.width = w + 'px'; node.style.height = h + 'px';
    node.style.overflow = 'hidden';
  }
  root.style.position = 'absolute'; root.style.left = '0'; root.style.top = '0';

  // 4. freeze animations at canonical frame
  if (!document.getElementById('rb-capture-freeze')) {
    const st = document.createElement('style');
    st.id = 'rb-capture-freeze';
    st.textContent = '*{animation:none !important;transition:none !important;}';
    document.head.appendChild(st);
  }
  return { w, h };
};

// Position the [data-screen] root so that tile i's region sits at the pane's top-left, and pin
// the visible document window to exactly one tile so the stitcher can crop deterministically.
window.RB_TILE = { w: 640, h: 360, cols: 3, rows: 3 };
window.RB_tile = function RB_tile(i) {
  const T = window.RB_TILE;
  const root = document.querySelector('[data-screen]') || document.querySelector('[data-screen-label]');
  const x = (i % T.cols) * T.w, y = Math.floor(i / T.cols) * T.h;
  root.style.left = -x + 'px'; root.style.top = -y + 'px';
  for (const node of [document.documentElement, document.body]) {
    node.style.width = T.w + 'px'; node.style.height = T.h + 'px'; node.style.overflow = 'hidden';
  }
  return { i, x, y };
};
