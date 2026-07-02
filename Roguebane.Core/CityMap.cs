namespace Roguebane.Core;

public enum CityMapOutcome
{
    Marching,
    CastleCracked, // the leg is won — the war party disbands
    Overrun,       // the war party reached camp first, or supplies ran dry — the leg is lost
}

// The run map: a half-blind beacon chart the player picks a path across, under two-way pressure.
// Each jump spends one Supply. In parallel, the castle's war party marches on the camp one step per
// jump — crack the castle before it arrives (CastleCracked) or be Overrun. Taking a resource-hold
// banks rallied support for the castle siege.
//
// Fog (working default): resource-holds and the castle read from afar; a merchant resolves one jump
// out; everything else stays `?` until adjacent or visited.
public sealed class CityMap
{
    private readonly Dictionary<string, MapNode> _nodes;
    private readonly IReadOnlyList<MapNode> _order;
    private readonly bool _autoResolveCastle;

    public string CurrentId { get; private set; }
    public int Supplies { get; private set; }
    public int MaxSupplies { get; }                    // the starting supply (jump budget) — for X/max readouts
    public int WarPartyDistance { get; private set; } // steps from camp; 0 = camp overrun
    public int MarchLength { get; }                    // the war party's start distance (track scale)
    public int SupportBank { get; private set; }
    public CityMapOutcome Outcome { get; private set; } = CityMapOutcome.Marching;

    // autoResolveCastle: standalone navigation treats castle arrival as an instant crack (POC). A
    // combat driver (Expedition) passes false and instead calls CrackCastle() when the siege clears.
    public CityMap(IReadOnlyList<MapNode> nodes, string startId, int supplies, int marchLength,
        bool autoResolveCastle = true)
    {
        if (nodes.Count == 0) throw new ArgumentException("a map needs nodes", nameof(nodes));
        if (marchLength <= 0) throw new ArgumentOutOfRangeException(nameof(marchLength));
        _nodes = nodes.ToDictionary(n => n.Id);
        _order = nodes.ToList();
        if (!_nodes.ContainsKey(startId)) throw new ArgumentException("start not on the map", nameof(startId));
        _autoResolveCastle = autoResolveCastle;
        CurrentId = startId;
        Supplies = supplies;
        MaxSupplies = supplies;
        MarchLength = marchLength;
        WarPartyDistance = marchLength;
        _nodes[startId].MarkVisited();
    }

    public MapNode Current => _nodes[CurrentId];

    public bool AtCastle => Current.Type == NodeType.Castle;

    // The combat driver won the siege: disband the war party and win the leg.
    public void CrackCastle()
    {
        if (Outcome == CityMapOutcome.Marching && AtCastle) Outcome = CityMapOutcome.CastleCracked;
    }

    // The combat driver CLEARED the current resource-hold: bank its rallied support now (not on arrival,
    // so a hold abandoned mid-fight banks nothing). Standalone navigation banks in MoveTo instead.
    public void BankHold()
    {
        if (Outcome == CityMapOutcome.Marching && Current.Type == NodeType.ResourceHold) SupportBank++;
    }

    // Movement is ANY-DIRECTION along the chart's edges: a link is traversable both ways, so the player
    // may double back (e.g. to a merchant). Each move still spends a supply and advances the war party.
    private bool Adjacent(string nodeId) =>
        _nodes.ContainsKey(nodeId) &&
        (Current.Next.Contains(nodeId) || _nodes[nodeId].Next.Contains(CurrentId));

    public IReadOnlyList<MapNode> Options => _order.Where(n => Adjacent(n.Id)).ToList();

    public MapNode Node(string id) => _nodes[id];

    // The whole chart (for the graph render); order is the map's declared node order.
    public IReadOnlyList<MapNode> Nodes => _order;

    // What the player can see of a node through the fog, given where they stand now.
    public NodeType Sees(MapNode node)
    {
        if (node.Visited) return node.Type;
        var adjacent = Adjacent(node.Id);
        return node.Type switch
        {
            NodeType.Camp => NodeType.Camp,                 // your own origin — always known
            NodeType.ResourceHold => NodeType.ResourceHold, // visible afar
            NodeType.Castle => NodeType.Castle,             // visible afar
            NodeType.Merchant when adjacent => NodeType.Merchant, // resolves one jump out
            _ when adjacent => node.Type,                   // adjacency resolves the rest
            _ => NodeType.Unknown,                          // still fogged
        };
    }

    public bool CanMoveTo(string nodeId) =>
        Outcome == CityMapOutcome.Marching && Supplies > 0 && Adjacent(nodeId);

    // Jump to a charted neighbour: spend a supply, the war party advances a step, then resolve the
    // node we land on. In standalone navigation (no combat driver) a resource-hold banks and the castle
    // cracks on ARRIVAL; with a combat driver (Expedition) both happen on CLEAR — see BankHold/CrackCastle.
    public bool MoveTo(string nodeId)
    {
        if (!CanMoveTo(nodeId)) return false;

        Supplies--;
        CurrentId = nodeId;
        Current.MarkVisited();
        AdvanceWarParty();
        if (Outcome != CityMapOutcome.Marching) return true;

        if (_autoResolveCastle) // standalone nav: there is no fight to win, so resolve on arrival
            switch (Current.Type)
            {
                case NodeType.ResourceHold: SupportBank++; break;
                case NodeType.Castle: Outcome = CityMapOutcome.CastleCracked; break;
            }

        // Out of supplies short of the castle: the march can't continue and the war party arrives.
        if (Supplies == 0 && Outcome == CityMapOutcome.Marching && !AtCastle)
            Outcome = CityMapOutcome.Overrun;
        return true;
    }

    private void AdvanceWarParty()
    {
        WarPartyDistance--;
        if (WarPartyDistance <= 0) Outcome = CityMapOutcome.Overrun;
    }
}
