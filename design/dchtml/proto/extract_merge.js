// Roguebane — EXTRACT MERGE (persistent, run-script side; pairs with proto/extract_all.html's PNG
// data channel). Decodes the grey-nibble-block segment captures (save_screenshot in_memory_png_key)
// back into the extract JSON and merges {screens, templates, style} into Content/layout.json,
// preserving the roster-generated `figures` / `gear` sections.
//
// Run (run_script), after capturing the segments to key 'rbx':
//   var module={exports:{}}; eval(await readFile('proto/extract_merge.js'));
//   log(await module.exports.RB_mergeExtract({ getCaptures, readFile, saveFile, createCanvas, key:'rbx' }));
async function RB_mergeExtract(env) {
  const { getCaptures, readFile, saveFile, createCanvas, key } = env;
  const blobs = await getCaptures(key);
  const B = 4, W = 896;               // must match extract_all.html RB_PNGCH
  const bpr = W / B;
  const segs = [];
  let total = null;
  for (const blob of blobs) {
    const bmp = await createImageBitmap(blob);
    // capture is at CSS scale (viewport-sized shot, canvas at 0,0 maps 1:1) — sample block centers
    // directly (2026-07-03: computing S = bmp.width/W is WRONG — the shot spans the whole viewport,
    // not just the canvas). The 16-level nibble encoding tolerates ±8/channel resample error.
    const c = createCanvas(bmp.width, bmp.height);
    const x = c.getContext('2d');
    x.drawImage(bmp, 0, 0);
    const id = x.getImageData(0, 0, bmp.width, bmp.height).data;
    const nib = (i) => {
      const bx = i % bpr, by = (i / bpr) | 0;
      const j = ((by * B + 2) * bmp.width + (bx * B + 2)) * 4;
      const v = (id[j] + id[j + 1] + id[j + 2]) / 3;
      return Math.max(0, Math.min(15, Math.round(v / 17)));
    };
    const byte = (bi) => (nib(bi * 2) << 4) | nib(bi * 2 + 1);
    if (byte(0) !== 0xA5) throw new Error('bad segment magic: 0x' + byte(0).toString(16));
    const segIdx = byte(1);
    const t = (byte(2) << 24) | (byte(3) << 16) | (byte(4) << 8) | byte(5);
    const segLen = (byte(6) << 8) | byte(7);
    if (total === null) total = t; else if (t !== total) throw new Error('total mismatch');
    const bytes = new Uint8Array(segLen);
    for (let i = 0; i < segLen; i++) bytes[i] = byte(8 + i);
    segs[segIdx] = bytes;
  }
  let n = 0; segs.forEach(s => { n += s.length; });
  if (n !== total) throw new Error('reassembled ' + n + ' of ' + total + ' bytes');
  const all = new Uint8Array(total);
  let o = 0; segs.forEach(s => { all.set(s, o); o += s.length; });
  const json = new TextDecoder().decode(all);
  const extract = JSON.parse(json);                       // throws if any byte corrupted
  const layout = JSON.parse(await readFile('Content/layout.json'));
  layout.screens = extract.screens;
  layout.templates = extract.templates;
  layout.style = extract.style;
  await saveFile('Content/layout.json', JSON.stringify(layout, null, 2));   // pretty — matches repo convention
  const counts = Object.keys(extract.screens).map(s => s + ':' + extract.screens[s].elements.length).join(' ');
  return 'merged ' + total + ' bytes — ' + counts + ' — templates: ' + Object.keys(extract.templates).length;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_mergeExtract };
