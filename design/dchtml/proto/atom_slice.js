// Roguebane — ATOM SLICE / BUILD (persistent, run-script side). Produces the Tier-1 UI atoms that the
// Combat screen defines: the 5 pool pips and the 5 technique glyph chips. Reproducible — NO throwaway
// inline scripts (golden rule). Two halves:
//
//   RB_renderPips(env)      — FULLY DETERMINISTIC from the Encounter tokens (no screenshot needed). The pip
//                             is a procedural tile: a solid/hatch fill + a stamped 1px(×4) black frame.
//                             Tokens are mirrored from attribute-model.js / Encounter.dc.html exactly, so the
//                             asset matches the screen by construction (shared tokens = in sync).
//   RB_buildTechChips(env)  — deterministic chip (exact glyphBg + size + black frame) with the GLYPH
//                             lifted from a live-screen capture as an alpha mask and re-centred by its ink
//                             bbox. The chip/colour/border are forced; only the glyph SHAPE comes from the
//                             screen (so the screen's font rendering is preserved, but position is exact).
//                             A border-flood removes the chip's own border from the glyph mask.
//
// REPEATABLE WORKFLOW (see LAYOUT_CONTRACT §12):
//   1. show_html("Encounter.dc.html"); save_screenshot hq -> "design/_cap.png" with the capture overlay
//      built inside the step (RB_buildCaptureOverlay from atom_capture.js).
//   2. run_script:
//        eval(await readFile('proto/atom_slice.js'));
//        await RB_renderPips({ createCanvas, saveFile });
//        await RB_buildTechChips({ readImage, createCanvas, saveFile, capture:'design/_cap.png', vw:924 });
//   3. delete the temp screenshot; rebuild Content.mgcb + asset-manifest.js + audit gate.
//
// The overlay lays atoms at k=4x geometry: pips row at y6 (h80), chips row at y96 (h120); x positions per
// CAP_RECTS below (the data-atom order in Combat). If that layout changes, update CAP_RECTS.

