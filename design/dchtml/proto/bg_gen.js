// Roguebane — BACKDROP generator (persistent, §10 rule A). Produces Content/bg/*.png
// as the STATIC backdrop each screen composites behind its UI (gradient + EGA dither +
// silhouette motif + vignette). Screens consume url(bg/<name>.png); animated lighting
// (torch glow) stays a runtime overlay on top. Deterministic — fixed seed, no Math.random.
//
// Run via run_script:
//   eval(await readFile('proto/bg_gen.js'));
//   await RB_buildBackdrops({ createCanvas, saveFile, log });

async function RB_buildBackdrops(env) {
  const { createCanvas, saveFile, log } = env;
  const W = 1920, H = 1080;

  // deterministic PRNG (mulberry32)
  function rng(seed){ return function(){ seed|=0; seed=seed+0x6D2B79F5|0; let t=Math.imul(seed^seed>>>15,1|seed); t=t+Math.imul(t^t>>>7,61|t)^t; return ((t^t>>>14)>>>0)/4294967296; }; }
  const hex = (h)=>({r:parseInt(h.slice(1,3),16),g:parseInt(h.slice(3,5),16),b:parseInt(h.slice(5,7),16)});
  const mix=(a,b,t)=>({r:a.r+(b.r-a.r)*t,g:a.g+(b.g-a.g)*t,b:a.b+(b.b-a.b)*t});
  const css=(c)=>`rgb(${c.r|0},${c.g|0},${c.b|0})`;

  // vertical multi-stop gradient
  function vgrad(ctx, stops){ const g=ctx.createLinearGradient(0,0,0,H); stops.forEach(([o,h])=>g.addColorStop(o,h)); ctx.fillStyle=g; ctx.fillRect(0,0,W,H); }

  // EGA ordered-dither texture between two close tones (subtle, not a harsh checker)
  const BAYER=[[0,8,2,10],[12,4,14,6],[3,11,1,9],[15,7,13,5]];
  function dither(ctx, y0, y1, lo, hi, cell, strength){
    const L=hex(lo), Hc=hex(hi);
    for(let y=y0; y<y1; y+=cell){
      for(let x=0; x<W; x+=cell){
        const th=BAYER[(y/cell|0)%4][(x/cell|0)%4]/16;
        const t=th*strength;
        ctx.fillStyle=css(mix(L,Hc,t));
        ctx.globalAlpha=0.22;
        ctx.fillRect(x,y,cell,cell);
      }
    }
    ctx.globalAlpha=1;
  }

  // blocky crenellated battlement / skyline band (EGA silhouette). `rim` (optional) draws a thin
  // rim-light hairline along the SAME per-unit top edge computed for the fill — sharing one rand()
  // sequence so the highlight never drifts from the silhouette it's supposed to outline.
  function skyline(ctx, baseY, color, rand, units, h, merlon, rim){
    ctx.fillStyle=color;
    const uw=W/units;
    for(let i=0;i<units;i++){
      const x=i*uw, th=h*(0.7+rand()*0.6);
      ctx.fillRect(x, baseY-th, uw+1, th+ (H-baseY));
      if(merlon){ // crenellations on top
        const mw=uw/5;
        for(let m=0;m<5;m+=2) ctx.fillRect(x+m*mw, baseY-th-10, mw+1, 12);
      }
      if(rim){ ctx.fillStyle=rim; ctx.fillRect(x, baseY-th-1, uw+1, 1); ctx.fillStyle=color; }
    }
  }

  function vignette(ctx, edge, soft){
    const g=ctx.createRadialGradient(W/2,H*0.42,0,W/2,H*0.42,W*0.75);
    g.addColorStop(0,'rgba(0,0,0,0)'); g.addColorStop(soft,'rgba(0,0,0,0)'); g.addColorStop(1,edge);
    ctx.fillStyle=g; ctx.fillRect(0,0,W,H);
  }
  function scanlines(ctx){ ctx.globalAlpha=0.2; ctx.fillStyle='#000'; for(let y=0;y<H;y+=3) ctx.fillRect(0,y,W,1); ctx.globalAlpha=1; }

  // ---- hi-fi chrome pass v2 additions: deterministic atmospheric detail layers ----
  // twinkling starfield (night skies) — small soft dots (never a bare 1px pixel — a single full-alpha
  // pixel against a smooth gradient sky reads as a "hot pixel" artifact, especially once the image is
  // downscaled/point-sampled anywhere other than 1:1) — brightness/size varied by seeded rand, never
  // random per-frame.
  function stars(ctx, y0, y1, count, rand, tint){
    for(let i=0;i<count;i++){
      const x=rand()*W|0, y=(y0+rand()*(y1-y0))|0, big=rand()<0.16, a=0.16+rand()*0.34;
      ctx.globalAlpha=a; ctx.fillStyle=tint||'#e8dfc8';
      ctx.fillRect(x,y,2,2);
      if(big){ ctx.globalAlpha=a*0.5; ctx.fillRect(x-1,y,1,2); ctx.fillRect(x+2,y,1,2); ctx.fillRect(x,y-1,2,1); ctx.fillRect(x,y+2,2,1); }
    }
    ctx.globalAlpha=1;
  }
  // warm rising embers (forge/torch scenes) — small glowing particles, denser near the source. Same
  // "never a bare 1px" rule as stars() — minimum 2×2 with a soft 1px halo so it survives downscaling.
  function embers(ctx, cx, cy, count, rand, tint){
    for(let i=0;i<count;i++){
      const ang=rand()*Math.PI*2, dist=Math.pow(rand(),1.6)*520;
      const x=(cx+Math.cos(ang)*dist)|0, y=(cy-Math.abs(Math.sin(ang))*dist*0.7-rand()*160)|0;
      const a=0.12+rand()*0.38*(1-dist/560);
      if(a<=0) continue;
      ctx.globalAlpha=a; ctx.fillStyle=tint||'#e8913a'; ctx.fillRect(x,y,2,2);
      ctx.globalAlpha=a*0.4; ctx.fillRect(x-1,y-1,4,4);
    }
    ctx.globalAlpha=1;
  }
  // rim-light hairline along a skyline silhouette's top edge — cheap but reads as "lit from behind".
  // kept only as a standalone helper for callers that don't need the merged skyline(...,rim) form.
  function skylineRim(ctx, baseY, color, rand, units, h){
    ctx.fillStyle=color;
    const uw=W/units;
    for(let i=0;i<units;i++){ const x=i*uw, th=h*(0.7+rand()*0.6); ctx.fillRect(x, baseY-th-1, uw+1, 1); }
  }
  // small ink-blot texture (war-table parchment stains) — deterministic soft blobs
  function inkSpots(ctx, count, rand, color){
    for(let i=0;i<count;i++){
      const x=rand()*W, y=rand()*H, r=14+rand()*46;
      const g=ctx.createRadialGradient(x,y,0,x,y,r);
      g.addColorStop(0, color); g.addColorStop(1,'rgba(0,0,0,0)');
      ctx.globalAlpha=0.10+rand()*0.08; ctx.fillStyle=g; ctx.beginPath(); ctx.arc(x,y,r,0,7); ctx.fill();
    }
    ctx.globalAlpha=1;
  }

  // ---- contextual-encounter helpers (Doug direction 2026-07-12: "a sense of journey" — every
  // encounter node type gets a scene-appropriate backdrop; same toolkit, same idiom) ----
  // conifer treeline band — sawtooth spruce silhouettes with an optional lit-edge hairline
  function conifers(ctx, baseY, color, rand, units, h, rim){
    const uw=W/units;
    for(let i=0;i<units;i++){
      const x=i*uw+uw/2, th=h*(0.6+rand()*0.7), tw=uw*(0.65+rand()*0.3);
      ctx.fillStyle=color;
      ctx.beginPath(); ctx.moveTo(x,baseY-th);
      ctx.lineTo(x+tw/2,baseY); ctx.lineTo(x-tw/2,baseY); ctx.fill();
      ctx.beginPath(); ctx.moveTo(x,baseY-th*0.68);            // upper tier
      ctx.lineTo(x+tw*0.34,baseY-th*0.30); ctx.lineTo(x-tw*0.34,baseY-th*0.30); ctx.fill();
      if(rim){ ctx.strokeStyle=rim; ctx.lineWidth=1;
        ctx.beginPath(); ctx.moveTo(x,baseY-th); ctx.lineTo(x-tw/2,baseY); ctx.stroke(); }
    }
    ctx.fillStyle=color; ctx.fillRect(0,baseY-2,W,2+(H-baseY));  // close the band under the trees
  }
  // jagged mountain ridge through deterministic points; snowcaps on the highest verts
  function ridge(ctx, baseY, color, rand, step, hMin, hMax, rim, snow){
    const pts=[];
    for(let x=0;x<=W+step;x+=step){ const up=(pts.length%2===0);
      pts.push([x, baseY-(up? hMax*(0.55+rand()*0.45) : hMin*(0.3+rand()*0.7))]); }
    ctx.fillStyle=color; ctx.beginPath(); ctx.moveTo(0,baseY);
    pts.forEach(([x,y])=>ctx.lineTo(x,y)); ctx.lineTo(W,H); ctx.lineTo(0,H); ctx.fill();
    if(rim){ ctx.strokeStyle=rim; ctx.lineWidth=1; ctx.beginPath();
      pts.forEach(([x,y],j)=> j? ctx.lineTo(x,y-1) : ctx.moveTo(x,y-1)); ctx.stroke(); }
    if(snow){ ctx.fillStyle=snow;
      pts.forEach(([x,y],j)=>{ if(j%2===1 && y<baseY-hMax*0.7){
        ctx.beginPath(); ctx.moveTo(x,y); ctx.lineTo(x+step*0.16,y+hMax*0.14); ctx.lineTo(x-step*0.16,y+hMax*0.14); ctx.fill(); } }); }
  }
  // clean dark ground band + horizon line (combat_field's proven treatment)
  function ground(ctx, gy, topHex, botHex, lineHex){
    const gg=ctx.createLinearGradient(0,gy,0,H);
    gg.addColorStop(0,topHex); gg.addColorStop(1,botHex);
    ctx.fillStyle=gg; ctx.fillRect(0,gy,W,H-gy);
    ctx.fillStyle=lineHex; ctx.fillRect(0,gy-2,W,4);
  }
  // ridge-pole tent silhouette; `litX` (fire position) picks which slope takes the rim-light
  function tent(ctx, cx, baseY, w, h, color, rimColor){
    ctx.fillStyle=color;
    ctx.beginPath(); ctx.moveTo(cx,baseY-h); ctx.lineTo(cx+w/2,baseY); ctx.lineTo(cx-w/2,baseY); ctx.fill();
    ctx.strokeStyle=rimColor; ctx.lineWidth=2;
    ctx.beginPath(); ctx.moveTo(cx,baseY-h); ctx.lineTo(cx-w/2,baseY); ctx.stroke();  // fire-lit slope
    ctx.fillStyle='rgba(0,0,0,.55)';                                                   // door slit
    ctx.beginPath(); ctx.moveTo(cx-w*0.06,baseY-h*0.52); ctx.lineTo(cx+w*0.10,baseY); ctx.lineTo(cx-w*0.22,baseY); ctx.fill();
  }

  const out = {};

  // ---- 1. combat_field : dusk battlefield before a keep ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(101);
    vgrad(ctx,[[0,'#2b1f3a'],[0.40,'#241a30'],[0.62,'#1a1422'],[0.78,'#120d18'],[1,'#0c0812']]);
    // moon glow
    const mg=ctx.createRadialGradient(W*0.7,H*0.20,0,W*0.7,H*0.20,560);
    mg.addColorStop(0,'rgba(184,156,214,0.20)'); mg.addColorStop(1,'rgba(184,156,214,0)');
    ctx.fillStyle=mg; ctx.fillRect(0,0,W,H);
    stars(ctx, 20, 520, 140, r, '#e6dcf0');
    dither(ctx, 120, 560, '#3a2b4a','#2b1f3a', 8, 0.6);
    skyline(ctx, 560, '#191226', r, 7, 150, true, 'rgba(150,130,200,.30)');   // distant keep battlements
    skyline(ctx, 600, '#120d1c', r, 11, 90, true, 'rgba(120,104,168,.22)');    // nearer wall
    // clean dark ground (no dither speckle)
    const gg=ctx.createLinearGradient(0,636,0,H);
    gg.addColorStop(0,'#0e0a18'); gg.addColorStop(1,'#08060f');
    ctx.fillStyle=gg; ctx.fillRect(0,636,W,H-636);
    ctx.fillStyle='#060410'; ctx.fillRect(0,634,W,4);      // horizon line
    vignette(ctx,'rgba(8,7,16,0.9)',0.55); scanlines(ctx);
    out['combat_field']=c;
  })();

  // ---- 2. build_alcove : warm forge/armory niche ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(202);
    vgrad(ctx,[[0,'#2c2016'],[0.5,'#1d150e'],[1,'#120c08']]);
    // forge ember glow low-center
    const fg=ctx.createRadialGradient(W/2,H*0.92,0,W/2,H*0.92,760);
    fg.addColorStop(0,'rgba(224,145,58,0.18)'); fg.addColorStop(1,'rgba(224,145,58,0)');
    ctx.fillStyle=fg; ctx.fillRect(0,0,W,H);
    dither(ctx, 0, H, '#34261a','#241a12', 8, 0.5);
    // stone arch silhouette (two piers + lintel framing the niche)
    ctx.fillStyle='#160f0a';
    ctx.fillRect(0,0,240,H); ctx.fillRect(W-240,0,240,H);    // piers
    ctx.fillRect(0,0,W,120);                                  // lintel
    // arch curve
    ctx.beginPath(); ctx.moveTo(240,120); ctx.quadraticCurveTo(W/2,300,W-240,120); ctx.lineTo(W-240,120); ctx.lineTo(240,120); ctx.fill();
    // pier stone-block texture — deterministic, but low-contrast + slightly irregular row heights so
    // it reads as weathered masonry, not a mechanical printed grid
    ctx.globalAlpha=0.30; ctx.strokeStyle='rgba(0,0,0,.55)'; ctx.lineWidth=2;
    [[0,240],[W-240,W]].forEach(([x0,x1])=>{
      let y=0, row=0;
      while(y<H){ const rh=42+Math.round(r()*14); ctx.beginPath(); ctx.moveTo(x0,y); ctx.lineTo(x1,y); ctx.stroke();
        const off=(row%2)?26:0;
        for(let x=x0+off; x<x1; x+=52){ ctx.beginPath(); ctx.moveTo(x,y); ctx.lineTo(x,y+rh); ctx.stroke(); }
        y+=rh; row++;
      }
    });
    ctx.globalAlpha=1;
    // NOTE: no ember speckle here — floating dust dots over a floor with no visible fire/forge shape
    // to anchor them just read as noise, not "embers" (flagged by user review). The warm glow pool
    // above (`fg`) carries the forge-light implication on its own.
    vignette(ctx,'rgba(10,7,4,0.9)',0.5); scanlines(ctx);
    out['build_alcove']=c;
  })();

  // ---- 3. map_chart : dark war-table parchment ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(303);
    const rg=ctx.createRadialGradient(W*0.32,H*0.36,0,W*0.32,H*0.36,1500);
    rg.addColorStop(0,'#2c2132'); rg.addColorStop(0.55,'#1c1626'); rg.addColorStop(1,'#0a0710');
    ctx.fillStyle=rg; ctx.fillRect(0,0,W,H);
    dither(ctx, 0, H, '#2c2132','#231a2a', 8, 0.45);
    inkSpots(ctx, 22, r, '#4a3a5a');
    // faint contour grid
    ctx.strokeStyle='rgba(180,150,120,0.05)'; ctx.lineWidth=1;
    for(let x=0;x<W;x+=120){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,H);ctx.stroke();}
    for(let y=0;y<H;y+=120){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(W,y);ctx.stroke();}
    vignette(ctx,'rgba(8,7,16,0.95)',0.4); scanlines(ctx);
    out['map_chart']=c;
  })();

  // ---- 4. spine_road : receding road over dusk hills ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(404);
    vgrad(ctx,[[0,'#322438'],[0.45,'#26203a'],[0.72,'#1d1830'],[1,'#141022']]);
    const sg=ctx.createRadialGradient(W/2,H*0.5,0,W/2,H*0.5,640);
    sg.addColorStop(0,'rgba(224,170,90,0.16)'); sg.addColorStop(1,'rgba(224,170,90,0)');
    ctx.fillStyle=sg; ctx.fillRect(0,0,W,H);
    stars(ctx, 10, 480, 110, rng(405), '#f0e6d0');
    dither(ctx, 80, 620, '#3a2e4a','#2a2240', 8, 0.55);
    // layered hills (each gets a thin rim-light along its ridge, lit from the road glow)
    ['#1b1530','#161126','#100c1c'].forEach((col,i)=>{
      const baseY=560+i*70;
      const ridge=[];
      ctx.fillStyle=col; ctx.beginPath(); ctx.moveTo(0,baseY);
      for(let x=0;x<=W;x+=160){ const y=baseY - Math.sin((x/W)*Math.PI*(1.5+i)+i)*60*(1+r()*0.3); ridge.push([x,y]); ctx.lineTo(x,y); }
      ctx.lineTo(W,H); ctx.lineTo(0,H); ctx.fill();
      ctx.strokeStyle=`rgba(224,170,90,${0.14-i*0.04})`; ctx.lineWidth=2;
      ctx.beginPath(); ridge.forEach(([x,y],j)=> j? ctx.lineTo(x,y-1) : ctx.moveTo(x,y-1)); ctx.stroke();
    });
    // road tapering to horizon
    ctx.fillStyle='#0f0b1a'; ctx.beginPath();
    ctx.moveTo(W/2-18,560); ctx.lineTo(W/2+18,560); ctx.lineTo(W*0.74,H); ctx.lineTo(W*0.26,H); ctx.fill();
    vignette(ctx,'rgba(8,6,14,0.9)',0.5); scanlines(ctx);
    out['spine_road']=c;
  })();

  // ---- 5. merchant_stall : lantern-lit trade tent (DESIGN_SPEC §12 merchant screen) ----
  // Procedural stand-in like every backdrop (Doug's call — no hand-painted scene art): striped
  // awning band with a scalloped hem up top, lantern glow, side posts, a dark counter band low.
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(505);
    vgrad(ctx,[[0,'#2e2114'],[0.5,'#1f150d'],[1,'#120c08']]);
    // the merchant's lantern — warm pool upper-left
    const lg=ctx.createRadialGradient(W*0.24,H*0.30,0,W*0.24,H*0.30,720);
    lg.addColorStop(0,'rgba(224,163,74,0.20)'); lg.addColorStop(1,'rgba(224,163,74,0)');
    ctx.fillStyle=lg; ctx.fillRect(0,0,W,H);
    dither(ctx, 0, H, '#34261a','#241a12', 8, 0.5);
    // canvas awning: alternating stripes + scalloped hem
    const stripes=16, sw=W/stripes;
    for(let i=0;i<stripes;i++){
      ctx.fillStyle= i%2 ? '#241610' : '#33200f';
      ctx.fillRect(i*sw,0,sw+1,140);
      ctx.beginPath(); ctx.arc(i*sw+sw/2,140,sw/2,0,Math.PI); ctx.fill();
    }
    ctx.globalAlpha=0.35; ctx.fillStyle='#000';
    for(let i=0;i<stripes;i++){ ctx.beginPath(); ctx.arc(i*sw+sw/2,144,sw/2,0,Math.PI); ctx.fill(); }
    ctx.globalAlpha=1;
    // stall posts framing the scene
    ctx.fillStyle='#160f0a'; ctx.fillRect(88,120,28,H-120); ctx.fillRect(W-116,120,28,H-120);
    // counter band + crate/barrel silhouettes (low, mostly behind the UI)
    ctx.fillStyle='#181009'; ctx.fillRect(0,H-210,W,210);
    ctx.fillStyle='rgba(0,0,0,.45)'; ctx.fillRect(0,H-214,W,4);
    ctx.fillStyle='#130c07';
    for(let i=0;i<7;i++){ const x=140+r()*(W-360), w=70+r()*90, h=60+r()*70; ctx.fillRect(x,H-210-h,w,h); ctx.fillStyle='rgba(0,0,0,.5)'; ctx.fillRect(x,H-210-h,w,3); ctx.fillStyle='#130c07'; }
    vignette(ctx,'rgba(10,7,4,0.9)',0.5); scanlines(ctx);
    out['merchant_stall']=c;
  })();

  // ==== CONTEXTUAL ENCOUNTER BACKDROPS (Doug 2026-07-12: "a sense of journey" — the Encounter
  // shell picks a scene per node via bg/{encounter.scene}; combat_field stays the plain default).
  // All keep the family idiom: dusk gradient → glow → stars → dither → silhouettes → CLEAN dark
  // ground (~y610–650, figures + HUD live there) → vignette → scanlines. Deterministic seeds. ====

  // ---- 6. enc_camp : YOUR camp in the right foreground (replaces the campMarker icon+note —
  // campfire, tents, gear where a foe would stand; foeless arrival, so the right side is free) ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(1212);
    vgrad(ctx,[[0,'#2b1f3a'],[0.40,'#241a30'],[0.62,'#1b1522'],[0.78,'#141018'],[1,'#0d0a12']]);
    const mg=ctx.createRadialGradient(W*0.24,H*0.16,0,W*0.24,H*0.16,480);
    mg.addColorStop(0,'rgba(184,156,214,0.14)'); mg.addColorStop(1,'rgba(184,156,214,0)');
    ctx.fillStyle=mg; ctx.fillRect(0,0,W,H);
    stars(ctx, 20, 500, 140, r, '#e6dcf0');
    dither(ctx, 120, 560, '#3a2b4a','#2b1f3a', 8, 0.55);
    conifers(ctx, 596, '#141020', r, 18, 84, 'rgba(150,130,200,.16)');
    ground(ctx, 636, '#0e0a18', '#08060f', '#060410');
    // fire glow owns the right half
    let fg=ctx.createRadialGradient(1460,640,0,1460,640,560);
    fg.addColorStop(0,'rgba(232,145,58,0.26)'); fg.addColorStop(1,'rgba(232,145,58,0)');
    ctx.fillStyle=fg; ctx.fillRect(0,0,W,H);
    fg=ctx.createRadialGradient(1460,652,0,1460,652,190);
    fg.addColorStop(0,'rgba(244,178,86,0.22)'); fg.addColorStop(1,'rgba(244,178,86,0)');
    ctx.fillStyle=fg; ctx.fillRect(0,0,W,H);
    // tents (fire-lit slopes face the fire)
    tent(ctx, 1728, 682, 306, 272, '#171019', 'rgba(240,170,90,.38)');
    tent(ctx, 1318, 664, 178, 146, '#120d15', 'rgba(240,170,90,.26)');
    // gear: crates + a stump seat + a spear rack leaning by the big tent
    ctx.fillStyle='#100b12'; ctx.fillRect(1152,606,44,36); ctx.fillRect(1186,618,32,24);
    ctx.fillStyle='rgba(240,170,90,.14)'; ctx.fillRect(1152,606,44,3); ctx.fillRect(1186,618,32,2);
    ctx.fillStyle='#120c10'; ctx.fillRect(1372,636,30,22);
    ctx.fillStyle='rgba(240,170,90,.16)'; ctx.fillRect(1372,636,30,2);
    ctx.strokeStyle='#0f0a10'; ctx.lineWidth=3;
    [[1546,668,1568,556],[1560,668,1572,552],[1576,668,1578,550]].forEach(([x0,y0,x1,y1])=>{
      ctx.beginPath(); ctx.moveTo(x0,y0); ctx.lineTo(x1,y1); ctx.stroke(); });
    // the campfire itself: crossed logs + layered flame + ground light pool
    ctx.fillStyle='rgba(232,145,58,.16)'; ctx.beginPath(); ctx.ellipse(1460,668,120,20,0,0,7); ctx.fill();
    ctx.save(); ctx.translate(1460,658);
    ctx.rotate(0.42); ctx.fillStyle='#241408'; ctx.fillRect(-26,-4,52,8);
    ctx.rotate(-0.84); ctx.fillRect(-26,-4,52,8); ctx.restore();
    ctx.fillStyle='rgba(232,120,40,.9)'; ctx.beginPath(); ctx.moveTo(1460,606); ctx.lineTo(1476,652); ctx.lineTo(1444,652); ctx.fill();
    ctx.fillStyle='rgba(248,200,90,.95)'; ctx.beginPath(); ctx.moveTo(1460,622); ctx.lineTo(1469,652); ctx.lineTo(1451,652); ctx.fill();
    embers(ctx, 1460, 630, 90, r, '#e8a13a');
    vignette(ctx,'rgba(8,7,16,0.9)',0.55); scanlines(ctx);
    out['enc_camp']=c;
  })();

  // ---- 7. enc_forest : deep-wood clearing (any wooded node) ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(606);
    vgrad(ctx,[[0,'#22301f'],[0.40,'#1b2a1e'],[0.62,'#152016'],[0.78,'#0f1710'],[1,'#0a0f0a']]);
    const mg=ctx.createRadialGradient(W*0.65,H*0.18,0,W*0.65,H*0.18,520);
    mg.addColorStop(0,'rgba(200,224,180,0.19)'); mg.addColorStop(1,'rgba(200,224,180,0)');
    ctx.fillStyle=mg; ctx.fillRect(0,0,W,H);
    stars(ctx, 20, 440, 90, r, '#e0ecd0');
    dither(ctx, 120, 540, '#2c3a28','#22301f', 8, 0.55);
    conifers(ctx, 548, '#131c12', r, 24, 116, 'rgba(170,210,150,.26)');
    // low mist pooling between the tree bands
    const fmist=ctx.createLinearGradient(0,540,0,600);
    fmist.addColorStop(0,'rgba(170,200,150,0)'); fmist.addColorStop(0.5,'rgba(170,200,150,0.07)'); fmist.addColorStop(1,'rgba(170,200,150,0)');
    ctx.fillStyle=fmist; ctx.fillRect(0,540,W,60);
    conifers(ctx, 618, '#0d140c', r, 13, 176, 'rgba(150,190,130,.20)');
    // fireflies drifting over the undergrowth line
    for(let i=0;i<38;i++){ const x=r()*W|0, y=(470+r()*150)|0, a=0.14+r()*0.30;
      ctx.globalAlpha=a; ctx.fillStyle='#c8d878'; ctx.fillRect(x,y,2,2);
      ctx.globalAlpha=a*0.4; ctx.fillRect(x-1,y-1,4,4); }
    ctx.globalAlpha=1;
    ground(ctx, 640, '#0c120a', '#070b06', '#050904');
    vignette(ctx,'rgba(6,10,6,0.9)',0.55); scanlines(ctx);
    out['enc_forest']=c;
  })();

  // ---- 8. enc_mountain : high pass under cold peaks ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(707);
    vgrad(ctx,[[0,'#232840'],[0.40,'#1d2236'],[0.65,'#161a29'],[0.80,'#10131d'],[1,'#0a0c12']]);
    const mg=ctx.createRadialGradient(W*0.30,H*0.14,0,W*0.30,H*0.14,520);
    mg.addColorStop(0,'rgba(190,210,240,0.16)'); mg.addColorStop(1,'rgba(190,210,240,0)');
    ctx.fillStyle=mg; ctx.fillRect(0,0,W,H);
    stars(ctx, 16, 460, 170, r, '#dfe8f4');
    // thin cloud streaks hanging on the ranges
    for(let i=0;i<4;i++){ const y=210+i*46+r()*20, x=r()*W*0.6, w=420+r()*520;
      const cg=ctx.createLinearGradient(x,0,x+w,0);
      cg.addColorStop(0,'rgba(200,214,238,0)'); cg.addColorStop(0.5,'rgba(200,214,238,0.07)'); cg.addColorStop(1,'rgba(200,214,238,0)');
      ctx.fillStyle=cg; ctx.fillRect(x,y,w,10); }
    dither(ctx, 100, 520, '#2a3048','#232840', 8, 0.5);
    ridge(ctx, 520, '#141827', r, 150, 60, 230, 'rgba(180,200,235,.30)', 'rgba(210,225,245,.38)');
    ridge(ctx, 616, '#0d101b', r, 210, 40, 140, 'rgba(160,180,220,.20)', null);
    skyline(ctx, 648, '#0a0c14', r, 30, 26, false);
    ground(ctx, 650, '#0b0d14', '#060810', '#04060a');
    vignette(ctx,'rgba(7,8,14,0.9)',0.55); scanlines(ctx);
    out['enc_mountain']=c;
  })();

  // ---- 9. enc_river : moonlit ford — water band runs the midground, fight on the near bank ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(808);
    vgrad(ctx,[[0,'#243046'],[0.42,'#1e2839'],[0.62,'#17202c'],[0.78,'#101620'],[1,'#0a0e14']]);
    const mg=ctx.createRadialGradient(W*0.68,H*0.17,0,W*0.68,H*0.17,540);
    mg.addColorStop(0,'rgba(190,205,230,0.27)'); mg.addColorStop(1,'rgba(190,205,230,0)');
    ctx.fillStyle=mg; ctx.fillRect(0,0,W,H);
    stars(ctx, 18, 460, 130, r, '#dce6f2');
    dither(ctx, 110, 500, '#2b3852','#243046', 8, 0.5);
    skyline(ctx, 508, '#131a26', r, 16, 64, false, 'rgba(150,180,220,.14)');   // far bank rises
    // the water band: flat sheet + drifting shimmer + the moon's broken reflection column
    const wg=ctx.createLinearGradient(0,512,0,606);
    wg.addColorStop(0,'#1a2432'); wg.addColorStop(1,'#121a26');
    ctx.fillStyle=wg; ctx.fillRect(0,512,W,94);
    for(let i=0;i<130;i++){ const x=r()*W|0, y=(516+r()*84)|0, w=(10+r()*36)|0;
      ctx.globalAlpha=0.08+r()*0.09; ctx.fillStyle='#9ab0cc'; ctx.fillRect(x,y,w,1); }
    for(let y=514;y<604;y+=5){ const jx=(W*0.68+(r()-0.5)*54)|0, w=(8+r()*30)|0;
      ctx.globalAlpha=0.18+r()*0.30; ctx.fillStyle='#c8d8ec'; ctx.fillRect(jx-w/2,y,w,2); }
    ctx.globalAlpha=1;
    // reeds along the near waterline
    ctx.fillStyle='#080c12';
    for(let x=0;x<W;x+=8+((r()*10)|0)){ const h=(7+r()*13)|0; ctx.fillRect(x,606-h,2,h); }
    ground(ctx, 612, '#0c0f16', '#070910', '#05070c');
    vignette(ctx,'rgba(7,9,14,0.9)',0.55); scanlines(ctx);
    out['enc_river']=c;
  })();

  // ---- 10. enc_meadow : open grassland at dusk ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(909);
    vgrad(ctx,[[0,'#33283a'],[0.40,'#2c2433'],[0.62,'#241f28'],[0.78,'#181419'],[1,'#0e0c10']]);
    const hg=ctx.createRadialGradient(W*0.5,H*0.55,0,W*0.5,H*0.55,900);
    hg.addColorStop(0,'rgba(224,170,90,0.19)'); hg.addColorStop(1,'rgba(224,170,90,0)');
    ctx.fillStyle=hg; ctx.fillRect(0,0,W,H);
    stars(ctx, 18, 430, 100, r, '#f0e6d0');
    dither(ctx, 100, 520, '#3a2e42','#33283a', 8, 0.5);
    // rolling hills, warm rim; a couple of lone broadleaf trees on the mid ridge
    ['#1d1a22','#171420','#110f18'].forEach((col,i)=>{
      const baseY=540+i*44, ridgePts=[];
      ctx.fillStyle=col; ctx.beginPath(); ctx.moveTo(0,baseY);
      for(let x=0;x<=W;x+=160){ const y=baseY - Math.sin((x/W)*Math.PI*(1.1+i*0.5)+i*1.7)*38*(1+r()*0.3); ridgePts.push([x,y]); ctx.lineTo(x,y); }
      ctx.lineTo(W,H); ctx.lineTo(0,H); ctx.fill();
      ctx.strokeStyle=`rgba(224,170,90,${0.17-i*0.045})`; ctx.lineWidth=2;
      ctx.beginPath(); ridgePts.forEach(([x,y],j)=> j? ctx.lineTo(x,y-1) : ctx.moveTo(x,y-1)); ctx.stroke();
      if(i===1){ [[430,-6],[1490,-2]].forEach(([tx,dy])=>{
        const ty=baseY-30+dy;
        ctx.fillStyle='#100e16'; ctx.fillRect(tx-3,ty-26,6,26);
        ctx.beginPath(); ctx.arc(tx,ty-38,22,0,7); ctx.fill();
        ctx.beginPath(); ctx.arc(tx-14,ty-28,14,0,7); ctx.fill();
        ctx.beginPath(); ctx.arc(tx+15,ty-30,15,0,7); ctx.fill(); }); }
    });
    // grass-tuft band along the fore ridge
    ctx.fillStyle='#0d0b12';
    for(let x=0;x<W;x+=6+((r()*9)|0)){ const h=(8+r()*12)|0; ctx.fillRect(x,634-h,2,h); }
    // fireflies low over the grass
    for(let i=0;i<26;i++){ const x=r()*W|0, y=(520+r()*110)|0, a=0.12+r()*0.26;
      ctx.globalAlpha=a; ctx.fillStyle='#d8c878'; ctx.fillRect(x,y,2,2); }
    ctx.globalAlpha=1;
    ground(ctx, 636, '#0e0c12', '#080709', '#060508');
    vignette(ctx,'rgba(8,7,12,0.9)',0.55); scanlines(ctx);
    out['enc_meadow']=c;
  })();

  // ---- 11. enc_quarry : cut-stone terraces + hoist (ResourceHold — stone operation) ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(1010);
    vgrad(ctx,[[0,'#2e2a2a'],[0.42,'#262322'],[0.62,'#1d1a19'],[0.78,'#141211'],[1,'#0c0b0a']]);
    const dg=ctx.createRadialGradient(W*0.5,H*0.5,0,W*0.5,H*0.5,880);
    dg.addColorStop(0,'rgba(200,180,150,0.10)'); dg.addColorStop(1,'rgba(200,180,150,0)');
    ctx.fillStyle=dg; ctx.fillRect(0,0,W,H);
    stars(ctx, 16, 380, 60, r, '#e6ded0');
    dither(ctx, 90, 480, '#363130','#2e2a2a', 8, 0.5);
    // stepped benches cut down toward the pit floor from both sides
    for(let i=0;i<5;i++){ const y=402+i*52, xw=560-i*96;
      ctx.fillStyle= i%2 ? '#181514' : '#151211';
      ctx.fillRect(0,y,xw,H-y); ctx.fillRect(W-xw,y,xw,H-y);
      ctx.fillStyle='rgba(210,190,160,.10)'; ctx.fillRect(0,y,xw,2); ctx.fillRect(W-xw,y,xw,2); }
    // timber hoist over the pit
    ctx.fillStyle='#0e0c0b';
    ctx.fillRect(1272,336,10,304);                      // mast
    ctx.save(); ctx.translate(1277,344); ctx.rotate(0.35); ctx.fillRect(0,-5,250,9); ctx.restore();  // jib
    ctx.fillRect(1246,614,62,26);                       // base
    ctx.strokeStyle='#0e0c0b'; ctx.lineWidth=2;
    ctx.beginPath(); ctx.moveTo(1511,430); ctx.lineTo(1511,538); ctx.stroke();   // rope
    ctx.fillStyle='#12100e'; ctx.fillRect(1499,538,24,20);                        // hanging block
    ctx.fillStyle='rgba(210,190,160,.12)'; ctx.fillRect(1499,538,24,2);
    // cut-block piles low on the benches
    ctx.fillStyle='#131110';
    [[300,596,54,40],[352,610,40,26],[1600,590,60,46],[1662,614,36,22],[820,616,44,22]].forEach(([x,y,w,h])=>{
      ctx.fillRect(x,y,w,h); ctx.fillStyle='rgba(210,190,160,.10)'; ctx.fillRect(x,y,w,2); ctx.fillStyle='#131110'; });
    ground(ctx, 640, '#100e0d', '#090807', '#070605');
    vignette(ctx,'rgba(9,8,7,0.9)',0.5); scanlines(ctx);
    out['enc_quarry']=c;
  })();

  // ---- 12. enc_lumber : clear-cut + log decks (ResourceHold — timber operation) ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(1111);
    vgrad(ctx,[[0,'#2c2618'],[0.45,'#221d12'],[0.65,'#19150d'],[0.80,'#110e09'],[1,'#0a0806']]);
    const lg=ctx.createRadialGradient(W*0.30,H*0.48,0,W*0.30,H*0.48,640);
    lg.addColorStop(0,'rgba(224,163,74,0.15)'); lg.addColorStop(1,'rgba(224,163,74,0)');
    ctx.fillStyle=lg; ctx.fillRect(0,0,W,H);
    stars(ctx, 16, 400, 80, r, '#ece0c8');
    dither(ctx, 100, 500, '#332c1c','#2c2618', 8, 0.5);
    conifers(ctx, 540, '#161408', r, 20, 104, 'rgba(224,190,130,.12)');   // the wood not yet felled
    ground(ctx, 640, '#0e0b07', '#080604', '#060503');
    // stump field across the clear-cut band (pale cut faces catch the lantern light)
    for(let i=0;i<26;i++){ const x=(60+r()*(W-120))|0, y=(560+r()*66)|0, w=(8+r()*8)|0, h=(6+r()*7)|0;
      ctx.fillStyle='#0d0a06'; ctx.fillRect(x,y,w,h);
      ctx.fillStyle='rgba(214,186,140,.20)'; ctx.fillRect(x,y,w,2); }
    // log decks (stacked trunks, pale end-grain rings) + support posts
    function deck(bx,by,rows,rad){
      for(let row=0;row<rows;row++){ const n=rows-row;
        for(let k=0;k<n;k++){ const cx=bx+k*rad*2+row*rad, cy=by-row*(rad*1.8);
          ctx.fillStyle='#171208'; ctx.beginPath(); ctx.arc(cx,cy,rad,0,7); ctx.fill();
          ctx.fillStyle='rgba(224,190,130,.16)'; ctx.beginPath(); ctx.arc(cx,cy,rad*0.62,0,7); ctx.fill();
          ctx.fillStyle='#171208'; ctx.beginPath(); ctx.arc(cx,cy,rad*0.28,0,7); ctx.fill(); } }
      ctx.fillStyle='#100c06';
      ctx.fillRect(bx-rad-8, by-rad*1.6, 6, rad*2.6);
      ctx.fillRect(bx+(rows-1)*rad*2+rad+2, by-rad*1.6, 6, rad*2.6); }
    deck(1330,632,3,17); deck(1620,630,2,20);
    // bucking frame (two X-legs + a trunk across) by the lantern glow
    ctx.strokeStyle='#120e08'; ctx.lineWidth=6;
    [[470,640,510,560],[510,640,470,560],[650,640,690,560],[690,640,650,560]].forEach(([x0,y0,x1,y1])=>{
      ctx.beginPath(); ctx.moveTo(x0,y0); ctx.lineTo(x1,y1); ctx.stroke(); });
    ctx.fillStyle='#171208'; ctx.fillRect(440,552,280,14);
    ctx.fillStyle='rgba(224,190,130,.14)'; ctx.fillRect(440,552,280,2);
    embers(ctx, W*0.30, 560, 36, r, '#d8b878');   // sawdust motes in the lantern pool
    vignette(ctx,'rgba(9,7,4,0.9)',0.5); scanlines(ctx);
    out['enc_lumber']=c;
  })();

  // ---- 13. enc_city_gates : the march arrives — walled city on the horizon, road to the gate ----
  (function(){
    const c=createCanvas(W,H), ctx=c.getContext('2d'), r=rng(1313);
    vgrad(ctx,[[0,'#322438'],[0.45,'#26203a'],[0.72,'#1d1830'],[1,'#141022']]);
    const cg=ctx.createRadialGradient(W*0.5,540,0,W*0.5,540,760);
    cg.addColorStop(0,'rgba(224,170,90,0.18)'); cg.addColorStop(1,'rgba(224,170,90,0)');
    ctx.fillStyle=cg; ctx.fillRect(0,0,W,H);
    stars(ctx, 14, 420, 110, r, '#f0e6d0');
    dither(ctx, 90, 500, '#3a2e4a','#2a2240', 8, 0.5);
    // distant spires behind the wall
    ctx.fillStyle='#1b1530';
    [[700,470,26,120],[860,440,30,150],[1010,455,24,135],[1150,478,28,112]].forEach(([x,y,w,h])=>{
      ctx.fillRect(x,y,w,h); ctx.beginPath(); ctx.moveTo(x-4,y); ctx.lineTo(x+w/2,y-26); ctx.lineTo(x+w+4,y); ctx.fill(); });
    // curtain wall + towers + the lit gatehouse
    ctx.fillStyle='#161126'; ctx.fillRect(430,562,1060,60);
    for(let x=430;x<1490;x+=36) ctx.fillRect(x,550,20,12);                     // crenellations
    [[500,72,166],[724,62,132],[1160,62,132],[1372,72,166]].forEach(([x,w,h])=>{
      ctx.fillStyle='#131022'; ctx.fillRect(x,622-h,w,h);
      for(let mx=x-4;mx<x+w;mx+=22) ctx.fillRect(mx,622-h-10,12,12);
      for(let i=0;i<5;i++){ ctx.globalAlpha=0.35+r()*0.4; ctx.fillStyle='#e0aa5a';
        ctx.fillRect(x+8+((r()*(w-18))|0), 622-h+16+((r()*(h-40))|0), 3,5); }
      ctx.globalAlpha=1; });
    ctx.fillStyle='#110e20'; ctx.fillRect(886,438,148,184);                    // gatehouse
    for(let mx=886;mx<1034;mx+=24) ctx.fillRect(mx,426,14,14);
    ctx.fillStyle='rgba(232,170,80,.55)';                                      // the open gate's light
    ctx.beginPath(); ctx.moveTo(936,622); ctx.lineTo(936,566); ctx.quadraticCurveTo(960,540,984,566); ctx.lineTo(984,622); ctx.fill();
    ground(ctx, 626, '#0f0b1a', '#0a0714', '#080512');
    // road tapering up to the gate, faint wheel ruts
    ctx.fillStyle='#141024'; ctx.beginPath();
    ctx.moveTo(W/2-26,626); ctx.lineTo(W/2+26,626); ctx.lineTo(W*0.72,H); ctx.lineTo(W*0.28,H); ctx.fill();
    ctx.strokeStyle='rgba(224,170,90,.08)'; ctx.lineWidth=3;
    ctx.beginPath(); ctx.moveTo(W/2-13,630); ctx.lineTo(W*0.39,H); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(W/2+13,630); ctx.lineTo(W*0.61,H); ctx.stroke();
    vignette(ctx,'rgba(8,6,14,0.9)',0.55); scanlines(ctx);
    out['enc_city_gates']=c;
  })();

  for(const name in out) await saveFile('Content/bg/'+name+'.png', out[name]);
  log('backdrops:', Object.keys(out).join(', '));
  return Object.keys(out);
}

if (typeof module !== 'undefined' && module.exports) module.exports = { RB_buildBackdrops };
