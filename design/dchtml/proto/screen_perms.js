// Roguebane — SCREEN PERMUTATION RENDER DRIVER (persistent, capture-pipeline side).
// Pairs with proto/screen_capture_prep.js (geometry pin + 3×3 tiler) and proto/ref_stitch.js
// (tile → 1920×1080 stitch). Encounter.dc.html and Equipment.dc.html are core-parameterised
// (a `core` enum prop, see core-kits.js): each of the six cores is a distinct whole-screen state,
// so — per CLAUDE.md "Exercising dynamic states" — every core is shipped as its OWN design render
// (design/01-encounter-<core>.png, design/02-equipment-<core>.png + reference/screens/*). This file
// is the reproducible source for that permutation set: the core order + the setProps driver + the
// per-permutation run recipe. No annotation labels are ever added — each screen self-identifies via
// its in-UI core name / figure (these renders are the direct game-dev correctness reference).

// Canonical core order (matches core-kits.js ORDER + the NewGame grid).
window.RB_CORES = ['grunt', 'warden', 'adept', 'summoner', 'reaver', 'ranger'];

// Set the open screen's lit core and wait for the React re-render to settle.
// Uses the DC host bridge: __dcSetProps(rootName, overrides) → runtime.setProps → bump → re-render.
window.RB_setCore = async function RB_setCore(core, waitMs) {
  const root = window.__dcRootName ? window.__dcRootName() : null;
  if (!root) throw new Error('RB_setCore: no DC root (is a screen open?)');
  window.__dcSetProps(root, { core });
  await new Promise(r => setTimeout(r, waitMs || 900)); // let async setState/render flush
  return root + ' → core=' + core;
};

// ─── RUN RECIPE (per permutation) ──────────────────────────────────────────────────────────────
// The preview pane (~924×540) is smaller than the 1920×1080 screen root, so a permutation is
// captured as a 3×3 grid of 640×360 tiles and stitched. save_screenshot caches by save_path, so
// every permutation writes to a FRESH tile dir.
//
//   0. show_html('Encounter.dc.html')                       // open the screen once
//   1. eval_js (async IIFE):                                // load pipeline + set the core + prep
//        const load = s => new Promise((res,rej)=>{const e=document.createElement('script');
//          e.src=s+'?'+Date.now();e.onload=res;e.onerror=rej;document.head.appendChild(e);});
//        for (const s of ['proto/frame_render_shim.js','proto/screen_capture_prep.js',
//                         'proto/ref_stitch.js','proto/screen_perms.js'])
//          if (!/* its global */) await load(s);
//        await RB_setCore('warden');                        // ← the permutation
//        await RB_prepScreenCapture();                      // de-scale + pin 1920×1080 + freeze
//   2. save_screenshot hq:true, save_path 'scratch_tiles/enc_warden/t.png',
//        steps = [{code:'RB_tile(0)'},…,{code:'RB_tile(8)'}]  // → 01-t.png … 09-t.png
//   3. run_script: RB_stitchRef({readImage,createCanvas,saveFile,log},
//        'scratch_tiles/enc_warden',
//        ['design/01-encounter-warden.png','reference/screens/encounter-warden.png']);
//
// Loop step 1's core over RB_CORES for Encounter, then Equipment (design/02-equipment-<core>.png).
// NewGame is NOT permutated — it shows all six cores in one grid → single design/05-newgame.png.
window.RB_PERM_OUT = function RB_PERM_OUT(screenNum, screenSlug, core) {
  return ['design/' + screenNum + '-' + screenSlug + '-' + core + '.png',
          'reference/screens/' + screenSlug + '-' + core + '.png'];
};