// Pip colour tokens — attribute-model.js COLOR + the RunMap resource pools (Supplies/Support).
// A pip is a flat token (fill or hatch) + a stamped black frame; the special resource EMPTIES use a
// dashed coloured frame exactly as the screens draw them. Token-stamped (not screenshot-captured)
// because a pip is fully specified by tokens — fill+frame+hatch — so it matches the screen by
// construction and avoids the sub-pixel-border capture fight. (Glyph/font atoms still use capture.)
const RB_PIP_COLORS = { str: '#c2553f', int: '#6f8fc4', dex: '#82a85e', con: '#cf9a44', supplies: '#7fa05a', support: '#d9a441' };
const RB_PIP_ATTR = ['str', 'int', 'dex', 'con'];          // pools that gear-reserve (get a reserved variant)
const RB_PIP_EMPTY_FILL = '#241b14', RB_PIP_INK = '#0a0807', RB_PIP_EQH = '#0a0807aa';
const RB_PIP_SPECIAL_EMPTY = { supplies: '#5a4636', support: '#d9a441' }; // dashed-frame colour per resource pool
const RB_TECHS = [ // [name, glyphBg, overlay-x] — glyphBg from Combat technique defs
  ['swing', '#c2553f', 6], ['frenzy', '#46434e', 136], ['firebolt', '#6f8fc4', 266],
  ['disarm', '#c2553f', 396], ['brace', '#cf9a44', 526],
];
const RB_TECHS_SYNTH = [ // techniques with NO card on the locked Encounter layout (adding one would be
  // layout drift) — chip reconstructed by atom_capture.js RB_buildChipOverlay from the exact techCard
  // chip spec, overlay row starts at x=6,y=6 (overlay padding). Slice: RB_buildTechChips({techs:RB_TECHS_SYNTH, y:6, …}).
  ['shot', '#82a85e', 6, '➳'], // bow's shield-piercing shot — DEX green, feathered-arrow glyph
  // 2026-07-03 pm reconcile residual #4: the engine's built roster uses these 8 with no card on any
  // locked screen — same synth pipeline. glyphBg = canonical attribute colour; glyphs stay in the
  // established pictogram family (⚔ ⇶ ✦ ✂ ◈ ➳). Capture in BATCHES of ≤5 chips per overlay/screenshot
  // (a longer row wraps at narrow viewports and the x list would drift): x = 6 + i*130 within a batch.
  // Slice each batch: RB_buildTechChips({ techs: RB_TECHS_SYNTH.slice(a,b), y: 6, … }).
  ['bandage',   '#cf9a44', 136, '✚'], // CON — field dressing cross          (batch A with shot)
  ['block',     '#cf9a44', 266, '▣'], // CON — raised square shield
  ['cleave',    '#c2553f', 396, '⚒'], // STR — heavy two-hand chop
  ['drain',     '#6f8fc4', 526, '↧'], // INT — life siphoned downward
  ['ember',     '#6f8fc4', 6,   '✴'], // INT — small fire mote (firebolt's ✦ kin)  (batch B row start)
  ['jab',       '#82a85e', 136, '↗'], // DEX — quick thrust
  ['lunge',     '#82a85e', 266, '➤'], // DEX — darting extension
  ['stoneskin', '#6f8fc4', 396, '⬢'], // INT — living-stone hex plate
];
// v6 kit glyphs with no card on any locked screen (DROP_AUDIT pass 7 FYI / DEV_LOOP #34.4). Same synth
// pipeline; glyph chars + glyphBg mirror core-kits.js `T` (the design font's render is the source of
// SHAPE). Two batches of ≤5 at x=6+i*130 — capture each batch to its own screenshot, slice with y:6.
//   batch A: RB_buildTechChips({ techs: RB_TECHS_V6.slice(0,5), y:6, outDirs, … })
//   batch B: RB_buildTechChips({ techs: RB_TECHS_V6.slice(5),  y:6, outDirs, … })
const RB_TECHS_V6 = [
  ['siphon',     '#6f8fc4', 6,   '◉'], // INT — draining bolt (Adept)
  ['sacrifice',  '#6a5a48', 136, '❖'], // — (minion cost) — consume a minion to mend (Summoner)
  ['barkskin',   '#6f8fc4', 266, '❦'], // INT — lesser ward (Summoner/Adept find)
  ['flurry',     '#82a85e', 396, '⇉'], // DEX — cheap dual-wield flurry (Reaver)
  ['aimed_shot', '#82a85e', 526, '➶'], // DEX — heavy piercing bow shot (Ranger)  [slug of "Aimed Shot"]
  ['bind',       '#c2553f', 6,   '⛓'], // STR — raw-sinew ward (Barbarian, B18)   [batch B row start]
];
// DUAL-ATTR ("pay in either pool") glyph chips — glyphBg is a 2-colour ARRAY [top, bottom] and the chip
// gets a hard 50/50 split (STR-red top / DEX-green bottom, NO seam — matches core-kits `glyphFill`). The
// glyph SHAPE is still lifted from the live-font capture; keying is done against the DARKER half so both
// bg halves zero out cleanly. Overlay renders these with a matching gradient bg (RB_buildChipOverlay).
// Batch of 2 at x = 6 + i*130, y:6:
//   RB_buildTechChips({ techs: RB_TECHS_SPLIT, y:6, outDirs, capture, vw })
const RB_TECHS_SPLIT = [
  ['frenzy', ['#c2553f', '#82a85e'], 6,   '⇶'], // STR|DEX — three-arc dual-wield (Reaver)
  ['flurry', ['#c2553f', '#82a85e'], 136, '⇉'], // STR|DEX — fast dual-wield flurry (Reaver)
];
const RB_TECH_Y = 96, RB_TECH_W = 120; // overlay chip row geometry (k=4 of 30px)

