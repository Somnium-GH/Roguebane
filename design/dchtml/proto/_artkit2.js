// Roguebane FLAT-BLOCKY artkit v2 — mono-color interiors, CRISP popout bevel edges, hard 1px outline.
// Direction (LOCKED): each shape = one flat interior color + a 1px highlight on top/left edge + a 1px
// shadow on bottom/right edge. NO interior gradients/blending. Arms at sides, simple stance.
// eval(await readFile('Content/_artkit2.js')) → globalThis.AK
globalThis.AK = (function(){
  const OUT='#181019';
  function cv(w,h){const c=createCanvas(w,h),x=c.getContext('2d');x.imageSmoothingEnabled=false;return [c,x];}
  function up(c,s){const [o,x]=cv(c.width*s,c.height*s);x.drawImage(c,0,0,o.width,o.height);return o;}
  // ramp: returns {base,hi,sh} from a hex
  function hx(h){h=h.replace('#','');return [parseInt(h.slice(0,2),16),parseInt(h.slice(2,4),16),parseInt(h.slice(4,6),16)];}
  function hex(r){return '#'+r.map(v=>Math.max(0,Math.min(255,Math.round(v))).toString(16).padStart(2,'0')).join('');}
  function mix(a,b,t){return a.map((v,i)=>v+(b[i]-v)*t);}
  function tone(base){const c=hx(base);return {base, hi:hex(mix(c,[255,250,240],0.34)), sh:hex(mix(c,[20,12,22],0.42))};}
  // a "grid" is just a canvas+ctx; we draw flat blocks then bevel edges then outline.
  // We track an occupancy mask so bevel only lights true silhouette edges, not internal seams.
  function frameBuf(W,H){ const [c,x]=cv(W,H); return {c,x,W,H, fills:[]}; }
  // rect with mono interior; bevel applied later globally so adjacent blocks merge cleanly
  function rect(F,x,y,w,h,base){ F.x.fillStyle=base; F.x.fillRect(x,y,w,h); F.fills.push([x,y,w,h,base]); }
  function disc(F,cx,cy,r,base){ F.x.fillStyle=base; for(let j=-r;j<=r;j++)for(let i=-r;i<=r;i++){ if(i*i+j*j<=r*r) F.x.fillRect(cx+i,cy+j,1,1);} F.fills.push(['disc',cx,cy,r,base]); }
  function px(F,x,y,c){F.x.fillStyle=c;F.x.fillRect(x,y,1,1);}
  // read alpha mask
  function mask(F){ const d=F.x.getImageData(0,0,F.W,F.H).data, m=new Uint8Array(F.W*F.H); for(let i=0;i<F.W*F.H;i++)m[i]=d[i*4+3]>0?1:0; return m; }
  function colorAt(F,x,y){ const d=F.x.getImageData(x,y,1,1).data; return [d[0],d[1],d[2],d[3]]; }
  // BEVEL: for each filled pixel, if the neighbor ABOVE or LEFT is empty(silhouette) -> highlight;
  // if neighbor BELOW or RIGHT is empty -> shadow. Uses per-pixel tone of its own base color.
  function bevelAndOutline(F, toneMap){
    const W=F.W,H=F.H, img=F.x.getImageData(0,0,W,H), a=img.data;
    const on=(i,j)=> i>=0&&j>=0&&i<W&&j<H && a[(j*W+i)*4+3]>0;
    const out=F.x.createImageData(W,H), b=out.data;
    function key(i,j){const k=(j*W+i)*4;return a[k]+','+a[k+1]+','+a[k+2];}
    for(let j=0;j<H;j++)for(let i=0;i<W;i++){
      const k=(j*W+i)*4;
      if(a[k+3]>0){
        const t=toneMap[key(i,j)];
        let col=[a[k],a[k+1],a[k+2]];
        if(t){
          const topL = !on(i,j-1) || !on(i-1,j);
          const botR = !on(i,j+1) || !on(i+1,j);
          if(topL) col=hx(t.hi); else if(botR) col=hx(t.sh);
        }
        b[k]=col[0];b[k+1]=col[1];b[k+2]=col[2];b[k+3]=255;
      } else {
        let nr=false; for(let q=-1;q<=1;q++)for(let p=-1;p<=1;p++) if(on(i+p,j+q)) nr=true;
        if(nr){ const o=hx(OUT); b[k]=o[0];b[k+1]=o[1];b[k+2]=o[2];b[k+3]=255; }
      }
    }
    F.x.putImageData(out,0,0);
    return F.c;
  }
  // convenience: build, draw via cb(F, T) where T(hex)->registers tone, then finish
  function make(W,H,draw){
    const F=frameBuf(W,H); const tones={};
    const reg=(base)=>{ const t=tone(base); const c=hx(base); tones[c[0]+','+c[1]+','+c[2]]=t; return base; };
    draw(F, reg);
    return bevelAndOutline(F, tones);
  }
  return {OUT,cv,up,tone,make,rect,disc,px};
})();
