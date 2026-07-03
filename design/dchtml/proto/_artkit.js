// Roguebane hi-fi pixel-art toolkit (shared). Read + eval in run_script via: eval(await readFile('Content/_artkit.js'))
// Exposes: ARTKIT with grid/ramp/limb/sphere/blade helpers producing 5-6 tone hand-shaded sprites,
// low-radius normal bevel (top-left light, bottom-right dark on edge band only), 1px dark outline.
globalThis.ARTKIT = (function(){
  const OUT = '#140d10';
  // build a 6-stop ramp from a base hex (perceptual-ish lighten/darken)
  function hx(h){h=h.replace('#','');return [parseInt(h.slice(0,2),16),parseInt(h.slice(2,4),16),parseInt(h.slice(4,6),16)];}
  function hex(r){return '#'+r.map(v=>Math.max(0,Math.min(255,Math.round(v))).toString(16).padStart(2,'0')).join('');}
  function mix(a,b,t){return a.map((v,i)=>v+(b[i]-v)*t);}
  function ramp(base,{warm=true}={}){
    const c=hx(base), white=[255,250,242], black=[24,16,18];
    // warm highlights, cool-ish shadows for painted depth
    const hi2=mix(c,white,0.55), hi=mix(c,white,0.28), mid=c, sh=mix(c,black,0.30), sh2=mix(c,black,0.55), sh3=mix(c,black,0.74);
    return [hi2,hi,mid,sh,sh2,sh3].map(hex);
  }
  function grid(W,H){return Array.from({length:H},()=>new Array(W).fill(null));}
  function inb(g,x,y){return g[y]&&x>=0&&x<g[0].length;}
  function P(g,x,y,c){x=Math.round(x);y=Math.round(y);if(inb(g,x,y))g[y][x]=c;}
  // light dir upper-left
  const LX=-0.62, LY=-0.66;
  function shadeAt(R,nx,ny,ao){ // nx,ny normal; ao 0..1 ambient occlusion (1=open)
    let v = nx*LX+ny*LY;            // -1..1
    v = v*0.5 + 0.5;               // 0..1
    v = v*0.7 + ao*0.3;            // blend with openness
    const idx = v>0.80?0 : v>0.62?1 : v>0.44?2 : v>0.28?3 : v>0.13?4 : 5;
    return R[idx];
  }
  // rounded capsule with painted shading; r1->r2 taper. squash via yscale handled by caller coords.
  function capsule(g,R,x1,y1,r1,x2,y2,r2){
    const W=g[0].length,Hh=g.length,dx=x2-x1,dy=y2-y1,L2=dx*dx+dy*dy||1;
    const pad=Math.max(r1,r2)+2;
    const minx=Math.max(0,Math.floor(Math.min(x1,x2)-pad)),maxx=Math.min(W-1,Math.ceil(Math.max(x1,x2)+pad));
    const miny=Math.max(0,Math.floor(Math.min(y1,y2)-pad)),maxy=Math.min(Hh-1,Math.ceil(Math.max(y1,y2)+pad));
    for(let y=miny;y<=maxy;y++)for(let x=minx;x<=maxx;x++){
      let s=((x-x1)*dx+(y-y1)*dy)/L2; s=Math.max(0,Math.min(1,s));
      const px=x1+s*dx,py=y1+s*dy,r=r1+(r2-r1)*s,d=Math.hypot(x-px,y-py);
      if(d<=r){ const nx=(x-px)/r, ny=(y-py)/r; const ao=Math.max(0,1-d/r*0.6); g[y][x]=shadeAt(R,nx,ny,ao); }
    }
  }
  function sphere(g,R,cx,cy,rx,ry){
    for(let y=Math.floor(cy-ry-1);y<=Math.ceil(cy+ry+1);y++)for(let x=Math.floor(cx-rx-1);x<=Math.ceil(cx+rx+1);x++){
      if(!inb(g,x,y))continue; const ux=(x-cx)/rx,uy=(y-cy)/ry,q=ux*ux+uy*uy;
      if(q<=1){ const ao=1-q*0.5; g[y][x]=shadeAt(R,ux,uy,ao); }
    }
  }
  // soft box (rounded rect) with bevel: low-radius normal bevel = lighten top+left edge band, darken bottom+right edge band
  function panel(g,R,x,y,w,h,{round=2}={}){
    for(let j=0;j<h;j++)for(let i=0;i<w;i++){
      const xx=x+i,yy=y+j; if(!inb(g,xx,yy))continue;
      // rounded corner cull
      const cxr = (i<round && j<round)?Math.hypot(round-i,round-j):
                  (i>=w-round && j<round)?Math.hypot(i-(w-round-1),round-j):
                  (i<round && j>=h-round)?Math.hypot(round-i,j-(h-round-1)):
                  (i>=w-round && j>=h-round)?Math.hypot(i-(w-round-1),j-(h-round-1)):0;
      if(cxr>round)continue;
      const edgeT=j, edgeL=i, edgeB=h-1-j, edgeR=w-1-i;
      let c=R[2];
      if(edgeT<2||edgeL<2) c=R[1];
      if(edgeT<1||edgeL<1) c=R[0];
      if(edgeB<2||edgeR<2) c=R[3];
      if(edgeB<1||edgeR<1) c=R[4];
      g[yy][xx]=c;
    }
  }
  function bladeShape(g,R,x,yTop,yBot,wTop,wBot){ // double-edged blade, bright center fuller
    const Hh=yBot-yTop;
    for(let j=0;j<=Hh;j++){ const w=Math.round(wTop+(wBot-wTop)*j/Hh); const x0=Math.round(x-w/2);
      for(let i=0;i<w;i++){ const t=w<=1?0:i/(w-1); const c = t<0.3?R[1] : t<0.5?R[0] : t<0.72?R[2] : R[3]; P(g,x0+i,yTop+j,c); }
    }
  }
  // damage helpers
  function gash(g,col,pts,wid=1){ for(let i=1;i<pts.length;i++){ const a=pts[i-1],b=pts[i],n=Math.hypot(b[0]-a[0],b[1]-a[1])|0;
    for(let t=0;t<=n;t++){ const x=a[0]+(b[0]-a[0])*t/n,y=a[1]+(b[1]-a[1])*t/n; for(let o=0;o<wid;o++)P(g,x+o,y,col);} } }
  function clear(g,x,y,w,h){ for(let j=0;j<h;j++)for(let i=0;i<w;i++){ if(inb(g,x+i,y+j))g[y+j][x+i]=null; } }
  // 1px outline (skips already-colored) + optional inner AO darken next to outline
  function outline(g){ const W=g[0].length,H=g.length,o=g.map(r=>r.slice());
    for(let y=0;y<H;y++)for(let x=0;x<W;x++)if(g[y][x]===null){ let nr=false;
      for(let a=-1;a<=1;a++)for(let b=-1;b<=1;b++){ const nx=x+b,ny=y+a; if(g[ny]&&g[ny][nx]&&g[ny][nx]!=='OUT')nr=true; }
      if(nr)o[y][x]='OUT'; }
    return o; }
  function paint(g){ const W=g[0].length,H=g.length,cv=createCanvas(W,H),ctx=cv.getContext('2d');
    for(let y=0;y<H;y++)for(let x=0;x<W;x++){ const c=g[y][x]; if(!c)continue; ctx.fillStyle=c==='OUT'?OUT:c; ctx.fillRect(x,y,1,1);} return cv; }
  function up(c,s){ const o=createCanvas(c.width*s,c.height*s),x=o.getContext('2d'); x.imageSmoothingEnabled=false; x.drawImage(c,0,0,o.width,o.height); return o; }
  return {OUT,ramp,grid,P,capsule,sphere,panel,bladeShape,gash,clear,outline,paint,up,inb};
})();