async function RB_renderPips(env) {
  const { createCanvas, saveFile } = env;
  const PW = 128, PH = 80, B = 4, LW = 12, PER = 24;
  const frame = (cx, w, h, fw, col) => { cx.fillStyle = col; cx.fillRect(0,0,w,fw); cx.fillRect(0,h-fw,w,fw); cx.fillRect(0,0,fw,h); cx.fillRect(w-fw,0,fw,h); };
  const dashedFrame = (cx, w, h, fw, col) => { cx.save(); cx.strokeStyle = col; cx.lineWidth = fw; cx.lineCap = 'butt'; cx.setLineDash([14,10]); cx.strokeRect(fw/2, fw/2, w-fw, h-fw); cx.restore(); };
  const hatch = (cx, w, h, dir, color) => { cx.save(); cx.beginPath(); cx.rect(0,0,w,h); cx.clip(); cx.strokeStyle = color; cx.lineWidth = LW; cx.lineCap = 'butt';
    if (dir === '\\') { for (let i=-h;i<w+h;i+=PER){ cx.beginPath(); cx.moveTo(i,0); cx.lineTo(i+h,h); cx.stroke(); } }
    else { for (let i=0;i<w+h;i+=PER){ cx.beginPath(); cx.moveTo(i,0); cx.lineTo(i-h,h); cx.stroke(); } }
    cx.restore(); };
  const mk = (fn) => { const c = createCanvas(PW, PH); const cx = c.getContext('2d'); cx.imageSmoothingEnabled = false; fn(cx); return c; };
  const out = {};
  // FULL — one per colour (attributes + resource pools)
  for (const k in RB_PIP_COLORS) out['ui/pip/pip_full_' + k] = mk(cx => { cx.fillStyle = RB_PIP_COLORS[k]; cx.fillRect(0,0,PW,PH); frame(cx,PW,PH,B,RB_PIP_INK); });
  out['ui/pip/pip_full'] = out['ui/pip/pip_full_str'];        // generic exemplar (engine tints) = STR red
  // RESERVED — gear hatch over stat colour, attribute pools only
  for (const k of RB_PIP_ATTR) out['ui/pip/pip_reserved_' + k] = mk(cx => { cx.fillStyle = RB_PIP_COLORS[k]; cx.fillRect(0,0,PW,PH); hatch(cx,PW,PH,'\\',RB_PIP_EQH); frame(cx,PW,PH,B,RB_PIP_INK); });
  out['ui/pip/pip_reserved'] = out['ui/pip/pip_reserved_str'];
  // EMPTY — generic (solid black frame) + the two special dashed resource empties
  out['ui/pip/pip_empty'] = mk(cx => { cx.fillStyle = RB_PIP_EMPTY_FILL; cx.fillRect(0,0,PW,PH); frame(cx,PW,PH,B,RB_PIP_INK); });
  for (const k in RB_PIP_SPECIAL_EMPTY) out['ui/pip/pip_empty_' + k] = mk(cx => { cx.fillStyle = RB_PIP_EMPTY_FILL; cx.fillRect(0,0,PW,PH); dashedFrame(cx,PW,PH,B,RB_PIP_SPECIAL_EMPTY[k]); });
  // DEBUFF / DAMAGE — hatch + black frame, transparent interior (never change colour)
  out['ui/pip/pip_debuff'] = mk(cx => { hatch(cx,PW,PH,'/','#d9a441cc'); frame(cx,PW,PH,B,RB_PIP_INK); });
  out['ui/pip/pip_damage'] = mk(cx => { hatch(cx,PW,PH,'/','#b23b32cc'); frame(cx,PW,PH,B,RB_PIP_INK); });
  for (const name in out) await saveFile('Content/' + name + '.png', out[name]);
  return Object.keys(out).length;
}

