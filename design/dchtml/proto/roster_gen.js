/* Roguebane roster generator — THE persistent source of truth for character ART and LAYOUT.
 * Follows ART_RULES.md (per-part 1px black outline #15100c, flat fill + 1px bottom/right bevel,
 * crisp nearest-neighbor, square torso, long arms, short stubby legs) AND LAYOUT_CONTRACT.md
 * (emits modular part PNGs per damage state + Content/layout.json with figure rects/pivots/
 * sockets/z, gear pivots, and responsive screen manifests).
 *
 * Assembly RECORDS each part into its own figure-space grid (same paint calls, same order as
 * before) so the flattened composite is byte-identical to the old single-grid render, while the
 * per-part grids give us trimmed part PNGs + exact blit rects.
 *
 * Harness (run_script):
 *   const src = await readFile('proto/roster_gen.js'); eval(src);
 *   const R = generateAll(createCanvas);
 *   for (const f of R.figures) {
 *     await saveFile('proto/roster/'+f.name+'.png', f.flat);
 *     for (const p of f.parts) await saveFile('Content/sprites/body/'+f.name+'/'+p.name+'_'+p.state+'.png', p.canvas);
 *   }
 *   for (const g of R.gear) await saveFile('Content/sprites/gear/'+g.name+'.png', g.canvas);
 *   await saveFile('Content/layout.json', JSON.stringify(R.layout, null, 2));
 * generateRoster(createCanvas) stays as a back-compat wrapper returning [{name,canvas}] (flattened only).
 */
