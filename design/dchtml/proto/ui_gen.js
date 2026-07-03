/* Roguebane UI button generator — THE persistent source of truth for ui/button/*.png.
 * Golden rule: every asset is script-generated. Regenerate via run_script:
 *   eval(await readFile('proto/ui_gen.js')); const out = buildButtons(createCanvas);
 *   for (const [name, cv] of out) await saveFile('Content/ui/button/'+name+'.png', cv);
 *
 * Tier-1 button skin @ 320x88 — 2× the 160×44 design box, painted at native 1080-class density
 * (LAYOUT_CONTRACT §11: never author at 540 and upscale). 9-sliceable (corners 12px, so safe-stretch
 * the middle; all metrics are K=2× the original spec):
 *   - HARD 2px black outer border (#0a0807) — the locked UI border, same as pips/chips.
 *   - a thin engraved dark line just inside the border (double-line, matches the ui/frame/* family)
 *   - 1px state-coloured inner accent line (this is what makes the states read at a glance: brown=idle,
 *     amber=hover/on, dim=disabled).
 *   - flat fill + a soft top-sheen gloss (glossy highlight gradient across the upper ~55%, INSET/dark
 *     for the pressed state) + a 1px bevel: RAISED (light top / dark bottom) for normal·hover·on,
 *     INSET (dark top / light bottom) for down, NONE for disabled (flat + low-contrast, no gloss/rivets).
 *   - small corner rivet dots (tinted with the accent) echoing the panel/card 9-slice frame's corner
 *     medallions, so button chrome reads as the same carved-metal family (hi-fi chrome pass v2).
 * Labels are runtime text drawn over the skin (never baked): light ink on the dark frames,
 * dark ink (#1a130c) on `button_on`.
 */
function buildButtons(createCanvas) {
  const K = 2;                        // density scale — §11 1080-class (2× the 960×540 design grid)
  const W = 160 * K, H = 44 * K, OB = 2 * K; // size + outer black border thickness
  const INK = [10, 8, 7];
  // outer = black always; inner = accent line; fill = panel; bevel = raised|inset|none
  const FRAMES = {
    button_normal:   { inner: [90, 70, 54],   fill: [42, 32, 24],  bevel: 'raised' },
    button_hover:    { inner: [217, 164, 65],  fill: [54, 41, 26],  bevel: 'raised' }, // amber accent = interactive
    button_down:     { inner: [74, 58, 44],   fill: [27, 20, 13],  bevel: 'inset'  },
    button_disabled: { inner: [58, 46, 36],   fill: [34, 27, 19],  bevel: 'none'   }, // flat, low contrast
    button_on:       { inner: [240, 198, 110], fill: [217, 164, 65], bevel: 'raised' }, // lit toggle (amber)
  };

  function frame(spec) {
    const cv = createCanvas(W, H);
    const ctx = cv.getContext('2d');
    ctx.imageSmoothingEnabled = false;
    const rgb = a => `rgb(${a[0]},${a[1]},${a[2]})`;
    const [fr, fg, fb] = spec.fill;
    // 1) black outer border
    ctx.fillStyle = rgb(INK); ctx.fillRect(0, 0, W, H);
    // 2) inner accent line (1px ring just inside the border)
    ctx.fillStyle = rgb(spec.inner); ctx.fillRect(OB, OB, W - OB * 2, H - OB * 2);
    // 2b) thin engraved dark line just inside the accent — double-line, matches the ui/frame/* family
    const g = OB + K;
    ctx.fillStyle = 'rgba(0,0,0,.4)'; ctx.fillRect(g, g, W - g * 2, H - g * 2);
    // 3) flat fill inside the accent
    const i = g + K, iw = W - i * 2, ih = H - i * 2;
    ctx.fillStyle = rgb(spec.fill); ctx.fillRect(i, i, iw, ih);
    // 3b) soft top-sheen gloss (skip on disabled — stays flat/dead per the locked idiom)
    if (spec.bevel !== 'none') {
      const sheen = ctx.createLinearGradient(0, i, 0, i + ih * 0.55);
      sheen.addColorStop(0, spec.bevel === 'inset' ? 'rgba(0,0,0,.18)' : 'rgba(255,255,255,.14)');
      sheen.addColorStop(1, 'rgba(255,255,255,0)');
      ctx.fillStyle = sheen; ctx.fillRect(i, i, iw, Math.round(ih * 0.55));
    }
    // 4) bevel
    if (spec.bevel !== 'none') {
      const lite = `rgb(${Math.min(255, fr + 30)},${Math.min(255, fg + 27)},${Math.min(255, fb + 21)})`;
      const dark = `rgb(${Math.max(0, fr - 34)},${Math.max(0, fg - 30)},${Math.max(0, fb - 22)})`;
      const top = spec.bevel === 'raised' ? lite : dark;
      const bot = spec.bevel === 'raised' ? dark : lite;
      ctx.fillStyle = top; ctx.fillRect(i, i, iw, K);
      ctx.fillStyle = bot; ctx.fillRect(i, H - i - K, iw, K);
    }
    // 5) corner rivet dots — small tinted studs echoing the ui/frame/* corner medallions (skip on disabled)
    if (spec.bevel !== 'none') {
      const rr = Math.max(1, Math.round(OB * 0.9));
      const pad = OB + 3 * K;
      ctx.fillStyle = rgb(spec.inner);
      [[pad, pad], [W - pad, pad], [pad, H - pad], [W - pad, H - pad]].forEach(([cx, cy]) => {
        ctx.beginPath(); ctx.arc(cx, cy, rr, 0, Math.PI * 2); ctx.fill();
      });
    }
    return cv;
  }

  return Object.entries(FRAMES).map(([name, spec]) => [name, frame(spec)]);
}
if (typeof module !== 'undefined') module.exports = { buildButtons };
