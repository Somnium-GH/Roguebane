// Roguebane — per-core PROTOTYPE kit data + the DETERMINISTIC gear/reservation model (balance v6, T1).
// ONE source of truth for the Encounter, Equipment AND NewGame screens, so no number, cost, state,
// rules-text, race stat or core stat-bonus can drift between them. Everything below is a pure function
// of this data + the per-core run scenario — same data ⇒ identical screens (LAYOUT_CONTRACT §4).
//
// STATUS: PROTOTYPE Core Effects (supersede §11 canon — payload B15) tuned to the **v6 balance sheet**
// (2026-07-05 session). v6 is the number gospel: attributes, reserve costs, default loadouts.
//
// ================================ DETERMINISTIC REQUIREMENTS (v6, written down) =====================
// RACES (baseline 4/4/4/4; Human +1 across = breadth, specialists +2 into one affinity = depth):
//   Human 5/5/5/5 · Elf 4/6/4/4 (INT) · Dwarf 4/4/4/6 (CON) · Halfling 4/4/6/4 (DEX).
//   HP = 10 + 2×race CON (§7 "CON adds +2 HP/pt"): Human 20 · Elf 18 · Dwarf 22 · Halfling 18.
// CORE STAT BONUSES (payload B16, additive on the race base; effective pool = race + core):
//   Grunt +1 all · Warden +5 CON · Adept +5 INT · Summoner +3 INT +2 CON · Reaver +5 DEX ·
//   Ranger +4 DEX +1 CON.
//
// STARTER GEAR + KITS (v6 §C, all T1):
//   Grunt    — Iron Longsword + Wooden Shield; Iron plate ×4 · Jab, Brace, Bandage · no minions.
//   Warden   — Iron Longsword + Iron Buckler; Iron plate ×4 (CON via Fortified) · Jab, Brace, Bandage.
//   Adept    — Wooden Staff; Cotton Robe + Cloth Cap · Ember, Siphon, Stoneskin.
//   Summoner — Adept Wand + Wooden Charm; Cotton Robe + Cloth Cap · Ember, Sacrifice, Barkskin · Skeleton.
//   Reaver   — 2× Iron Dagger; Leather ×4 · Frenzy, Flurry (no heal — glass cannon).
//   Ranger   — Iron Dagger + Short Bow; Leather ×4 · Aimed Shot, Lunge, Bandage · Hound.
//
// CORE EFFECT DISCOUNTS (v6 §A — applied by the resolver, costs below are BASE sheet costs):
//   Grunt "Jack of All Trades" — every attribute cost −1.
//   Warden "Fortified"         — plate (STR-line) armor is paid in CON at −1 per tier.
//   Barbarian "Warlord's Might"— two-handed swords (claymore) cost 2 less STR to equip.
//   Reaver "Finesse"           — techniques requiring two weapons cost −1.
//   Ranger "Fletcher's Luck"   — bows cost −1 per tier to equip (the 20% no-charge roll is engine RNG).
//
// RESERVATION MODEL (v6 §C: "Requirement = the fully-active reserve demand in each stat
// (armor + weapons + skills + minions), effect discounts applied" — so ARMOR RESERVES POOL PIPS now;
// this supersedes the earlier threshold-only reading, closing the DROP_AUDIT Doug call):
//   • ALL gear (weapons + shield object + worn armor) makes a STANDING reservation against its gate
//     stat — hatched pips from the LEFT (−45°), opposite the damage/debuff hatch (+45°, from the right).
//   • Physical gates first: a weapon in a BROKEN arm can never be held; armor whose own covered part
//     BROKE disables. Then the §6e cascade when a pool shrinks: highest effective requirement first,
//     ties last-equipped-first.
//   • Techniques do NOT reserve on the EQUIPMENT screen — only while ACTIVE on ENCOUNTER (HELD /
//     TARGETING / CHARGING reserve; READY / COOLDOWN don't). Unpayable → LOCKED; a minion whose gate
//     can't be sustained → IDLE (starved, §9).
//   • Utilization notes (v6 reqs vs pools): Human fits every core EXCEPT Ranger (req DEX 10 vs pool 9 —
//     1 short at full activation; Halfling fits it exactly). Warden/Adept/Summoner fit Human exactly.

// ---- shared atoms ------------------------------------------------------------------------------
export const ATTR = { STR: '#c2553f', INT: '#6f8fc4', DEX: '#82a85e', CON: '#cf9a44' };
export const PART = { STR: 'Arms', INT: 'Head', DEX: 'Legs', CON: 'Chest' };
export const KEYS = ['STR', 'INT', 'DEX', 'CON'];
export const CHARGE = '#c9a24a';                          // bow Charge-resource accent
export const ORDER = ['grunt', 'warden', 'adept', 'summoner', 'reaver', 'ranger', 'barbarian'];

// ---- RACES (v6 §B; blurbs/tags are SAMPLE copy — final wording is Doug's, payload B17) -----------
export const RACE_ORDER = ['human', 'elf', 'dwarf', 'halfling', 'half_giant'];
export const RACES = {
  human:    { id: 'human',    name: 'Human',    tag: 'THE FOUNDER LINE',    attrs: { STR: 5, INT: 5, DEX: 5, CON: 5 },
              blurb: 'No innate edge or lack — runs almost any core it can afford, but always tight.' },
  elf:      { id: 'elf',      name: 'Elf',      tag: 'THE KEEN',            attrs: { STR: 4, INT: 6, DEX: 4, CON: 4 },
              blurb: 'Keen beyond mortal measure, but frail — born to the casting lines.' },
  dwarf:    { id: 'dwarf',    name: 'Dwarf',    tag: 'THE STOUT & STEADFAST', attrs: { STR: 4, INT: 4, DEX: 4, CON: 6 },
              blurb: 'Stout and enduring — shrugs off blows that would fell another line.' },
  halfling: { id: 'halfling', name: 'Halfling', tag: 'THE SMALL & SWIFT',   attrs: { STR: 4, INT: 4, DEX: 6, CON: 4 },
              blurb: 'Small and quick — hard to hit, harder to pin down.' },
  half_giant: { id: 'half_giant', name: 'Half-Giant', tag: 'THE TOWERING', attrs: { STR: 6, INT: 4, DEX: 4, CON: 4 },
              blurb: 'Head and shoulders above the field — raw strength in a towering frame.' },
};
export const raceHp = (raceId) => 10 + 2 * (RACES[raceId] || RACES.human).attrs.CON;

