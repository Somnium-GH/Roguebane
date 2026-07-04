// Roguebane — shared attribute / reservation model.
// ONE source of truth for both the Combat and Build (equipment) screens:
// stat caps, current combat damage + debuff, every gear / technique / minion cost,
// and how a stat pool reserves its points. Imported by both .dc.html screens so
// their numbers can never drift apart.
//
// Reservation rules:
//   • Gear is the permanent loadout — it ALWAYS reserves and is resolved first, in
//     list order. A piece that no longer fits the (damaged) pool is force-unequipped
//     and rendered bare; it is the one thing the player cannot toggle.
//   • Techniques & minions reserve ONLY while active, drawing on whatever gear leaves.
//     An inactive one is "ready" if it would still fit, otherwise "locked".

export const KEYS  = ['STR', 'INT', 'DEX', 'CON'];
export const PART  = { STR: 'Arms', INT: 'Head', DEX: 'Legs', CON: 'Chest' };
export const COLOR = { STR: '#c2553f', INT: '#6f8fc4', DEX: '#82a85e', CON: '#cf9a44' };

export const CAP    = { STR: 12, INT: 8, DEX: 8, CON: 8 };
export const DMG    = { STR: 2,  INT: 0, DEX: 8, CON: 0 };  // combat damage: arms grazed, legs broken
export const DEBUFF = { STR: 0,  INT: 2, DEX: 0, CON: 0 };  // temporary debuff: INT marked

export const GEAR = [
  { name: 'Sword',           attr: 'STR', cost: 3, rarity: 'EPIC' },
  { name: 'Steel Helm',      attr: 'STR', cost: 2, rarity: 'RARE' },   // §6c rename: old "Steel Helmet" retired
  { name: 'Steel Breastplate', attr: 'STR', cost: 3, rarity: 'EPIC' }, // §6c rename: old "Plate" retired
  { name: 'Wooden Shield',   attr: 'CON', cost: 2, rarity: 'MAGIC' },
  { name: 'Steel Vambraces', attr: 'STR', cost: 3, rarity: 'COMMON', part: 'armL' },
  { name: 'Iron Greaves',    attr: 'STR', cost: 3, rarity: 'MAGIC',  part: 'legL' },
];

export const TECH = [
  { name: 'Swing',    attr: 'STR', cost: 1, active: true  },
  { name: 'Firebolt', attr: 'INT', cost: 2, active: true  },
  { name: 'Disarm',   attr: 'STR', cost: 1, active: true  },
  { name: 'Brace',    attr: 'CON', cost: 1, active: true  },
  { name: 'Frenzy',   attr: 'STR', cost: 2, active: false },
  { name: 'Cleave',   attr: 'STR', cost: 2, active: false },
];

export const MINIONS = [
  { name: 'Skeleton',      attr: 'INT', cost: 2, active: true  },
  { name: 'Bound Wisp',    attr: 'INT', cost: 1, active: false },
  { name: 'Carrion Hound', attr: '—',  cost: 0, active: false, neutral: true },
];

// Resolve the whole loadout against the current pools. Returns, per stat:
//   available  ceiling after damage + debuff
//   allocGear  points reserved by equipped gear only        (the equipment screen's view)
//   allocFull  gear + active techniques + active minions     (the combat screen's view)
//   free       points still unreserved after everything active
// and a per-item map: { state, attr, cost, short } where state is one of
//   gear:    'equipped' | 'dropped'
//   ability: 'active' | 'starved' (slotted but unaffordable) | 'ready' | 'locked' | 'neutral'
export function compute() {
  const available = {}, rem = {};
  KEYS.forEach(k => { available[k] = Math.max(0, CAP[k] - DMG[k] - DEBUFF[k]); rem[k] = available[k]; });

  const item = {};
  const allocGear = { STR: 0, INT: 0, DEX: 0, CON: 0 };

  GEAR.forEach(g => {
    if (g.cost <= rem[g.attr]) {
      rem[g.attr] -= g.cost; allocGear[g.attr] += g.cost;
      item[g.name] = { state: 'equipped', attr: g.attr, cost: g.cost, short: 0 };
    } else {
      item[g.name] = { state: 'dropped', attr: g.attr, cost: g.cost, short: g.cost - rem[g.attr] };
    }
  });

  const allocFull = { ...allocGear };
  [...TECH, ...MINIONS].forEach(a => {
    if (a.neutral || !(a.attr in rem)) {
      item[a.name] = { state: 'neutral', attr: a.attr, cost: a.cost, short: 0 };
    } else if (a.active) {
      if (a.cost <= rem[a.attr]) {
        rem[a.attr] -= a.cost; allocFull[a.attr] += a.cost;
        item[a.name] = { state: 'active', attr: a.attr, cost: a.cost, short: 0 };
      } else {
        item[a.name] = { state: 'starved', attr: a.attr, cost: a.cost, short: a.cost - rem[a.attr] };
      }
    } else {
      const fits = a.cost <= rem[a.attr];
      item[a.name] = { state: fits ? 'ready' : 'locked', attr: a.attr, cost: a.cost, short: fits ? 0 : a.cost - rem[a.attr] };
    }
  });

  return { available, allocGear, allocFull, free: rem, item };
}

// Bar-segment percentages over the FULL cap for one stat, given how many points are
// shown as allocated (gear-only on the equipment screen, full on combat).
export function bars(k, alloc) {
  const cap = CAP[k], deb = DEBUFF[k], dmg = DMG[k], avail = available_(k);
  const pct = n => (n / cap * 100) + '%';
  return {
    solidPct: pct(alloc),
    debLeft: pct(avail),        debW: pct(deb),
    dmgLeft: pct(avail + deb),  dmgW: pct(dmg),
  };
}
function available_(k) { return Math.max(0, CAP[k] - DMG[k] - DEBUFF[k]); }

export const HASH = {
  dmg: 'repeating-linear-gradient(45deg,#b23b3288 0 3px,transparent 3px 6px)',
  deb: 'repeating-linear-gradient(45deg,#d9a44188 0 3px,transparent 3px 6px)',
};

// availability colour for the "/ total" number: red if damaged (wins), else yellow if
// debuffed, else dim.
export function availColor(k) { return DMG[k] > 0 ? '#d0744a' : (DEBUFF[k] > 0 ? '#d9a441' : '#6b5a44'); }
