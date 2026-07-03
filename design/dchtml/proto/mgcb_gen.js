// Roguebane — Content.mgcb GENERATOR (persistent, §6 enforcement).
// The pipeline file must be a deterministic projection of the on-disk asset tree:
// every PNG gets a /build texture block, plain-data files (layout.json) get /copy.
// Run it after ANY asset add/remove so mgcb can never drift from disk again.
//
// Run via run_script:
//   eval(await readFile('proto/mgcb_gen.js'));
//   await RB_buildMgcb({ ls, readFile, saveFile, log });

async function RB_buildMgcb(env) {
  const { ls, saveFile, log } = env;

  async function walk(dir, acc) {
    for (const f of await ls(dir)) {
      const p = dir + '/' + f;
      if (/\.[a-z0-9]+$/i.test(f)) acc.push(p);
      else { try { await walk(p, acc); } catch (e) {} }
    }
    return acc;
  }
  const all = (await walk('Content', [])).map(p => p.replace('Content/', ''));
  const pngs = all.filter(p => /\.png$/i.test(p)).sort();
  const copies = all.filter(p => /\.json$/i.test(p)).sort();   // plain data -> /copy

  const HEADER =
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

  const tex = (p) =>
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

  const copy = (p) =>
`#begin ${p}
/copy:${p}
`;

  let out = HEADER + '\n';
  out += pngs.map(tex).join('\n') + '\n';
  out += copies.map(copy).join('\n');

  await saveFile('Content/Content.mgcb', out);
  log('Content.mgcb rebuilt: ' + pngs.length + ' textures + ' + copies.length + ' copies (' + copies.join(', ') + ')');
  return { pngs: pngs.length, copies: copies.length };
}

if (typeof module !== 'undefined' && module.exports) module.exports = { RB_buildMgcb };