// ---- CORE STAT BONUSES (payload B16) -------------------------------------------------------------
export const CORE_BONUS = {
  grunt:    { STR: 1, INT: 1, DEX: 1, CON: 1 },
  warden:   { CON: 5 },
  adept:    { INT: 5 },
  summoner: { INT: 3, CON: 2 },
  reaver:   { DEX: 5 },
  ranger:   { DEX: 4, CON: 1 },
  barbarian: { STR: 4, CON: 1 },   // payload B16
};
// effective pool per stat = race base + core bonus
export function pools(coreId, raceId) {
  const race = RACES[raceId] || RACES.human, bonus = CORE_BONUS[coreId] || {};
  const p = {}; KEYS.forEach(k => { p[k] = race.attrs[k] + (bonus[k] || 0); });
  return p;
}
// B16 statBonus datum — TWO forms:
// statBonus(): compact chips (Equipment identity block) — a uniform all-stat bonus collapses to ONE
//   "+N ALL" chip in parchment so the row never wraps (Doug 2026-07-05).
// statBonusFull(): one entry per non-zero stat, NO collapse (the NewGame core-rune cards — Doug:
//   "for grunt put all 4 instead of summarizing"; rendered in the exact race-card attr-box idiom).
export function statBonus(coreId) {
  const bonus = CORE_BONUS[coreId] || {};
  const vals = KEYS.map(k => bonus[k] || 0);
  if (vals[0] && vals.every(v => v === vals[0])) return [{ key: 'ALL', plus: vals[0], color: '#cdb497' }];
  return KEYS.filter(k => bonus[k]).map(k => ({ key: k, plus: bonus[k], color: ATTR[k] }));
}
export function statBonusFull(coreId) {
  const bonus = CORE_BONUS[coreId] || {};
  return KEYS.filter(k => bonus[k]).map(k => ({ key: k, plus: bonus[k], color: ATTR[k] }));
}
// AGGREGATION NOTE (Doug 2026-07-05): a race+core aggregated chip row ("+2 ALL") was tried on the
// Equipment identity block and REVERTED same-session — the identity block shows the CORE's own bonus
// only; the pools/preview tiles are where race + core get added up. Don't reintroduce.

// pip fill patterns
export const HASH = {
  dmg:  'repeating-linear-gradient(45deg,#b23b3288 0 3px,transparent 3px 6px)',   // damage — from the RIGHT, +45°
  deb:  'repeating-linear-gradient(45deg,#d9a44188 0 3px,transparent 3px 6px)',   // debuff — from the RIGHT, +45°
  gear: (c) => 'repeating-linear-gradient(-45deg,#0a0807aa 0 3px,transparent 3px 6px),' + c, // gear — from the LEFT, −45°
};

// which figure part carries each attribute (paired parts split the stat), + the two arm sockets
const ATTR_PARTS = { STR: ['armL', 'armR'], INT: ['head'], DEX: ['legL', 'legR'], CON: ['torso'] };
// which figure part(s) an armor SLOT covers (for the "own part broke" disable)
const SLOT_PARTS = { head: ['head'], torso: ['torso'], arms: ['armL', 'armR'], legs: ['legL', 'legR'] };
const SOCKET_ARM = { handL: 'armL', handR: 'armR', ranged: 'armL' };
// tier token → tier number (for the per-tier effect discounts)
const TIER_NUM = { iron: 1, steel: 2, mithral: 3, dwarven: 4, plain: 1, hardened: 2, studded: 3, reinforced: 4,
  cotton: 1, silk: 2, ornate: 3, humming: 4, wooden: 1, twisted: 2, adept: 1, gemstone: 3, glowing: 4,
  oldworn: 1, leather: 2, bone: 2, short: 1, long: 2, compound: 3, elven: 4, t1: 1, t2: 2, t3: 3, t4: 4 };

// ---- gear factory ------------------------------------------------------------------------------
// kind: 'weapon' | 'shield' | 'armor' — ALL reserve pool pips (v6). socket: hand for weapons/shield.
const W = (id, name, attr, cost, socket, tier, rarity) => ({ id, name, attr, kind: 'weapon', socket, cost, tier, rarity });
const SH = (id, name, cost, socket, tier, rarity) => ({ id, name, attr: 'CON', kind: 'shield', socket, cost, tier, rarity });
const AR = (id, name, attr, part, cost, tier, rarity) => ({ id, name, attr, kind: 'armor', part, socket: null, cost, tier, rarity });

// four Iron-plate pieces (STR line, 2 reserve each — v6 §D)
const IRON_PLATE = [
  AR('armor_str_head_iron',  'Iron Helm',        'STR', 'head',  2, 'iron', 'COMMON'),
  AR('armor_str_chest_iron', 'Iron Breastplate', 'STR', 'torso', 2, 'iron', 'MAGIC'),
  AR('armor_str_arms_iron',  'Iron Vambraces',   'STR', 'arms',  2, 'iron', 'COMMON'),
  AR('armor_str_legs_iron',  'Iron Greaves',     'STR', 'legs',  2, 'iron', 'COMMON'),
];
// four Plain-leather pieces (DEX line, 1 each — v6 §D)
const PLAIN_LEATHER = [
  AR('armor_dex_head_plain',  'Leather Cap',      'DEX', 'head',  1, 'plain', 'COMMON'),
  AR('armor_dex_chest_plain', 'Padded Armor',     'DEX', 'torso', 1, 'plain', 'COMMON'),
  AR('armor_dex_arms_plain',  'Leather Bracers',  'DEX', 'arms',  1, 'plain', 'COMMON'),
  AR('armor_dex_legs_plain',  'Leather Leggings', 'DEX', 'legs',  1, 'plain', 'COMMON'),
];
// robe (INT line, chest 2 + head 1 — v6 §D)
const ROBE = [
  AR('armor_int_chest_cotton', 'Cotton Robe', 'INT', 'torso', 2, 'cotton', 'COMMON'),
  AR('armor_int_head_cotton',  'Cloth Cap',   'INT', 'head',  1, 'cotton', 'COMMON'),
];

