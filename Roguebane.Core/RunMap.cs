namespace Roguebane.Core;

public enum RunMapOutcome
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
public sealed class RunMap
{
    private readonly Dictionary<string, MapNode> _nodes;
    private readonly IReadOnlyList<MapNode> _order;
    private readonly bool _autoResolveCastle;

    public string CurrentId { get; private set; }
    public int Supplies { get; private set; }
    public int WarPartyDistance { get; private set; } // steps from camp; 0 = camp overrun
    public int MarchLength { get; }                    // the war party's start distance (track scale)
    public int SupportBank { get; private set; }
    public RunMapOutcome Outcome { get; private set; } = RunMapOutcome.Marching;

    // autoResolveCastle: standalone navigation treats castle arrival as an instant crack (POC). A
    // combat driver (Expedition) passes false and instead calls CrackCastle() when the siege clears.
    public RunMap(IReadOnlyList<MapNode> nodes, string startId, int supplies, int marchLength,
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
        MarchLength = marchLength;
        WarPartyDistance = marchLength;
        _nodes[startId].MarkVisited();
    }

    public MapNode Current => _nodes[CurrentId];

    public bool AtCastle => Current.Type == NodeType.Castle;

    // The combat driver won the siege: disband the war party and win the leg.
    public void CrackCastle()
    {
        if (Outcome == RunMapOutcome.Marching && AtCastle) Outcome = RunMapOutcome.CastleCracked;
    }

    public IReadOnlyList<MapNode> Options => Current.Next.Select(id => _nodes[id]).ToList();

    public MapNode Node(string id) => _nodes[id];

    // The whole chart (for the graph render); order is the map's declared node order.
    public IReadOnlyList<MapNode> Nodes => _order;

    // What the player can see of a node through the fog, given where they stand now.
    public NodeType Sees(MapNode node)
    {
        if (node.Visited) return node.Type;
        var adjacent = Current.Next.Contains(node.Id);
        return node.Type switch
        {
            NodeType.ResourceHold => NodeType.ResourceHold, // visible afar
            NodeType.Castle => NodeType.Castle,             // visible afar
            NodeType.Merchant when adjacent => NodeType.Merchant, // resolves one jump out
            _ when adjacent => node.Type,                   // adjacency resolves the rest
            _ => NodeType.Unknown,                          // still fogged
        };
    }

    public bool CanMoveTo(string nodeId) =>
        Outcome == RunMapOutcome.Marching && Supplies > 0 && Current.Next.Contains(nodeId);

    // Jump to a charted neighbour: spend a supply, the war party advances a step, then resolve the
    // node we land on. Banking a resource-hold and cracking the castle happen on arrival.
    public bool MoveTo(string nodeId)
    {
        if (!CanMoveTo(nodeId)) return false;

        Supplies--;
        CurrentId = nodeId;
        Current.MarkVisited();
        AdvanceWarParty();
        if (Outcome != RunMapOutcome.Marching) return true;

        switch (Current.Type)
        {
            case NodeType.ResourceHold:
                SupportBank++;
                break;
            case NodeType.Castle:
                if (_autoResolveCastle) Outcome = RunMapOutcome.CastleCracked;
                break;
        }

        // Out of supplies short of the castle: the march can't continue and the war party arrives.
        if (Supplies == 0 && Outcome == RunMapOutcome.Marching && !AtCastle)
            Outcome = RunMapOutcome.Overrun;
        return true;
    }

    private void AdvanceWarParty()
    {
        WarPartyDistance--;
        if (WarPartyDistance <= 0) Outcome = RunMapOutcome.Overrun;
    }
}
