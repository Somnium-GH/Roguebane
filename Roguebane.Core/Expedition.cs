using Roguebane.Core.Content;

namespace Roguebane.Core;

public enum ExpeditionState
{
    Choosing, // at a beacon, picking the next jump
    Fighting, // a battle is underway at the current node
    Won,      // the castle fell
    Lost,     // the player fell, or the war party overran the camp
}

// The real game loop: a run across the map with combat at its nodes. The player picks a charted
// jump (spending a supply and letting the war party march a step), fights what they land on, and
// presses for the castle before being overrun. The same body/caster/loadout carries between fights —
// parts erode and HP only refills at a merchant — so the run is a war of attrition under the clock.
public sealed class Expedition
{
    private readonly Fighter _player;
    private readonly Caster _caster;
    private readonly IReadOnlyList<Technique> _loadout;
    private readonly Stash _stash;

    public RunMap Map { get; }
    public Battle? Battle { get; private set; }
    public ExpeditionState State { get; private set; } = ExpeditionState.Choosing;

    public Expedition(Fighter player, Caster caster, IReadOnlyList<Technique> loadout, RunMap map,
        Stash? stash = null)
    {
        _player = player;
        _caster = caster;
        _loadout = loadout;
        Map = map;
        _stash = stash ?? new Stash();
    }

    public Fighter Player => _player;
    public IReadOnlyList<Technique> Loadout => _loadout;
    public IReadOnlyList<MapNode> Options => Map.Options;

    // The economy: spoils from cleared nodes buy repair potions at merchants. Potions restore PARTS
    // (the healing split — never HP); HP refills only via the merchant service. The Stash carries
    // gold and potions across the legs of a campaign.
    public Stash Stash => _stash;
    public int Gold => _stash.Gold;
    public int Potions => _stash.Potions;

    private const int PotionCost = 4;
    private const int PotionRepair = 2; // restored to every damaged part — low scale
    private const int HealCost = 3;

    private static int Spoils(NodeType type) => type switch
    {
        NodeType.Castle => 10,
        NodeType.ResourceHold => 4,
        NodeType.Skirmish => 3,
        _ => 2,
    };

    public bool AtMerchant => State == ExpeditionState.Choosing && Map.Current.Type == NodeType.Merchant;

    // Spend spoils on a repair potion (carried for later) at a merchant.
    public bool BuyPotion()
    {
        if (!AtMerchant || !_stash.TrySpend(PotionCost)) return false;
        _stash.AddPotion();
        return true;
    }

    // Use a carried potion to repair the body — out of combat only (between jumps / fights).
    public bool UsePotion()
    {
        if (State != ExpeditionState.Choosing || !_stash.TryUsePotion()) return false;
        foreach (var part in _player.Body.Parts) _player.Body.Repair(part, PotionRepair);
        return true;
    }

    // Pay a merchant for the out-of-combat HP service.
    public bool BuyHeal()
    {
        if (!AtMerchant || _player.Hp >= _player.MaxHp || !_stash.TrySpend(HealCost)) return false;
        _player.Heal(_player.MaxHp);
        return true;
    }

    public bool IsActive(Technique technique) => _caster.IsActive(technique);

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
    public void ClearAim(Technique technique) => _caster.ClearAim(technique); // right-click clears the target
    public bool Fire(Technique technique) => _caster.Fire(technique);
    public void SetAuto(bool auto) => _caster.SetAutoAll(auto); // ONE global toggle for the whole bar
    public bool IsAuto() => _caster.AutoAll;
    public bool IsReady(Technique technique) => _caster.IsReady(technique);
    public ICombatTarget? AimOf(Technique technique) => _caster.AimOf(technique);

    // Travel to a charted neighbour and resolve what we land on.
    public bool Enter(string nodeId)
    {
        if (State != ExpeditionState.Choosing) return false;
        if (!Map.MoveTo(nodeId)) return false;

        if (Map.Outcome == RunMapOutcome.Overrun) { State = ExpeditionState.Lost; return true; }

        var node = Map.Current;
        if (node.Type == NodeType.Merchant)
            return true; // stay at the merchant: BuyPotion / BuyHeal / UsePotion are the verbs here

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
                else State = ExpeditionState.Choosing;
                break;
        }
    }

    // Break off the current fight and fall back to the chart (the war party keeps coming).
    public void Flee()
    {
        if (State != ExpeditionState.Fighting) return;
        Battle?.Flee();
        State = ExpeditionState.Choosing;
    }
}