// ---- shared technique defs (v6 §D BASE costs — the resolver applies effect discounts) ------------
const T = {
  jab:       { name: 'Jab',        attr: 'STR', cost: 1, glyph: '⚔', needs: 'melee',      desc: 'A quick, light strike.' },
  cleave:    { name: 'Cleave',     attr: 'STR', cost: 2, glyph: '⚒', needs: 'melee',      desc: 'A heavy, committed arc; one big hit to break through.' },
  lunge:     { name: 'Lunge',      attr: 'DEX', cost: 1, glyph: '➤', needs: 'melee',      desc: 'A darting thrust; charge-free DEX damage.' },
  frenzy:    { name: 'Frenzy',     attr: 'STR', either: ['STR', 'DEX'], cost: 3, glyph: '⇶', needs: 'twoWeapons', desc: 'Both blades in three wild arcs — paid in STR or DEX. (3 → 2 with Finesse)' },
  flurry:    { name: 'Flurry',     attr: 'STR', either: ['STR', 'DEX'], cost: 2, glyph: '⇉', needs: 'twoWeapons', desc: 'A fast dual-wield flurry — paid in STR or DEX. (2 → 1 with Finesse)' },
  aimedshot: { name: 'Aimed Shot', attr: 'DEX', cost: 2, glyph: '➶', needs: 'bow', charge: 1, desc: 'A slow, heavy bow shot; pierces the shield pool.' },
  ember:     { name: 'Ember',      attr: 'INT', cost: 1, glyph: '✦', desc: 'A fast fire bolt; a targeted hit feeds Resonance.' },
  siphon:    { name: 'Siphon',     attr: 'INT', cost: 2, glyph: '◉', desc: 'A draining bolt; a landed part-hit heals you for the damage dealt.' },
  barkskin:  { name: 'Barkskin',   attr: 'INT', cost: 1, glyph: '❦', desc: 'A lesser INT ward; pool 2, refills a pip every 3.0s.' },
  stoneskin: { name: 'Stoneskin',  attr: 'INT', cost: 2, glyph: '❄', desc: 'An INT ward; pool 3, refills a pip every 3.0s.' },
  brace:     { name: 'Brace',      attr: 'CON', cost: 2, glyph: '◈', needs: 'shield',     desc: 'Raise your shield and turn each blow aside; pool 4, refills a pip every 2.0s.' },
  bandage:   { name: 'Bandage',    attr: 'CON', cost: 2, glyph: '✚', desc: 'Mend one of your damaged parts, chosen randomly each pass.' },
  bind:      { name: 'Bind',       attr: 'STR', cost: 2, glyph: '⛓', desc: 'A ward of raw sinew and will — a STR shield source; pool 3, refills a pip every 2.5s.' },
  sacrifice: { name: 'Sacrifice',  attr: '—',   cost: 0, glyph: '❖', costLabel: '1 MINION', desc: 'Consume one of your minions to mend your body.' },
};
const tk = (key, intent) => ({ ...T[key], intent });
// minions (v6 §D: Skeleton 1 · Iron Golem 3 (sheet) · Hound 1)
const M = {
  skeleton: { name: 'Skeleton',   attr: 'INT', cost: 1, desc: 'A fast, frail thrall; strikes every 3.0s.' },
  golem:    { name: 'Iron Golem', attr: 'INT', cost: 3, desc: 'A slow, iron-treated thrall; hits hard every 5.0s.' },
  hound:    { name: 'Hound',      attr: 'DEX', cost: 1, desc: 'A swift pet; nips every 4.0s and sharpens your aim (+5%) while active.' },
};
const mk = (key, intent) => ({ ...M[key], intent });

