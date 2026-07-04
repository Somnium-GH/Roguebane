using Roguebane.Core.Content;

namespace Roguebane.Core;

public enum ExpeditionState
{
    Choosing, // at a beacon, picking the next jump
    Fighting, // a battle is underway at the current node
    Cleared,  // the fight is won but not yet left — the player must REDEPLOY to return to the chart
    Won,      // the castle fell
    Lost,     // the player fell, or the war party overran the camp
}

// The real game loop: a run across the map with combat at its nodes. The player picks a charted
// jump (spending a supply and letting the war party march a step), fights what they land on, and
// presses for the castle before being overrun. The same body/caster/equipment carries between fights —
// parts erode and HP only refills at a merchant — so the run is a war of attrition under the clock.
public sealed class Expedition
{
    private readonly Fighter _player;
    private readonly Caster _caster;
    private readonly IReadOnlyList<Technique> _equipment;
    private readonly Stash _stash;
    // §12 gear stock: rolled ONCE per merchant node from its seed (reproducible), purchases consume
    // it. Techniques/minions/runes are OFFERED but not yet buyable — their receiving models (mid-run
    // palette/bay/rune mutation) are the design-open gate.
    private sealed class GearStock
    {
        public required List<Weapon> Weapons;
        public required List<Armor> Armor;
        public required List<Technique> Techniques;
        public required List<Minion> Minions;
        public required List<Mark> Marks;
    }

    private const ulong GearSalt = 0x47454152; // "GEAR"
    private readonly Dictionary<string, GearStock> _gearStock = new();

    private GearStock CurrentStock()
    {
        var id = Map.Current.Id;
        if (_gearStock.TryGetValue(id, out var s)) return s;
        var roll = MerchantStock.Roll(Seed(id) ^ GearSalt,
            Armory.All, Shops.ArmorPool, Content.Techniques.All, Content.Minions.All, Paths.AllMarks);
        s = new GearStock
        {
            Weapons = roll.Weapons.ToList(),
            Armor = roll.Armor.ToList(),
            Techniques = roll.Techniques.ToList(),
            Minions = roll.Minions.ToList(),
            Marks = roll.Marks.ToList(),
        };
        _gearStock[id] = s;
        return s;
    }

    public CityMap Map { get; }
    public Battle? Battle { get; private set; }
    public ExpeditionState State { get; private set; } = ExpeditionState.Choosing;

    private readonly bool _refundSummonsOnRedeploy; // the Summoner's Core Effect [LOCKED §11]

    public Expedition(Fighter player, Caster caster, IReadOnlyList<Technique> equipment, CityMap map,
        Stash? stash = null, string figureId = "human_grunt", bool refundSummonsOnRedeploy = false)
    {
        _refundSummonsOnRedeploy = refundSummonsOnRedeploy;
        _player = player;
        _caster = caster;
        _equipment = equipment;
        Map = map;
        _stash = stash ?? new Stash();
        FigureId = figureId;
    }

    // The chassis figure to render the player with (manifest figure key); carried from assembly so
    // the shell picks the right modular sprite set. Defaults keep legacy/test construction working.
    public string FigureId { get; }

    public Fighter Player => _player;
    public IReadOnlyList<Technique> Equipment => _equipment;
    public IReadOnlyList<MapNode> Options => Map.Options;

    // The economy: spoils from cleared nodes fund the merchant's HP service (part-heals are in-combat
    // techniques, not buyable). The Stash carries gold + the gear pack across the legs of a campaign.
    public Stash Stash => _stash;
    public int Gold => _stash.Gold;

    private const ulong HealSalt = 0x4845414C; // decorrelate the heal-price roll from combat seeds

    private static int Spoils(NodeType type) => type switch
    {
        NodeType.Castle => 10,
        NodeType.ResourceHold => 4,
        NodeType.Skirmish => 3,
        _ => 2,
    };

    public bool AtMerchant => State == ExpeditionState.Choosing && Map.Current.Type == NodeType.Merchant;

