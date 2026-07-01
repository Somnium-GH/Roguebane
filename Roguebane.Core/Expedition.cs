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
    private readonly List<Weapon> _stockWeapons = new(Shops.Weapons); // this merchant's gear stock (per leg)
    private readonly List<Armor> _stockArmor = new(Shops.Armor);

    public CityMap Map { get; }
    public Battle? Battle { get; private set; }
    public ExpeditionState State { get; private set; } = ExpeditionState.Choosing;

    public Expedition(Fighter player, Caster caster, IReadOnlyList<Technique> equipment, CityMap map,
        Stash? stash = null, string figureId = "human_grunt")
    {
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

    private const int HealCost = 3;

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
    public IReadOnlyList<Weapon> OfferedWeapons => _stockWeapons;
    public IReadOnlyList<Armor> OfferedArmor => _stockArmor;
    public static int Price(Weapon weapon) => weapon.Reserve + weapon.Power;
    public static int Price(Armor armor) => armor.Value + 2;

    public bool BuyWeapon(Weapon weapon)
    {
        if (!AtMerchant || !_stockWeapons.Contains(weapon) || !_stash.TrySpend(Price(weapon))) return false;
        _stockWeapons.Remove(weapon);
        _stash.AddWeapon(weapon);
        return true;
    }

    public bool BuyArmor(Armor armor)
    {
        if (!AtMerchant || !_stockArmor.Contains(armor) || !_stash.TrySpend(Price(armor))) return false;
        _stockArmor.Remove(armor);
        _stash.AddArmor(armor);
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

    // Pay a merchant for the out-of-combat HP service.
    public bool BuyHeal()
    {
        if (!AtMerchant || _player.Hp >= _player.MaxHp || !_stash.TrySpend(HealCost)) return false;
        _player.Heal(_player.MaxHp);
        return true;
    }

    public bool IsActive(Technique technique) => _caster.IsActive(technique);

    // Bay occupants (the summoned minions) + total bays for the combat minion-bay lane.
    public int MinionCount => _caster.MinionCount;
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

    // FTL targeting surface for the shell: the live foes, per-technique aim, and the AUTO toggle.
    // A powered technique fires automatically when charged AND targeted (no fire button). AUTO off
    // (default) is one-shot — it clears the target after the shot; AUTO on keeps firing at the target.
    public IReadOnlyList<Foe> Foes => Battle?.Encounter.Foes ?? Array.Empty<Foe>();
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
        if (node.Type == NodeType.Merchant)
            return true; // stay at the merchant: BuyHeal / buy gear are the verbs here

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
        if (State == ExpeditionState.Cleared) State = ExpeditionState.Choosing;
    }

    // RETREAT: break off an ACTIVE fight and fall back to the chart (the war party keeps coming).
    public void Retreat()
    {
        if (State != ExpeditionState.Fighting) return;
        Battle?.Retreat();
        State = ExpeditionState.Choosing;
    }
}