// ---- the six cores (v6 §C defaults; badge/blurb feed the NewGame cards) --------------------------
export const CORES = {
  grunt: {
    id: 'grunt', cls: 'Grunt', role: 'THE GENERALIST', badge: 'STARTER', accent: '#7fa05a',
    figure: 'grunt', budget: 20,
    effect: { name: 'Jack of All Trades', rules: 'Every attribute cost you pay is reduced by 1.' },
    blurb: 'No edge, no hole — its −1 to every cost shines through what you bolt on.',
    // SCENARIO — both arms grazed (STR −3). The plate cascade sheds the greaves + vambraces to fit
    // the shrunken pool; the sword holds, and the discounted Jab — mid-aim at the foe (TARGETING, so
    // the default render exercises the §8d aim-tag wire) — is still free to swing.
    scenario: { damage: { STR: 3 }, debuff: {}, figStates: { armL: 'damaged', armR: 'damaged' } },
    gear: [
      W('longsword_iron', 'Iron Longsword', 'STR', 2, 'handL', 'iron', 'MAGIC'),
      SH('shield_wooden', 'Wooden Shield', 1, 'handR', 't1', 'COMMON'),
      ...IRON_PLATE,
    ],
    techCap: 4,
    techniques: [tk('jab', 'TARGETING'), tk('brace', 'HELD'), tk('bandage', 'COOLDOWN')],
    bayCap: 2, bays: [],
    finds: {
      gear: [
        AR('armor_str_head_steel', 'Steel Helm', 'STR', 'head', 2, 'steel', 'RARE'),
        W('mace_iron', 'Iron Mace', 'STR', 3, 'handL', 'iron', 'COMMON'),
        SH('shield_buckler', 'Iron Buckler', 2, 'handR', 't2', 'MAGIC'),
      ],
      tech: [{ ...T.cleave }, { ...T.stoneskin }],
      minions: [{ ...M.skeleton }],
    },
  },

  warden: {
    id: 'warden', cls: 'Warden', role: 'THE WALL', badge: 'BULWARK', accent: '#cf9a44',
    figure: 'warden', budget: 18,
    effect: { name: 'Fortified', rules: 'Plate armor is paid in CON at 1 less per tier.' },
    blurb: 'Plate drawn from CON at a discount — soaks blows and holds the line.',
    // SCENARIO — chest caved in (CON −4, torso broken). The breastplate disables with its part; the
    // rest of the fortified rig still fits, but both CON techniques lock — the Wall can't ward or mend.
    scenario: { damage: { CON: 4 }, debuff: {}, figStates: { torso: 'broken' } },
    gear: [
      W('longsword_iron', 'Iron Longsword', 'STR', 2, 'handL', 'iron', 'MAGIC'),
      SH('shield_buckler', 'Iron Buckler', 2, 'handR', 't2', 'MAGIC'),
      ...IRON_PLATE,
    ],
    techCap: 4,
    techniques: [tk('jab', 'READY'), tk('brace', 'HELD'), tk('bandage', 'COOLDOWN')],
    bayCap: 1, bays: [],
    finds: {
      gear: [
        AR('armor_str_chest_steel', 'Steel Breastplate', 'STR', 'torso', 2, 'steel', 'RARE'),
        SH('shield_kite', 'Kite Shield', 3, 'handR', 't3', 'RARE'),
      ],
      tech: [{ ...T.cleave }],
      minions: [{ ...M.skeleton }],
    },
  },

  adept: {
    id: 'adept', cls: 'Adept', role: 'THE SCHOLAR', badge: 'CASTER', accent: '#6f8fc4',
    figure: 'adept', budget: 16,
    effect: { name: 'Resonance', rules: 'Each targeted spell that hits reduces its next charge time by 2%, stacking up to 5 times.' },
    blurb: 'All-INT, no CON tax — spells that heal as they build Resonance.',
    // SCENARIO — head grazed (INT −1). Ember + Siphon still cast, but the Stoneskin ward collapses:
    // an all-INT kit that fits the Human pool EXACTLY when healthy loses its ward first.
    scenario: { damage: { INT: 1 }, debuff: {}, figStates: { head: 'damaged' } },
    gear: [
      W('staff_wooden', 'Wooden Staff', 'INT', 2, 'handR', 'wooden', 'COMMON'),
      ...ROBE,
    ],
    techCap: 4,
    techniques: [tk('ember', 'TARGETING'), tk('siphon', 'CHARGING'), tk('stoneskin', 'HELD')],
    bayCap: 1, bays: [],
    finds: {
      gear: [
        AR('armor_int_head_silk', 'Silk Hood', 'INT', 'head', 1, 'silk', 'MAGIC'),
        W('wand_adept', 'Adept Wand', 'INT', 1, 'handL', 'adept', 'MAGIC'),
      ],
      tech: [{ ...T.barkskin }, { ...T.bandage }],
      minions: [{ ...M.skeleton }],
    },
  },

  summoner: {
    id: 'summoner', cls: 'Summoner', role: 'THE BINDER', badge: 'SPECIALIST', accent: '#9a78b0',
    figure: 'summoner', budget: 17,
    effect: { name: 'Conscription', rules: 'Minions do not consume Summons when activated.' },
    blurb: 'Starts with a thrall and open bays — its Summons are never spent.',
    // SCENARIO — head grazed (INT −2). Wand, charm and the Barkskin ward all still fit, but the last
    // free INT pip is gone: the Skeleton's gate can't be sustained and it drops to IDLE (starved, §9).
    scenario: { damage: { INT: 2 }, debuff: {}, figStates: { head: 'damaged' } },
    gear: [
      W('wand_adept', 'Adept Wand', 'INT', 1, 'handL', 'adept', 'MAGIC'),
      W('charm_wooden', 'Wooden Charm', 'INT', 1, 'handR', 'wooden', 'COMMON'),
      ...ROBE,
    ],
    techCap: 3,
    techniques: [tk('ember', 'READY'), tk('sacrifice', 'READY'), tk('barkskin', 'HELD')],
    bayCap: 3,
    bays: [mk('skeleton', 'ACTIVE')],
    finds: {
      gear: [
        W('tome_oldworn', 'Old Worn Tome', 'INT', 1, 'handR', 'oldworn', 'COMMON'),
      ],
      tech: [{ ...T.siphon }, { ...T.bandage }],
      minions: [{ ...M.golem }, { ...M.hound }],
    },
  },

  reaver: {
    id: 'reaver', cls: 'Reaver', role: 'THE DUELIST', badge: 'SPECIALIST', accent: '#c2553f',
    figure: 'reaver', budget: 19,
    effect: { name: 'Finesse', rules: 'Techniques requiring two weapons cost 1 less to activate.' },
    blurb: 'Twin blades, no shield, no heal — pure offense, cheaper in pairs.',
    // SCENARIO — one leg broken (DEX −3). The leggings disable with the part; both daggers still fit,
    // and the Finesse-discounted pair techniques keep swinging — the glass cannon fights on, unhealed.
    scenario: { damage: { DEX: 3 }, debuff: {}, figStates: { legL: 'broken' } },
    gear: [
      W('dagger_iron', 'Iron Dagger', 'DEX', 1, 'handL', 'iron', 'COMMON'),
      W('dagger_iron', 'Iron Dagger', 'DEX', 1, 'handR', 'iron', 'COMMON'),
      ...PLAIN_LEATHER,
    ],
    techCap: 4,
    techniques: [tk('frenzy', 'READY'), tk('flurry', 'READY')],
    bayCap: 0, bays: [],
    finds: {
      gear: [
        W('rapier_iron', 'Iron Rapier', 'DEX', 2, 'handL', 'iron', 'MAGIC'),
        AR('armor_dex_chest_hardened', 'Leather Armor', 'DEX', 'torso', 1, 'hardened', 'MAGIC'),
      ],
      tech: [{ ...T.lunge }, { ...T.bandage }],
      minions: [{ ...M.hound }],
    },
  },

  ranger: {
    id: 'ranger', cls: 'Ranger', role: 'THE MARKSMAN', badge: 'SPECIALIST', accent: '#82a85e',
    figure: 'ranger', budget: 18,
    effect: { name: "Fletcher's Luck", rules: 'Bow techniques have a 20% chance to consume no charge when fired, and bows cost 1 less per tier to equip.' },
    blurb: 'A steady draw at range — a fifth of shots cost no charge at all.',
    // SCENARIO — a leg grazed (DEX −2). The full kit needs DEX 10 and even a healthy Human has 9, so
    // the heavy Aimed Shot lapses first; the dagger Lunge and the Hound's gate still hold.
    scenario: { damage: { DEX: 2 }, debuff: {}, figStates: { legL: 'damaged' } },
    gear: [
      W('dagger_iron', 'Iron Dagger', 'DEX', 1, 'handR', 'iron', 'COMMON'),
      W('bow_short', 'Short Bow', 'DEX', 2, 'ranged', 'short', 'MAGIC'),
      ...PLAIN_LEATHER,
    ],
    techCap: 4,
    techniques: [tk('aimedshot', 'CHARGING'), tk('lunge', 'READY'), tk('bandage', 'COOLDOWN')],
    bayCap: 2,
    bays: [mk('hound', 'ACTIVE')],
    finds: {
      gear: [
        W('bow_long', 'Long Bow', 'DEX', 2, 'ranged', 'long', 'RARE'),
        AR('armor_dex_legs_hardened', 'Hardened Leggings', 'DEX', 'legs', 1, 'hardened', 'MAGIC'),
      ],
      tech: [{ ...T.stoneskin }],
      minions: [{ ...M.skeleton }],
    },
  },

  barbarian: {
    id: 'barbarian', cls: 'Barbarian', role: 'THE WARLORD', badge: 'SPECIALIST', accent: '#cf7a44',
    figure: 'barbarian', budget: 14,
    effect: { name: "Warlord's Might", rules: 'Two-handed swords cost 2 less strength to equip.' },
    blurb: 'A greatsword swung one-handed — Warlord\u2019s Might makes the claymore all but free.',
    // SCENARIO — the sword-arm is BROKEN. A 2H claymore can't be held in a broken arm, so it drops off
    // the paper-doll and Cleave (needs a melee weapon) LOCKS — but the raw-STR Bind ward still holds.
    scenario: { damage: {}, debuff: {}, figStates: { armL: 'broken' } },
    gear: [
      W('claymore_iron', 'Iron Claymore', 'STR', 3, 'handL', 'iron', 'MAGIC'),
      ...IRON_PLATE,
    ],
    techCap: 3,
    techniques: [tk('cleave', 'READY'), tk('bind', 'HELD'), tk('bandage', 'COOLDOWN')],
    bayCap: 1, bays: [],
    finds: {
      gear: [
        W('claymore_steel', 'Steel Claymore', 'STR', 3, 'handL', 'steel', 'RARE'),
        AR('armor_str_chest_steel', 'Steel Breastplate', 'STR', 'torso', 2, 'steel', 'RARE'),
      ],
      tech: [{ ...T.jab }, { ...T.brace }],
      minions: [{ ...M.skeleton }],
    },
  },
};

