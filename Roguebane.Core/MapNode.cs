namespace Roguebane.Core;

// The kinds of beacon on the run map. Unknown is fog: its true kind is hidden until the player
// charts close enough to resolve it.
public enum NodeType
{
    Camp,         // the leg's origin — safe ground, never a fight, always known
    Skirmish,     // a fight (control point)
    ResourceHold, // a fight that, once taken, banks rallied support for the castle
    Merchant,     // shop + out-of-combat HP service (economy = G8)
    Quest,        // two-step accept/decline prompt, no fight (placeholder catalog, real mechanism)
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

    // Layout hints for the chart render: Col = depth from camp, Row = lane within that depth. Pure
    // data (no engine types) so Core stays headless; the shell maps them to screen coordinates.
    public int Col { get; private set; }
    public int Row { get; private set; }

    public MapNode(string id, NodeType type, params string[] next)
    {
        Id = id;
        Type = type;
        Next = next;
    }

    // Place this node on the chart grid (fluent, returns self for terse map data).
    public MapNode At(int col, int row)
    {
        Col = col;
        Row = row;
        return this;
    }

    public void MarkVisited() => Visited = true;
}
