// Roguebane — ENEMY WAR-PARTY assets GENERATOR (persistent, ART_RULES flat-bevel idiom).
// The enemy host marches from the castle (RIGHT) toward your camp (LEFT). Rendered as a top-of-map
// DOOM BAR: [camp] ←—— red track with a marching host marker ——— [castle]. Reaching camp = you lose.
// World-art (sprite idiom), NOT UI chrome: flat single-colour fills, own 1px black border per part,
// single bottom/right shadow bevel, crisp nearest-neighbor, transparent bg. LEFT-FACING (advancing).
// Assets:
//   enemy_host        — a mounted knight + war banner, facing LEFT (the marker that rides the bar front)
//   enemy_host_near   — same, banner + barding brighter red (danger, when close to camp)
// Run via run_script:
//   eval(await readFile('proto/party_gen.js'));
//   await RB_buildParty({ createCanvas, saveFile, log });

async function RB_buildParty(env) {
  const { createCanvas, saveFile, log } = env;
  const OUT = [21, 16, 12];
  const C = {
    horse:[74,60,50], horseSh:[52,41,34],
    iron:[98,102,116], ironSh:[66,70,84],
    steel:[150,156,168], steelSh:[104,110,124],
    dark:[46,40,54], darkSh:[28,24,34],
    red:[188,58,48], redSh:[138,40,32],
    redHot:[224,78,58], redHotSh:[176,52,38],
    pole:[150,110,60], poleSh:[104,74,40],
  };
  const grid = (W,H) => ({ W, H, px: Array.from({length:H},()=>new Array(W).fill(null)) });
  const setPx = (g,x,y,c) => { if(x<0||y<0||x>=g.W||y>=g.H) return; g.px[y][x]=c; };
  function paint(g, mask, base, shadow) {
    const E = (x,y) => mask(x,y) && (!mask(x-1,y)||!mask(x+1,y)||!mask(x,y-1)||!mask(x,y+1));
    for (let y=0;y<g.H;y++) for (let x=0;x<g.W;x++) {
      if (!mask(x,y)) continue;
      if (E(x,y)) { setPx(g,x,y,OUT); continue; }
      setPx(g,x,y,(E(x+1,y)||E(x,y+1))?shadow:base);
    }
  }
  const rect = (x0,y0,x1,y1)=>(x,y)=>x>=x0&&x<=x1&&y>=y0&&y<=y1;
  const disc = (cx,cy,r)=>(x,y)=>(x-cx)*(x-cx)+(y-cy)*(y-cy)<=r*r;
  // left-flying pennant: solid at the pole (right) with a swallowtail notch cut into the LEFT (fly) edge
  const pennantL = (x0,y0,x1,y1)=>{ const my=(y0+y1)/2, depth=(x1-x0)*0.42;
    return (x,y)=> x>=x0&&x<=x1&&y>=y0&&y<=y1 && (x > x0+depth || Math.abs(y-my) >= ((x0+depth)-x)*((y1-y0)/2)/depth); };
  // thick line segment (for weapon shafts / arms)
  const seg = (x0,y0,x1,y1,w)=>{ const dx=x1-x0, dy=y1-y0, L2=(dx*dx+dy*dy)||1, h=w/2;
    return (x,y)=>{ let t=((x-x0)*dx+(y-y0)*dy)/L2; t=Math.max(0,Math.min(1,t));
      const px=x0+t*dx, py=y0+t*dy; return (x-px)*(x-px)+(y-py)*(y-py) <= h*h; }; };

  // compact LEFT-FACING warhorse (feet baseline oy, front-left at ox). Kept small so the trio scales big.
  function horse(g, ox, oy, o) {
    const H=C.horse, HS=C.horseSh, red=o.near?C.redHot:C.red, redSh=o.near?C.redHotSh:C.redSh;
    for (const lx of [ox+4, ox+7, ox+13, ox+16]) paint(g, rect(lx, oy-6, lx+1, oy), H, HS); // 4 legs
    paint(g, rect(ox+3, oy-12, ox+17, oy-6), H, HS);                                         // barrel + haunch
    paint(g, (x,y)=> x>=ox-1 && x<=ox+5 && y>=oy-15 && y<=oy-8 && (x-(ox-1)) >= (oy-8-y)-2, H, HS); // neck
    paint(g, rect(ox-5, oy-15, ox-1, oy-11), H, HS);                                         // head
    paint(g, rect(ox-6, oy-13, ox-5, oy-11), H, HS);                                         // muzzle
    paint(g, rect(ox-2, oy-17, ox-1, oy-15), H, HS);                                         // ear
    paint(g, (x,y)=> x>=ox+17 && x<=ox+20 && y>=oy-12 && y<=oy-3 && (x-(ox+17)) <= (y-(oy-12))/2.2, H, HS); // tail
    paint(g, (x,y)=> x>=ox+4 && x<=ox+16 && y>=oy-8 && y<=oy-6 && ((x-(ox+4))%6)!==5, red, redSh); // caparison
  }

  // one MOUNTED soldier: a compact horse + a rider seated on the barrel top. Rider seat at oy-12.
  function mounted(g, ox, oy, o) {
    horse(g, ox, oy, o);
    const red=o.near?C.redHot:C.red, redSh=o.near?C.redHotSh:C.redSh;
    // weapon shaft drawn BEHIND the rider
    if (o.weapon === 'spear') {
      paint(g, seg(ox-6, oy-20, ox+9, oy-14, 2), C.pole, C.poleSh);                          // shaft forward-left over horse head
      paint(g, (x,y)=> x>=ox-7 && x<=ox-2 && Math.abs(y-(oy-20)) <= (x-(ox-7))*0.7, C.steel, C.steelSh); // head
    } else if (o.weapon === 'banner') {
      paint(g, rect(ox+9, oy-34, ox+10, oy-13), C.pole, C.poleSh);                           // tall pole
      paint(g, disc(ox+9.5, oy-34, 1.6), red, redSh);                                        // finial
      paint(g, pennantL(ox+0, oy-32, ox+9, oy-22), red, redSh);                              // pennant flies left
      paint(g, disc(ox+4, oy-27, 1.9), C.dark, C.darkSh);                                    // emblem
    }
    paint(g, rect(ox+8, oy-13, ox+10, oy-8), C.iron, C.ironSh);                              // rider leg over near side
    paint(g, rect(ox+6, oy-20, ox+13, oy-12), C.iron, C.ironSh);                             // torso
    paint(g, rect(ox+6, oy-16, ox+13, oy-15), red, redSh);                                   // sash
    paint(g, rect(ox+7, oy-24, ox+11, oy-20), C.steel, C.steelSh);                           // square head, attached
    if (o.weapon === 'banner') {
      paint(g, seg(ox+9, oy-17, ox+9, oy-21, 2.5), C.steel, C.steelSh);                      // raised grip on pole
    } else if (o.weapon === 'sword') {
      paint(g, seg(ox+6, oy-17, ox+1, oy-21, 2.5), C.steel, C.steelSh);                      // arm up-left
      paint(g, seg(ox+1, oy-20, ox-2, oy-27, 2),   C.steel, C.steelSh);                      // raised blade
      paint(g, rect(ox,   oy-21, ox+3, oy-20),     C.dark, C.darkSh);                         // crossguard
    } else { // spear
      paint(g, seg(ox+6, oy-16, ox+1, oy-15, 2.5), C.steel, C.steelSh);                      // arm forward-left onto shaft
    }
  }

  function build(near) {
    const W=66, H=66, g=grid(W,H);
    // three MOUNTED soldiers, facing LEFT, TIGHT diamond (front → back), figures overlapping:
    //   spear (front, low-left) · sword (mid, high) · banner (back, low-right, slight lag)
    mounted(g, 42, 62, { weapon:'banner', near });  // back
    mounted(g, 25, 52, { weapon:'sword',  near });  // mid
    mounted(g,  8, 60, { weapon:'spear',  near });  // front
    // export 1:1
    const cv=createCanvas(W,H), ctx=cv.getContext('2d'), id=ctx.createImageData(W,H);
    for (let y=0;y<H;y++) for (let x=0;x<W;x++){ const p=g.px[y][x]; const i=(y*W+x)*4;
      if(!p){ id.data[i+3]=0; continue; } id.data[i]=p[0]; id.data[i+1]=p[1]; id.data[i+2]=p[2]; id.data[i+3]=255; }
    ctx.putImageData(id,0,0); return cv;
  }

  await saveFile('Content/icons/map/enemy_host.png', build(false));
  await saveFile('Content/icons/map/enemy_host_near.png', build(true));
  if (log) log('enemy host markers written: enemy_host, enemy_host_near (66x66, 3 mounted riders, tight, left-facing)');
  return 2;
}
if (typeof module !== 'undefined' && module.exports) module.exports = { RB_buildParty };
