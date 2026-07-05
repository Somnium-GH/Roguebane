// Roguebane — INCREMENTAL asset manifest update (persistent). The fast path for a PURE ADD/REMOVE of a
// handful of PNGs (e.g. capturing a few technique glyphs): update asset-manifest.js + Content.mgcb by
// EDITING the sorted list, never by re-walking the ~3000-file Content tree. The full-disk walkers
// (asset_manifest_gen.js / mgcb_gen.js) stay the periodic reconciliation; use them when MANY assets
// churn or to prove no drift. For a small delta they are pure waste — O(all) work for O(few) change.
//
// Both files are a deterministic projection of ONE sorted PNG list, so we keep the manifest array as
// the source list and REBUILD mgcb from it (preserving the .json /copy blocks parsed out of the old
// mgcb) — no disk walk needed and the two can't drift from each other.
//
// Run via run_script:
//   eval(await readFile('proto/asset_incremental.js'));
//   await RB_addAssets({ readFile, saveFile, log,
//     add:    ['Content/icons/technique/bind.png', ...],   // Content-relative, .png
//     remove: [],                                          // optional
//     drop:   'drop' });                                   // optional: mirror both files into the drop
//
// Idempotent: re-adding an existing path is a no-op. Returns the new PNG count.

const RB_MGCB_HEADER =
`#----------------------------- Global Properties ----------------------------#

/outputDir:bin/$(Platform)
/intermediateDir:obj/$(Platform)
/platform:DesktopGL
/config:
/profile:Reach
/compress:False

#-------------------------------- References --------------------------------#


#---------------------------------- Content ---------------------------------#
`;
const RB_mgcbTex = (p) =>
`#begin ${p}
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:ColorKeyEnabled=False
/processorParam:GenerateMipmaps=False
/processorParam:PremultiplyAlpha=True
/processorParam:ResizeToPowerOfTwo=False
/processorParam:MakeSquare=False
/processorParam:TextureFormat=Color
/build:${p}
`;
const RB_mgcbCopy = (p) =>
`#begin ${p}
/copy:${p}
`;

function RB_mgcbFromList(pngsContentRel, copiesRel) {
  const pngs = pngsContentRel.map(p => p.replace(/^Content\//, '')).sort();
  let out = RB_MGCB_HEADER + '\n';
  out += pngs.map(RB_mgcbTex).join('\n') + '\n';
  out += copiesRel.map(RB_mgcbCopy).join('\n');
  return out;
}

async function RB_addAssets(env) {
  const { readFile, saveFile, log } = env;
  const add = (env.add || []).map(p => p.startsWith('Content/') ? p : 'Content/' + p);
  const remove = new Set((env.remove || []).map(p => p.startsWith('Content/') ? p : 'Content/' + p));

  // 1) manifest list = source of truth for the sorted PNG set
  const manRaw = await readFile('asset-manifest.js');
  const arr = JSON.parse(manRaw.slice(manRaw.indexOf('['), manRaw.lastIndexOf(']') + 1));
  const set = new Set(arr);
  add.forEach(p => set.add(p));
  remove.forEach(p => set.delete(p));
  const pngs = [...set].sort();
  const manifest = 'window.ROGUEBANE_ASSETS = ' + JSON.stringify(pngs) + ';\n';
  await saveFile('asset-manifest.js', manifest);

  // 2) rebuild mgcb from the list, PRESERVING the .json /copy blocks already in it (no disk walk)
  const mgcbRaw = await readFile('Content/Content.mgcb');
  const copies = [...mgcbRaw.matchAll(/\/copy:(\S+)/g)].map(m => m[1]).sort();
  const mgcb = RB_mgcbFromList(pngs, copies);
  await saveFile('Content/Content.mgcb', mgcb);

  // 3) mirror into the drop so an incremental add lands straight in the handoff — no re-stage
  if (env.drop) {
    await saveFile(env.drop + '/Roguebane.Content/Content.mgcb', mgcb);
    await saveFile(env.drop + '/design/dchtml/asset-manifest.js', manifest);
  }
  log('manifest: ' + pngs.length + ' PNGs (+' + add.length + ' / -' + remove.size + '); mgcb ' + copies.length + ' copies' + (env.drop ? '; mirrored to ' + env.drop : ''));
  return pngs.length;
}

if (typeof module !== 'undefined' && module.exports) module.exports = { RB_addAssets, RB_mgcbFromList };