// ---- Encounter technique-card styling per state ------------------------------------------------
export const STATE_STYLE = {
  READY:     { border: '#7fa05a', bg: '#1c140d', stateColor: '#7fa05a', barColor: '#7fa05a', pct: '100%', time: 'ready',           op: '1',   borderStyle: 'solid',  pulse: false },
  HELD:      { border: '#cf9a44', bg: '#221a0e', stateColor: '#cf9a44', barColor: '#cf9a44', pct: '100%', time: 'holding',         op: '1',   borderStyle: 'solid',  pulse: false },
  TARGETING: { border: '#c2553f', bg: '#20140f', stateColor: '#e0654b', barColor: '#c2553f', pct: '65%',  time: 'charging · 0.5s', op: '1',   borderStyle: 'solid',  pulse: true  },
  CHARGING:  { border: '#5c6f92', bg: '#161a24', stateColor: '#9fb4d6', barColor: '#6f8fc4', pct: '55%',  time: 'charging · 1.2s', op: '1',   borderStyle: 'solid',  pulse: false },
  COOLDOWN:  { border: '#3a3328', bg: '#1c140d', stateColor: '#a07a6e', barColor: '#7a5a52', pct: '35%',  time: '1.8s',            op: '1',   borderStyle: 'solid',  pulse: false },
  LOCKED:    { border: '#56535f', bg: 'repeating-linear-gradient(45deg,#1a140d 0 7px,#15100b 7px 14px)', stateColor: '#8a8694', barColor: '#46434e', pct: '0%', time: '', op: '.82', borderStyle: 'dashed', pulse: false },
};
export const BAY_STYLE = {
  ACTIVE:  { border: '#4f9a8a', bg: '#142019', stateColor: '#7fc4b0', op: '1'  },
  IDLE:    { border: '#8a5a4a', bg: '#1f150f', stateColor: '#d0744a', op: '.82' }, // starved — gate stat can't be paid
};

// ---- reservation states → row card colours (§6e ONE family) ------------------------------------
export const CARD = {
  equipped:   { border: '#7fa05a', bg: '#1d2417' },
  disabled:   { border: '#b0473a', bg: '#271211' },
  equippable: { border: '#5a4030', bg: '#1f160d' },
  locked:     { border: '#4a382a', bg: '#1a130d' },
};
export const CARD_STATE = {
  equipped:   { label: '✓ EQUIPPED',   color: '#7fa05a' },
  disabled:   { label: '✕ DISABLED',   color: '#d0744a' },
  equippable: { label: 'EQUIPPABLE',   color: '#d9a441' },
  locked:     { label: 'LOCKED',       color: '#8a8694' },
};

