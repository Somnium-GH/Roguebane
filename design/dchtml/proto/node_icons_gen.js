/* Roguebane node-token icon generator — persistent source of truth for Content/icons/node/*.png
 * Run via run_script:
 *   const src = await readFile('proto/node_icons_gen.js'); eval(src);
 *   const out = generateNodeIcons(createCanvas);   // [{name, canvas}]
 *   for (const o of out) await saveFile('Content/icons/node/'+o.name+'.png', o.canvas);
 *
 * Style (matches the existing token set): a colored shape per node TYPE, vertical light→dark
 * gradient fill, 2px near-black outline (#140d10), a 1px light top-left + dark bottom-right bevel,
 * and a soft drop shadow. Iconography (mountain ridges, castle battlements, the "?" glyph) sits on
 * top in the same palette. Sizes are LOCKED to the on-disk set: 56×56 (castle 128×128) — do not
 * change without updating Content.mgcb + asset-manifest.js.
 */
function generateNodeIcons(createCanvas) {
  const OUT = '#140d10';
  // sampled centers from the shipped tokens, so the 5 unchanged ones don't drift
  const C = {
    camp:'#a7c08a', skirmish:'#d2806c', control:'#e6bd72', merchant:'#cdb293',
    mountain:'#9d7fb0', unknown:'#bcaa93', castle:'#8a7257',
  };
  function adj(hex, f) {
    let r=parseInt(hex.slice(1,3),16), g=parseInt(hex.slice(3,5),16), b=parseInt(hex.slice(5,7),16);
    if (f>=1){ const t=f-1; r+= (255-r)*t; g+=(255-g)*t; b+=(255-b)*t; } else { r*=f; g*=f; b*=f; }
    const c=v=>Math.round(Math.max(0,Math.min(255,v))).toString(16).padStart(2,'0');
    return '#'+c(r)+c(g)+c(b);
  }
  function ctxFor(W,H){ const cv=createCanvas(W,H); const ctx=cv.getContext('2d'); ctx.imageSmoothingEnabled=true; ctx.lineJoin='round'; ctx.lineCap='round'; return {cv,ctx}; }
  function vGrad(ctx, base, y0, y1){ const g=ctx.createLinearGradient(0,y0,0,y1); g.addColorStop(0,adj(base,1.34)); g.addColorStop(0.55,base); g.addColorStop(1,adj(base,0.72)); return g; }

  // draw a closed path (point list or fn), with shadow + gradient fill + outline + bevel
  function token(W, H, base, pathFn, opts){
    opts = opts || {};
    const {cv,ctx} = ctxFor(W,H);
    const lw = opts.lw || Math.max(2, Math.round(W/28));
    // soft drop shadow
    ctx.save();
    ctx.shadowColor='rgba(8,5,8,0.55)'; ctx.shadowBlur=Math.round(W/14); ctx.shadowOffsetY=Math.round(W/22);
    ctx.beginPath(); pathFn(ctx); ctx.closePath();
    ctx.fillStyle=adj(base,0.72); ctx.fill();
    ctx.restore();
    // gradient body
    ctx.beginPath(); pathFn(ctx); ctx.closePath();
    ctx.fillStyle=vGrad(ctx, base, lw, H-lw); ctx.fill();
    // interior iconography (drawn clipped to the shape)
    if (opts.inner){ ctx.save(); ctx.beginPath(); pathFn(ctx); ctx.closePath(); ctx.clip(); opts.inner(ctx, adj, base); ctx.restore(); }
    // bevel: light top-left, dark bottom-right (inset stroke)
    ctx.save(); ctx.beginPath(); pathFn(ctx); ctx.closePath(); ctx.clip();
    ctx.lineWidth=2; ctx.strokeStyle='rgba(255,255,255,0.30)'; ctx.beginPath(); pathFn(ctx); ctx.closePath();
    ctx.translate(1.2,1.2); ctx.stroke();
    ctx.restore();
    ctx.save(); ctx.beginPath(); pathFn(ctx); ctx.closePath(); ctx.clip();
    ctx.lineWidth=2; ctx.strokeStyle='rgba(10,6,9,0.34)'; ctx.beginPath(); pathFn(ctx); ctx.closePath();
    ctx.translate(-1.2,-1.2); ctx.stroke();
    ctx.restore();
    // crisp outline
    ctx.beginPath(); pathFn(ctx); ctx.closePath();
    ctx.lineWidth=lw; ctx.strokeStyle=OUT; ctx.stroke();
    return cv;
  }

  const out = [];
  const P = (W,H,base,fn,opts)=>token(W,H,base,fn,opts);

  // ---- camp: orb ----
  out.push({name:'camp', canvas: P(56,56,C.camp, c=>c.arc(28,28,21,0,Math.PI*2), { inner:(c)=>{
    const g=c.createRadialGradient(20,18,2,28,28,26); g.addColorStop(0,'rgba(255,255,255,0.5)'); g.addColorStop(0.5,'rgba(255,255,255,0)'); c.fillStyle=g; c.fillRect(0,0,56,56);
  }})});
  // ---- skirmish: rounded square ----
  out.push({name:'skirmish', canvas: P(56,56,C.skirmish, c=>roundRect(c,8,8,40,40,9))});
  // ---- control: diamond ----
  out.push({name:'control', canvas: P(56,56,C.control, c=>{ c.moveTo(28,6); c.lineTo(50,28); c.lineTo(28,50); c.lineTo(6,28); })});
  // ---- merchant: square ----
  out.push({name:'merchant', canvas: P(56,56,C.merchant, c=>roundRect(c,9,9,38,38,5))});
  // ---- unknown: orb + ? ----
  out.push({name:'unknown', canvas: P(56,56,C.unknown, c=>c.arc(28,28,21,0,Math.PI*2), { inner:(c)=>{
    const g=c.createRadialGradient(20,18,2,28,28,26); g.addColorStop(0,'rgba(255,255,255,0.45)'); g.addColorStop(0.5,'rgba(255,255,255,0)'); c.fillStyle=g; c.fillRect(0,0,56,56);
    c.fillStyle='rgba(12,7,10,0.78)'; c.font='bold 30px Georgia, serif'; c.textAlign='center'; c.textBaseline='middle'; c.fillText('?',28,30);
  }})});

  // ---- mountain: twin peak w/ snow caps + lit/shadow faces ----
  out.push({name:'mountain', canvas: (()=>{
    const W=56,H=56, base=C.mountain;
    // back (smaller) peak first
    let cv = P(W,H,base, c=>{ c.moveTo(40,18); c.lineTo(52,48); c.lineTo(28,48); }, {});
    // draw main peak on the SAME canvas (composite) — redo with full control
    const ctx=cv.getContext('2d'); ctx.imageSmoothingEnabled=true;
    const main = c=>{ c.moveTo(22,7); c.lineTo(48,49); c.lineTo(4,49); };
    // shadow
    ctx.save(); ctx.shadowColor='rgba(8,5,8,0.5)'; ctx.shadowBlur=4; ctx.shadowOffsetY=2;
    ctx.beginPath(); main(ctx); ctx.closePath(); ctx.fillStyle=adj(base,0.72); ctx.fill(); ctx.restore();
    // body gradient
    ctx.beginPath(); main(ctx); ctx.closePath(); ctx.fillStyle=vGrad(ctx,base,7,49); ctx.fill();
    // lit left face / shadow right face split down the apex
    ctx.save(); ctx.beginPath(); main(ctx); ctx.closePath(); ctx.clip();
    ctx.beginPath(); ctx.moveTo(22,7); ctx.lineTo(48,49); ctx.lineTo(22,49); ctx.closePath();
    ctx.fillStyle='rgba(20,12,18,0.30)'; ctx.fill(); // right (shadow) face
    ctx.beginPath(); ctx.moveTo(22,7); ctx.lineTo(4,49); ctx.lineTo(22,49); ctx.closePath();
    ctx.fillStyle='rgba(255,255,255,0.14)'; ctx.fill(); // left (lit) face
    // snow cap — jagged white wedge at the apex
    ctx.beginPath(); ctx.moveTo(22,7); ctx.lineTo(33,24); ctx.lineTo(28,21); ctx.lineTo(24,26); ctx.lineTo(20,21); ctx.lineTo(15,25); ctx.lineTo(11,24); ctx.closePath();
    ctx.fillStyle='#f2eef6'; ctx.fill(); ctx.lineWidth=1; ctx.strokeStyle='rgba(120,105,140,0.5)'; ctx.stroke();
    ctx.restore();
    // outline main peak
    ctx.beginPath(); main(ctx); ctx.closePath(); ctx.lineWidth=2; ctx.strokeStyle=OUT; ctx.stroke();
    // small snow cap on back peak
    ctx.save(); ctx.beginPath(); ctx.moveTo(40,18); ctx.lineTo(46,27); ctx.lineTo(43,25); ctx.lineTo(40,28); ctx.lineTo(37,24); ctx.closePath(); ctx.fillStyle='#eae4f0'; ctx.fill(); ctx.restore();
    return cv;
  })()});

  // ---- castle: keep + two towers, crenellated, with gate + windows (128×128) ----
  out.push({name:'castle', canvas: (()=>{
    const W=128,H=128, base=C.castle;
    // unified silhouette path: left tower, keep, right tower, all with merlons
    function merlonTop(c, x0, x1, y, n){
      const span=x1-x0, mw=span/(2*n+1);
      c.lineTo(x0, y);
      for(let i=0;i<n;i++){ const mx=x0+mw*(2*i+1); c.lineTo(mx, y); c.lineTo(mx, y-8); c.lineTo(mx+mw, y-8); c.lineTo(mx+mw, y); }
      c.lineTo(x1, y);
    }
    function silo(c){
      const baseY=112;
      c.moveTo(20, baseY);
      c.lineTo(20, 44);                 // left tower up
      merlonTop(c, 20, 46, 44, 2);      // left tower battlements
      c.lineTo(46, 56);                 // step down to keep wall
      c.lineTo(46, 34);                 // keep up
      merlonTop(c, 46, 82, 34, 3);      // keep battlements (taller)
      c.lineTo(82, 56);                 // step down to right tower
      c.lineTo(82, 44);
      merlonTop(c, 82, 108, 44, 2);     // right tower battlements
      c.lineTo(108, baseY);             // right tower down
    }
    const cv = P(W,H,base, silo, { lw:4, inner:(c)=>{
      // consistent left-lit / right-shadow shading (NO diagonal)
      c.fillStyle='rgba(255,255,255,0.10)'; c.fillRect(0,0,64,128);
      c.fillStyle='rgba(16,9,13,0.16)'; c.fillRect(64,0,64,128);
      // gate (dark arch)
      c.fillStyle='#1c1014'; roundRectTop(c,54,78,20,34,10); c.fill();
      c.fillStyle='#2a1a1f'; roundRectTop(c,57,82,14,30,7); c.fill();
      // portcullis bars
      c.strokeStyle='rgba(120,95,70,0.5)'; c.lineWidth=1.5;
      for(let x=60;x<=68;x+=4){ c.beginPath(); c.moveTo(x,84); c.lineTo(x,112); c.stroke(); }
      // windows (dark slits) on towers + keep
      c.fillStyle='#1c1014';
      slit(c,28,60); slit(c,96,60); slit(c,60,46); slit(c,71,46);
      function slit(c,x,y){ c.beginPath(); roundRectTop(c,x,y,6,12,3); c.fill(); }
    }});
    return cv;
  })()});

  function roundRect(c,x,y,w,h,r){ c.moveTo(x+r,y); c.arcTo(x+w,y,x+w,y+h,r); c.arcTo(x+w,y+h,x,y+h,r); c.arcTo(x,y+h,x,y,r); c.arcTo(x,y,x+w,y,r); }
  function roundRectTop(c,x,y,w,h,r){ c.beginPath(); c.moveTo(x,y+h); c.lineTo(x,y+r); c.arcTo(x,y,x+r,y,r); c.lineTo(x+w-r,y); c.arcTo(x+w,y,x+w,y+r,r); c.lineTo(x+w,y+h); c.closePath(); }

  return out;
}
if (typeof module !== 'undefined') module.exports = { generateNodeIcons };
