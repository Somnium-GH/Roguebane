/* Roguebane — ASSET SHEET generator (persistent source of truth for design/00-assets-*.png).
 * Golden rule: never hand-build these review sheets. They are composited DIRECTLY from the live
 * files under Content/ (and the assembled composites under proto/roster/), so they can never drift
 * out of sync with the real asset package again.
 *
 * Run via run_script:
 *   const src = await readFile('proto/sheet_gen.js'); (0,eval)(src);
 *   await RB_generateSheets({readImage,createCanvas,saveFile,readFile,log}, 'ui');   // 'figures' | 'parts' | 'ui'
 * Each sheet is generated in its own call to stay well under the run_script time budget.
 *
 * To change WHAT appears: edit the SHEETS config below. To change the LOOK: edit C / cell sizing.
 * Sheets reflect reality — if an asset is added/removed/renamed in Content, just rerun the relevant sheet.
 */
globalThis.RB_generateSheets = async function (H, only) {
  const { readImage, createCanvas, saveFile, log } = H;

  // ---- palette / type ----
  const C = {
    bg: '#15100a', cell: '#1d1610', cellEdge: '#3a2c1d', rule: '#34281b',
    title: '#ece0cb', sub: '#7a674c', head: '#c9b48d', name: '#a99173', size: '#6f5c44',
    figLabel: '#bfa…'.slice(0, 0) + '#b9a079',
  };
  const W = 1120, M = 30, FONT_MONO = 'monospace';

  // ---- the three sheets ----
  const BODY = ['human_grunt', 'human_warden', 'human_barbarian', 'human_reaver', 'human_adept', 'human_summoner', 'wraith', 'skeleton', 'bandit', 'ogre', 'troll', 'gargoyle'];
  // race-morph comparison row (B17: Human/Elf/Dwarf/Halfling/Half-Giant — dwarf stout+short, halfling
  // small+swift, half-giant tall; barbarian shown across the morphs)
  const RACE_ROW = ['human_grunt', 'elf_grunt', 'dwarf_grunt', 'halfling_grunt', 'half_giant_grunt', 'human_barbarian', 'dwarf_barbarian', 'half_giant_barbarian'];
  const partOrder = (a, b) => key(a) - key(b);
  function key(fn) {
    const m = fn.replace('.png', '');
    const bases = ['head', 'torso', 'armL', 'armR', 'legL', 'legR', 'boots', 'back'];
    let bi = bases.findIndex(p => m.startsWith(p)); if (bi < 0) bi = 8;
    let rest = bi < 8 ? m.slice(bases[bi].length + 1) : m;
    const bare = rest.startsWith('bare'); if (bare) rest = rest.slice(4);
    const sr = { healthy: 0, damaged: 1, broken: 2 }[rest] ?? 9;
    return bi * 10 + (bare ? 3 : 0) + sr;
  }

  const SHEETS = {
    armor: {   // B12 (corrected 2026-07-04) worn-armor part set — curated samples (full set = 744 files; inventory = layout.json `worn`)
      file: 'design/00-assets-4-armor.png',
      title: 'Asset Sheet · 4 — Worn-Armor Part Set (B12 corrected)',
      sub: 'FULL part sprites, race-first · sprites/gear/worn/<race>/<slot>/[<core>/]<type>_<tier>_<cond> · bare = unarmored terminal (no "plain" type) · themed = favored line ONLY · fallback: themed→generic→bare',
      cw: 104, imgH: 100, gap: 9, pixel: true,
      sections: [
        { head: 'BARE — the unarmored fallback terminal (human) + conditions', root: 'Content/sprites/gear/worn/human/',
          files: ['head/bare_healthy.png','chest/bare_healthy.png','arms/bare_healthy.png','legs/bare_healthy.png','chest/bare_damaged.png','chest/bare_broken.png'] },
        { head: 'GENERIC STR (core-agnostic) — chest tiers 1→4 + slots @2', root: 'Content/sprites/gear/worn/human/',
          files: ['chest/str_1_healthy.png','chest/str_2_healthy.png','chest/str_3_healthy.png','chest/str_4_healthy.png','head/str_2_healthy.png','arms/str_2_healthy.png','legs/str_2_healthy.png'] },
        { head: 'GENERIC DEX (core-agnostic) — chest tiers 1→4 + slots @2', root: 'Content/sprites/gear/worn/human/',
          files: ['chest/dex_1_healthy.png','chest/dex_2_healthy.png','chest/dex_3_healthy.png','chest/dex_4_healthy.png','head/dex_2_healthy.png','arms/dex_2_healthy.png','legs/dex_2_healthy.png'] },
        { head: 'GENERIC INT (chest + head only, §6c) — chest tiers 1→4 + head @2', root: 'Content/sprites/gear/worn/human/',
          files: ['chest/int_1_healthy.png','chest/int_2_healthy.png','chest/int_3_healthy.png','chest/int_4_healthy.png','head/int_2_healthy.png','arms/bare_healthy.png'] },
        { head: 'THEMED STR — GRUNT (practical kit) vs WARDEN (edges/rivets/boss, visor helm) @2', root: 'Content/sprites/gear/worn/human/',
          files: ['chest/grunt/str_2_healthy.png','head/grunt/str_2_healthy.png','arms/grunt/str_2_healthy.png','chest/warden/str_2_healthy.png','head/warden/str_2_healthy.png','arms/warden/str_2_healthy.png','legs/warden/str_2_healthy.png'] },
        { head: 'THEMED DEX — REAVER (twin-blade etch, studs) vs RANGER (fur trim, quiver strap) @2', root: 'Content/sprites/gear/worn/human/',
          files: ['chest/reaver/dex_2_healthy.png','head/reaver/dex_2_healthy.png','arms/reaver/dex_2_healthy.png','chest/ranger/dex_2_healthy.png','head/ranger/dex_2_healthy.png','arms/ranger/dex_2_healthy.png','legs/ranger/dex_2_healthy.png'] },
        { head: 'THEMED INT — ADEPT (runic trim, restrained) vs SUMMONER (bone/chain, darker) @2', root: 'Content/sprites/gear/worn/human/',
          files: ['chest/adept/int_2_healthy.png','head/adept/int_2_healthy.png','chest/summoner/int_2_healthy.png','head/summoner/int_2_healthy.png','chest/adept/int_4_healthy.png','chest/summoner/int_4_healthy.png'] },
        { head: 'ELF — every slot authored per race (ears clear the headgear; slimmer chest)', root: 'Content/sprites/gear/worn/elf/',
          files: ['head/bare_healthy.png','chest/bare_healthy.png','head/warden/str_3_healthy.png','chest/ranger/dex_4_healthy.png','chest/summoner/int_3_healthy.png','arms/str_2_healthy.png'] },
        { head: 'CONDITIONS — healthy → damaged → broken (warden chest @2)', root: 'Content/sprites/gear/worn/human/',
          files: ['chest/warden/str_2_healthy.png','chest/warden/str_2_damaged.png','chest/warden/str_2_broken.png'] },
      ],
    },
    figures: {
      file: 'design/00-assets-1-figures.png',
      title: 'Asset Sheet · 1 — Figures, Cores, Minions, Gear',
      sub: 'ASSEMBLED COMPOSITES (proto/roster) · MINIONS · GEAR — flat 8-bit sprite tier',
      cw: 196, imgH: 200, gap: 14, pixel: true,
      sections: [
        { head: 'RACE MORPH — Human · Elf · Dwarf · Halfling · Half-Giant + Barbarian (B17: dwarf stout+short, halfling small+swift, half-giant tall)', root: 'proto/roster/',
          files: RACE_ROW.map(f => f + '.png') },
        { head: 'ASSEMBLED FIGURES — players + foes (proto/roster)', root: 'proto/roster/',
          files: BODY.map(f => f + '.png') },
        { head: 'MINIONS (Content/sprites/minions)', root: 'Content/sprites/minions/',
          files: ['skeleton.png', 'golem.png', 'hound.png', 'imp.png', 'wisp.png'] },
        { head: 'GEAR — WEAPONS & SHIELDS (Content/sprites/gear)', root: 'Content/sprites/gear/',
          files: ['sword.png', 'club.png', 'dagger.png', 'staff.png', 'round_shield.png', 'tower_shield.png'] },
        { head: 'B2-GO — WEAPON TYPES (Steel tier shown; Iron/Mithral/Dwarven Steel are palette swaps)', root: 'Content/sprites/gear/',
          files: ['longsword_steel.png', 'claymore_steel.png', 'axe_steel.png', 'battleaxe_steel.png', 'mace_steel.png', 'warhammer_steel.png', 'dagger_steel.png', 'rapier_steel.png', 'shortsword_steel.png'] },
        { head: 'B2-GO — NEW FAMILIES + SHIELD LADDER', root: 'Content/sprites/gear/',
          files: ['sling_braided.png', 'staff_ornate.png', 'charm_ornate.png', 'tome_ornate.png', 'wand_gemstone.png', 'shield_wooden.png', 'shield_buckler.png', 'shield_kite.png', 'shield_tower.png'] },
        { head: 'B11 — BOW LADDER + RANGED BACK-MOUNTS (slung art, pivots to sockets.back)', root: 'Content/sprites/gear/',
          files: ['bow_short.png', 'bow_long.png', 'bow_compound.png', 'bow_elven.png', 'bow_long_back.png', 'bow_elven_back.png', 'sling_braided_back.png', 'sling_giantsbane_back.png'] },
        { head: 'B2-GO — ARMOR ICONS (one tier per line shown: STR=Steel, DEX=Hardened, INT=Silk)', root: 'Content/sprites/gear/',
          files: ['armor_str_head_steel.png', 'armor_str_chest_steel.png', 'armor_str_arms_steel.png', 'armor_str_legs_steel.png', 'armor_dex_head_hardened.png', 'armor_dex_chest_hardened.png', 'armor_int_chest_silk.png', 'armor_int_head_silk.png'] },
      ],
    },
    parts: {
      file: 'design/00-assets-2-parts.png',
      title: 'Asset Sheet · 2 — Modular Body Parts',
      sub: 'EVERY decomposed part by figure · states: healthy → damaged → broken (grunt also carries bare/armored)',
      cw: 104, imgH: 100, gap: 9, pixel: true,
      sections: null, // built below from PARTS
    },
    ui: {
      file: 'design/00-assets-3-ui.png',
      title: 'Asset Sheet · 3 — Icons, UI, Targeting, Backdrops',
      sub: 'HIGH-DEF UI tier — captured from the live screens, rendered pixel-exact',
      cw: 168, imgH: 116, gap: 14, pixel: false,
      sections: [
        { head: 'ICONS · ATTRIBUTES (flat stat swatches)', root: 'Content/icons/attr/',
          files: ['strength.png', 'dexterity.png', 'constitution.png', 'intellect.png'], pixel: true },
        { head: 'ICONS · TECHNIQUES', root: 'Content/icons/technique/',
          files: ['swing.png', 'frenzy.png', 'brace.png', 'disarm.png', 'firebolt.png', 'shot.png'] },
        { head: 'ICONS · RUNES (tier-shaped: ◆ mark · ⬠ path_minor · ⬡ path_major · ⯃ keystone)', root: 'Content/icons/rune/',
          files: ['mark.png', 'path_minor.png', 'path_major.png', 'keystone.png'] },
        { head: 'ICONS · NODES (depth-preserved, transparent corners)', root: 'Content/icons/node/',
          files: ['camp.png', 'merchant.png', 'resource.png', 'castle.png', 'unknown.png', 'skirmish.png'] },
        { head: 'ICONS · RESOURCES', root: 'Content/icons/resource/',
          files: ['hp.png', 'spoils.png', 'supplies.png', 'support.png', 'charge.png'] },
        { head: 'UI · PIPS — fills (str/int/dex/con/supplies/support + generic)', root: 'Content/ui/pip/', cw: 120, imgH: 92, pixel: true,
          files: ['pip_full_str.png', 'pip_full_int.png', 'pip_full_dex.png', 'pip_full_con.png', 'pip_full_supplies.png', 'pip_full_support.png', 'pip_full.png'] },
        { head: 'UI · PIPS — reserved / empty / states', root: 'Content/ui/pip/', cw: 120, imgH: 92, pixel: true,
          files: ['pip_reserved_str.png', 'pip_reserved_int.png', 'pip_reserved_dex.png', 'pip_reserved_con.png', 'pip_reserved.png', 'pip_empty.png', 'pip_empty_supplies.png', 'pip_empty_support.png', 'pip_damage.png', 'pip_debuff.png'] },
        { head: 'UI · RETICLES & TARGETING', root: 'Content/ui/reticle/',
          files: ['aiming.png', 'focus.png', 'focus_p0.png', 'focus_p1.png', 'focus_p2.png', 'secondary.png'] },
        { head: 'UI · BUTTONS (states)', root: 'Content/ui/button/', cw: 196, imgH: 70,
          files: ['button_normal.png', 'button_hover.png', 'button_down.png', 'button_on.png', 'button_disabled.png'] },
        { head: 'BACKDROPS (EGA fantasy fields)', root: 'Content/bg/', cw: 256, imgH: 150,
          files: ['combat_field.png', 'build_alcove.png', 'map_chart.png', 'spine_road.png'] },
      ],
    },
  };

  // parts sheet: one section per figure — BASE parts only (worn armor layers have their own sheet 4;
  // 112 worn files per standard figure would swamp this one)
  SHEETS.parts.sections = [];
  for (const fig of BODY) {
    // discover this figure's files via the manifest-independent ls
    const root = 'Content/sprites/body/' + fig + '/';
    let files;
    try { files = (await H.ls('Content/sprites/body/' + fig)).filter(f => f.endsWith('.png') && !/_(str|dex|int)_/.test(f)); }
    catch (e) { files = []; }
    files.sort(partOrder);
    SHEETS.parts.sections.push({ head: fig.toUpperCase() + ' (' + files.length + ' parts)', root, files });
  }

  const sheet = SHEETS[only];
  if (!sheet) throw new Error('unknown sheet: ' + only);

  // preload images (collect sizes) — parallel batches to stay within the time budget
  const imgs = {};
  const paths = [];
  for (const sec of sheet.sections) for (const f of sec.files) paths.push(sec.root + f);
  const BATCH = 40;
  for (let i = 0; i < paths.length; i += BATCH) {
    const slice = paths.slice(i, i + BATCH);
    await Promise.all(slice.map(async p => {
      try { imgs[p] = await readImage(p); } catch (e) { imgs[p] = null; log('MISSING ' + p); }
    }));
  }

  // ---- layout (measure → draw) ----
  function layout(ctx) {
    let y = M;
    if (ctx) {
      ctx.fillStyle = C.title; ctx.font = '600 30px Georgia, serif'; ctx.textAlign = 'left'; ctx.textBaseline = 'alphabetic';
      ctx.fillText(sheet.title, M, y + 26);
      ctx.fillStyle = C.sub; ctx.font = '12px ' + FONT_MONO;
      ctx.fillText(sheet.sub, M, y + 48);
    }
    y += 78;

    for (const sec of sheet.sections) {
      const cw = sec.cw || sheet.cw, imgH = sec.imgH || sheet.imgH, gap = sec.gap || sheet.gap;
      const pixel = sec.pixel != null ? sec.pixel : sheet.pixel;
      const cellH = imgH + 34;
      // header + rule
      if (ctx) {
        ctx.fillStyle = C.head; ctx.font = '12px ' + FONT_MONO; ctx.textAlign = 'left';
        const t = sec.head.toUpperCase(); ctx.fillText(t, M, y + 11);
        const tw = ctx.measureText(t).width;
        ctx.strokeStyle = C.rule; ctx.lineWidth = 1;
        ctx.beginPath(); ctx.moveTo(M + tw + 14, y + 7.5); ctx.lineTo(W - M, y + 7.5); ctx.stroke();
      }
      y += 24;
      // cells, wrapping
      const cols = Math.max(1, Math.floor((W - 2 * M + gap) / (cw + gap)));
      let i = 0;
      while (i < sec.files.length) {
        const rowN = Math.min(cols, sec.files.length - i);
        for (let c = 0; c < rowN; c++) {
          const f = sec.files[i + c];
          const x = M + c * (cw + gap);
          if (ctx) drawCell(ctx, x, y, cw, imgH, imgs[sec.root + f], f.replace('.png', ''), sec.root + f, pixel);
        }
        i += rowN; y += cellH;
      }
      y += 16;
    }
    return y + M - 16;
  }

  function drawCell(ctx, x, y, cw, imgH, img, label, path, pixel) {
    ctx.fillStyle = C.cell; ctx.fillRect(x, y, cw, imgH);
    ctx.strokeStyle = C.cellEdge; ctx.lineWidth = 1; ctx.strokeRect(x + 0.5, y + 0.5, cw - 1, imgH - 1);
    if (img) {
      const padW = cw - 18, padH = imgH - 18;
      const s = Math.min(padW / img.width, padH / img.height, 1);
      const dw = Math.round(img.width * s), dh = Math.round(img.height * s);
      ctx.imageSmoothingEnabled = !pixel;
      ctx.drawImage(img, Math.round(x + (cw - dw) / 2), Math.round(y + (imgH - dh) / 2), dw, dh);
    }
    ctx.textAlign = 'center';
    ctx.fillStyle = C.name; ctx.font = '11px ' + FONT_MONO;
    ctx.fillText(label, x + cw / 2, y + imgH + 15);
    if (img) { ctx.fillStyle = C.size; ctx.font = '10px ' + FONT_MONO; ctx.fillText(img.width + '×' + img.height, x + cw / 2, y + imgH + 28); }
    ctx.textAlign = 'left';
  }

  const Htot = Math.ceil(layout(null));
  const canvas = createCanvas(W, Htot);
  const ctx = canvas.getContext('2d');
  ctx.fillStyle = C.bg; ctx.fillRect(0, 0, W, Htot);
  layout(ctx);
  await saveFile(sheet.file, canvas);
  log('wrote ' + sheet.file + '  ' + W + '×' + Htot);
};
