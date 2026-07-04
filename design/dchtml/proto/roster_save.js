// Roguebane — roster SAVE driver (persistent, golden rule: no throwaway inline save/merge logic).
// Runs proto/roster_gen.js (generateAll) and writes its outputs to the project:
//   parts   -> Content/sprites/body/<figure>/<name>_<state>.png   (base | all)
//   flats   -> proto/roster/<figure>.png
//   gear    -> Content/sprites/gear/<id>.png                      ('new' = only ids missing on disk)
//   worn    -> Content/sprites/gear/worn/… (the B12 corrected worn-armor part set, 744 files)
//   layout  -> Content/layout.json  — replaces the generator-owned `figures`/`gear`/`worn` sections,
//              PRESERVES the extraction-owned screens/templates/style (LAYOUT_CONTRACT §9), and
//              refuses the merge if the generator LOST a previously-present figure/gear key
//              (same key-set guard class as extract_merge.js / payload B1b).
//   catalog -> Content/gear_catalog.json
//
// Run via run_script (chunk with opts to stay inside the time budget):
//   var module={exports:{}}; (0,eval)(await readFile('proto/roster_save.js'));
//   await RB_saveRoster({readFile,saveFile,createCanvas,ls,log},
//     { figures:/^human_/, parts:'base', gear:'new', worn:true, layout:true, catalog:true });
async function RB_saveRoster(env, opts) {
  const { readFile, saveFile, createCanvas, ls, log } = env;
  opts = opts || {};
  (0, eval)(await readFile('proto/roster_gen.js'));
  const out = generateAll(createCanvas);
  let saved = 0;
  const isWorn = (n) => /_(str|dex|int)_/.test(n);
  const figRe = opts.figures instanceof RegExp ? opts.figures : (opts.figures ? new RegExp(opts.figures) : null);
  if (figRe) for (const f of out.figures) {
    if (!figRe.test(f.name)) continue;
    if (opts.parts) for (const p of f.parts) {
      if (opts.parts === 'worn' && !isWorn(p.name)) continue;
      if (opts.parts === 'base' && isWorn(p.name)) continue;
      await saveFile('Content/sprites/body/' + f.name + '/' + p.name + '_' + p.state + '.png', p.canvas); saved++;
    }
    if (opts.flats) { await saveFile('proto/roster/' + f.name + '.png', f.flat); saved++; }
  }
  if (opts.gear) {
    const existing = opts.gear === 'new' ? new Set(await ls('Content/sprites/gear')) : null;
    for (const g of out.gear) {
      if (existing && existing.has(g.name + '.png')) continue;
      if (opts.gear instanceof RegExp && !opts.gear.test(g.name)) continue;
      await saveFile('Content/sprites/gear/' + g.name + '.png', g.canvas); saved++;
    }
  }
  if (opts.worn) for (const w of out.worn) { await saveFile('Content/' + w.path, w.canvas); saved++; }
  if (opts.layout) {
    const cur = JSON.parse(await readFile('Content/layout.json'));
    const lost = Object.keys(cur.figures || {}).filter(k => !out.layout.figures[k]).map(k => 'figures.' + k)
      .concat(Object.keys(cur.gear || {}).filter(k => !out.layout.gear[k]).map(k => 'gear.' + k));
    if (lost.length) throw new Error('REFUSING layout merge — generator lost keys vs previous manifest: ' + lost.join(', '));
    cur.figures = out.layout.figures;
    cur.gear = out.layout.gear;
    cur.worn = out.layout.worn;
    await saveFile('Content/layout.json', JSON.stringify(cur, null, 2));
  }
  if (opts.catalog) await saveFile('Content/gear_catalog.json', JSON.stringify(out.gearCatalog, null, 2));
  log('RB_saveRoster: ' + saved + ' sprites' + (opts.layout ? ' + layout.json (figures/gear)' : '') + (opts.catalog ? ' + gear_catalog.json' : ''));
  return saved;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_saveRoster };