// ================================ helpers =======================================================
export function get(id) { return CORES[id] || CORES.grunt; }
export function figureId(coreId, raceId) { return (RACES[raceId] ? raceId : 'human') + '_' + (CORES[coreId] ? coreId : 'grunt'); }

// -------- v6 effect discounts (see header) --------------------------------------------------------
// gearGate → the EFFECTIVE equip gate {attr, cost} for a piece on this core.
export function gearGate(coreId, piece) {
  let attr = piece.attr, cost = piece.cost;
  if (coreId === 'warden' && piece.kind === 'armor' && piece.attr === 'STR') {   // Fortified: plate → CON, −1/tier
    attr = 'CON'; cost = Math.max(0, cost - (TIER_NUM[piece.tier] || 1));
  }
  if (coreId === 'ranger' && /^bow_/.test(piece.id)) cost = Math.max(0, cost - (TIER_NUM[piece.tier] || 1)); // Fletcher's Luck
  if (coreId === 'barbarian' && /^claymore/.test(piece.id)) cost = Math.max(0, cost - 2);   // Warlord's Might (2H sword)
  if (coreId === 'grunt' && ATTR[attr]) cost = Math.max(0, cost - 1);            // Jack of All Trades
  return { attr, cost };
}
export function techCost(coreId, t) {
  if (!ATTR[t.attr]) return t.cost;
  let c = t.cost;
  if (coreId === 'reaver' && t.needs === 'twoWeapons') c = Math.max(0, c - 1);   // Finesse
  if (coreId === 'grunt') c = Math.max(0, c - 1);                                 // Jack of All Trades
  return c;
}
export function bayCost(coreId, b) {
  return (coreId === 'grunt' && ATTR[b.attr]) ? Math.max(0, b.cost - 1) : b.cost;
}

export function costLabel(t) {
  if (t.costLabel) return t.costLabel;
  if (t.attr === '—') return '—';
  if (t.either) return t.either.join('/') + ' ' + t.cost;   // dual-attr: pay in either pool
  const base = t.attr + ' ' + t.cost;
  return t.charge ? base + ' · CHG ' + t.charge : base;
}
export function costColor(t) {
  if (t.attr === '—' || t.costLabel) return CHARGE;
  return ATTR[t.attr] || '#9a8468';
}
// Split-fill background for a technique's glyph chip: a solid stat colour normally, but a dual-attr
// ("pay in either") technique gets a top/bottom split — either[0] on top, either[1] on the bottom,
// parted by a 2px black seam (the same 1px-black-border language as the pips/parts). STR-red over
// DEX-green for Frenzy/Flurry.
export function glyphFill(t) {
  if (t.either) return 'linear-gradient(' + ATTR[t.either[0]] + ' 0 50%,' + ATTR[t.either[1]] + ' 50% 100%)';
  return ATTR[t.attr] || '#6a5a48';
}
// Two-row cost readout for a dual-attr technique (else null): [{attr,cost,color} top, … bottom].
export function costSplit(t) {
  if (!t.either) return null;
  return t.either.map(a => ({ attr: a, cost: String(t.cost), color: ATTR[a] }));
}
export function availColor(dmg, deb) { return dmg > 0 ? '#d0744a' : (deb > 0 ? '#d9a441' : '#6b5a44'); }

// -------- gear effect blurbs (prototyped §6c armor / §6d weapons, blessed-initial) ---------------
const WSTAT = {
  dagger:     { dmg: 1, timer: '0.6×', dps: '0.21', note: '0.42 dual-wield' },
  rapier:     { dmg: 2, timer: '0.7×', dps: '0.36' },
  shortsword: { dmg: 3, timer: '0.8×', dps: '0.47' },
  axe:        { dmg: 3, timer: '0.9×', dps: '0.42' },
  longsword:  { dmg: 4, timer: '1.0×', dps: '0.50' },
  claymore:   { dmg: 6, timer: '1.4×', dps: '0.52', note: 'two-handed' },
  mace:       { dmg: 5, timer: '1.1×', dps: '0.57' },
  bow:        { note: 'Pierces the shield pool; spends Charge. Both arms.' },
  wand:       { dmg: 2, note: 'Shield-subtraction (no Charge); dual-wieldable.' },
  staff:      { dmg: 2, note: '+0.2× spell damage per tier — twice a tome; two-handed.' },
  charm:      { note: '+0.1× minion damage per tier (off-hand).' },
  tome:       { note: '+0.1× spell damage per tier (off-hand).' },
};
export function gearEffect(g) {
  if (g.kind === 'armor') {
    if (g.attr === 'STR') return '−2 damage to the part it covers.';
    if (g.attr === 'DEX') return '+2% evasion · stacks across worn pieces.';
    if (g.attr === 'INT') return '+2 spell damage (robe + cap cap at 2).';
  }
  if (g.kind === 'shield') return '+2% shield-pool recharge · enables the CON block.';
  if (g.kind === 'weapon') {
    const s = WSTAT[g.id.split('_')[0]]; if (!s) return '';
    if (s.dmg != null && s.timer) return s.dmg + ' dmg · ' + s.timer + ' timer · ' + s.dps + ' DPS' + (s.note ? ' · ' + s.note : '') + '.';
    if (s.dmg != null) return s.dmg + ' dmg · ' + (s.note || '');
    return s.note || '';
  }
  return '';
}
const MINION_RARITY = { 'Iron Golem': 'MAGIC', 'Skeleton': 'COMMON', 'Hound': 'COMMON' };
// minion deploy-art sprite ids (Content/sprites/minions/<id>.png)
export const MINION_SPRITE = { 'Skeleton': 'skeleton', 'Iron Golem': 'golem', 'Hound': 'hound' };

