// Roguebane — REFERENCE STITCHER (persistent, capture-pipeline side; pairs with
// proto/screen_capture_prep.js). The preview pane (~924×540) is smaller than the 1920×1080
// screen roots, so refs are captured as a 3×3 grid of 640×360 tiles (RB_tile(i)) and stitched
// here into an EXACT 1920×1080 PNG — the reference export contract (payload 2026-07-03 #11:
// every design/0N-*.png and reference/screens/*.png must be exactly 2× the 960×540 design space;
// the old pane-fit captures shipped warped 924×540 refs).
//
// RUN (inside run_script, after save_screenshot wrote tiles 01-t.png … 09-t.png to <tileDir>):
//   eval(await readFile('proto/ref_stitch.js'));
//   await RB_stitchRef({ readImage, createCanvas, saveFile, log },
//                      'scratch_tiles/enc_b', ['design/01-encounter.png', 'reference/screens/encounter.png']);
//
// ⚠ save_screenshot caches by save_path — every capture run needs a FRESH tile dir.
// ⚠ view_image can ALSO serve stale bytes for a just-overwritten path — verify a rewritten ref by
//   copying it to a fresh scratch path first (2026-07-03: cost a false "stale render" chase).

async function RB_stitchRef(env, tileDir, outPaths) {
  const { readImage, createCanvas, saveFile, log } = env;
  // env.skip (default 0): number of leading non-tile captures in the dir — a capture run whose
  // step 0 is the async prep (load pipeline + RB_prepScreenCapture) writes 01-t.png as a throwaway
  // pane shot and the real tiles land at 02..10; pass skip:1 for that pattern.
  const skip = env.skip || 0;
  const T = { w: 640, h: 360, cols: 3, n: 9 };
  const canvas = createCanvas(1920, 1080);
  const ctx = canvas.getContext('2d');
  for (let i = 0; i < T.n; i++) {
    const img = await readImage(tileDir + '/' + String(i + 1 + skip).padStart(2, '0') + '-t.png');
    if (img.width < T.w || img.height < T.h) throw new Error('tile ' + i + ' smaller than tile size: ' + img.width + 'x' + img.height + ' — pane too small?');
    ctx.drawImage(img, 0, 0, T.w, T.h, (i % T.cols) * T.w, Math.floor(i / T.cols) * T.h, T.w, T.h);
  }
  for (const p of outPaths) await saveFile(p, canvas);
  log('stitched 1920×1080 → ' + outPaths.join(', '));
}
