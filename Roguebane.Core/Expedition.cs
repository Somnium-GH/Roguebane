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

    public RunMap Map { get; }
    public Battle? Battle { get; private set; }
    public ExpeditionState State { get; private set; } = ExpeditionState.Choosing;

    public Expedition(Fighter player, Caster caster, IReadOnlyList<Technique> loadout, RunMap map)
    {
        _player = player;
        _caster = caster;
        _loadout = loadout;
        Map = map;
    }

    public Fighter Player => _player;
    public IReadOnlyList<Technique> Loadout => _loadout;
    public IReadOnlyList<MapNode> Options => Map.Options;

    public bool IsActive(Technique technique) => _caster.IsActive(technique);

    public void Toggle(Technique technique)
    {
        if (_caster.IsActive(technique)) _caster.Deactivate(technique);
        else _caster.Activate(technique);
    }

    // Travel to a charted neighbour and resolve what we land on.
    public bool Enter(string nodeId)
    {
        if (State != ExpeditionState.Choosing) return false;
        if (!Map.MoveTo(nodeId)) return false;

        if (Map.Outcome == RunMapOutcome.Overrun) { State = ExpeditionState.Lost; return true; }

        var node = Map.Current;
        if (node.Type == NodeType.Merchant)
        {
            _player.Heal(_player.MaxHp); // out-of-combat HP service; shop economy is G8 (Debt)
            return true;                 // stay at the merchant, still Choosing
        }

        Battle = new Battle(_caster, Maps.EncounterFor(node, Map.SupportBank), _player);
        State = ExpeditionState.Fighting;
        return true;
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
