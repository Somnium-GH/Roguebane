namespace Roguebane.Core;

// A run is an ordered sequence of encounters: two control points, then the castle. You only
// advance once the current encounter is cleared; the run is complete when the castle falls.
public sealed class Run
{
    private readonly List<Encounter> _nodes;

    public Run(IReadOnlyList<Encounter> nodes)
    {
        if (nodes.Count == 0) throw new ArgumentException("a run needs at least one encounter", nameof(nodes));
        _nodes = nodes.ToList();
    }

    public int Index { get; private set; }

    public IReadOnlyList<Encounter> Nodes => _nodes;

    public Encounter Current => _nodes[Index];

    public bool OnFinalEncounter => Index == _nodes.Count - 1;

    public bool Completed => _nodes[^1].Cleared;

    public bool TryAdvance()
    {
        if (!Current.Cleared || OnFinalEncounter) return false;
        Index++;
        return true;
    }
}