const partBroken = (fs, part) => fs && fs[part] === 'broken';

// -------- THE resolver: one deterministic pass over a core's run scenario ------------------------
// resolve(core, raceId) — race sets the base pools (race attrs + core stat bonus). Returns everything
// both screens need, so nothing is computed twice / differently.
export function resolve(core, raceId) {
  raceId = RACES[raceId] ? raceId : 'human';
  const base = pools(core.id, raceId);
  const dmg = core.scenario.damage || {}, deb = core.scenario.debuff || {}, fs = core.scenario.figStates || {};
  const avail = {}; KEYS.forEach(k => { avail[k] = Math.max(0, base[k] - (dmg[k] || 0) - (deb[k] || 0)); });

  const itemState = {};             // gear id#index → 'equipped' | 'disabled'
  const itemGate = {};              // gear id#index → effective {attr, cost}
  const hiddenSockets = [];         // paper-doll mounts to suppress (socket AND gear id)
  const gearReserve = { STR: 0, INT: 0, DEX: 0, CON: 0 };
  const hideMount = (o) => { if (o.g.socket && !hiddenSockets.includes(o.g.socket)) hiddenSockets.push(o.g.socket); if (!hiddenSockets.includes(o.g.id)) hiddenSockets.push(o.g.id); };

  // 1) ALL GEAR reserves per effective gate (v6), with the §6e cascade (highest eff cost, ties
  //    last-equipped). Keys are the piece's index in core.gear (stable across duplicate ids).
  const reservers = core.gear.map((g, i) => ({ g, i, gate: gearGate(core.id, g) }));
  reservers.forEach(o => { itemGate[o.g.id + '#' + o.i] = o.gate; });
  // physical gates first: a weapon in a BROKEN arm can never be held; armor whose covered part broke disables.
  reservers.forEach(o => {
    if (o.g.kind !== 'armor' && o.g.socket && partBroken(fs, SOCKET_ARM[o.g.socket])) { itemState[o.g.id + '#' + o.i] = 'disabled'; hideMount(o); }
    if (o.g.kind === 'armor' && (SLOT_PARTS[o.g.part] || []).some(p => partBroken(fs, p))) itemState[o.g.id + '#' + o.i] = 'disabled';
  });
  KEYS.forEach(k => {
    const group = reservers.filter(o => o.gate.attr === k);
    const live = group.filter(o => itemState[o.g.id + '#' + o.i] !== 'disabled');
    let sum = live.reduce((n, o) => n + o.gate.cost, 0);
    const order = [...live].sort((a, b) => (b.gate.cost - a.gate.cost) || (b.i - a.i));
    let oi = 0;
    while (sum > avail[k] && oi < order.length) { const o = order[oi++]; itemState[o.g.id + '#' + o.i] = 'disabled'; if (o.g.kind !== 'armor') hideMount(o); sum -= o.gate.cost; }
    live.forEach(o => { if (itemState[o.g.id + '#' + o.i] !== 'disabled') { itemState[o.g.id + '#' + o.i] = 'equipped'; gearReserve[k] += o.gate.cost; } });
  });

  // helper flags for §6d technique gates (from the LIVE, non-disabled gear)
  const meleeCount = core.gear.filter((g, i) => g.kind === 'weapon' && g.socket !== 'ranged' && itemState[g.id + '#' + i] === 'equipped').length;
  const hasBow = core.gear.some((g, i) => g.socket === 'ranged' && itemState[g.id + '#' + i] === 'equipped');
  const hasShield = core.gear.some((g, i) => g.kind === 'shield' && itemState[g.id + '#' + i] === 'equipped');
  const needMet = (need) => {
    if (need === 'twoWeapons') return meleeCount >= 2;
    if (need === 'bow') return hasBow;
    if (need === 'melee') return meleeCount >= 1;
    if (need === 'shield') return hasShield;
    return true;
  };

  // 2) TECHNIQUES — combat resolution at EFFECTIVE cost. Reserving intents draw on the pool AFTER
  //    gear; unaffordable or weapon-gated-out → LOCKED. The states MATCH the reservation.
  const free = { ...avail }; KEYS.forEach(k => free[k] = Math.max(0, avail[k] - gearReserve[k]));
  const RESERVING = { HELD: 1, TARGETING: 1, CHARGING: 1 };
  const activeReserve = { STR: 0, INT: 0, DEX: 0, CON: 0 };
  const techniques = core.techniques.map(t => {
    const eff = techCost(core.id, t);
    let state = t.intent, lockNote = '';
    // dual-attr techniques (t.either) may be paid from EITHER pool — pick the one that can afford it,
    // else the one with the most room left (for the lock shortfall). Single-attr keeps t.attr.
    const cand = t.either || [t.attr];
    const payAttr = cand.find(a => (a in free) && eff <= free[a]) || cand.reduce((b, a) => ((free[a] ?? -1) > (free[b] ?? -1) ? a : b), cand[0]);
    const isAttr = payAttr in free;
    if (!needMet(t.needs)) { state = 'LOCKED'; lockNote = t.needs === 'bow' ? 'no bow' : (t.needs === 'shield' ? 'no shield' : (t.needs === 'twoWeapons' ? 'need 2 weapons' : 'no weapon')); }
    else if (isAttr && eff > free[payAttr]) { state = 'LOCKED'; lockNote = '+' + (eff - free[payAttr]) + ' ' + cand.join('/'); }
    if (state !== 'LOCKED' && RESERVING[state] && isAttr) { free[payAttr] -= eff; activeReserve[payAttr] += eff; }
    const label = t.costLabel || (t.charge ? t.attr + ' ' + eff + ' · CHG ' + t.charge : undefined);
    return { ...t, cost: eff, baseCost: t.cost, costLabel: label, state, lockNote, payAttr };
  });

  // 3) MINIONS (bays) — ACTIVE draws its effective gate from what's left; can't pay → IDLE (starved, §9).
  const bays = (core.bays || []).map(b => {
    const eff = bayCost(core.id, b);
    let state = b.intent;
    if (b.intent === 'ACTIVE') {
      if (b.attr in free && eff <= free[b.attr]) { free[b.attr] -= eff; activeReserve[b.attr] += eff; }
      else state = 'IDLE';
    }
    return { ...b, cost: eff, baseCost: b.cost, state };
  });

  return { base, avail, dmg, deb, fs, itemState, itemGate, hiddenSockets, gearReserve, activeReserve,
           techniques, bays, meleeCount, hasBow, hasShield };
}

