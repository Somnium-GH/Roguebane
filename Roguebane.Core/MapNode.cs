namespace Roguebane.Core;

// The kinds of beacon on the run map. Unknown is fog: its true kind is hidden until the player
// charts close enough to resolve it.
public enum NodeType
{
    Skirmish,     // a fight (control point)
    ResourceHold, // a fight that, once taken, banks rallied support for the castle
    Merchant,     // shop + out-of-combat HP service (economy = G8)
    Unknown,      // fogged; resolves to a concrete kind when revealed
    Castle,       // the structural boss; cracking it disbands the war party and wins the leg
}

// One beacon on the chart. Type is the TRUE kind; while fogged the player only sees a `?`. Links
// are the charted jumps onward (branching / dead-ends fall out of the link structure).
public sealed class MapNode
{
    public string Id { get; }
    public NodeType Type { get; }
    public IReadOnlyList<string> Next { get; }
    public bool Visited { get; private set; }

    public MapNode(string id, NodeType type, params string[] next)
    {
        Id = id;
        Type = type;
        Next = next;
    }

    public void MarkVisited() => Visited = true;
}