    // The merchant's gear stock and its prices (placeholder-sane: weapon = reserve+power, armor =
    // value+2). Buying spends gold, moves the piece into the Stash pack, and clears it from the stock.
    public IReadOnlyList<Weapon> OfferedWeapons => AtMerchant ? CurrentStock().Weapons : Array.Empty<Weapon>();
    public IReadOnlyList<Armor> OfferedArmor => AtMerchant ? CurrentStock().Armor : Array.Empty<Armor>();
    public IReadOnlyList<Technique> OfferedTechniques => AtMerchant ? CurrentStock().Techniques : Array.Empty<Technique>();
    public IReadOnlyList<Minion> OfferedMinions => AtMerchant ? CurrentStock().Minions : Array.Empty<Minion>();
    public IReadOnlyList<Mark> OfferedMarks => AtMerchant ? CurrentStock().Marks : Array.Empty<Mark>();
    public static int Price(Weapon weapon) => weapon.Reserve + weapon.Power;
    public static int Price(Armor armor) => 2 * armor.Tier + 2; // placeholder pricing, economy tune

    public bool BuyWeapon(Weapon weapon)
    {
        if (!AtMerchant || !CurrentStock().Weapons.Contains(weapon) || !_stash.TrySpend(Price(weapon))) return false;
        CurrentStock().Weapons.Remove(weapon);
        _stash.AddWeapon(weapon);
        return true;
    }

    public bool BuyArmor(Armor armor)
    {
        if (!AtMerchant || !CurrentStock().Armor.Contains(armor) || !_stash.TrySpend(Price(armor))) return false;
        CurrentStock().Armor.Remove(armor);
        _stash.AddArmor(armor);
        return true;
    }

    // §12 receiving (LOCKED 2026-07-03): every ware category is click-to-buy. A purchase spends gold,
    // clears the roll, and lands in the run inventory — technique -> palette pool, minion -> minion
    // inventory, rune (Mark) -> rune bag. Slotting/climbing stays the Equipment screen's job.
    // Prices are placeholder-sane (flagged to the economy tune with the rest).
    public static int Price(Technique technique) => technique.Reserve + technique.Power + 2;
    public static int Price(Minion minion) => minion.Reserve + minion.Power + 3;
    public static int Price(Mark mark) => mark.Cost + 2;

    public bool BuyTechnique(Technique technique)
    {
        if (!AtMerchant || !CurrentStock().Techniques.Contains(technique)
            || !_stash.TrySpend(Price(technique))) return false;
        CurrentStock().Techniques.Remove(technique);
        _stash.AddTechnique(technique);
        return true;
    }

    public bool BuyMinion(Minion minion)
    {
        if (!AtMerchant || !CurrentStock().Minions.Contains(minion)
            || !_stash.TrySpend(Price(minion))) return false;
        CurrentStock().Minions.Remove(minion);
        _stash.AddMinion(minion);
        return true;
    }

    public bool BuyMark(Mark mark)
    {
        if (!AtMerchant || !CurrentStock().Marks.Contains(mark)
            || !_stash.TrySpend(Price(mark))) return false;
        CurrentStock().Marks.Remove(mark);
        _stash.AddMark(mark);
        return true;
    }

    // Equip / unequip carried gear onto the player's body — out of combat only (between jumps/fights),
    // moving the piece between the Stash pack and the body via Gearing (which enforces the wield/wear
    // gates). Returns false if mid-fight or the move isn't legal.
    public bool EquipWeapon(Weapon weapon) =>
        State == ExpeditionState.Choosing && Gearing.EquipWeapon(_stash, _player.Body, weapon);
    public bool UnequipWeapon(Weapon weapon) =>
        State == ExpeditionState.Choosing && Gearing.UnequipWeapon(_stash, _player.Body, weapon);
    public bool EquipArmor(Armor armor) =>
        State == ExpeditionState.Choosing && Gearing.EquipArmor(_stash, _player.Body, armor);
    public bool UnequipArmor(Stat group) =>
        State == ExpeditionState.Choosing && Gearing.UnequipArmor(_stash, _player.Body, group);