async function RB_buildTechChips(env) {
  const { readImage, createCanvas, saveFile, capture, vw } = env;
  const techs = env.techs || RB_TECHS;               // pass RB_TECHS_SYNTH for the reconstructed chips
  const rowY = (env.y == null) ? RB_TECH_Y : env.y;  // synthetic overlay rows start at y=6
  // outDirs: base dirs each chip is written under as <dir>/icons/technique/<nm>.png. Default is just
  // the Content source of truth; pass ['Content','drop/Roguebane.Content'] to land straight in the drop.
  const outDirs = env.outDirs || ['Content'];
  const img = await readImage(capture); const scale = img.width / vw;
  const hexrgb = h => [parseInt(h.slice(1,3),16), parseInt(h.slice(3,5),16), parseInt(h.slice(5,7),16)];
  const lum = (r,g,b) => 0.3*r + 0.59*g + 0.11*b;
  const S = 120, CB = 8, INK = '#0a0807', link = 8.5;
  for (const [nm, glyphBg, cx0] of techs) {
    const split = Array.isArray(glyphBg);                 // dual-attr → [topHex, bottomHex] 50/50 fill
    const bgColors = split ? glyphBg : [glyphBg];
    // key the glyph mask against the DARKER half so BOTH bg halves fall to alpha 0 while the dark ink → 1
    const keyHex = split ? bgColors.slice().sort((a, b) => lum(...hexrgb(a)) - lum(...hexrgb(b)))[0] : glyphBg;
    const sx = Math.round(cx0*scale), sy = Math.round(rowY*scale);
    const cw = Math.round((cx0+RB_TECH_W)*scale) - sx, ch = Math.round((rowY+RB_TECH_W)*scale) - sy;
    const cap = createCanvas(cw, ch); cap.getContext('2d').drawImage(img, sx, sy, cw, ch, 0, 0, cw, ch);
    const data = cap.getContext('2d').getImageData(0,0,cw,ch).data;
    const bg = hexrgb(keyHex), lbg = lum(bg[0],bg[1],bg[2]);
    const alpha = new Float32Array(cw*ch), dark = new Uint8Array(cw*ch);
    for (let p=0;p<cw*ch;p++){ const i=p*4; let a=(lbg-lum(data[i],data[i+1],data[i+2]))/(lbg-link); a=a<0?0:a>1?1:a; alpha[p]=a; dark[p]=a>0.4?1:0; }
    // flood-fill: remove the chip's own black border (dark pixels connected to an edge)
    const border = new Uint8Array(cw*ch), stack = [];
    for (let X=0;X<cw;X++) for (const Y of [0,ch-1]){ const p=Y*cw+X; if (dark[p]&&!border[p]){ border[p]=1; stack.push(p);} }
    for (let Y=0;Y<ch;Y++) for (const X of [0,cw-1]){ const p=Y*cw+X; if (dark[p]&&!border[p]){ border[p]=1; stack.push(p);} }
    while (stack.length){ const p=stack.pop(), X=p%cw, Y=(p/cw)|0;
      for (const [nx,ny] of [[X-1,Y],[X+1,Y],[X,Y-1],[X,Y+1]]){ if(nx<0||ny<0||nx>=cw||ny>=ch) continue; const q=ny*cw+nx; if (dark[q]&&!border[q]){ border[q]=1; stack.push(q);} } }
    let minX=cw,minY=ch,maxX=-1,maxY=-1;
    for (let Y=0;Y<ch;Y++) for (let X=0;X<cw;X++){ const p=Y*cw+X; if (dark[p]&&!border[p]){ if(X<minX)minX=X; if(X>maxX)maxX=X; if(Y<minY)minY=Y; if(Y>maxY)maxY=Y; } }
    const gw=maxX-minX+1, gh=maxY-minY+1;
    const gC=createCanvas(gw,gh); const gctx=gC.getContext('2d'); const gid=gctx.createImageData(gw,gh);
    for (let Y=0;Y<gh;Y++) for (let X=0;X<gw;X++){ const sp=(Y+minY)*cw+(X+minX); const a=border[sp]?0:alpha[sp]; const j=(Y*gw+X)*4; gid.data[j]=10; gid.data[j+1]=8; gid.data[j+2]=7; gid.data[j+3]=Math.round(a*255); }
    gctx.putImageData(gid,0,0);
    const d=createCanvas(S,S); const dx=d.getContext('2d'); dx.imageSmoothingEnabled=false;
    if (split) { dx.fillStyle=bgColors[0]; dx.fillRect(0,0,S,S/2); dx.fillStyle=bgColors[1]; dx.fillRect(0,S/2,S,S/2); }
    else { dx.fillStyle=glyphBg; dx.fillRect(0,0,S,S); }
    dx.drawImage(gC, Math.round((S-gw)/2), Math.round((S-gh)/2));
    dx.fillStyle=INK; dx.fillRect(0,0,S,CB); dx.fillRect(0,S-CB,S,CB); dx.fillRect(0,0,CB,S); dx.fillRect(S-CB,0,CB,S);
    for (const dir of outDirs) await saveFile(dir + '/icons/technique/' + nm + '.png', d);
  }
  return techs.length;
}

