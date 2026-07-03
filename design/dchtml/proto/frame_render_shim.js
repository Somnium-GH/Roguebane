// Roguebane — FRAME RENDER SHIM (persistent, capture-pipeline side).
// WHY: html-to-image (the engine behind every screenshot/reference render we take) does NOT paint
// CSS `border-image` AT ALL — discovered 2026-07-01. Every design/0N-*.png and reference/screens/*.png
// captured before then silently omitted the ornate 9-slice frames ("never ship a lossy export" — this
// closes that hole). The LIVE browser paints border-image fine (provided the element has a real
// border-style — Chrome skips border-image painting entirely on border-width:0 elements, which is why
// every `data-frame` element must carry `border:<w>px solid transparent` matching its
// border-image-width; see UI_ASSET_MAP.md).
//
// WHAT: before a capture, RB_applyFrameShim() finds every [data-frame] element, reads its COMPUTED
// border-image properties (source, slice, width, repeat), slices the frame PNG into 9 canvases and
// overlays them as plain background-image divs (corners fixed, edges tiled, center tiled) — which
// html-to-image DOES render. The element's own border-image is suppressed while the shim is on.
// Pixel-faithful to the CSS `round` blit up to the tile-count rounding.
//
// RUN (inside the capture eval, before save_screenshot):
//   eval(await fetch('proto/frame_render_shim.js',{cache:'no-store'}).then(r=>r.text()));
//   await RB_applyFrameShim();       // returns count of shimmed elements
//   ...screenshot...
//   RB_clearFrameShim();
window.RB_applyFrameShim = async function () {
  window.RB_clearFrameShim();
  const els = [...document.querySelectorAll('[data-frame]')];
  let n = 0;
  for (const el of els) {
    const cs = getComputedStyle(el);
    const src = (cs.borderImageSource.match(/url\(["']?([^"')]+)["']?\)/) || [])[1];
    if (!src) continue;
    const M = parseFloat(cs.borderImageSlice) || 0;                 // uniform slice (our frames are uniform)
    const w = parseFloat(cs.borderImageWidth) || parseFloat(cs.borderTopWidth) || 0;
    if (!M || !w) continue;
    const blob = await fetch(src, { cache: 'no-store' }).then(r => r.blob());
    const img = await createImageBitmap(blob);
    const S = img.width, T = S - 2 * M;                             // source size, edge/center tile span
    const cut = (sx, sy, sw, sh) => {
      const c = document.createElement('canvas'); c.width = sw; c.height = sh;
      c.getContext('2d').drawImage(img, sx, sy, sw, sh, 0, 0, sw, sh);
      return 'url(' + c.toDataURL() + ')';
    };
    const corner = { tl: cut(0, 0, M, M), tr: cut(S - M, 0, M, M), bl: cut(0, S - M, M, M), br: cut(S - M, S - M, M, M) };
    const edge = { t: cut(M, 0, T, M), b: cut(M, S - M, T, M), l: cut(0, M, M, T), r: cut(S - M, M, M, T) };
    const center = cut(M, M, T, T);
    const tile = w * T / M;                                          // displayed tile length at this scale
    const ov = document.createElement('div');
    ov.className = '__rbframeshim';
    // z-index:-1 keeps the shim UNDER the element's in-flow content (a positioned child would
    // otherwise paint over it); the element's own opaque background is suppressed while the shim is
    // on so the negative-z layer shows through — the shim's opaque center tile takes its place.
    ov.style.cssText = 'position:absolute;inset:0;pointer-events:none;z-index:-1;';
    const part = (css) => { const d = document.createElement('div'); d.style.cssText = 'position:absolute;' + css; ov.appendChild(d); };
    // center first (under the ring)
    part('left:' + w + 'px;top:' + w + 'px;right:' + w + 'px;bottom:' + w + 'px;background-image:' + center
      + ';background-size:' + tile + 'px ' + tile + 'px;background-repeat:repeat;');
    part('left:' + w + 'px;right:' + w + 'px;top:0;height:' + w + 'px;background-image:' + edge.t + ';background-size:' + tile + 'px ' + w + 'px;background-repeat:repeat-x;');
    part('left:' + w + 'px;right:' + w + 'px;bottom:0;height:' + w + 'px;background-image:' + edge.b + ';background-size:' + tile + 'px ' + w + 'px;background-repeat:repeat-x;');
    part('top:' + w + 'px;bottom:' + w + 'px;left:0;width:' + w + 'px;background-image:' + edge.l + ';background-size:' + w + 'px ' + tile + 'px;background-repeat:repeat-y;');
    part('top:' + w + 'px;bottom:' + w + 'px;right:0;width:' + w + 'px;background-image:' + edge.r + ';background-size:' + w + 'px ' + tile + 'px;background-repeat:repeat-y;');
    part('left:0;top:0;width:' + w + 'px;height:' + w + 'px;background-image:' + corner.tl + ';background-size:100% 100%;');
    part('right:0;top:0;width:' + w + 'px;height:' + w + 'px;background-image:' + corner.tr + ';background-size:100% 100%;');
    part('left:0;bottom:0;width:' + w + 'px;height:' + w + 'px;background-image:' + corner.bl + ';background-size:100% 100%;');
    part('right:0;bottom:0;width:' + w + 'px;height:' + w + 'px;background-image:' + corner.br + ';background-size:100% 100%;');
    if (getComputedStyle(el).position === 'static') { el.dataset.rbShimPos = '1'; el.style.position = 'relative'; }
    el.dataset.rbShimBis = el.style.borderImageSource || '';
    el.style.borderImageSource = 'none';                             // suppress the (uncaptured) real one
    el.dataset.rbShimBg = el.style.backgroundColor || '';
    el.style.backgroundColor = 'transparent';                        // let the negative-z shim show through
    el.insertBefore(ov, el.firstChild);
    n++;
  }
  return n;
};
window.RB_clearFrameShim = function () {
  document.querySelectorAll('.__rbframeshim').forEach(e => e.remove());
  document.querySelectorAll('[data-rb-shim-bis]').forEach(e => {
    e.style.borderImageSource = e.dataset.rbShimBis || '';
    delete e.dataset.rbShimBis;
    e.style.backgroundColor = e.dataset.rbShimBg || '';
    delete e.dataset.rbShimBg;
    if (e.dataset.rbShimPos) { e.style.position = ''; delete e.dataset.rbShimPos; }
  });
  return true;
};