// -------- pip rows for a pool -------------------------------------------------------------------
// mode 'equip'  → gear reservation only (Equipment)
// mode 'combat' → gear reservation (hatch) + active reservation (solid) (Encounter)
export function poolRows(core, mode, raceId) {
  const r = resolve(core, raceId);
  return KEYS.map(k => {
    const kl = k.toLowerCase();
    const total = r.base[k];
    const dmg = r.dmg[k] || 0, deb = r.deb[k] || 0;
    const gear = r.gearReserve[k] || 0;
    const active = mode === 'combat' ? (r.activeReserve[k] || 0) : 0;
    const avail = r.avail[k];
    const free = Math.max(0, avail - gear - active);
    const cells = [];
    for (let i = 0; i < gear; i++)   cells.push({ bg: HASH.gear(ATTR[k]), bd: '#0a0807', style: 'solid', state: 'reserved', asset: 'pip_reserved_' + kl });
    for (let i = 0; i < active; i++) cells.push({ bg: ATTR[k],            bd: '#0a0807', style: 'solid', state: 'full',     asset: 'pip_full_' + kl });
    for (let i = 0; i < free; i++)   cells.push({ bg: '#241b14',          bd: '#0a0807', style: 'solid', state: 'empty',    asset: 'pip_empty' });
    for (let i = 0; i < deb; i++)    cells.push({ bg: HASH.deb,           bd: '#0a0807', style: 'solid', state: 'debuff',   asset: 'pip_debuff' });
    for (let i = 0; i < dmg; i++)    cells.push({ bg: HASH.dmg,           bd: '#0a0807', style: 'solid', state: 'damage',   asset: 'pip_damage' });
    return { key: k, part: PART[k], color: ATTR[k], alloc: gear + active, available: avail,
             availColor: availColor(dmg, deb), pips: cells };
  });
}

// -------- inventory cards for one Equipment tab -------------------------------------------------
// Returns [{ id, name, attr, cost, state, rarity, kindLabel }] with the §6e ONE state family.
// Costs shown are EFFECTIVE for this core (the discount is what the pool actually pays).
export function inventory(core, tab, raceId) {
  const r = resolve(core, raceId);
  const KIND = { weapon: 'WEAPON', shield: 'SHIELD', armor: 'ARMOR' };
  const freeGear = {}; KEYS.forEach(k => { freeGear[k] = Math.max(0, r.avail[k] - r.gearReserve[k]); });
  if (tab === 'gear') {
    const equipped = core.gear.map((g, i) => {
      const gate = r.itemGate[g.id + '#' + i] || gearGate(core.id, g);
      return { id: g.id, name: g.name, attr: gate.attr, cost: gate.cost, rarity: g.rarity, effect: gearEffect(g),
               kindLabel: KIND[g.kind], state: r.itemState[g.id + '#' + i] || 'equipped' };
    });
    const finds = (core.finds.gear || []).map(g => {
      const gate = gearGate(core.id, g);
      return { id: g.id, name: g.name, attr: gate.attr, cost: gate.cost, rarity: g.rarity, effect: gearEffect(g),
        kindLabel: KIND[g.kind], state: freeGear[gate.attr] >= gate.cost ? 'equippable' : 'locked' };
    });
    return [...equipped, ...finds];
  }
  if (tab === 'tech') {
    // slotted techniques: EQUIPPED, or DISABLED if their weapon-gate was lost (§6e); never reservation-disabled here
    const slotted = r.techniques.map(t => {
      const gateLost = (t.state === 'LOCKED' && t.lockNote && /no |need /.test(t.lockNote));
      return { id: t.name.toLowerCase().replace(/[^a-z0-9]+/g, '_'), name: t.name, attr: t.attr, either: t.either, cost: t.cost,
               costLabel: t.costLabel, kindLabel: 'TECHNIQUE', rarity: '', effect: t.desc, state: gateLost ? 'disabled' : 'equipped' };
    });
    const barFull = core.techniques.length >= core.techCap;
    const finds = (core.finds.tech || []).map(t => ({ id: t.name.toLowerCase().replace(/[^a-z0-9]+/g, '_'), name: t.name,
      attr: t.attr, either: t.either, cost: techCost(core.id, t), costLabel: t.costLabel, kindLabel: 'TECHNIQUE', rarity: '', effect: t.desc,
      state: barFull ? 'locked' : 'equippable' }));
    return [...slotted, ...finds];
  }
  if (tab === 'minions') {
    const slotted = r.bays.map(b => ({ id: b.name.toLowerCase().replace(/[^a-z0-9]+/g, '_'), name: b.name, attr: b.attr, cost: b.cost,
      kindLabel: 'MINION', rarity: MINION_RARITY[b.name] || 'COMMON', effect: b.desc, state: 'equipped' }));
    const baysFull = (core.bays || []).length >= core.bayCap;
    const finds = (core.finds.minions || []).map(m => ({ id: m.name.toLowerCase().replace(/[^a-z0-9]+/g, '_'), name: m.name, attr: m.attr, cost: bayCost(core.id, m),
      kindLabel: 'MINION', rarity: MINION_RARITY[m.name] || 'COMMON', effect: m.desc, state: baysFull ? 'locked' : 'equippable' }));
    return [...slotted, ...finds];
  }
  return [];
}