    // The merchant's HP service price (§10): gold per 1 HP, randomized within a loot-bounded range and
    // STABLE per merchant node (same node id => same price, so the run stays reproducible).
    public int HealPricePerHp => 1 + new Rng(Seed(Map.Current.Id) ^ HealSalt).Next(2); // 1..2 gold / HP

    // §12 merchant healing: a 1-HP buy at the per-HP price, and a FULL repair at a premium (placeholder
    // +1 gold per missing HP — tune with the rest of the economy). HP only, out of combat (§10).
    public bool BuyHeal()
    {
        if (!AtMerchant) return false;
        if (_player.MaxHp - _player.Hp <= 0) return false;
        if (!_stash.TrySpend(HealPricePerHp)) return false;
        _player.Heal(1);
        return true;
    }

    public int FullHealPrice => (_player.MaxHp - _player.Hp) * (HealPricePerHp + 1);

    public bool BuyFullHeal()
    {
        if (!AtMerchant) return false;
        var missing = _player.MaxHp - _player.Hp;
        if (missing <= 0 || !_stash.TrySpend(FullHealPrice)) return false;
        _player.Heal(missing);
        return true;
    }

    // §12 merchant RESOURCE stock: small seeded quantities of Supplies and Charge, stable per node so
    // the run stays reproducible. (Summons joins when its §9 resource model lands.) Prices are
    // placeholder-sane — the economy tune owns the numbers.
    private const ulong StockSalt = 0x53544F43; // "STOC"
    private int StockRoll(int lane, int max) =>
        new Rng(Seed(Map.Current.Id) ^ StockSalt ^ (ulong)lane).Next(max + 1);

    private int _suppliesBought, _chargeBought; // per-visit consumption (stock is per NODE seed)

    public int SuppliesStock => AtMerchant ? Math.Max(0, 1 + StockRoll(1, 2) - _suppliesBought) : 0;
    public int ChargeStock => AtMerchant ? Math.Max(0, 1 + StockRoll(2, 1) - _chargeBought) : 0;
    public int SuppliesPrice => 2 + StockRoll(3, 2); // 2..4 gold per supply
    public int ChargePrice => 3 + StockRoll(4, 2);   // 3..5 gold per charge

    public bool BuySupplies()
    {
        if (!AtMerchant || SuppliesStock <= 0 || Map.Supplies >= Map.MaxSupplies) return false;
        if (!_stash.TrySpend(SuppliesPrice)) return false;
        Map.AddSupplies(1);
        _suppliesBought++;
        return true;
    }

    public bool BuyCharge()
    {
        if (!AtMerchant || ChargeStock <= 0 || Charge >= MaxCharge) return false;
        if (!_stash.TrySpend(ChargePrice)) return false;
        _caster.Recharge(1);
        _chargeBought++;
        return true;
    }

    private int _summonsBought;
    public int SummonsStock => AtMerchant && MaxSummons > 0 ? Math.Max(0, 1 + StockRoll(5, 1) - _summonsBought) : 0;
    public int SummonsPrice => 4 + StockRoll(6, 2); // 4..6 gold per summon (placeholder)

    public bool BuySummons()
    {
        if (!AtMerchant || SummonsStock <= 0 || Summons >= MaxSummons) return false;
        if (!_stash.TrySpend(SummonsPrice)) return false;
        _caster.AddSummons(1);
        _summonsBought++;
        return true;
    }

    public bool IsActive(Technique technique) => _caster.IsActive(technique);

    // Bay occupants (the summoned minions) + total bays for the combat minion-bay lane.
    public int MinionCount => _caster.MinionCount;
    public int Charge => _caster.Charge;       // the shield-pierce resource (§6b) — readout + spend
    public int MaxCharge => _caster.MaxCharge;
    public int Summons => _caster.SummonsLeft; // §9 deploy resource — readout + merchant refill
    public int MaxSummons => _caster.MaxSummons;
    public IReadOnlyList<Minion> Minions => _caster.Minions;
    public int Bays => _caster.BayCap;

    // Live per-technique state for the action-bar render (cooldown fill + card state).
    public Caster.TechStatus Status(Technique technique) => _caster.StatusOf(technique);

