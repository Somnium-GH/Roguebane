namespace Roguebane.Core;

// A Mark is one rung on a Path's ladder. Content, not code: cost, refund, and what the rung
// grants are all data. Climbing to a Mark overwrites the rung below it; Refund is the budget
// reclaimed when this Mark is the one being overwritten. Grants are body parts the rung sockets
// onto the chassis when held — a chassis-extending rune that widens the live stat pool.
public sealed record Mark(
    string Path,
    int Rank,
    int Cost,
    int Refund,
    bool Keystone = false,
    IReadOnlyList<BodyPart>? Grants = null,
    IReadOnlyList<Technique>? Techniques = null,
    IReadOnlyList<Minion>? Minions = null,
    string Name = "") // DISPLAY-ONLY rune-card title (design/02); "" falls back to "<Path> <Rank>"
{
    public string DisplayName => Name.Length > 0
        ? Name : char.ToUpperInvariant(Path[0]) + Path[1..] + " " + Rank;

    public IReadOnlyList<BodyPart> Granted => Grants ?? Array.Empty<BodyPart>();

    // Non-extension effects: a rune can also unlock a technique or a minion the chassis lacks —
    // "build something it wasn't built for". One data path interprets all rune grants.
    public IReadOnlyList<Technique> GrantedTechniques => Techniques ?? Array.Empty<Technique>();
    public IReadOnlyList<Minion> GrantedMinions => Minions ?? Array.Empty<Minion>();
}
