/* Roguebane — roster mockup builder (persistent, per golden rule).
 * Composes the flattened, gear-mounted figure thumbnails in proto/roster/ into the single labelled
 * review strip proto/roster_mockup.png — the regression reference for assembled figures (ART_RULES §
 * "rebuild proto/roster_mockup.png after any change that affects assembled figures").
 *
 * ONE uniform scale is applied to every flat and all figures are bottom-aligned, so relative sizes
 * (ogre/troll tower over the human cores) read true. Crisp nearest-neighbor only.
 *
 * Run via run_script:
 *   eval(await readFile('proto/roster_mockup_gen.js'));
 *   await RB_buildRosterMockup({ readImage, createCanvas, saveFile, log });
 */
globalThis.RB_buildRosterMockup = async function (H) {
  const { readImage, createCanvas, saveFile, log } = H;
  // display order: all 4 races × 6 cores (race×core morph regression grid), then the standalone foes
  const RACES = ['human', 'elf', 'dwarf', 'halfling', 'half_giant'], CORES = ['grunt','warden','adept','summoner','reaver','ranger','barbarian'];
  const FIGS = [].concat(...RACES.map(r => CORES.map(c => r + '_' + c)))
                 .concat(['skeleton','bandit','wraith','ogre','troll','gargoyle']);
  const C = { bg:'#2b2540', label:'#7a6f92' };
  const TARGET_H = 440;   // px the TALLEST figure maps to
  const GAP = 26, PADX = 34, PADTOP = 34, LABEL_H = 26, BASELINE_PAD = 30;

  const imgs = {};
  for (const f of FIGS) { try { imgs[f] = await readImage('proto/roster/'+f+'.png'); } catch(e){ imgs[f]=null; log('MISSING '+f); } }
  const maxH = Math.max(...FIGS.map(f => imgs[f] ? imgs[f].height : 0));
  const scale = TARGET_H / maxH;

  const cellW = FIGS.map(f => imgs[f] ? Math.round(imgs[f].width*scale) : 40);
  const W = PADX*2 + cellW.reduce((a,b)=>a+b,0) + GAP*(FIGS.length-1);
  const figAreaH = Math.round(maxH*scale);
  const baseY = PADTOP + figAreaH;
  const H_ = baseY + BASELINE_PAD + LABEL_H;

  const cv = createCanvas(W, H_); const ctx = cv.getContext('2d');
  ctx.fillStyle = C.bg; ctx.fillRect(0,0,W,H_);
  ctx.imageSmoothingEnabled = false;

  let x = PADX;
  FIGS.forEach((f,i) => {
    const im = imgs[f]; const w = cellW[i];
    if (im) {
      const h = Math.round(im.height*scale);
      ctx.drawImage(im, x, baseY - h, w, h);
    }
    ctx.fillStyle = C.label; ctx.font = '13px monospace'; ctx.textAlign = 'center'; ctx.textBaseline = 'alphabetic';
    ctx.fillText(f, x + w/2, baseY + BASELINE_PAD + 14);
    x += w + GAP;
  });

  await saveFile('proto/roster_mockup.png', cv);
  log('roster_mockup.png rebuilt: '+W+'×'+H_+' — '+FIGS.join(', '));
};
