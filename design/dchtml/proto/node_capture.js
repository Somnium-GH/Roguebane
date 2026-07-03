// Roguebane — NODE TOKEN capture (RunMap), persistent + reproducible.
// Supersedes the node path in atom_slice.js (RB_buildNodes), whose crop shaved the sub-pixel far
// border → tokens came out clipped flat on the right & bottom. Here the slice pads the crop
// (-1 near / +2 far, per ASSET_GEN_METHOD §5) and recovers transparency by dual-background.
//
// WHY a dedicated file: the RunMap exposes only the 4 ROUND exemplars as live `[data-atom]` nodes
// (camp/resource/merchant/unknown, 56×56). There is no castle BEACON on the map (the castle shows only
// as a top-bar <img>), so the castle token is reconstructed here from RunMap's exact TYPE.castle spec
// + the shared BEV bevel. If a castle beacon is ever added to the map, tag it and switch castle to the
// cloned path like the others.
//
// RUN (per fix):
//   1. show_html("CityMap.dc.html")
//   2. save_screenshot hq (10 steps): each step calls RB_nodeOverlay(nameOrSpec, bg, K) inline, then
//      captures. Order = [camp,resource,merchant,unknown]×{#000,#fff} at K=4, then castle×{#000,#fff}
//      at K=3. A magenta 40px calibration marker at (0,0) makes the slice independent of viewport/DPR.
//   3. run_script: RB_nodeSlice({ readImage, createCanvas, saveFile, log, pairs }).
//   4. rebuild Content.mgcb + asset-manifest.js + audit gate.

// ---- in-page: build the K× capture overlay (KEEP depth: gloss + canonical bevel) over `bg` ----
window.RB_nodeOverlay = function (nameOrSpec, bg, K) {
  var old = document.getElementById('__rbnode'); if (old) old.remove();
  var ov = document.createElement('div');
  ov.id = '__rbnode';
  ov.style.cssText = 'position:fixed;left:0;top:0;z-index:2147483647;margin:0;padding:0;width:100vw;height:100vh;background:' + bg + ';';
  document.body.appendChild(ov);
  // calibration marker — pure magenta 40 CSS px at the fixed origin (0,0)
  var cal = document.createElement('div');
  cal.style.cssText = 'position:absolute;left:0;top:0;width:40px;height:40px;background:#ff00ff;';
  ov.appendChild(cal);

  var BEV = 'inset ' + (3 * K) + 'px ' + (3 * K) + 'px 0 rgba(255,246,222,.16),inset ' + (-4 * K) + 'px ' + (-5 * K) + 'px 0 rgba(0,0,0,.46)';
  var clone;
  if (typeof nameOrSpec === 'string') {
    var src = document.querySelector('[data-atom="' + nameOrSpec + '"]');
    if (!src) return false;
    var w = src.offsetWidth, h = src.offsetHeight;
    clone = src.cloneNode(true);
    clone.removeAttribute('data-atom');
    clone.style.width = (w * K) + 'px'; clone.style.height = (h * K) + 'px';
    var og = src.querySelector('span'), cg = clone.querySelector('span');
    if (og && cg) cg.style.fontSize = (parseFloat(getComputedStyle(og).fontSize) * K) + 'px';
  } else {
    // reconstructed token (castle) from RunMap's exact TYPE spec + shared gloss
    var s = nameOrSpec;
    clone = document.createElement('div');
    clone.style.cssText = 'width:' + (s.base * K) + 'px;height:' + (s.base * K) + 'px;background:' + s.bg
      + ';border-radius:' + s.radius + ';display:flex;align-items:center;justify-content:center;';
    clone.style.border = (3 * K) + 'px ' + (s.bstyle || 'solid') + ' ' + s.bcolor;
    var sp = document.createElement('span');
    sp.textContent = s.glyph;
    sp.style.cssText = "font-family:'JetBrains Mono',monospace;line-height:1;font-size:" + (s.glyphSize * K) + 'px;color:' + s.glyphColor + ';';
    clone.appendChild(sp);
  }
  clone.style.position = 'absolute'; clone.style.left = '60px'; clone.style.top = '60px';
  clone.style.boxSizing = 'border-box'; clone.style.margin = '0';
  clone.style.opacity = '1'; clone.style.animation = 'none'; clone.style.transition = 'none';
  clone.style.borderWidth = (3 * K) + 'px';       // scale border with K (all node borders are 3px)
  clone.style.boxShadow = BEV;                     // canonical bevel (normalise any state glow/ring)
  ov.appendChild(clone);
  return true;
};