// ---- NODE TOKENS (RunMap) — deterministic SHAPE (transparent canvas, exact size/centre, full border)
// ---- NODE TOKENS (RunMap) — captured WITH depth (gloss + bevel), made transparent by DUAL-BACKGROUND
// recovery. The overlay clones each real node at K× and keeps its gradient + bevel box-shadow (only
// transient state — opacity/animation/distance-glow — is normalised). We screenshot the SAME overlay
// over black and over white; per pixel α = 1−(white−black)/255 and colour = black/α. Opaque interior
// pixels (the gloss/bevel) come back identical; only the round corner goes transparent. Pixel-perfect
// to the screen, with clean transparent edges — NOT flattened (UI chrome is allowed depth).
async function RB_buildNodes(env) {
  const { readImage, createCanvas, saveFile, captureBlack, captureWhite, vw, rects } = env;
  const B = await readImage(captureBlack), Wi = await readImage(captureWhite);
  const scale = B.width / vw;
  for (const r of rects) {
    const sx = Math.round(r.x*scale), sy = Math.round(r.y*scale), cw = Math.round(r.w*scale), ch = Math.round(r.h*scale);
    const cb = createCanvas(cw, ch); cb.getContext('2d').drawImage(B, sx, sy, cw, ch, 0, 0, cw, ch);
    const cw2 = createCanvas(cw, ch); cw2.getContext('2d').drawImage(Wi, sx, sy, cw, ch, 0, 0, cw, ch);
    const bd = cb.getContext('2d').getImageData(0,0,cw,ch).data;
    const wd = cw2.getContext('2d').getImageData(0,0,cw,ch).data;
    const out = createCanvas(cw, ch); const octx = out.getContext('2d'); const oid = octx.createImageData(cw, ch);
    for (let p=0; p<cw*ch; p++) { const i=p*4;
      const ar = 1-(wd[i]-bd[i])/255, ag = 1-(wd[i+1]-bd[i+1])/255, ab = 1-(wd[i+2]-bd[i+2])/255;
      let a = (ar+ag+ab)/3; if (a<0) a=0; if (a>1) a=1;
      if (a < 0.004) { oid.data[i+3]=0; continue; }
      oid.data[i]   = Math.min(255, Math.max(0, Math.round(bd[i]/a)));
      oid.data[i+1] = Math.min(255, Math.max(0, Math.round(bd[i+1]/a)));
      oid.data[i+2] = Math.min(255, Math.max(0, Math.round(bd[i+2]/a)));
      oid.data[i+3] = Math.round(a*255);
    }
    octx.putImageData(oid, 0, 0);
    await saveFile('Content/' + r.name + '.png', out);
  }
  return rects.length;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_renderPips, RB_buildTechChips, RB_buildNodes, RB_TECHS_SYNTH, RB_TECHS_V6, RB_TECHS_SPLIT };
