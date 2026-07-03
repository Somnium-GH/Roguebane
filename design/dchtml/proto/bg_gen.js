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

  for(const name in out) await saveFile('Content/bg/'+name+'.png', out[name]);
  log('backdrops:', Object.keys(out).join(', '));
  return Object.keys(out);
}

if (typeof module !== 'undefined' && module.exports) module.exports = { RB_buildBackdrops };
