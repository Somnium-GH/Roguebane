// Roguebane — UI ATOM generator (persistent). Draws the icons that are NOT present on the screens as
// polished art: attribute pictographs, rune/resource glyphs, targeting reticles, minion mark. Smooth
// anti-aliased, hi-res (4×), crisp black borders — the Tier-1 UI register (see ASSET_MANIFEST).
// NOTE: technique glyph chips and pool pips are NOT drawn here — they are CAPTURED from the live
// screens by proto/atom_capture.js (§12). Do not re-add them here or a re-run would overwrite the
// captured, screen-identical assets. Colours from style_tokens.js.
//
// Run via run_script:
//   eval(await readFile('style_tokens.js'));
//   eval(await readFile('proto/ui_atoms_gen.js'));
//   await RB_buildAtoms({ createCanvas, saveFile, log, P: self.RB_STYLE.palette });

async function RB_buildAtoms(env) {
  const { createCanvas, saveFile, log, P } = env;
  const OUT = '#0a0807';                       // the screens' chip border + glyph colour
  const hex = h => ({ r: parseInt(h.slice(1,3),16), g: parseInt(h.slice(3,5),16), b: parseInt(h.slice(5,7),16) });
  const cs = c => `rgb(${Math.max(0,Math.min(255,c.r))|0},${Math.max(0,Math.min(255,c.g))|0},${Math.max(0,Math.min(255,c.b))|0})`;
  const shade = (h,f) => { const c=hex(h); return cs({r:c.r*f,g:c.g*f,b:c.b*f}); };
  function cv(w,h){ const c=createCanvas(w,h); const x=c.getContext('2d'); return [c,x]; } // smoothing ON (hi-res UI)
  function rrect(x,X,Y,W,H,R){ x.beginPath(); x.moveTo(X+R,Y); x.arcTo(X+W,Y,X+W,Y+H,R); x.arcTo(X+W,Y+H,X,Y+H,R); x.arcTo(X,Y+H,X,Y,R); x.arcTo(X,Y,X+W,Y,R); x.closePath(); }
  function poly(x,pts){ x.beginPath(); pts.forEach((p,i)=> i?x.lineTo(p[0],p[1]):x.moveTo(p[0],p[1])); x.closePath(); }
  function shape(x, pathFn, fill, S){ pathFn(); x.fillStyle=fill; x.fill(); pathFn(); x.lineWidth=Math.max(3,S*0.06); x.strokeStyle=OUT; x.lineJoin='round'; x.stroke(); }

  const out = {};
  const S = 120;                               // 4× the 30px screen chip → hi-res
  const B = Math.round(S * 2 / 30);            // border = same 2/30 proportion as the screens

  // square chip: full-bleed black border + saturated fill (the screens' literal technique/attr chip)
  function chip(x, fill){ x.fillStyle=OUT; x.fillRect(0,0,S,S); x.fillStyle=fill; x.fillRect(B,B,S-2*B,S-2*B); }

  // ---------- ATTRIBUTE — PLAIN colour swatch, NO glyph (matches the screens' pool/attr boxes:
  // a flat stat-colour square with the locked 1px black border). The engine could equally draw this as
  // a tinted rect; the PNG just makes the `icons/attr/*` binding concrete. ----------
  [['strength', P.str], ['intellect', P.int], ['dexterity', P.dex], ['constitution', P.con]]
    .forEach(([nm, fill]) => { const [c, x] = cv(S, S); chip(x, fill); out['icons/attr/' + nm] = c; });

  // ---------- RUNE TIER ICONS — shape encodes tier (the differentiator): Mark=diamond(4),
  // Path Minor=pentagon(5), Path Major=hexagon(6), Keystone=octagon(8). Bold black border. ----------
  function ngon(n, R, rot){ const cx=S*0.5, cy=S*0.51, pts=[]; for(let i=0;i<n;i++){ const a=rot + i*2*Math.PI/n; pts.push([cx+Math.cos(a)*R, cy+Math.sin(a)*R]); } return pts; }
  [['mark',4,'#cf9038'], ['path_minor',5,'#5fa3c4'], ['path_major',6,'#9b6fc0'], ['keystone',8,'#d9a441']]
    .forEach(([nm,n,fill])=>{ const [c,x]=cv(S,S); shape(x, ()=>poly(x, ngon(n, S*0.34, -Math.PI/2)), fill, S); out['icons/rune/'+nm]=c; });

  // ---------- RESOURCE ----------
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.03); shape(x, ()=>rrect(x,S*0.24,S*0.28,S*0.52,S*0.44,S*0.04), P.mutedWarm, S);
    x.strokeStyle=OUT; x.lineWidth=S*0.045; x.beginPath(); x.moveTo(S*0.24,S*0.42); x.lineTo(S*0.76,S*0.42); x.moveTo(S*0.5,S*0.28); x.lineTo(S*0.5,S*0.72); x.stroke(); out['icons/resource/supplies']=c; })();
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.03); x.strokeStyle=OUT; x.lineWidth=S*0.055; x.lineCap='round'; x.beginPath(); x.moveTo(S*0.32,S*0.20); x.lineTo(S*0.32,S*0.80); x.stroke();
    shape(x, ()=>poly(x,[[S*0.32,S*0.23],[S*0.76,S*0.30],[S*0.61,S*0.41],[S*0.76,S*0.52],[S*0.32,S*0.58]]), P.int, S); out['icons/resource/support']=c; })();
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.03); shape(x, ()=>{ x.beginPath(); x.arc(S*0.5,S*0.5,S*0.27,0,7); x.closePath(); }, P.amber, S);
    x.strokeStyle=OUT; x.lineWidth=S*0.04; x.beginPath(); x.arc(S*0.5,S*0.5,S*0.155,0,7); x.stroke(); out['icons/resource/spoils']=c; })();
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.03); shape(x, ()=>{ x.beginPath(); x.moveTo(S*0.5,S*0.72); x.bezierCurveTo(S*0.18,S*0.50,S*0.30,S*0.23,S*0.5,S*0.41); x.bezierCurveTo(S*0.70,S*0.23,S*0.82,S*0.50,S*0.5,S*0.72); x.closePath(); }, P.blood, S); out['icons/resource/hp']=c; })();
  // charge — the shield-PIERCE resource (builds up, spent to punch through a shield): a steel heater
  // shield run through top-to-bottom by a bolt. Bolt in `hit` red-orange (the damage/pierce accent).
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.03);
    shape(x, ()=>poly(x,[[S*0.26,S*0.22],[S*0.74,S*0.22],[S*0.74,S*0.50],[S*0.5,S*0.80],[S*0.26,S*0.50]]), '#9a8c7a', S);
    shape(x, ()=>poly(x,[[S*0.58,S*0.08],[S*0.40,S*0.46],[S*0.52,S*0.46],[S*0.38,S*0.92],[S*0.63,S*0.44],[S*0.51,S*0.44],[S*0.66,S*0.08]]), P.hit, S);
    out['icons/resource/charge']=c; })();
  // summons — the minion-deploy resource (DESIGN_SPEC §9/§14: spent ONCE to FIELD a minion, on top of
  // reserving its gate stat): a conjured spirit rising from a summoning circle. Circle in minion-bay
  // teal, spirit in mint (the minion-active accent) — the same colour family as the bays it feeds.
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.03);
    shape(x, ()=>{ x.beginPath(); x.ellipse(S*0.5,S*0.72,S*0.30,S*0.115,0,0,7); x.closePath(); }, P.teal, S);
    shape(x, ()=>{ x.beginPath(); x.moveTo(S*0.5,S*0.14); x.bezierCurveTo(S*0.71,S*0.33,S*0.67,S*0.57,S*0.5,S*0.63); x.bezierCurveTo(S*0.33,S*0.57,S*0.29,S*0.33,S*0.5,S*0.14); x.closePath(); }, P.mintActive, S);
    out['icons/resource/summons']=c; })();

  // ---------- TARGETING RETICLES (transparent) ----------
  // brackets(col,R,f): f = pulse-frame spread factor. m grows / L shrinks as the reticle "breathes"
  // outward. FOCUS PULSE FRAMES (payload 2026-07-03 #12c): the locked reticle pulses by cycling
  // authored frames p0→p1→p2 on the engine's fixed tick — p0 is IDENTICAL to focus.png (canonical
  // frame; captures freeze here), p1/p2 step outward with a thinning stroke.
  function brackets(col,R,f){ f=f||0; const [c,x]=cv(R,R); x.strokeStyle=col; x.lineWidth=R*(0.055-0.008*f); x.lineCap='round'; const m=R*(0.13-0.045*f),L=R*(0.24-0.02*f);
    [[m,m,1,1],[R-m,m,-1,1],[m,R-m,1,-1],[R-m,R-m,-1,-1]].forEach(([X,Y,dx,dy])=>{ x.beginPath(); x.moveTo(X,Y+dy*L); x.lineTo(X,Y); x.lineTo(X+dx*L,Y); x.stroke(); }); return c; }
  out['ui/reticle/focus']=brackets(P.hit,128);
  out['ui/reticle/focus_p0']=brackets(P.hit,128,0);
  out['ui/reticle/focus_p1']=brackets(P.hit,128,1);
  out['ui/reticle/focus_p2']=brackets(P.hit,128,2);
  out['ui/reticle/secondary']=brackets(P.mutedDim,128);
  // AIMING = the CURSOR while a technique is actively targeting (click its tile → this replaces the
  // pointer until you click a body part or right-click to cancel). RED like focus (2026-07-03 review
  // note — it was amber, which read as a third state).
  (function(){ const R=128,[c,x]=cv(R,R); x.strokeStyle=P.hit; x.lineWidth=R*0.04; x.setLineDash([R*0.09,R*0.06]); x.beginPath(); x.arc(R*0.5,R*0.5,R*0.34,0,7); x.stroke(); x.setLineDash([]);
    x.lineCap='round'; x.beginPath(); x.moveTo(R*0.5,R*0.15); x.lineTo(R*0.5,R*0.32); x.moveTo(R*0.5,R*0.68); x.lineTo(R*0.5,R*0.85); x.moveTo(R*0.15,R*0.5); x.lineTo(R*0.32,R*0.5); x.moveTo(R*0.68,R*0.5); x.lineTo(R*0.85,R*0.5); x.stroke(); out['ui/reticle/aiming']=c; })();
  // (ui/reticle/target_tag RETIRED 2026-07-03 — the dropped-pin target pin was judged unnecessary.)

  // ---------- MINION (skull) ----------
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.025); shape(x, ()=>{ x.beginPath(); x.arc(S*0.5,S*0.44,S*0.27,Math.PI,0); x.lineTo(S*0.67,S*0.66); x.lineTo(S*0.58,S*0.66); x.lineTo(S*0.58,S*0.75); x.lineTo(S*0.42,S*0.75); x.lineTo(S*0.42,S*0.66); x.lineTo(S*0.33,S*0.66); x.closePath(); }, '#cdc8b8', S);
    x.fillStyle=OUT; x.beginPath(); x.arc(S*0.40,S*0.46,S*0.065,0,7); x.fill(); x.beginPath(); x.arc(S*0.60,S*0.46,S*0.065,0,7); x.fill();
    poly(x,[[S*0.47,S*0.54],[S*0.53,S*0.54],[S*0.5,S*0.61]]); x.fill(); out['icons/minion/skeleton']=c; })();

  // ---------- MINION — iron golem (payload B18): riveted iron visage, same flat-pictogram register
  // as the skull (fill + slit eyes + rivets, bold black outline). ----------
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.02);
    shape(x, ()=>poly(x,[[S*0.30,S*0.20],[S*0.70,S*0.20],[S*0.76,S*0.34],[S*0.72,S*0.70],[S*0.60,S*0.80],[S*0.40,S*0.80],[S*0.28,S*0.70],[S*0.24,S*0.34]]), '#9aa0a6', S);
    x.fillStyle=OUT; x.fillRect(S*0.34,S*0.42,S*0.115,S*0.06); x.fillRect(S*0.545,S*0.42,S*0.115,S*0.06);   // slit eyes
    x.fillRect(S*0.44,S*0.60,S*0.12,S*0.05);                                                                // mouth seam
    [[S*0.33,S*0.27],[S*0.67,S*0.27],[S*0.31,S*0.66],[S*0.69,S*0.66]].forEach(([X,Y])=>{ x.beginPath(); x.arc(X,Y,S*0.028,0,7); x.fill(); });  // rivets
    out['icons/minion/golem']=c; })();

  // ---------- MINION — hound (payload B18): pricked-ear head in profile, flat + outlined. ----------
  (function(){ const [c,x]=cv(S,S); x.translate(0,S*0.03);
    shape(x, ()=>poly(x,[[S*0.28,S*0.18],[S*0.42,S*0.30],[S*0.58,S*0.30],[S*0.80,S*0.46],[S*0.80,S*0.55],[S*0.62,S*0.58],[S*0.52,S*0.72],[S*0.34,S*0.70],[S*0.24,S*0.50],[S*0.24,S*0.32]]), '#a97d4f', S);
    x.fillStyle=OUT; x.beginPath(); x.arc(S*0.42,S*0.44,S*0.05,0,7); x.fill();                              // eye
    x.beginPath(); x.arc(S*0.77,S*0.49,S*0.045,0,7); x.fill();                                              // nose
    out['icons/minion/hound']=c; })();

  const names = env.only ? Object.keys(out).filter(k => env.only.includes(k)) : Object.keys(out);
  const dirs = env.outDirs || ['Content'];   // pass ['Content','drop/Roguebane.Content'] to land straight in the drop
  for (const k of names) for (const d of dirs) await saveFile(d + '/' + k + '.png', out[k]);
  log('UI atoms ('+names.length+'):', names.join(', '));
  return names;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_buildAtoms };
