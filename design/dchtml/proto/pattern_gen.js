// Roguebane — TILEABLE PATTERN GENERATOR (persistent, golden rule). Emits the small set of
// engine-tiled pattern PNGs that CSS can express but layout.json fills cannot (payload 2026-07-03 pm
// residual #6: citymap doomFill's diagonal HAZARD STRIPES extracted as a flat blood→blood "gradient").
// The screen element carries data-image-bind="ui/pattern/<name>"; the engine TILES the PNG across the
// element rect (transparent pixels show the trough beneath, exactly like the CSS alpha stripe).
//
// Run (run_script):
//   var module={exports:{}}; eval(await readFile('proto/pattern_gen.js'));
//   await module.exports.RB_renderPatterns({ createCanvas, saveFile });
// Then rebuild Content.mgcb + asset-manifest.js.
//
// SEAMLESS-45° MATH: a square tile of side S tiles diagonal stripes seamlessly when stripe phase is
// a function of (x+y) mod S. S=26 gives a visual stripe period of 26/√2 ≈ 18.4px — matching the
// screen's authored `repeating-linear-gradient(-45deg, … 9px … 18px)` to within half a pixel.
const RB_PATTERNS = {
  // doom bar covered-ground hazard: stripe A opaque blood #b8382e, stripe B #a23028 ramping
  // alpha 0x10→0xff (the CSS gradient's '#a2302810 9px, #a23028 18px' fade), -45° direction.
  'ui/pattern/doom_stripe': { size: 26, a: [0xb8, 0x38, 0x2e], b: [0xa2, 0x30, 0x28] },
};

async function RB_renderPatterns(env) {
  const { createCanvas, saveFile } = env;
  for (const path in RB_PATTERNS) {
    const P = RB_PATTERNS[path], S = P.size, H = S / 2;
    const c = createCanvas(S, S), x = c.getContext('2d');
    const id = x.createImageData(S, S);
    for (let Y = 0; Y < S; Y++) for (let X = 0; X < S; X++) {
      const ph = (X + Y) % S, j = (Y * S + X) * 4;   // -45° phase
      if (ph < H) { id.data[j] = P.a[0]; id.data[j+1] = P.a[1]; id.data[j+2] = P.a[2]; id.data[j+3] = 255; }
      else { const t = (ph - H) / (H - 1); // alpha ramp 0x10 → 0xff across stripe B
        id.data[j] = P.b[0]; id.data[j+1] = P.b[1]; id.data[j+2] = P.b[2];
        id.data[j+3] = Math.round(0x10 + t * (0xff - 0x10)); }
    }
    x.putImageData(id, 0, 0);
    await saveFile('Content/' + path + '.png', c);
  }
  return Object.keys(RB_PATTERNS).length;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_renderPatterns, RB_PATTERNS };
