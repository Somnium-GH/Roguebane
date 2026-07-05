// Roguebane — asset-manifest.js GENERATOR (persistent; pairs with proto/mgcb_gen.js).
// `Asset Review.dc.html` renders every PNG under Content/ off this baked list, because the browser
// can't scan the filesystem. Rerun after ANY asset add/remove/rename so the review page stays complete.
//
// Run via run_script:
//   eval(await readFile('proto/asset_manifest_gen.js'));
//   await RB_buildAssetManifest({ ls, saveFile, log });

async function RB_buildAssetManifest(env) {
  const { ls, saveFile, log } = env;
  // PARALLEL walk (2026-07-05): the worn tree (4 races × slots × core dirs) pushed a serial
  // per-directory walk past the 30s script budget — fetch every subdirectory level concurrently.
  async function walk(dir) {
    let names; try { names = await ls(dir); } catch (e) { return []; }
    const here = [], subs = [];
    for (const f of names) {
      const p = dir + '/' + f;
      if (/\.png$/i.test(f)) here.push(p);
      else if (!/\.[a-z0-9]+$/i.test(f)) subs.push(walk(p));
    }
    return here.concat(...(await Promise.all(subs)));
  }
  const pngs = (await walk('Content')).sort();
  await saveFile('asset-manifest.js', 'window.ROGUEBANE_ASSETS = ' + JSON.stringify(pngs) + ';\n');
  log('asset-manifest.js: ' + pngs.length + ' PNGs');
  return pngs.length;
}