    // The player toggle POWERS a technique: it reserves its stat and charges. It does NOT target — an
    // untargeted technique holds at the ready and fires nothing until the player aims it. (requireAim.)
    public void Toggle(Technique technique)
    {
        if (_caster.IsActive(technique)) _caster.Deactivate(technique);
        else _caster.Activate(technique, auto: true); // discharges on cadence ONCE it has a target
    }

    // FTL targeting surface for the shell: the one live enemy (null between fights), per-technique aim,
    // and the AUTO toggle. A powered technique fires automatically when charged AND targeted (no fire
    // button). AUTO off (default) is one-shot — clears the target after the shot; AUTO on keeps firing.
    public Foe? Enemy => Battle?.Encounter.Enemy;
    public void Aim(Technique technique, ICombatTarget target) => _caster.Aim(technique, target);
    public void Aim(Technique technique, ICombatTarget target, BodyPart part) => _caster.Aim(technique, target, part);
    public void ClearAim(Technique technique) => _caster.ClearAim(technique); // right-click clears the target
    public bool Fire(Technique technique) => _caster.Fire(technique);
    public void SetAuto(bool auto) => _caster.SetAutoAll(auto); // ONE global toggle for the whole bar
    public bool IsAuto() => _caster.AutoAll;
    public bool IsReady(Technique technique) => _caster.IsReady(technique);
    public ICombatTarget? AimOf(Technique technique) => _caster.AimOf(technique);
    public BodyPart? PartOf(Technique technique) => _caster.PartOf(technique); // which foe part it's aimed at

    // Travel to a charted neighbour and resolve what we land on.
    public bool Enter(string nodeId)
    {
        if (State != ExpeditionState.Choosing) return false;
        if (!Map.MoveTo(nodeId)) return false;

        if (Map.Outcome == CityMapOutcome.Overrun) { State = ExpeditionState.Lost; return true; }

        var node = Map.Current;
        if (node.Type is NodeType.Merchant or NodeType.Camp)
            return true; // no fight here: the merchant's verbs are shop/heal, the camp is safe ground

        Battle = new Battle(_caster, Maps.EncounterFor(node, Map.SupportBank), _player, Seed(node.Id));
        State = ExpeditionState.Fighting;
        return true;
    }

    // A stable per-node seed: same node id => same combat rolls, so the run stays reproducible.
    private static ulong Seed(string nodeId)
    {
        ulong h = 1469598103934665603; // FNV-1a over the id
        foreach (var c in nodeId) { h ^= c; h *= 1099511628211; }
        return h;
    }

    public void Tick()
    {
        if (State != ExpeditionState.Fighting || Battle is null) return;

        Battle.Step();
        switch (Battle.Outcome)
        {
            case BattleOutcome.Lost:
                State = ExpeditionState.Lost;
                break;
            case BattleOutcome.Cleared:
                _stash.AddGold(Spoils(Map.Current.Type)); // spoils for taking the node
                _caster.Recharge();                       // magic refills in the lull after a fight
                if (Map.AtCastle) { Map.CrackCastle(); State = ExpeditionState.Won; }
                // A cleared node banks its hold, then HOLDS at Cleared — the shell shows the result and
                // the player must REDEPLOY to return to the chart (no silent auto-return to the map).
                else { Map.BankHold(); State = ExpeditionState.Cleared; }
                break;
        }
    }

    // Leave a cleared fight and return to the chart to pick the next jump. Only valid post-clear
    // (Redeploy = out of combat); a no-op otherwise.
    public void Redeploy()
    {
        if (State != ExpeditionState.Cleared) return;
        State = ExpeditionState.Choosing;
        // The Summoner's Core Effect [LOCKED §11]: each SURVIVING minion (idle counts — it is still
        // summoned) refunds its Summons on redeploy — its economy edge.
        if (_refundSummonsOnRedeploy) _caster.RefundSummons(_caster.MinionCount);
    }

    // RETREAT: break off an ACTIVE fight and fall back to the chart (the war party keeps coming).
    public void Retreat()
    {
        if (State != ExpeditionState.Fighting) return;
        Battle?.Retreat();
        State = ExpeditionState.Choosing;
    }
}