// ---- run_script: recover each token from its black/white pair, corrected padded crop ----
async function RB_nodeSlice(env) {
  const { readImage, createCanvas, saveFile, log, pairs } = env;
  const CSSX = 60, CSSY = 60, MARK = 40;

  function toData(img) {
    const c = createCanvas(img.width, img.height);
    c.getContext('2d').drawImage(img, 0, 0);
    return c.getContext('2d').getImageData(0, 0, img.width, img.height);
  }
  // find the magenta marker bbox → origin + scale (px per CSS px)
  function marker(id) {
    let x0 = 1e9, y0 = 1e9, x1 = -1, y1 = -1;
    for (let y = 0; y < id.height; y++) for (let x = 0; x < id.width; x++) {
      const i = (y * id.width + x) * 4;
      if (id.data[i] > 180 && id.data[i + 1] < 90 && id.data[i + 2] > 180) {
        if (x < x0) x0 = x; if (y < y0) y0 = y; if (x > x1) x1 = x; if (y > y1) y1 = y;
      }
    }
    return { x0, y0, s: (x1 - x0 + 1) / MARK };
  }

  const done = [];
  for (const p of pairs) {
    const B = await readImage(p.black), W = await readImage(p.white);
    const bd = toData(B), wd = toData(W);
    const m = marker(bd);
    const cssW = p.K * p.base, cssH = p.K * p.base;
    const sx = Math.max(0, Math.round(m.x0 + CSSX * m.s) - 1);
    const sy = Math.max(0, Math.round(m.y0 + CSSY * m.s) - 1);
    const ex = Math.round(m.x0 + (CSSX + cssW) * m.s) + 2;
    const ey = Math.round(m.y0 + (CSSY + cssH) * m.s) + 2;
    const cw = ex - sx, ch = ey - sy;
    const out = createCanvas(cw, ch);
    const octx = out.getContext('2d');
    const oid = octx.createImageData(cw, ch);
    for (let yy = 0; yy < ch; yy++) for (let xx = 0; xx < cw; xx++) {
      const srcI = ((sy + yy) * bd.width + (sx + xx)) * 4;
      const dstI = (yy * cw + xx) * 4;
      const br = bd.data[srcI], bg = bd.data[srcI + 1], bb = bd.data[srcI + 2];
      const wr = wd.data[srcI], wg = wd.data[srcI + 1], wb = wd.data[srcI + 2];
      let a = 1 - ((wr - br) + (wg - bg) + (wb - bb)) / (3 * 255);
      if (a < 0) a = 0; if (a > 1) a = 1;
      if (a < 0.004) { oid.data[dstI + 3] = 0; continue; }
      oid.data[dstI] = Math.min(255, Math.max(0, Math.round(br / a)));
      oid.data[dstI + 1] = Math.min(255, Math.max(0, Math.round(bg / a)));
      oid.data[dstI + 2] = Math.min(255, Math.max(0, Math.round(bb / a)));
      oid.data[dstI + 3] = Math.round(a * 255);
    }
    octx.putImageData(oid, 0, 0);
    await saveFile('Content/icons/node/' + p.name + '.png', out);
    done.push(p.name + ' ' + cw + 'x' + ch);
  }
  log('nodes rebuilt: ' + done.join(', '));
  return done;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_nodeSlice };
