// Roguebane — asset-manifest.js GENERATOR (persistent; pairs with proto/mgcb_gen.js).
// `Asset Review.dc.html` renders every PNG under Content/ off this baked list, because the browser
// can't scan the filesystem. Rerun after ANY asset add/remove/rename so the review page stays complete.
//
// Run via run_script:
//   eval(await readFile('proto/asset_manifest_gen.js'));
//   await RB_buildAssetManifest({ ls, saveFile, log });

async function RB_buildAssetManifest(env) {
  const { ls, saveFile, log } = env;
  async function walk(dir, acc) {
    for (const f of await ls(dir)) {
      const p = dir + '/' + f;
      if (/\.png$/i.test(f)) acc.push(p);
      else if (!/\.[a-z0-9]+$/i.test(f)) { try { await walk(p, acc); } catch (e) {} }
    }
    return acc;
  }
  const pngs = (await walk('Content', [])).sort();
  await saveFile('asset-manifest.js', 'window.ROGUEBANE_ASSETS = ' + JSON.stringify(pngs) + ';\n');
  log('asset-manifest.js: ' + pngs.length + ' PNGs');
  return pngs.length;
}