function generateAll(createCanvas) {
  const S = 8;
  const OUT = [21, 16, 12];
  const STATES = ['healthy', 'damaged', 'broken'];
  // ---- palette (sampled from approved figures) ----
  const C = {
    skin: [224,180,134], skinSh: [189,141,94],
    hair: [134,96,58], hairSh: [92,64,34],
    plate: [205,211,222], plateSh: [154,160,172],
    blade: [223,228,236], bladeSh: [170,176,188],
    wood: [176,126,68], woodSh: [126,88,48],
    gold: [230,187,82], goldSh: [176,138,50],
    red: [196,68,56], redSh: [146,48,31],
    dark: [46,40,54], darkSh: [26,22,34],
    bone: [226,218,190], boneSh: [182,172,140],
    cloth: [136,88,46], clothSh: [96,60,30],
    ogre: [134,160,78], ogreSh: [100,124,56],
    troll: [122,138,90], trollSh: [90,106,64],
    stone: [154,160,166], stoneSh: [115,122,130],
    tusk: [226,214,186], tuskSh: [176,164,132],
    eyeRed: [210,59,47],
    adept: [95,120,184], adeptSh: [68,90,144],
    summ: [154,120,176], summSh: [116,88,140],
    wraith: [60,56,88], wraithSh: [39,35,64],
    teal: [127,196,176], tealSh: [88,154,134],
    mail: [150,156,168], mailSh: [108,114,126],
    royal: [66,98,160], royalSh: [44,68,118],
    leather: [107,122,74], leatherSh: [74,86,48],
    hide: [158,118,74], hideSh: [116,84,50],   // barbarian light-medium brown
  };

  // ---- RACE: base-body axis (skin/hair ramp + ears + BUILD). A figure = race base × core-rune kit.
  // Human is the locked baseline; every other race is a permutation. Morph axes (all px deltas vs the
  // locked human build — this is the BODY-MORPH layer, B17 'rip the bandaid off'):
  // `ears`  = px each ear point protrudes past the head edge (0 human, 1 small point, 2 elf).
  // `slim`  = px shaved off torso/shoulder WIDTH, symmetric about mid (NEGATIVE = stouter/wider).
  // `short` = px removed from LEG height (human figs) / added to robe rTop (robe figs) — whole figure
  //           stands shorter, feet stay planted on the ground line.
  // `armD`  = px removed from ARM length (keeps knuckles off the ground on short-leg builds).
  // `headW`/`headH` = px added to head width/height (dwarf +2/−1 wider+shorter; halfling −1/−2 —
  //           subtle, per the 'not awkwardly' rule). Applies to helmeted heads too (helms fit heads).
  const RACE = {
    human:    { id:'human',    skin: C.skin,        skinSh: C.skinSh,       hair: C.hair,      hairSh: C.hairSh,      ears: 0, slim: 0,  short: 0, armD: 0, headW: 0,  headH: 0 },
    elf:      { id:'elf',      skin: [206,200,190],  skinSh: [162,156,146],  hair: [150,128,74], hairSh: [104,84,44],  ears: 2, slim: 2,  short: 0, armD: 0, headW: 0,  headH: 0 },
    // B17 Dwarf — CON affinity: stout heavy build (wider torso), the first true body morph (short
    // legs), head slightly wider + slightly shorter than human; ruddy skin, ginger hair.
    dwarf:    { id:'dwarf',    skin: [214,158,112],  skinSh: [168,118,76],   hair: [158,84,40],  hairSh: [110,58,26],  ears: 0, slim: -2, short: 3, armD: 1, headW: 2,  headH: -1 },
    // B17 Halfling — DEX affinity: small + nimble, elf-like slim build with small ear points and a
    // shorter head — but WARM skin (explicitly not the elf pallor), chestnut hair.
    halfling: { id:'halfling', skin: [226,178,122],  skinSh: [182,136,86],   hair: [118,78,42],  hairSh: [82,54,28],   ears: 1, slim: 2,  short: 2, armD: 1, headW: -1, headH: -2 },
    // B17 Half-Giant — STR affinity: a TALL silhouette (not a re-skinned human) — mainly HEIGHT via the
    // grow axes: much taller torso (torsoH +10) + longer legs (short −8) and arms (armD −6); only
    // SLIGHTLY wider (slim −2) with a slightly wider + taller head (headW/headH +1). Weathered olive-tan
    // giant skin, dark hair.
    half_giant: { id:'half_giant', skin: [198,176,140], skinSh: [156,136,104], hair: [92,66,40], hairSh: [64,44,26], ears: 0, slim: -1, short: -2, armD: -2, headW: 1, headH: 1, torsoH: 3 },
  };

  // ---- low-level grid + painter ----
  function grid(W, H) { return { W, H, px: Array.from({ length: H }, () => new Array(W).fill(null)) }; }
  function setPx(g, x, y, c) { if (x < 0 || y < 0 || x >= g.W || y >= g.H) return; g.px[y][x] = c; }
  function paint(g, mask, base, shadow) {
    const E = (x, y) => mask(x, y) && (!mask(x-1,y) || !mask(x+1,y) || !mask(x,y-1) || !mask(x,y+1));
    for (let y = 0; y < g.H; y++) for (let x = 0; x < g.W; x++) {
      if (!mask(x, y)) continue;
      if (E(x, y)) { setPx(g, x, y, OUT); continue; }
      setPx(g, x, y, (E(x+1, y) || E(x, y+1)) ? shadow : base);
    }
  }
  const rect = (x0,y0,x1,y1) => (x,y) => x>=x0 && x<=x1 && y>=y0 && y<=y1;
  const disc = (cx,cy,r) => (x,y) => (x-cx)*(x-cx)+(y-cy)*(y-cy) <= r*r;

  // ---- gear helpers (paint into a passed grid at given coords) ----
  function sword(g, cx, topY, len) {           // blade points UP
    paint(g, rect(cx-1, topY, cx+1, topY+len-1), C.blade, C.bladeSh);   // blade
    paint(g, rect(cx-3, topY+len, cx+3, topY+len+1), C.gold, C.goldSh); // crossguard
    paint(g, rect(cx-1, topY+len+2, cx+1, topY+len+5), C.wood, C.woodSh);// grip
    paint(g, rect(cx-1, topY+len+6, cx+1, topY+len+7), C.gold, C.goldSh);// pommel
  }
  function daggerDown(g, cx, gripY, len) {     // blade points DOWN, hilt/grip sits in the hand
    paint(g, rect(cx-1, gripY-2, cx+1, gripY-1), C.dark, C.darkSh);        // pommel (3px ctr cx)
    paint(g, rect(cx-1, gripY, cx+1, gripY+2), C.dark, C.darkSh);          // grip/handle (3px ctr cx)
    paint(g, rect(cx-2, gripY+3, cx+2, gripY+4), C.dark, C.darkSh);        // crossguard (5px ctr cx)
    paint(g, rect(cx-1, gripY+5, cx+1, gripY+5+len-1), C.blade, C.bladeSh);// silver blade (3px -> interior shows)
  }
  function roundShield(g, cx, cy, r, base, sh) {
    paint(g, disc(cx, cy, r), base || C.wood, sh || C.woodSh);
    paint(g, disc(cx, cy, Math.max(1, Math.floor(r/3))), C.gold, C.goldSh); // boss
  }
  function towerShield(g, x0, y0, x1, y1, bb, bs) {
    const cx = Math.round((x0+x1)/2), cy = Math.floor((y0+y1)/2);
    paint(g, (x,y) => x>=x0 && x<=x1 && y>=y0+1 && y<=y1-1 || (x>x0&&x<x1&&y>=y0&&y<=y1), C.plate, C.plateSh);
    paint(g, rect(x0+2, cy-1, x1-2, cy), bb||C.gold, bs||C.goldSh);   // horizontal band
    paint(g, rect(cx-1, y0+2, cx, y1-2), bb||C.gold, bs||C.goldSh);   // vertical band -> cross
  }
  function club(g, cx, topY) {
    paint(g, disc(cx, topY+4, 4), C.wood, C.woodSh);                 // knob head
    paint(g, rect(cx-1, topY+7, cx+1, topY+20), C.wood, C.woodSh);   // shaft
  }
  function staff(g, cx, topY, hem, orb) {
    paint(g, rect(cx-1, topY+4, cx+1, hem), C.wood, C.woodSh);       // shaft
    paint(g, disc(cx, topY, 3), orb || C.teal, C.tealSh);            // orb
  }
  function bow(g, cx, topY, len, base, sh) {   // 3-segment stepped recurve silhouette (belly bulges away from string) + taut string
    const seg = Math.floor(len / 3);
    paint(g, rect(cx - 1, topY, cx + 1, topY + seg - 1), base || C.wood, sh || C.woodSh);                  // upper tip (near string)
    paint(g, rect(cx - 4, topY + seg, cx - 2, topY + 2 * seg - 1), base || C.wood, sh || C.woodSh);        // belly (bulges out)
    paint(g, rect(cx - 1, topY + 2 * seg, cx + 1, topY + len - 1), base || C.wood, sh || C.woodSh);        // lower tip (near string)
    paint(g, rect(cx + 2, topY, cx + 2, topY + len - 1), C.dark, C.darkSh);                                 // taut bowstring
  }

  // ============ B2-GO: MATERIAL TIER LADDERS (DESIGN_SPEC §6c/§6d) ============
  // Same silhouette per weapon TYPE / armor SLOT across all 4 tiers — palette swap ONLY, per
  // ART_RULES + §6c ("STR's per-tier bonus is part-damage mitigation, NOT reshaped art").
  const METAL = {
    iron:    { base: [132,134,140], sh: [92,94,100],   label: 'Iron' },
    steel:   { base: C.blade,       sh: C.bladeSh,      label: 'Steel' },
    mithral: { base: [214,226,238], sh: [166,182,200], label: 'Mithral' },
    dwarven: { base: [88,100,136],  sh: [56,66,96],     label: 'Dwarven Steel' },
  };
  const METAL_ORDER = ['iron', 'steel', 'mithral', 'dwarven'];
  // DEX leather ladder (Head/Chest/Arms/Legs) — deepening browns; the shadow tone reads as stud rivets.
  const LEATHER = {
    plain:      { base: [150,110,64], sh: [108,78,44], label: 'Leather' },
    hardened:   { base: [130,92,52],  sh: [92,64,34],  label: 'Hardened' },
    studded:    { base: [112,78,44],  sh: [78,52,26],  label: 'Studded' },
    reinforced: { base: [96,66,36],   sh: [64,42,20],  label: 'Reinforced' },
  };
  const LEATHER_ORDER = ['plain', 'hardened', 'studded', 'reinforced'];
  // INT robe ladder (Chest+Head only) — cloth deepening toward a faint arcane-blue top tier.
  const ROBE = {
    cotton:  { base: [196,188,168], sh: [150,142,124], label: 'Cotton' },
    silk:    { base: [150,140,196], sh: [110,102,150], label: 'Silk' },
    ornate:  { base: [124,108,188], sh: [88,76,140],   label: 'Ornate' },
    humming: { base: [108,150,214], sh: [72,108,168],  label: 'Humming', glow: true },
  };
  const ROBE_ORDER = ['cotton', 'silk', 'ornate', 'humming'];
  // Magic-item ladders (Sling/Staff/Charm/Tome/Wand) — each family's OWN four adjectives; mundane
  // tiers stay flat-material, ONLY the top tier gets a faint glow tint (the "tier-4 signature" rule:
  // magic gear's top adjective is supernatural — Humming/Glowing — mundane gear's is not).
  const SLING_TIER = {   // Shepherd's -> Braided -> Sinew -> Giantsbane (leather/cord, no glow — mundane)
    shepherds:  { base: [150,110,64], sh: [108,78,44], label: "Shepherd's" },
    braided:    { base: [124,92,54],  sh: [88,64,36],  label: 'Braided' },
    sinew:      { base: [196,188,168],sh: [150,142,124],label: 'Sinew' },
    giantsbane: { base: [96,66,36],   sh: [64,42,20],  label: "Giantsbane" },
  };
  const SLING_ORDER = ['shepherds', 'braided', 'sinew', 'giantsbane'];
  const STAFF_TIER = {   // Wooden -> Twisted -> Ornate -> Humming (magic — top tier glows)
    wooden:  { base: C.wood,       sh: C.woodSh,       label: 'Wooden', orb: C.teal },
    twisted: { base: [126,92,56],  sh: [90,64,36],     label: 'Twisted', orb: C.teal },
    ornate:  { base: C.gold,       sh: C.goldSh,       label: 'Ornate', orb: [150,196,214] },
    humming: { base: [108,150,214],sh: [72,108,168],   label: 'Humming', orb: [190,230,255], glow: true },
  };
  const STAFF_ORDER = ['wooden', 'twisted', 'ornate', 'humming'];
  const CHARM_TIER = {   // Wooden -> Bone -> Ornate -> Humming (magic — top tier glows)
    wooden:  { base: C.wood, sh: C.woodSh, label: 'Wooden' },
    bone:    { base: C.bone, sh: C.boneSh, label: 'Bone' },
    ornate:  { base: C.gold, sh: C.goldSh, label: 'Ornate' },
    humming: { base: [108,150,214], sh: [72,108,168], label: 'Humming', glow: true },
  };
  const CHARM_ORDER = ['wooden', 'bone', 'ornate', 'humming'];
  const TOME_TIER = {   // Old Worn -> Leather -> Ornate -> Glowing (magic — top tier glows)
    oldworn: { base: [150,110,64], sh: [108,78,44], label: 'Old Worn' },
    leather: { base: [112,78,44],  sh: [78,52,26],  label: 'Leather' },
    ornate:  { base: C.gold,       sh: C.goldSh,     label: 'Ornate' },
    glowing: { base: [108,150,214],sh: [72,108,168], label: 'Glowing', glow: true },
  };
  const TOME_ORDER = ['oldworn', 'leather', 'ornate', 'glowing'];
  const WAND_TIER = {   // Adept -> Twisted -> Gemstone -> Glowing (unchanged existing ladder, magic)
    adept:    { base: C.wood,        sh: C.woodSh,        label: 'Adept', orb: C.teal },
    twisted:  { base: [126,92,56],   sh: [90,64,36],      label: 'Twisted', orb: C.teal },
    gemstone: { base: C.summ,        sh: C.summSh,        label: 'Gemstone', orb: C.gold },
    glowing:  { base: [108,150,214], sh: [72,108,168],    label: 'Glowing', orb: [190,230,255], glow: true },
  };
  const WAND_ORDER = ['adept', 'twisted', 'gemstone', 'glowing'];
  // B11: bow ladder (§6d names locked: Short Bow → Long Bow → Compound Bow → Elven Bow). Ids are
  // bow_short/bow_long/bow_compound/bow_elven per the engine's ask — tier adjective, not material.
  // Bows are MUNDANE (tier-4 signature rule: no supernatural adjective, so no glow tier).
  const BOW_TIER = {
    short:    { len: 20, base: C.wood,       sh: C.woodSh,       label: 'Short Bow' },
    long:     { len: 30, base: C.wood,       sh: C.woodSh,       label: 'Long Bow' },
    compound: { len: 26, base: C.dark,       sh: C.darkSh,       label: 'Compound Bow', cams: true },
    elven:    { len: 28, base: C.bone,       sh: C.boneSh,       label: 'Elven Bow', goldGrip: true },
  };
  const BOW_ORDER = ['short', 'long', 'compound', 'elven'];
  function bowTierSil(g, cx, topY, pal) {
    bow(g, cx, topY, pal.len, pal.base, pal.sh);
    if (pal.cams) { paint(g, disc(cx, topY + 1, 2), C.mail, C.mailSh); paint(g, disc(cx, topY + pal.len - 2, 2), C.mail, C.mailSh); } // pulley cams
    if (pal.goldGrip) paint(g, rect(cx - 4, topY + Math.floor(pal.len/2) - 1, cx - 2, topY + Math.floor(pal.len/2) + 1), C.gold, C.goldSh); // wrapped grip on the belly
  }
  // B2-GO(2) residual: ranged BACK-MOUNT layer art (bow/sling slung diagonally across the back).
  // Mounted by aligning the sprite pivot (its centre) to figure sockets.back; drawn BEHIND all parts.
  function bowBackSil(g, pal) {
    const L = Math.min(24, pal.len + 2), x0 = 4, y0 = 4;
    paint(g, (px, py) => { const t = px - x0, d = (py - y0) - t; return t >= 0 && t <= L && d >= 0 && d <= 2; }, pal.base, pal.sh); // slanted stave (45°, 3px so the tier palette reads)
    paint(g, (px, py) => { const t = px - x0; return t >= 4 && t <= L - 4 && (py - y0) === t - 4; }, C.dark, C.darkSh);            // string chord
    if (pal.cams) { paint(g, disc(x0 + 1, y0 + 2, 2), C.mail, C.mailSh); paint(g, disc(x0 + L - 1, y0 + L, 2), C.mail, C.mailSh); }
  }
  function slingBackSil(g, pal) {
    const L = 12, x0 = 5, y0 = 5;
    paint(g, (px, py) => { const t = px - x0, d = (py - y0) - t; return t >= 0 && t <= L && d >= 0 && d <= 2; }, pal.base, pal.sh); // coiled cord band (3px so the tier palette reads)
    paint(g, disc(x0 + Math.floor(L/2), y0 + Math.floor(L/2) + 1, 3), pal.base, pal.sh);                                           // pouch at the crossing
  }

  // ---- weapon TYPE silhouettes (one per type; length/width vary, palette is the only tier axis) ----
  function bladeSil(g, cx, topY, len, halfW, guardHalfW, gripLen, base, sh, twoHand) {
    paint(g, rect(cx - halfW, topY, cx + halfW, topY + len - 1), base, sh);                        // blade
    paint(g, rect(cx - guardHalfW, topY + len, cx + guardHalfW, topY + len + 1), C.gold, C.goldSh); // crossguard
    paint(g, rect(cx - 1, topY + len + 2, cx + 1, topY + len + 1 + gripLen), C.wood, C.woodSh);      // grip
    const pw = twoHand ? 2 : 1;
    paint(g, rect(cx - pw, topY + len + 2 + gripLen, cx + pw, topY + len + 3 + gripLen), C.gold, C.goldSh); // pommel
  }
  function axeSil(g, cx, headTopY, haftLen, headSize, base, sh, twoHand) {
    const headBotY = headTopY + headSize - 1, hw = twoHand ? 2 : 1;
    paint(g, rect(cx - hw, headBotY + 1, cx + hw, headBotY + haftLen), C.wood, C.woodSh);           // haft
    paint(g, (px, py) => py >= headTopY && py <= headBotY && px > cx && px <= cx + headSize - (py - headTopY), base, sh); // blade wedge (right)
    if (twoHand) paint(g, (px, py) => py >= headTopY && py <= headBotY && px < cx && px >= cx - headSize + (py - headTopY), base, sh); // double-bit
  }
  function maceSil(g, cx, headCy, haftLen, headR, base, sh, twoHand) {
    paint(g, disc(cx, headCy, headR), base, sh);                                                    // flanged head
    const hw = twoHand ? 2 : 1;
    paint(g, rect(cx - hw, headCy + headR, cx + hw, headCy + headR + haftLen - 1), C.wood, C.woodSh); // haft
    if (twoHand) { paint(g, rect(cx - headR - 2, headCy - 1, cx - headR - 1, headCy + 1), base, sh); paint(g, rect(cx + headR + 1, headCy - 1, cx + headR + 2, headCy + 1), base, sh); } // flanges
  }
  const WEAPON_TYPES = {
    longsword:  { paint: (g,cx,topY,pal) => bladeSil(g,cx,topY,16,1,3,4,pal.base,pal.sh,false), hand:'STR', slot:'1H' },
    axe:        { paint: (g,cx,topY,pal) => axeSil(g,cx,topY,18,7,pal.base,pal.sh,false),        hand:'STR', slot:'1H' },
    mace:       { paint: (g,cx,topY,pal) => maceSil(g,cx,topY,16,4,pal.base,pal.sh,false),        hand:'STR', slot:'1H' },
    claymore:   { paint: (g,cx,topY,pal) => bladeSil(g,cx,topY,23,2,4,6,pal.base,pal.sh,true),    hand:'STR', slot:'2H' },
    battleaxe:  { paint: (g,cx,topY,pal) => axeSil(g,cx,topY,22,9,pal.base,pal.sh,true),          hand:'STR', slot:'2H' },
    warhammer:  { paint: (g,cx,topY,pal) => maceSil(g,cx,topY,20,5,pal.base,pal.sh,true),         hand:'STR', slot:'2H' },
    dagger:     { paint: (g,cx,topY,pal) => bladeSil(g,cx,topY,9, 1,2,3,pal.base,pal.sh,false),   hand:'DEX', slot:'1H' },
    rapier:     { paint: (g,cx,topY,pal) => bladeSil(g,cx,topY,20,1,1,4,pal.base,pal.sh,false),   hand:'DEX', slot:'1H' },
    shortsword: { paint: (g,cx,topY,pal) => bladeSil(g,cx,topY,12,1,2,3,pal.base,pal.sh,false),   hand:'DEX', slot:'1H' },
  };
  const WEAPON_NAME = { longsword:'Longsword', axe:'Axe', mace:'Mace', claymore:'Claymore', battleaxe:'Battleaxe',
    warhammer:'Warhammer', dagger:'Dagger', rapier:'Rapier', shortsword:'Short Sword' };

  // ---- new B2-GO item silhouettes (Sling/Charm/Tome/Wand — Staff already exists via staff()) ----
  function slingSil(g, cx, topY, pal) {
    paint(g, rect(cx - 4, topY, cx + 4, topY + 3), pal.base, pal.sh);         // pouch
    paint(g, rect(cx - 1, topY + 4, cx + 1, topY + 16), pal.base, pal.sh);    // gathered cords
  }
  function charmSil(g, cx, topY, pal) {
    paint(g, disc(cx, topY + 3, 3), pal.base, pal.sh);
    paint(g, rect(cx - 1, topY + 6, cx + 1, topY + 11), C.gold, C.goldSh);
    if (pal.glow) setPx(g, cx, topY + 2, [200,230,255]);
  }
  function tomeSil(g, cx, topY, pal) {
    paint(g, rect(cx - 4, topY, cx + 4, topY + 11), pal.base, pal.sh);
    paint(g, rect(cx - 4, topY + 5, cx + 4, topY + 6), C.gold, C.goldSh);
    if (pal.glow) setPx(g, cx, topY + 3, [200,230,255]);
  }
  function wandSil(g, cx, topY, len, pal) {
    paint(g, rect(cx - 1, topY + 4, cx + 1, topY + len - 1), C.wood, C.woodSh);
    paint(g, disc(cx, topY + 2, 2), pal.base, pal.sh);
  }

  // ---- armor SLOT silhouettes (inventory-icon scale; STR heavy / DEX leather share these shapes) ----
  function helmSil(g, cx, topY, pal) {
    paint(g, (px, py) => { const dy = py - topY; return dy >= 0 && dy <= 8 && Math.abs(px - cx) <= 6 - Math.max(0, dy - 5); }, pal.base, pal.sh);
    paint(g, rect(cx - 6, topY + 8, cx + 6, topY + 9), pal.base, pal.sh);
  }
  function breastplateSil(g, cx, topY, pal) {
    paint(g, rect(cx - 7, topY, cx + 7, topY + 13), pal.base, pal.sh);
    paint(g, rect(cx - 1, topY + 2, cx + 1, topY + 11), pal.sh, pal.sh);
  }
  function vambraceSil(g, cx, topY, pal) {
    paint(g, rect(cx - 3, topY, cx + 3, topY + 13), pal.base, pal.sh);
    paint(g, rect(cx - 3, topY + 4, cx + 3, topY + 4), pal.sh, pal.sh);
    paint(g, rect(cx - 3, topY + 9, cx + 3, topY + 9), pal.sh, pal.sh);
  }
  function greaveSil(g, cx, topY, pal) {
    paint(g, rect(cx - 4, topY, cx + 4, topY + 15), pal.base, pal.sh);
    paint(g, rect(cx - 4, topY + 5, cx + 4, topY + 5), pal.sh, pal.sh);
    paint(g, rect(cx - 4, topY + 11, cx + 4, topY + 11), pal.sh, pal.sh);
  }
  function hoodSil(g, cx, topY, pal) {
    paint(g, (px, py) => { const dy = py - topY; return dy >= 0 && dy <= 9 && Math.abs(px - cx) <= 6 - Math.max(0, dy - 4); }, pal.base, pal.sh);
    paint(g, rect(cx - 2, topY + 9, cx + 2, topY + 11), pal.base, pal.sh);
  }
  function robeChestSil(g, cx, topY, pal) {
    const topHalf = 6, botHalf = 9, h = 16;
    paint(g, (px, py) => { const t = (py - topY) / h; const half = topHalf + (botHalf - topHalf) * t; return py >= topY && py < topY + h && Math.abs(px - cx) <= half; }, pal.base, pal.sh);
  }
  const ARMOR_SLOTS = {
    // STR heavy plate — new B2-GO names (old Skull Cap/Barbute/etc. retired, see DEV_LOOP_MEMORY).
    // STR catalog names COMPOSE material + piece ("Steel Helm") — that composition IS the locked
    // convention (attribute-model.js samples match). DEX/INT instead carry the §6c canon ladder
    // per tier in `names` (payload B10: composing label+noun made "Leather Leather Cap" etc.).
    str_head:  { paint: helmSil,         name: 'Helm' },
    str_chest: { paint: breastplateSil,  name: 'Breastplate' },
    str_arms:  { paint: vambraceSil,     name: 'Vambraces' },
    str_legs:  { paint: greaveSil,       name: 'Greaves' },
    // DEX leather — same 4 slots, same shapes, leather palette; §6c canon names per tier
    dex_head:  { paint: helmSil,         names: ['Leather Cap', 'Hardened Cap', 'Studded Cap', 'Reinforced Hood'] },
    dex_chest: { paint: breastplateSil,  names: ['Padded Armor', 'Leather Armor', 'Studded Leather', 'Reinforced Leather'] },
    dex_arms:  { paint: vambraceSil,     names: ['Leather Bracers', 'Hardened Bracers', 'Studded Bracers', 'Reinforced Bracers'] },
    dex_legs:  { paint: greaveSil,       names: ['Leather Leggings', 'Hardened Leggings', 'Studded Leggings', 'Reinforced Leggings'] },
    // INT robe — chest + head ONLY (no arm/leg robe pieces exist, §6c); canon names per tier
    int_chest: { paint: robeChestSil,    names: ['Cotton Robe', 'Silk Robe', 'Ornate Robe', 'Humming Robe'] },
    int_head:  { paint: hoodSil,         names: ['Cloth Cap', 'Silk Hood', 'Ornate Circlet', 'Humming Circlet'] },
  };

  // ---- humanoid assembler: paints each part into its OWN grid (z-order = push order) ----
  function human(o) {
    const W = o.W, H = o.H, mid = Math.floor(W / 2);
    const race = o.race || RACE.human;
    const parts = [];
    const layer = (name) => { const g = grid(W, H); parts.push({ name, g }); return g; };
    const t = o.torso, tw = Math.max(6, t.w - (race.slim || 0)), tL = mid - Math.floor(tw/2), tR = tL + tw - 1, tTop = t.top, tBot = tTop + (t.h + (race.torsoH || 0)) - 1;
    // back (e.g. wings) — behind everything
    if (o.back) { const g = layer('back'); o.back(g, { mid, tL, tR, tTop, tBot, paint, rect, disc, setPx, C }); }
    // legs + boots — race morph: `short` shortens legs, `armD` shortens arms (B17 body morph)
    const a = o.arm, aTop = tTop + (a.drop || 0), aBot = aTop + (a.h - (race.armD || 0)) - 1;
    if (o.legs) {
      const lg = o.legs, half = Math.floor(lg.gap/2);
      const lLx1 = mid - half - 1, lLx0 = lLx1 - lg.w + 1;
      const rRx0 = mid + (lg.gap - half), rRx1 = rRx0 + lg.w - 1;
      const lTop = tBot + 1, lBot = lTop + (lg.h - (race.short || 0)) - 1;
      paint(layer('legL'), rect(lLx0, lTop, lLx1, lBot), lg.base, lg.sh);
      paint(layer('legR'), rect(rRx0, lTop, rRx1, lBot), lg.base, lg.sh);
      if (o.boots) {
        const bt = o.boots, bTop = lBot + 1, bBot = bTop + bt.h - 1, gb = layer('boots');
        paint(gb, rect(lLx0-1, bTop, lLx1, bBot), bt.base, bt.sh);
        paint(gb, rect(rRx0, bTop, rRx1+1, bBot), bt.base, bt.sh);
      }
    }
    // torso (+chest emblem)
    const gt = layer('torso');
    paint(gt, rect(tL, tTop, tR, tBot), t.base, t.sh);
    if (o.chest) o.chest(gt, { mid, tL, tR, tTop, tBot, paint, rect, disc, C });
    // arms — inner border shares torso outline col, top flush, in front
    paint(layer('armL'), rect(tL - a.w - 1, aTop, tL, aBot), a.base, a.sh);
    paint(layer('armR'), rect(tR, aTop, tR + a.w + 1, aBot), a.base, a.sh);
    const handLx = tL - Math.round((a.w+1)/2), handRx = tR + Math.round((a.w+1)/2), handY = aBot - 1;
    // head (sunk into torso top by `sink`) + head gear + eyes — race morph: headW/headH resize the
    // head box (helmeted heads too — helms fit heads); eyes/ears re-derive from the morphed box
    const h = o.head, hw = h.w + (race.headW || 0), hh = h.h + (race.headH || 0);
    const hL = mid - Math.floor(hw/2), hR = hL + hw - 1;
    const hBot = tTop + (h.sink != null ? h.sink : 1), hTop = hBot - hh + 1;
    const gh = layer('head');
    // race recolours flesh heads (base===C.skin) and adds pointed ears
    const skinHead = (h.base === C.skin);
    const hBase = skinHead ? race.skin : h.base, hSh = skinHead ? race.skinSh : h.sh;
    const ears = skinHead ? (race.ears || 0) : 0, earY = hTop + Math.floor(hh * 0.45);
    const headMask = (px,py) => {
      if (px>=hL && px<=hR && py>=hTop && py<=hBot) return true;
      if (ears) for (const s of [-1,1]) { const ex = s<0 ? hL : hR;
        if (px===ex+s   && py>=earY-1 && py<=earY+1) return true;
        if (px===ex+2*s && py>=earY-1 && py<=earY)   return true;
        if (ears>1 && px===ex+3*s && py===earY-1)     return true; }
      return false;
    };
    paint(gh, headMask, hBase, hSh);
    if (h.gear) h.gear(gh, { mid, hL, hR, hTop, hBot, paint, rect, disc, setPx, C });
    if (h.eye !== false) {
      const eY = h.eyeY != null ? h.eyeY : hTop + Math.floor(hh*0.5);
      const eC = h.eye || OUT;
      setPx(gh, mid - 3, eY, eC); setPx(gh, mid + 3, eY, eC);
    }
    // front gear (weapons/shields in hands) — baked layer keeps the flattened figure identical
    if (o.front) { const gf = layer('frontGear'); o.front(gf, { mid, tL, tR, tTop, tBot, aBot, handLx, handRx, handY, paint, rect, disc, sword, daggerDown, roundShield, towerShield, club, bow, C }); }
    const sockets = {
      handL: [handLx, handY], handR: [handRx, handY],
      neck: [mid, hBot], shoulderL: [tL - Math.floor((a.w+1)/2), aTop], shoulderR: [tR + Math.floor((a.w+1)/2), aTop],
      // B2-GO ranged BACK-MOUNT (§6d/§17 #22): a point behind the torso, over the shoulder blades,
      // so the engine can render an equipped bow/sling here when melee hands are already full —
      // a socket, not baked art, same morph-layer idea as hand-mounted gear.
      back: [mid, tTop + 2],
    };
    // geometry record for the WORN armor painters (same rect math as the base parts above —
    // worn layers repaint the EXACT base silhouettes so part rects in layout.json are unchanged)
    const legsGeom = o.legs ? (() => { const lg = o.legs, half = Math.floor(lg.gap/2);
      const lLx1 = mid - half - 1, lLx0 = lLx1 - lg.w + 1;
      const rRx0 = mid + (lg.gap - half), rRx1 = rRx0 + lg.w - 1;
      return { lLx0, lLx1, rRx0, rRx1, top: tBot + 1, bot: tBot + (lg.h - (race.short || 0)) }; })() : null;
    const geom = { kind: 'human', mid, tL, tR, tTop, tBot,
      arm: { w: a.w, top: aTop, bot: aBot },
      legs: legsGeom,
      head: { hL, hR, hTop, hBot, mask: headMask, eye: h.eye,
              eyeY: (h.eyeY != null ? h.eyeY : hTop + Math.floor(hh*0.5)) } };
    return { W, H, mid, parts, sockets, geom };
  }

  // ---- robe assembler (robe replaces torso+legs; arms stay, IN FRONT) ----
  function robe(o) {
    const W = o.W, H = o.H, mid = Math.floor(W / 2);
    const race = o.race || RACE.human;
    const parts = [];
    const layer = (name) => { const g = grid(W, H); parts.push({ name, g }); return g; };
    // race morph: `short` raises the robe collar (hem stays planted) so the whole figure stands shorter.
    // Only POSITIVE short raises it (small races); negative short (giants) would push the head off the
    // top of the fixed robe canvas, so it's clamped — half-giant casters grow via width + arm length.
    const rTop = o.rTop + Math.max(0, race.short || 0), rHem = o.rHem, topHalf = Math.floor((o.shoulderW - (race.slim||0))/2), botHalf = Math.floor((o.hemW - (race.slim||0))/2);
    const lX = y => Math.round((mid-topHalf) + ((mid-botHalf)-(mid-topHalf)) * (y-rTop)/(rHem-rTop));
    const rX = y => Math.round((mid+topHalf) + ((mid+botHalf)-(mid+topHalf)) * (y-rTop)/(rHem-rTop));
    // hemTatter: N -> jagged torn hem (deterministic broad tears, depth 0..N px cut up from rHem)
    const tat = o.hemTatter || 0;
    const notch = (x) => tat ? [0, 0, 1, tat - 1, tat, tat - 1, 1, 0][((x % 8) + 8) % 8] : 0;
    paint(layer('torso'), (x,y) => y>=rTop && y<=rHem - notch(x) && x>=lX(y) && x<=rX(y), o.base, o.sh);
    const sL = mid - topHalf, sR = mid + topHalf;
    // arms IN FRONT, natural height, inner border touches the robe shoulder
    const aw = o.armW, aTop = rTop, aBot = rTop + (o.armH - (race.armD || 0)) - 1;
    paint(layer('armL'), rect(sL - aw - 1, aTop, sL, aBot), o.base, o.sh);
    paint(layer('armR'), rect(sR, aTop, sR + aw + 1, aBot), o.base, o.sh);
    // head/hood — same race headW/headH morph as human()
    const h = o.head, hw = h.w + (race.headW || 0), hh = h.h + (race.headH || 0);
    const hL = mid - Math.floor(hw/2), hR = hL + hw - 1;
    const hBot = rTop + 2, hTop = hBot - hh + 1, gh = layer('head');
    const skinHead = (h.base === C.skin);
    const hBase = skinHead ? race.skin : h.base, hSh = skinHead ? race.skinSh : h.sh;
    const ears = skinHead ? (race.ears || 0) : 0, earY = hTop + Math.floor(hh * 0.45);
    const headMask = (px,py) => {
      if (px>=hL && px<=hR && py>=hTop && py<=hBot) return true;
      if (ears) for (const s of [-1,1]) { const ex = s<0 ? hL : hR;
        if (px===ex+s   && py>=earY-1 && py<=earY+1) return true;
        if (px===ex+2*s && py>=earY-1 && py<=earY)   return true;
        if (ears>1 && px===ex+3*s && py===earY-1)     return true; }
      return false;
    };
    paint(gh, headMask, hBase, hSh);
    if (h.gear) h.gear(gh, { mid, hL, hR, hTop, hBot, paint, rect, disc, setPx, C });
    const eY = hTop + Math.floor(hh*0.5);
    setPx(gh, mid-3, eY, h.eye||OUT); setPx(gh, mid+3, eY, h.eye||OUT);
    // front gear (staff)
    if (o.front) { const gf = layer('frontGear'); o.front(gf, { mid, sL, sR, rTop, paint, rect, disc, staff, C }); }
    const handY = aBot - 1;
    const sockets = {
      handL: [sL - Math.floor((aw+1)/2), handY], handR: [sR + Math.floor((aw+1)/2), handY],
      neck: [mid, hBot], shoulderL: [sL - Math.floor((aw+1)/2), aTop], shoulderR: [sR + Math.floor((aw+1)/2), aTop],
      back: [mid, rTop + 1],
    };
    const torsoMask = (x,y) => y>=rTop && y<=rHem - notch(x) && x>=lX(y) && x<=rX(y);
    const geom = { kind: 'robe', mid, rTop, rHem, sL, sR, torsoMask,
      arm: { w: aw, top: aTop, bot: aBot },
      head: { hL, hR, hTop, hBot, mask: headMask, eye: h.eye, eyeY: eY } };
    return { W, H, mid, parts, sockets, geom };
  }

  // ---- recolor a part grid (armor material -> bare flesh) keeping its outline ----
  function recolor(g, fb, fs, tb, ts) {
    const eq = (a,b) => a && b && a[0]===b[0] && a[1]===b[1] && a[2]===b[2];
    const o = grid(g.W, g.H);
    for (let y=0;y<g.H;y++) for (let x=0;x<g.W;x++) { const c=g.px[y][x]; if(!c) continue;
      o.px[y][x] = eq(c,fb) ? tb : (eq(c,fs) ? ts : c); }
    return o;
  }

  // ---- damage transform (conservative, deterministic; operates on a pre-scale part grid) ----
  function dim(c, k) { return c === OUT ? OUT : [Math.round(c[0]*k), Math.round(c[1]*k), Math.round(c[2]*k)]; }
  function bbox(g) {
    let minx=g.W,miny=g.H,maxx=-1,maxy=-1;
    for (let y=0;y<g.H;y++) for (let x=0;x<g.W;x++) if (g.px[y][x]) { if(x<minx)minx=x; if(x>maxx)maxx=x; if(y<miny)miny=y; if(y>maxy)maxy=y; }
    return { minx, miny, maxx, maxy, empty: maxx < 0 };
  }
  function damage(g, state) {
    if (state === 'healthy') return g;
    const k = state === 'damaged' ? 0.9 : 0.78;
    const out = grid(g.W, g.H);
    for (let y=0;y<g.H;y++) for (let x=0;x<g.W;x++) { const c=g.px[y][x]; if (c) out.px[y][x] = dim(c, k); }
    const b = bbox(out); if (b.empty) return out;
    // hairline crack: a short vertical run of outline pixels near center
    const cx = Math.floor((b.minx+b.maxx)/2), cyTop = b.miny + Math.floor((b.maxy-b.miny)*0.35);
    const crackLen = state === 'damaged' ? 2 : Math.max(3, Math.floor((b.maxy-b.miny)*0.5));
    for (let i=0;i<crackLen;i++) { const yy=cyTop+i, xx=cx + (i%2 ? 1 : 0); if (out.px[yy] && out.px[yy][xx]) out.px[yy][xx] = OUT; }
    if (state === 'broken') {
      // chip the bottom-right corner: null a small block, then it re-outlines on render
      for (let y=b.maxy; y>b.maxy-2 && y>=0; y--) for (let x=b.maxx; x>b.maxx-2 && x>=0; x--) if (out.px[y]) out.px[y][x] = null;
      // second crack branch
      for (let i=0;i<crackLen-1;i++) { const yy=cyTop+i, xx=cx-1-(i%2); if (out.px[yy] && out.px[yy][xx]) out.px[yy][xx] = OUT; }
    }
    return out;
  }

  // ---- render a grid region to a scaled canvas (no margin: exact part blit) ----
  function renderRegion(g, minx, miny, maxx, maxy) {
    const W = maxx-minx+1, H = maxy-miny+1;
    const cv = createCanvas(W*S, H*S), ctx = cv.getContext('2d'); ctx.imageSmoothingEnabled = false;
    for (let y=miny;y<=maxy;y++) for (let x=minx;x<=maxx;x++) { const c=g.px[y][x]; if(!c) continue;
      ctx.fillStyle='rgb('+c[0]+','+c[1]+','+c[2]+')'; ctx.fillRect((x-minx)*S,(y-miny)*S,S,S); }
    return cv;
  }
  // composite parts (healthy) into one grid in push order
  function flatten(fig) {
    const g = grid(fig.W, fig.H);
    for (const p of fig.parts) for (let y=0;y<fig.H;y++) for (let x=0;x<fig.W;x++) { const c=p.g.px[y][x]; if (c) g.px[y][x]=c; }
    return g;
  }
  // single-sprite finisher: trim to opaque bbox + 1px margin, render at scale S
  function finishSprite(g) {
    const b = bbox(g); if (b.empty) return renderRegion(g, 0, 0, 0, 0);
    return renderRegion(g, Math.max(0,b.minx-1), Math.max(0,b.miny-1), Math.min(g.W-1,b.maxx+1), Math.min(g.H-1,b.maxy+1));
  }

  // ============ FIGURE SPECS (art unchanged) ============
  const F = {};
  F.grunt = (race) => human({
    race,
    W: 56, H: 80,
    torso: { w: 20, h: 20, top: 30, base: C.mail, sh: C.mailSh },
    head:  { w: 13, h: 13, base: C.skin, sh: C.skinSh, gear: (g, x) =>
              paint(g, (px,py) => px>=x.hL && px<=x.hR && py>=x.hTop && py<=x.hBot && (py<=x.hTop+2 || px<=x.hL+1 || px>=x.hR-1), C.mail, C.mailSh) },
    arm:   { w: 4, h: 14, base: C.mail, sh: C.mailSh },
    legs:  { w: 5, h: 9, gap: 3, base: C.mail, sh: C.mailSh },
    boots: { w: 6, h: 3, base: C.wood, sh: C.woodSh },
    chest: (g, x) => paint(g, rect(x.mid-1, x.tTop+1, x.mid+1, x.tBot-1), C.red, C.redSh),
    front: (g, x) => { x.sword(g, x.handLx, x.handY-19, 16); x.roundShield(g, x.tR+6, x.handY, 7); },
  });
  F.warden = (race) => human({
    race,
    W: 60, H: 80,
    torso: { w: 22, h: 21, top: 30, base: C.plate, sh: C.plateSh },
    head:  { w: 14, h: 14, base: C.plate, sh: C.plateSh, eye: false,
              gear: (g, x) => { paint(g, rect(x.hL+2, x.hTop+6, x.hR-2, x.hTop+6), C.dark, C.darkSh);
                                paint(g, rect(x.mid, x.hTop+3, x.mid, x.hBot-2), C.dark, C.darkSh); } },
    arm:   { w: 5, h: 15, base: C.plate, sh: C.plateSh },
    legs:  { w: 6, h: 9, gap: 3, base: C.plate, sh: C.plateSh },
    boots: { w: 7, h: 3, base: C.plateSh, sh: C.plateSh },
    chest: (g, x) => { paint(g, (px,py) =>
        (px>=x.mid-1 && px<=x.mid+1 && py>=x.tTop+2 && py<=x.tBot-2) ||
        (px>=x.mid-5 && px<=x.mid+5 && py>=x.tTop+7 && py<=x.tTop+9), C.royal, C.royalSh); },
    front: (g, x) => x.towerShield(g, x.tR+3, x.tTop-2, x.tR+12, x.tTop+20, C.royal, C.royalSh),
  });
  F.reaver = (race) => human({
    race,
    W: 56, H: 80,
    torso: { w: 19, h: 19, top: 30, base: C.red, sh: C.redSh },
    head:  { w: 13, h: 13, base: C.skin, sh: C.skinSh, gear: (g, x) =>
              paint(g, (px,py) => px>=x.hL && px<=x.hR && py>=x.hTop && py<=x.hBot && (py<=x.hTop+3 || px<=x.hL+1 || px>=x.hR-1), C.red, C.redSh) },
    arm:   { w: 4, h: 13, base: C.red, sh: C.redSh },
    legs:  { w: 5, h: 10, gap: 3, base: C.dark, sh: C.darkSh },
    boots: { w: 6, h: 3, base: C.darkSh, sh: C.darkSh },
    front: (g, x) => { x.daggerDown(g, x.handLx, x.handY-1, 9); x.daggerDown(g, x.handRx-1, x.handY-1, 9); },
  });
  // B15/B18 Barbarian — THE WARLORD: a two-handed-claymore STR core. Bare-headed (hair, no helm) so
  // the race morph reads strongly (great for the half-giant); hide torso + fur collar, bracered arms.
  F.barbarian = (race) => human({
    race,
    W: 60, H: 84,
    torso: { w: 21, h: 20, top: 30, base: C.hide, sh: C.hideSh },
    head:  { w: 13, h: 13, base: C.skin, sh: C.skinSh },
    arm:   { w: 4, h: 14, base: C.hide, sh: C.hideSh },
    legs:  { w: 5, h: 9, gap: 3, base: C.hideSh, sh: C.hideSh },
    boots: { w: 6, h: 3, base: C.dark, sh: C.darkSh },
    chest: (g, x) => { paint(g, rect(x.tL, x.tTop, x.tR, x.tTop+1), C.tusk, C.tuskSh);   // pale fur collar
                       paint(g, rect(x.mid-1, x.tTop+3, x.mid+1, x.tBot-2), C.gold, C.goldSh);   // gold-buckled hide lacing
                       paint(g, rect(x.tL+1, x.tBot-2, x.tR-1, x.tBot-1), C.tuskSh, C.tuskSh); },   // belt
    front: (g, x) => x.sword(g, x.handLx, x.handY-24, 22),
  });
  F.ranger = (race) => human({
    race,
    W: 56, H: 80,
    torso: { w: 19, h: 19, top: 30, base: C.leather, sh: C.leatherSh },
    head:  { w: 13, h: 13, base: C.skin, sh: C.skinSh, gear: (g, x) =>
              paint(g, (px,py) => px>=x.hL && px<=x.hR && py>=x.hTop && py<=x.hBot && (py<=x.hTop+2 || px<=x.hL+1 || px>=x.hR-1), C.leather, C.leatherSh) },
    arm:   { w: 4, h: 14, base: C.leather, sh: C.leatherSh },
    legs:  { w: 5, h: 9, gap: 3, base: C.leather, sh: C.leatherSh },
    boots: { w: 6, h: 3, base: C.darkSh, sh: C.darkSh },
    // B2-GO: strap moved DOWN off the neckline (was tTop+2..+4, crowded/fused into the head) —
    // now sits mid-chest, matching the warden's chest-emblem band placement (tTop+7..+9).
    chest: (g, x) => paint(g, rect(x.mid-6, x.tTop+7, x.mid+6, x.tTop+9), C.woodSh, C.woodSh),  // quiver strap across chest
    front: (g, x) => x.bow(g, x.handLx, x.handY-20, 20, C.wood, C.woodSh),
  });
  F.skeleton = () => human({
    W: 54, H: 78,
    torso: { w: 18, h: 19, top: 30, base: C.bone, sh: C.boneSh },
    head:  { w: 13, h: 13, base: C.bone, sh: C.boneSh, eye: C.eyeRed, sink: 4 },
    arm:   { w: 4, h: 14, base: C.bone, sh: C.boneSh },
    legs:  { w: 5, h: 9, gap: 3, base: C.bone, sh: C.boneSh },
    boots: { w: 5, h: 3, base: C.boneSh, sh: C.boneSh },
    chest: (g, x) => { for (let r = 0; r < 3; r++) paint(g, rect(x.mid-5, x.tTop+6+r*4, x.mid+5, x.tTop+6+r*4), C.boneSh, C.boneSh); },
    front: (g, x) => { x.sword(g, x.handLx, x.handY-19, 16); x.roundShield(g, x.tR+6, x.handY, 7); },
  });
  F.bandit = () => human({
    W: 56, H: 80,
    torso: { w: 19, h: 19, top: 30, base: C.cloth, sh: C.clothSh },
    head:  { w: 13, h: 13, base: C.skin, sh: C.skinSh, gear: (g, x) =>
              paint(g, (px,py) => px>=x.hL && px<=x.hR && py>=x.hTop && py<=x.hBot && (py<=x.hTop+4 || px<=x.hL+1 || px>=x.hR-1), C.cloth, C.clothSh) },
    arm:   { w: 4, h: 13, base: C.cloth, sh: C.clothSh },
    legs:  { w: 5, h: 9, gap: 3, base: C.dark, sh: C.darkSh },
    boots: { w: 6, h: 3, base: C.darkSh, sh: C.darkSh },
    front: (g, x) => { x.daggerDown(g, x.handLx, x.handY-1, 8); x.daggerDown(g, x.handRx-1, x.handY-1, 8); },
  });
  F.ogre = () => human({
    W: 68, H: 88,
    torso: { w: 28, h: 25, top: 30, base: C.ogre, sh: C.ogreSh },
    head:  { w: 19, h: 17, base: C.ogre, sh: C.ogreSh, sink: 2, eyeY: 24,
              gear: (g, x) => { paint(g, rect(x.mid-5, x.hBot-1, x.mid-3, x.hBot+1), C.tusk, C.tuskSh);
                                paint(g, rect(x.mid+3, x.hBot-1, x.mid+5, x.hBot+1), C.tusk, C.tuskSh); } },
    arm:   { w: 7, h: 19, base: C.ogre, sh: C.ogreSh },
    legs:  { w: 8, h: 11, gap: 4, base: C.ogre, sh: C.ogreSh },
    boots: { w: 9, h: 4, base: C.ogreSh, sh: C.ogreSh },
    front: (g, x) => x.club(g, x.handRx, x.tTop-2),
  });
  F.troll = () => human({
    W: 66, H: 92,
    torso: { w: 26, h: 26, top: 34, base: C.troll, sh: C.trollSh },
    head:  { w: 18, h: 16, base: C.troll, sh: C.trollSh, sink: 5, eye: C.eyeRed, eyeY: 30,
              gear: (g, x) => { paint(g, rect(x.mid-5, x.hBot-1, x.mid-3, x.hBot+1), C.tusk, C.tuskSh);
                                paint(g, rect(x.mid+3, x.hBot-1, x.mid+5, x.hBot+1), C.tusk, C.tuskSh); } },
    arm:   { w: 7, h: 24, base: C.troll, sh: C.trollSh },
    legs:  { w: 8, h: 9, gap: 4, base: C.troll, sh: C.trollSh },
    boots: { w: 9, h: 4, base: C.trollSh, sh: C.trollSh },
  });
  F.gargoyle = () => human({
    W: 72, H: 84,
    torso: { w: 22, h: 22, top: 30, base: C.stone, sh: C.stoneSh },
    head:  { w: 15, h: 14, base: C.stone, sh: C.stoneSh, eye: C.eyeRed,
              gear: (g, x) => { // chunky horns — right-triangles rising at the head's outer edges
                paint(g, (px,py) => { const r = (x.hTop-1)-py; return r>=0 && r<=3 && px>=x.hL && px<=x.hL+4-r; }, C.stone, C.stoneSh);
                paint(g, (px,py) => { const r = (x.hTop-1)-py; return r>=0 && r<=3 && px<=x.hR && px>=x.hR-4+r; }, C.stone, C.stoneSh);
                // fangs at the jaw, tusk-toned like the troll/ogre
                paint(g, rect(x.mid-4, x.hBot-1, x.mid-2, x.hBot+1), C.tusk, C.tuskSh);
                paint(g, rect(x.mid+2, x.hBot-1, x.mid+4, x.hBot+1), C.tusk, C.tuskSh); } },
    arm:   { w: 5, h: 16, base: C.stone, sh: C.stoneSh },
    legs:  { w: 6, h: 8, gap: 4, base: C.stone, sh: C.stoneSh },
    boots: { w: 7, h: 3, base: C.stoneSh, sh: C.stoneSh },
    back: (g, x) => { // BAT wings: leading edge sweeps up-out, membrane edge scalloped (payload v4 #5)
      const span = 17;
      const wing = (edgeX, dir) => (px,py) => {
        const u = (px - edgeX) * dir;                       // 0 at the shoulder, grows outward
        if (u < 0 || u > span) return false;
        const tY = x.tTop + 2 - Math.round(u * 0.8);        // upswept leading edge
        const h  = 17 - Math.round(u * 0.8);                // wing tapers outward
        const bY = tY + h + [0, 1, 3, 2][u % 4] - 2;        // scalloped membrane trailing edge
        return py >= tY && py <= bY;
      };
      paint(g, wing(x.tL - 2, -1), C.stone, C.stoneSh);
      paint(g, wing(x.tR + 2,  1), C.stone, C.stoneSh); },
  });
  const robeHood = (col, colSh, depth) => (g,x) => {
    const D = depth == null ? 3 : depth;   // rows of face the hood covers (wraith runs deeper)
    paint(g, (px,py) => { if (py===x.hTop-1 && px>=x.mid-2 && px<=x.mid+2) return true;
      if (px<x.hL||px>x.hR||py<x.hTop||py>x.hBot) return false;
      return py<=x.hTop+D || px<=x.hL+1 || px>=x.hR-1; }, col, colSh);
  };
  F.adept = (race) => robe({
    race,
    W: 50, H: 56, rTop: 14, rHem: 49, shoulderW: 18, hemW: 30, armW: 6, armH: 16,
    base: C.adept, sh: C.adeptSh,
    head: { w: 13, h: 13, base: C.skin, sh: C.skinSh, eye: OUT, gear: robeHood(C.adept, C.adeptSh) },
    front: (g, x) => { paint(g, rect(x.sR+3, 11, x.sR+5, 46), C.wood, C.woodSh); paint(g, disc(x.sR+4, 7, 3), C.teal, C.tealSh); },
  });
  F.summoner = (race) => robe({
    race,
    W: 50, H: 56, rTop: 14, rHem: 49, shoulderW: 18, hemW: 30, armW: 6, armH: 16,
    base: C.summ, sh: C.summSh,
    head: { w: 13, h: 13, base: C.skin, sh: C.skinSh, eye: OUT, gear: robeHood(C.summ, C.summSh) },
    front: (g, x) => { paint(g, rect(x.sR+3, 14, x.sR+5, 46), C.wood, C.woodSh);
                       paint(g, disc(x.sR+4, 11, 3), C.teal, C.tealSh); paint(g, disc(x.sR+1, 6, 2), C.summ, C.summSh); },
  });
  F.wraith = () => robe({
    W: 50, H: 56, rTop: 14, rHem: 49, shoulderW: 18, hemW: 30, armW: 6, armH: 16,
    base: C.wraith, sh: C.wraithSh, hemTatter: 3,   // torn, trailing hem — nothing walks under a wraith
    // face is a shadowed VOID under a deep hood — only the red eyes read (payload v4 #5 foe-art pass)
    head: { w: 13, h: 13, base: C.dark, sh: C.darkSh, eye: C.eyeRed, gear: robeHood(C.wraith, C.wraithSh, 5) },
  });

  // ============ STANDALONE GEAR (own PNG + grip pivot) ============
  function makeGear(draw, pivot) {
    const g = grid(40, 44); draw(g);
    const b = bbox(g); if (b.empty) return null;
    const canvas = renderRegion(g, b.minx, b.miny, b.maxx, b.maxy);
    return { canvas, pivot: [ (pivot[0]-b.minx)*S + Math.floor(S/2), (pivot[1]-b.miny)*S + Math.floor(S/2) ] };
  }
  const GEAR = {
    sword:        () => makeGear(g => sword(g, 8, 6, 16),                 [8, 6+16+3]),
    round_shield: () => makeGear(g => roundShield(g, 10, 12, 7),          [10, 12]),
    tower_shield: () => makeGear(g => towerShield(g, 6, 4, 15, 30, C.royal, C.royalSh), [10, 17]),
    dagger:       () => makeGear(g => daggerDown(g, 6, 6, 9),             [6, 7]),
    club:         () => makeGear(g => club(g, 8, 6),                      [8, 6+20]),
    staff:        () => makeGear(g => staff(g, 8, 6, 40, C.teal),         [8, 24]),
    bow:          () => makeGear(g => bow(g, 10, 4, 32, C.wood, C.woodSh),[10, 4+16]),
  };

  // ---- B2-GO: programmatic tiered weapons + new families + armor icons (one entry per combo —
  // never hand-authored, per the golden "every asset is script-generated" rule) ----
  const WEAPON_PIVOT = {
    longsword: [8,26], claymore: [8,34], dagger: [8,19], rapier: [8,30], shortsword: [8,22],
    axe: [8,27], battleaxe: [8,32], mace: [8,22], warhammer: [8,26],
  };
  for (const wtype in WEAPON_TYPES) {
    const spec = WEAPON_TYPES[wtype];
    for (const tier of METAL_ORDER) {
      const pal = METAL[tier];
      GEAR[wtype + '_' + tier] = () => makeGear(g => spec.paint(g, 8, 6, pal), WEAPON_PIVOT[wtype]);
    }
  }
  for (const tier of SLING_ORDER)  { const pal = SLING_TIER[tier];  GEAR['sling_' + tier]  = () => makeGear(g => slingSil(g, 8, 6, pal),        [8, 22]); }
  for (const tier of STAFF_ORDER)  { const pal = STAFF_TIER[tier];  GEAR['staff_' + tier]  = () => makeGear(g => staff(g, 8, 6, 40, pal.orb),   [8, 24]); }
  for (const tier of CHARM_ORDER)  { const pal = CHARM_TIER[tier];  GEAR['charm_' + tier]  = () => makeGear(g => charmSil(g, 8, 6, pal),        [8, 17]); }
  for (const tier of TOME_ORDER)   { const pal = TOME_TIER[tier];   GEAR['tome_' + tier]   = () => makeGear(g => tomeSil(g, 8, 6, pal),         [8, 17]); }
  for (const tier of WAND_ORDER)   { const pal = WAND_TIER[tier];   GEAR['wand_' + tier]   = () => makeGear(g => wandSil(g, 8, 6, 22, pal),     [8, 25]); }
  // B11: bow ladder (bow_short/bow_long/bow_compound/bow_elven) + the ranged BACK-MOUNT layer
  // (bow/sling slung across the back — pivot = sprite centre, aligned to figure sockets.back,
  // drawn BEHIND all parts while melee hands are full; §6d/§17 #22).
  for (const tier of BOW_ORDER)   { const pal = BOW_TIER[tier];
    GEAR['bow_' + tier]           = () => makeGear(g => bowTierSil(g, 10, 4, pal), [10, 4 + Math.floor(pal.len/2)]);
    GEAR['bow_' + tier + '_back'] = () => makeGear(g => bowBackSil(g, pal), [16, 17]);
  }
  for (const tier of SLING_ORDER) { const pal = SLING_TIER[tier];
    GEAR['sling_' + tier + '_back'] = () => makeGear(g => slingBackSil(g, pal), [11, 12]);
  }
  // CON shield object ladder: Wooden Shield -> Iron Buckler -> Kite Shield -> Tower Shield (reuses
  // the existing round_shield/tower_shield painters — same silhouette family, size/palette vary).
  GEAR['shield_wooden']  = () => makeGear(g => roundShield(g, 10, 12, 6, C.wood, C.woodSh),                          [10, 12]);
  GEAR['shield_buckler'] = () => makeGear(g => roundShield(g, 8, 9, 4, METAL.iron.base, METAL.iron.sh),               [8, 9]);
  GEAR['shield_kite']    = () => makeGear(g => towerShield(g, 5, 3, 13, 24, METAL.steel.base, METAL.steel.sh),        [9, 14]);
  GEAR['shield_tower']   = () => makeGear(g => towerShield(g, 4, 2, 16, 32, METAL.dwarven.base, METAL.dwarven.sh),    [10, 17]);
  // Armor icons (inventory-card scale) — NOT hand-socket mounts, so the pivot is nominal (a body-layer
  // morph system to actually WEAR these on the figure is future work, see DEV_LOOP_MEMORY).
  const ARMOR_TIER_TABLE = { str: [METAL, METAL_ORDER], dex: [LEATHER, LEATHER_ORDER], int: [ROBE, ROBE_ORDER] };
  for (const slotKey in ARMOR_SLOTS) {
    const spec = ARMOR_SLOTS[slotKey];
    const fam = slotKey.split('_')[0];
    const [TABLE, ORDER] = ARMOR_TIER_TABLE[fam];
    for (const tier of ORDER) {
      const pal = TABLE[tier];
      GEAR['armor_' + slotKey + '_' + tier] = () => makeGear(g => spec.paint(g, 8, 4, pal), [8, 8]);
    }
  }
  // Catalog (name/attr/slot per tier) for card copy — consumed by attribute-model.js-style content,
  // not by the pixel pipeline. Exported below alongside GEAR/layout.
  const GEAR_CATALOG = [];
  for (const wtype in WEAPON_TYPES) for (const tier of METAL_ORDER)
    GEAR_CATALOG.push({ id: wtype + '_' + tier, name: METAL[tier].label + ' ' + WEAPON_NAME[wtype], attr: WEAPON_TYPES[wtype].hand, slot: WEAPON_TYPES[wtype].slot, tier });
  for (const tier of SLING_ORDER)  GEAR_CATALOG.push({ id:'sling_'+tier, name: SLING_TIER[tier].label + ' Sling', attr:'DEX', slot:'1H', tier });
  for (const tier of STAFF_ORDER)  GEAR_CATALOG.push({ id:'staff_'+tier, name: STAFF_TIER[tier].label + ' Staff', attr:'INT', slot:'2H', tier });
  for (const tier of CHARM_ORDER)  GEAR_CATALOG.push({ id:'charm_'+tier, name: CHARM_TIER[tier].label + ' Charm', attr:'INT', slot:'OFF', tier });
  for (const tier of TOME_ORDER)   GEAR_CATALOG.push({ id:'tome_'+tier,  name: TOME_TIER[tier].label + ' Tome',   attr:'INT', slot:'OFF', tier });
  for (const tier of WAND_ORDER)   GEAR_CATALOG.push({ id:'wand_'+tier,  name: WAND_TIER[tier].label + ' Wand',   attr:'INT', slot:'HAND', tier });
  // B11: bows fill the §6d RANGED slot (labels are the full locked names, no composed noun)
  for (const tier of BOW_ORDER)    GEAR_CATALOG.push({ id:'bow_'+tier,   name: BOW_TIER[tier].label,              attr:'DEX', slot:'RANGED', tier });
  const ARMOR_FAM_ATTR = { str:'STR', dex:'DEX', int:'INT' };
  for (const slotKey in ARMOR_SLOTS) {
    const spec = ARMOR_SLOTS[slotKey], fam = slotKey.split('_')[0], [TABLE, ORDER] = ARMOR_TIER_TABLE[fam];
    // B10: DEX/INT carry the §6c canon ladder verbatim (spec.names); STR composes material + piece
    // ("Steel Helm") — that composition is the locked new-name convention.
    for (const tier of ORDER) GEAR_CATALOG.push({ id:'armor_'+slotKey+'_'+tier,
      name: spec.names ? spec.names[ORDER.indexOf(tier)] : TABLE[tier].label + ' ' + spec.name,
      attr: ARMOR_FAM_ATTR[fam], slot: slotKey, tier });
  }
  GEAR_CATALOG.push({ id:'shield_wooden', name:'Wooden Shield', attr:'CON', slot:'SHIELD', tier:'t1' });
  GEAR_CATALOG.push({ id:'shield_buckler', name:'Iron Buckler', attr:'CON', slot:'SHIELD', tier:'t2' });
  GEAR_CATALOG.push({ id:'shield_kite', name:'Kite Shield', attr:'CON', slot:'SHIELD', tier:'t3' });
  GEAR_CATALOG.push({ id:'shield_tower', name:'Tower Shield', attr:'CON', slot:'SHIELD', tier:'t4' });
  // ============ B12 (CORRECTED 2026-07-04) — WORN-ARMOR PART SET (supersedes the per-figure themed
  // layers + B2-GO item 3's generic layers; they were one system, this is the single spec) ============
  // FULL PART SPRITES: each file is a COMPLETE ready-to-blit part image — the race's body part with
  // the armor already drawn in, NOT a transparent overlay. The engine swaps the whole part sprite by
  // (race, slot, wear-state); no runtime compositing.
  //   sprites/gear/worn/<race>/<slot>/bare_<condition>.png                  // unarmored — fallback terminal
  //   sprites/gear/worn/<race>/<slot>/<type>_<tier>_<condition>.png         // GENERIC armored (core-agnostic)
  //   sprites/gear/worn/<race>/<slot>/<core>/<type>_<tier>_<condition>.png  // THEMED — ONLY the core's favored line
  // race ∈ RACE keys — EVERY slot authored for EVERY race (the share-arms/legs-across-races
  // optimization is deliberately dropped); slot ∈ head/chest/arms/legs — ONE sprite per slot (the
  // engine reuses it for both arms / both legs); type ∈ str/dex/int — there is NO "plain" type, the
  // unarmored part is `bare` (bare has no tier); tier ∈ 1..4; condition ∈ healthy/damaged/broken
  // (no disabled variants — §6e disabled gear un-renders). int = chest+head only (§6c); CON shields
  // are hand items — no worn body parts. THEMED = FAVORED LINE ONLY, never a cross-product: a core
  // wearing a non-favored line renders the GENERIC art. Geometry = the race BASE body plan (human
  // baseline ± race build), core-agnostic — how these compose with per-core figure geometry is the
  // engine's §7/§17 #15 morph question. Engine fallback chain: themed → generic → generic healthy →
  // bare → bare healthy — partial coverage is always safe.
  const WORN_TYPES = { str: [METAL, METAL_ORDER], dex: [LEATHER, LEATHER_ORDER], int: [ROBE, ROBE_ORDER] };
  const WORN_SLOTS = { str: ['head','chest','arms','legs'], dex: ['head','chest','arms','legs'], int: ['head','chest'] };
  const FAVORED_LINE = { grunt:'str', warden:'str', adept:'int', summoner:'int', reaver:'dex', ranger:'dex', barbarian:'str' };
  // stamp a detail INSIDE an already-painted part (never touches the outline ring or empty px)
  function detail(g, mask, col) {
    for (let y=0;y<g.H;y++) for (let x=0;x<g.W;x++)
      if (mask(x,y) && g.px[y][x] && g.px[y][x] !== OUT) g.px[y][x] = col;
  }
  const dot = (g,x,y,col) => { if (g.px[y] && g.px[y][x] && g.px[y][x] !== OUT) g.px[y][x] = col; };
  const capMask = (G, D) => (px,py) => G.head.mask(px,py) && (py <= G.head.hTop + D || px <= G.head.hL + 1 || px >= G.head.hR - 1);
  const hoodMask = (G, D) => (px,py) => { const hd = G.head;
    if (py === hd.hTop - 1 && px >= G.mid - 2 && px <= G.mid + 2) return true;   // hood peak (base robe heads carry it too)
    if (px < hd.hL || px > hd.hR || py < hd.hTop || py > hd.hBot) return false;
    return py <= hd.hTop + D || px <= hd.hL + 1 || px >= hd.hR - 1; };
  // B12 theme hooks — each draws INTO the freshly base-coated part. Accents use the locked palette
  // only (gold/royal/dark/tusk/bone/teal/mail/wood); tiers stay a pure palette swap underneath.
  const WORN_THEMES = {
    generic_str: {   // plain serviceable plate (the cross-equip look on DEX-core figures)
      head:  (g,G,pal) => paint(g, capMask(G, 2), pal.base, pal.sh),
      chest: (g,G,pal) => detail(g, (x,y) => x === G.mid && y >= G.tTop+2 && y <= G.tBot-2, pal.sh),
      arms:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.top + Math.floor((r.bot-r.top)/2), pal.sh),
      legs:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.top + 2, pal.sh),
    },
    grunt: {   // B12 Grunt: versatile — practical, well-kept soldier's kit, deliberately unshowy
      head:  (g,G,pal) => paint(g, capMask(G, 2), pal.base, pal.sh),
      chest: (g,G,pal) => { detail(g, (x,y) => x === G.mid && y >= G.tTop+2 && y <= G.tBot-2, pal.sh);
                            detail(g, (x,y) => y === G.tBot-3 && x >= G.tL+2 && x <= G.tR-2, C.woodSh); },   // plain kit belt
      arms:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.top + Math.floor((r.bot-r.top)/2), pal.sh),
      legs:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.top + 2, pal.sh),
    },
    warden: {   // B12 Warden: block & armor — heaviest READ: thick edges, rivets, shield-boss motif
      headEyes: false,   // full-face helm, visor slit instead
      head:  (g,G,pal) => { const hd = G.head;
        paint(g, (px,py) => px >= hd.hL && px <= hd.hR && py >= hd.hTop && py <= hd.hBot, pal.base, pal.sh);
        detail(g, (x,y) => y === hd.hTop+6 && x >= hd.hL+2 && x <= hd.hR-2, C.dark);   // visor slit
        detail(g, (x,y) => x === G.mid && y >= hd.hTop+3 && y <= hd.hBot-2, C.dark);   // nasal ridge
        dot(g, G.mid, hd.hTop+1, C.gold); },                                            // boss crest stud
      chest: (g,G,pal) => { detail(g, (x,y) => (x <= G.tL+2 || x >= G.tR-2) && y >= G.tTop+1 && y <= G.tBot-1, pal.sh);   // reinforced edge plates
        for (let x = G.tL+3; x <= G.tR-3; x += 3) { dot(g, x, G.tTop+1, C.dark); dot(g, x, G.tBot-1, C.dark); }           // rivet rows
        paint(g, disc(G.mid, G.tTop + Math.floor((G.tBot-G.tTop)/2), 2), C.gold, C.goldSh); },                            // shield-boss motif
      arms:  (g,G,pal,side,r) => { const cy = r.top + Math.floor((r.bot-r.top)/2);
        detail(g, (x,y) => y === r.top+1 || y === cy || y === cy+1, pal.sh); dot(g, Math.floor((r.x0+r.x1)/2), r.top+1, C.dark); },
      legs:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.top+1 || y === r.top+3, pal.sh),
    },
    barbarian: {   // B15 Barbarian (str) — savage warlord kit: hide straps + tusk/fur accents over the plate, minimal helm
      head:  (g,G,pal) => { paint(g, capMask(G, 2), pal.base, pal.sh);
        for (const x of [G.head.hL+1, G.head.hR-1]) dot(g, x, G.head.hTop+1, C.tusk); },   // tusk crest studs
      chest: (g,G,pal) => { detail(g, (x,y) => x === G.mid && y >= G.tTop+2 && y <= G.tBot-2, C.wood);   // hide lacing down the center
        detail(g, (x,y) => (y === G.tTop+1) && x >= G.tL+1 && x <= G.tR-1, C.tusk);                      // fur collar band
        for (let y = G.tTop+4; y <= G.tBot-2; y += 3) { dot(g, G.tL+2, y, C.tusk); dot(g, G.tR-2, y, C.tusk); } },   // strap rivets
      arms:  (g,G,pal,side,r) => { detail(g, (x,y) => y === r.top+1, C.tusk); dot(g, Math.floor((r.x0+r.x1)/2), r.bot-1, C.wood); },   // fur shoulder + strap
      legs:  (g,G,pal,side,r) => { for (let x = r.x0; x <= r.x1; x += 2) dot(g, x, r.top, C.tusk); },   // fur tops
    },
    generic_dex: {   // plain leathers (the cross-equip look on non-dex cores; display "Plain leather" is a NAME — the type token is dex)
      head:  (g,G,pal) => paint(g, capMask(G, 2), pal.base, pal.sh),
      chest: (g,G,pal) => detail(g, (x,y) => (x === G.tL+3 || x === G.tR-3) && y >= G.tTop+1 && y <= G.tBot-1, pal.sh),   // vest panel seams
      arms:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.bot-1, pal.sh),
      legs:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.top, pal.sh),
    },
    generic_int: {   // plain cloth robes (the cross-equip look on non-caster cores)
      head:  (g,G,pal) => paint(g, hoodMask(G, 3), pal.base, pal.sh),
      chest: (g,G,pal) => { detail(g, (x,y) => y === G.tBot-1 && x >= G.tL+1 && x <= G.tR-1, pal.sh);   // hem band
        dot(g, G.mid-1, G.tTop+1, pal.sh); dot(g, G.mid+1, G.tTop+1, pal.sh); dot(g, G.mid, G.tTop+2, pal.sh); },   // neckline V
      arms:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.bot-1, pal.sh),
    },
    reaver: {   // B12 Reaver: dual-wield — light, aggressive cut; twin-blade X etching, dark studs
      head:  (g,G,pal) => { paint(g, capMask(G, 3), pal.base, pal.sh);
        dot(g, G.head.hL+2, G.head.hTop+1, C.dark); dot(g, G.head.hR-2, G.head.hTop+1, C.dark); },
      chest: (g,G,pal) => { const cy = G.tTop + Math.floor((G.tBot-G.tTop)/2);
        for (let i = -3; i <= 3; i++) { dot(g, G.mid+i, cy+i, C.dark); dot(g, G.mid+i, cy-i, C.dark); } },   // twin-blade X
      arms:  (g,G,pal,side,r) => { detail(g, (x,y) => y === r.bot-1, pal.sh); dot(g, Math.floor((r.x0+r.x1)/2), r.top+1, C.dark); },
      legs:  (g,G,pal,side,r) => detail(g, (x,y) => (side === 'L' ? x === r.x0+1 : x === r.x1-1) && y >= r.top && y <= r.bot, pal.sh),
    },
    ranger: {   // B12 Ranger: bow & pet — tracker/nature: fur (tusk) trim, quiver strap kept
      head:  (g,G,pal) => { paint(g, capMask(G, 2), pal.base, pal.sh);
        for (let x = G.head.hL+1; x <= G.head.hR-1; x += 2) dot(g, x, G.head.hTop+3, C.tusk); },   // fur brow line
      chest: (g,G,pal) => { detail(g, (x,y) => y >= G.tTop+7 && y <= G.tTop+9 && x >= G.mid-6 && x <= G.mid+6, C.woodSh);   // quiver strap (kept from the kit)
        for (let x = G.tL+1; x <= G.tR-1; x += 2) dot(g, x, G.tTop+1, C.tusk); },                                            // fur collar
      arms:  (g,G,pal,side,r) => detail(g, (x,y) => y === r.bot-1, C.tusk),                                                  // fur cuffs
      legs:  (g,G,pal,side,r) => { for (let x = r.x0; x <= r.x1; x += 2) dot(g, x, r.top, C.tusk); },                        // fur tops
    },
    adept: {   // B12 Adept: spellcasting — runic trim, flowing read, restrained arcane accent
      head:  (g,G,pal) => { paint(g, hoodMask(G, 3), pal.base, pal.sh);
        if (pal.glow) setPx(g, G.mid, G.head.hTop, [200,230,255]); },                                  // humming-tier brow gleam
      chest: (g,G,pal) => { detail(g, (x,y) => y === G.tTop+8 && x >= G.mid-5 && x <= G.mid+5, pal.sh);   // sash
        const rc = pal.glow ? [190,230,255] : C.teal;
        for (let x = G.tL+1; x <= G.tR-1; x += 3) dot(g, x, G.tBot-1, rc);                                // runic hem trim
        dot(g, G.mid, G.tTop+3, rc); dot(g, G.mid-1, G.tTop+4, rc); dot(g, G.mid+1, G.tTop+4, rc); dot(g, G.mid, G.tTop+5, rc); },   // chest sigil (diamond)
      arms:  (g,G,pal,side,r) => dot(g, Math.floor((r.x0+r.x1)/2), r.bot-1, pal.glow ? [190,230,255] : C.teal),   // cuff rune
    },
    summoner: {   // B12 Summoner: minions/binding — same robe silhouette, darker/heavier: bone + chain trim, ritual sigil
      head:  (g,G,pal) => { paint(g, hoodMask(G, 4), pal.base, pal.sh); dot(g, G.mid, G.head.hBot-1, C.bone); },   // deeper hood + bone clasp
      chest: (g,G,pal) => { detail(g, (x,y) => (y === G.tTop+7 || y === G.tTop+8) && x >= G.mid-6 && x <= G.mid+6, C.dark);   // heavy dark band
        for (let x = G.tL+1; x <= G.tR-1; x += 3) { dot(g, x, G.tBot-1, C.bone); dot(g, x+1, G.tBot-1, C.bone); }             // bone hem trim
        for (let x = G.mid-6; x <= G.mid+6; x += 2) dot(g, x, G.tBot-4, C.mail);                                              // chain links
        dot(g, G.mid, G.tTop+2, C.bone); dot(g, G.mid-1, G.tTop+3, C.bone); dot(g, G.mid+1, G.tTop+3, C.bone); dot(g, G.mid, G.tTop+4, C.bone); },   // ritual sigil
      arms:  (g,G,pal,side,r) => { const cx = Math.floor((r.x0+r.x1)/2); dot(g, cx, r.bot-1, C.bone); dot(g, cx, r.bot-3, C.bone); },   // bone cuff beads
    },
  };
  // Synthetic per-slot geometry on the race BASE body plan (human baseline: torso 20×20, head 13²,
  // arm 4×14 (6-wide mask incl. border cols, matching the figure arm masks), leg 5×9; race build
  // permutes width/ears/skin). One grid per sprite; bbox-trimmed on export like every other part.
  function wornGeom(race) {
    // spacious canvas (bbox-trimmed on export, so oversize is free) so the tall/wide half-giant deltas
    // never clip. All positions derive from `mid` + the race grow deltas (torsoH/slim/armD/short/head*).
    const th = race.torsoH || 0;
    const W = 40, H = 46, mid = 20;
    const tw = 20 - (race.slim || 0), tL = mid - Math.floor(tw/2), tR = tL + tw - 1, tTop = 6, tBot = tTop + 19 + th;
    const hw = 13 + (race.headW || 0), hh = 13 + (race.headH || 0);
    const hL = mid - Math.floor(hw/2), hR = hL + hw - 1, hTop = 6, hBot = hTop + hh - 1;
    const ears = race.ears || 0, earY = hTop + Math.floor(hh * 0.45);
    const headMask = (px,py) => {
      if (px>=hL && px<=hR && py>=hTop && py<=hBot) return true;
      if (ears) for (const s of [-1,1]) { const ex = s<0 ? hL : hR;
        if (px===ex+s   && py>=earY-1 && py<=earY+1) return true;
        if (px===ex+2*s && py>=earY-1 && py<=earY)   return true;
        if (ears>1 && px===ex+3*s && py===earY-1)     return true; }
      return false;
    };
    return { W, H, mid, tL, tR, tTop, tBot,
      head: { hL, hR, hTop, hBot, mask: headMask, eyeY: hTop + Math.floor(hh * 0.5) },
      arm: { x0: mid - 8, x1: mid - 3, top: 6, bot: 6 + 12 + Math.floor(th / 2) - (race.armD || 0) },
      leg: { x0: mid - 7, x1: mid - 3, top: 6, bot: 6 + 7 - (race.short || 0) } };
  }
  // One armored slot sprite (base coat in the tier palette + theme detailing).
  function wornSlotGrid(race, G, slot, pal, T) {
    const g = grid(G.W, G.H);
    if (slot === 'head') {
      paint(g, G.head.mask, race.skin, race.skinSh);
      T.head(g, G, pal);
      if (T.headEyes !== false) { setPx(g, G.mid-3, G.head.eyeY, OUT); setPx(g, G.mid+3, G.head.eyeY, OUT); }
    } else if (slot === 'chest') {
      paint(g, rect(G.tL, G.tTop, G.tR, G.tBot), pal.base, pal.sh); T.chest(g, G, pal);
    } else if (slot === 'arms') {
      const r = G.arm; paint(g, rect(r.x0, r.top, r.x1, r.bot), pal.base, pal.sh); T.arms(g, G, pal, 'R', r);
    } else {
      const r = G.leg; paint(g, rect(r.x0, r.top, r.x1, r.bot), pal.base, pal.sh); if (T.legs) T.legs(g, G, pal, 'R', r);
    }
    return g;
  }
  // The `bare` terminal: the unarmored body part (flesh; head gets a simple hair cap + eyes so a
  // bare head doesn't read as a blank block).
  function bareSlotGrid(race, G, slot) {
    const g = grid(G.W, G.H);
    if (slot === 'head') {
      paint(g, G.head.mask, race.skin, race.skinSh);
      paint(g, capMask(G, 2), race.hair, race.hairSh);
      setPx(g, G.mid-3, G.head.eyeY, OUT); setPx(g, G.mid+3, G.head.eyeY, OUT);
    } else if (slot === 'chest') paint(g, rect(G.tL, G.tTop, G.tR, G.tBot), race.skin, race.skinSh);
    else if (slot === 'arms') { const r = G.arm; paint(g, rect(r.x0, r.top, r.x1, r.bot), race.skin, race.skinSh); }
    else { const r = G.leg; paint(g, rect(r.x0, r.top, r.x1, r.bot), race.skin, race.skinSh); }
    return g;
  }
  // Build the whole worn set: [{path, canvas}] + the layout.json `worn` inventory block.
  function buildWornSet() {
    const files = [];
    const emit = (path, g, state) => {
      const dg = damage(g, state); const b = bbox(dg); if (b.empty) return;
      files.push({ path, canvas: renderRegion(dg, b.minx, b.miny, b.maxx, b.maxy) });
    };
    for (const rid in RACE) {
      const race = RACE[rid], G = wornGeom(race);
      for (const slot of ['head','chest','arms','legs']) {
        const g = bareSlotGrid(race, G, slot);
        for (const state of STATES) emit('sprites/gear/worn/' + rid + '/' + slot + '/bare_' + state + '.png', g, state);
      }
      for (const type in WORN_SLOTS) {
        const [TAB, ORDER] = WORN_TYPES[type];
        for (const slot of WORN_SLOTS[type]) for (let t = 1; t <= 4; t++) {
          const g = wornSlotGrid(race, G, slot, TAB[ORDER[t-1]], WORN_THEMES['generic_' + type]);
          for (const state of STATES) emit('sprites/gear/worn/' + rid + '/' + slot + '/' + type + '_' + t + '_' + state + '.png', g, state);
        }
      }
      for (const core in FAVORED_LINE) {
        const type = FAVORED_LINE[core], [TAB, ORDER] = WORN_TYPES[type];
        for (const slot of WORN_SLOTS[type]) for (let t = 1; t <= 4; t++) {
          const g = wornSlotGrid(race, G, slot, TAB[ORDER[t-1]], WORN_THEMES[core]);
          for (const state of STATES) emit('sprites/gear/worn/' + rid + '/' + slot + '/' + core + '/' + type + '_' + t + '_' + state + '.png', g, state);
        }
      }
    }
    const inv = { root: 'sprites/gear/worn', races: Object.keys(RACE),
      slots: { str: WORN_SLOTS.str, dex: WORN_SLOTS.dex, int: WORN_SLOTS.int },
      tiers: [1, 2, 3, 4], conditions: STATES,
      bare: { slots: ['head','chest','arms','legs'], conditions: STATES },
      themes: FAVORED_LINE,
      sprite: '<race>/<slot>/[<core>/]<type>_<tier>_<condition>',
      fallback: ['themed', 'generic', 'generic healthy', 'bare', 'bare healthy'] };
    return { files, inv };
  }

  // STRIPpable parts: limbs that wear (removable) armor. When the armor is force-unequipped
  // (disabled by attribute loss) the screen asks for a 'bare<state>' sprite — the same limb
  // recolored to flesh. Maps part -> [armorBase, armorShadow] to swap out for skin.
  const STRIP = {
    grunt: { armL: [C.mail, C.mailSh], armR: [C.mail, C.mailSh], legL: [C.mail, C.mailSh], legR: [C.mail, C.mailSh] },
  };

  // which gear each figure carries, and on which hand socket.
  // PLAYER CORES carry their v6 STARTER KIT (DESIGN_SPEC §7a + the balance-sheet v6 defaults) —
  // reconciled from the 2026-07-04 layout.json hand-patch (DEV_LOOP #34.3) so a regenerate no longer
  // reverts them: Grunt longsword+wooden shield · Warden longsword+buckler · Adept staff · Summoner
  // wand+charm · Reaver twin daggers · Ranger short bow + iron DAGGER (v6: dagger, not short sword).
  // Monsters keep the old generic gear ids.
  const MOUNTS = {
    grunt:   [['longsword_iron','handL'], ['shield_wooden','handR']],
    warden:  [['longsword_iron','handL'], ['shield_buckler','handR']],
    adept:   [['staff_wooden','handR']],
    summoner:[['wand_adept','handL'], ['charm_wooden','handR']],
    reaver:  [['dagger_iron','handL'], ['dagger_iron','handR']],
    ranger:  [['bow_short','handL'], ['dagger_iron','handR']],
    // B15 Barbarian: a two-handed claymore held at one hand socket (2H render, like the bow) + no shield
    barbarian:[['claymore_iron','handL']],
    skeleton:[['sword','handL'], ['round_shield','handR']],
    bandit:  [['dagger','handL'], ['dagger','handR']],
    ogre:    [['club','handR']],
  };

  // ============ MINIONS (summon creatures — single flat-bevel sprites) ============
  // Each part painted with its own 1px outline + bottom/right bevel, per ART_RULES.
  const MIN = {};
  MIN.skeleton = () => { const g = grid(30, 42), mid = 14;
    paint(g, rect(mid-4, 31, mid-2, 38), C.bone, C.boneSh);            // leg L
    paint(g, rect(mid+2, 31, mid+4, 38), C.bone, C.boneSh);            // leg R
    paint(g, rect(mid-6, 17, mid+6, 30), C.bone, C.boneSh);            // ribcage torso
    for (let r=0;r<3;r++) paint(g, rect(mid-4, 20+r*3, mid+4, 20+r*3), C.boneSh, C.boneSh); // ribs
    paint(g, rect(mid-9, 18, mid-7, 28), C.bone, C.boneSh);            // arm L
    paint(g, rect(mid+7, 18, mid+9, 28), C.bone, C.boneSh);            // arm R
    paint(g, rect(mid-6, 4, mid+6, 15), C.bone, C.boneSh);             // skull
    setPx(g, mid-3, 9, C.eyeRed); setPx(g, mid+3, 9, C.eyeRed);
    return finishSprite(g);
  };
  MIN.wisp = () => { const g = grid(30, 42), mid = 14;
    paint(g, disc(mid, 33, 3), C.teal, C.tealSh);                      // tail blob
    paint(g, disc(mid, 26, 4), C.teal, C.tealSh);                      // mid blob
    paint(g, disc(mid, 15, 8), C.teal, C.tealSh);                      // body orb
    setPx(g, mid-3, 13, OUT); setPx(g, mid+3, 13, OUT);                // eyes
    return finishSprite(g);
  };
  MIN.hound = () => { const g = grid(48, 34); const col = C.dark, sh = C.darkSh;
    paint(g, rect(12, 12, 36, 23), col, sh);                          // body
    paint(g, rect(3, 9, 14, 21), col, sh);                            // head (front-left)
    paint(g, rect(0, 15, 3, 20), col, sh);                            // snout
    paint(g, rect(8, 4, 11, 9), col, sh);                             // ear
    paint(g, rect(13, 24, 16, 31), col, sh);                          // leg
    paint(g, rect(19, 24, 22, 31), col, sh);
    paint(g, rect(27, 24, 30, 31), col, sh);
    paint(g, rect(32, 24, 35, 31), col, sh);
    paint(g, rect(37, 11, 43, 14), col, sh);                          // tail
    setPx(g, 6, 13, C.eyeRed);
    return finishSprite(g);
  };
  MIN.golem = () => { const g = grid(42, 46), mid = 20;
    paint(g, rect(mid-9, 35, mid-3, 43), C.stone, C.stoneSh);          // leg L
    paint(g, rect(mid+3, 35, mid+9, 43), C.stone, C.stoneSh);          // leg R
    paint(g, rect(mid-12, 15, mid+12, 34), C.stone, C.stoneSh);        // torso block
    paint(g, rect(19, 18, 19, 31), C.stoneSh, C.stoneSh);             // crack
    paint(g, rect(mid-18, 17, mid-13, 31), C.stone, C.stoneSh);        // arm L
    paint(g, rect(mid+13, 17, mid+18, 31), C.stone, C.stoneSh);        // arm R
    paint(g, rect(mid-7, 3, mid+7, 14), C.stone, C.stoneSh);           // head
    setPx(g, mid-3, 8, C.teal); setPx(g, mid+3, 8, C.teal);           // eyes
    return finishSprite(g);
  };
  MIN.imp = () => { const g = grid(36, 42), mid = 16;
    paint(g, rect(mid-5, 30, mid-2, 37), C.red, C.redSh);             // leg L
    paint(g, rect(mid+2, 30, mid+5, 37), C.red, C.redSh);             // leg R
    paint(g, rect(mid-6, 16, mid+6, 29), C.red, C.redSh);             // torso
    paint(g, rect(mid-10, 17, mid-7, 27), C.red, C.redSh);            // arm L
    paint(g, rect(mid+7, 17, mid+10, 27), C.red, C.redSh);            // arm R
    paint(g, rect(mid-6, 4, mid+6, 15), C.red, C.redSh);              // head
    paint(g, rect(mid-7, 1, mid-6, 3), C.dark, C.darkSh);            // horn L
    paint(g, rect(mid+6, 1, mid+7, 3), C.dark, C.darkSh);            // horn R
    paint(g, rect(mid+6, 27, mid+9, 29), C.red, C.redSh);            // tail base
    paint(g, rect(mid+8, 24, mid+10, 26), C.red, C.redSh);           // tail tip
    setPx(g, mid-3, 9, C.gold); setPx(g, mid+3, 9, C.gold);          // eyes
    return finishSprite(g);
  };

  // ============ SCREENS ============
  // Screen manifests are NOT emitted here: `Content/layout.json`'s screens/templates/style sections
  // are produced by the dc.html extraction pipeline (proto/extract_all.html + proto/extract_merge.js,
  // LAYOUT_CONTRACT §9) and MERGED alongside the figures/gear this generator owns. The old inline
  // SCREENS stub (pre-rename combat/build/runmap/campaign/newrun ids) was a clobber footgun — removed.

  // ============ BUILD EVERYTHING ============
  const RACE_CORES = ['grunt','warden','adept','summoner','reaver','ranger','barbarian'];   // the 7 player core runes (race × core)
  const MONSTERS   = ['skeleton','bandit','wraith','ogre','troll','gargoyle'];  // standalone foes (no race axis)
  const figuresOut = [];
  const layout = { figures: {}, gear: {} };

  // Emit ONE figure (modular parts + layout entry + flat). `key` is the output id (e.g. 'elf_grunt'),
  // `specName` is the F[] spec + STRIP/MOUNTS lookup key (e.g. 'grunt'), `race` is the base-body axis.
  function emitFigure(key, specName, race) {
    race = race || RACE.human;
    const fig = F[specName](race);
    const comp = flatten(fig);
    const cb = bbox(comp);
    const figMinx = Math.max(0, cb.minx-1), figMiny = Math.max(0, cb.miny-1);
    const figMaxx = Math.min(fig.W-1, cb.maxx+1), figMaxy = Math.min(fig.H-1, cb.maxy+1);
    const sizeW = (figMaxx-figMinx+1)*S, sizeH = (figMaxy-figMiny+1)*S;
    const flat = renderRegion(comp, figMinx, figMiny, figMaxx, figMaxy);
    const sock = {};
    for (const k in fig.sockets) { const [sx,sy]=fig.sockets[k]; sock[k] = [ (sx-figMinx)*S + Math.floor(S/2), (sy-figMiny)*S + Math.floor(S/2) ]; }
    const entry = {
      size: [sizeW, sizeH],
      race: race.id,
      pivot: [ (fig.mid-figMinx)*S + Math.floor(S/2), (cb.maxy-figMiny+1)*S ],
      z: fig.parts.map(p => p.name),
      parts: {}, sockets: sock,
      mounts: (MOUNTS[specName] || []).map(([gearId,hand]) => ({ gear: gearId, socket: hand })),
    };
    const partsOut = [];
    for (const p of fig.parts) {
      if (p.name === 'frontGear') continue;   // §2: equipment is mounted at runtime, NOT a modular part
      const pb = bbox(p.g); if (pb.empty) continue;
      entry.parts[p.name] = { rect: [ (pb.minx-figMinx)*S, (pb.miny-figMiny)*S, (pb.maxx-pb.minx+1)*S, (pb.maxy-pb.miny+1)*S ] };
      for (const state of STATES) {
        const dg = damage(p.g, state);
        const db = bbox(dg); if (db.empty) continue;
        partsOut.push({ name: p.name, state, canvas: renderRegion(dg, db.minx, db.miny, db.maxx, db.maxy) });
      }
      // bare (flesh) variants for stripped armor — same shape, recolored to THIS race's skin, per damage state
      const strip = (STRIP[specName] || {})[p.name];
      if (strip) {
        const bareGrid = recolor(p.g, strip[0], strip[1], race.skin, race.skinSh);
        for (const state of STATES) {
          const dg = damage(bareGrid, state);
          const db = bbox(dg); if (db.empty) continue;
          partsOut.push({ name: p.name, state: 'bare' + state, canvas: renderRegion(dg, db.minx, db.miny, db.maxx, db.maxy) });
        }
      }
    }
    layout.figures[key] = entry;
    figuresOut.push({ name: key, flat, parts: partsOut });
  }

  // RACE × CORE-RUNE: every playable race (INCLUDING human) emits its 6 cores as <race>_<core>
  // (LAYOUT_CONTRACT §1: humans are human_grunt/human_warden/… — no bare core ids anymore).
  for (const rid in RACE) {
    for (const core of RACE_CORES) emitFigure(rid + '_' + core, core, RACE[rid]);
  }
  // Monsters are standalone foes (human base body, no race axis) — keep bare ids.
  for (const name of MONSTERS) emitFigure(name, name, RACE.human);

  const gearOut = [];
  for (const gname in GEAR) {
    const r = GEAR[gname](); if (!r) continue;
    gearOut.push({ name: gname, canvas: r.canvas });
    layout.gear[gname] = { pivot: r.pivot };
  }

  // B12 (corrected): the worn-armor part set — files + the layout.json `worn` inventory block
  const wornSet = buildWornSet();
  layout.worn = wornSet.inv;

  const minionOrder = ['skeleton','wisp','hound','golem','imp'];
  const minionsOut = minionOrder.map(name => ({ name, canvas: MIN[name]() }));

  function trimFlat(fig) {
    const comp = flatten(fig); const b = bbox(comp); if (b.empty) return renderRegion(comp,0,0,0,0);
    return renderRegion(comp, Math.max(0,b.minx-1), Math.max(0,b.miny-1), Math.min(fig.W-1,b.maxx+1), Math.min(fig.H-1,b.maxy+1));
  }
  return { figures: figuresOut, gear: gearOut, minions: minionsOut, layout,
    races: RACE, gearCatalog: GEAR_CATALOG, worn: wornSet.files,
    buildRaceFigure: (name, race) => ({ name, race: (race||RACE.human).id, flat: trimFlat(F[name](race||RACE.human)) }) };
}

// back-compat: flattened figures only (mockup builder + thumbnails)
function generateRoster(createCanvas) {
  return generateAll(createCanvas).figures.map(f => ({ name: f.name, canvas: f.flat }));
}
if (typeof module !== 'undefined') module.exports = { generateAll, generateRoster };
