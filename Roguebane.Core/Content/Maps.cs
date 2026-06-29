namespace Roguebane.Core.Content;

// Run maps are content. A leg branches out of camp, rejoins, and ends at the castle — the player
// trades supplies and exposure for resource-holds while racing the war party home.
public static class Maps
{
    public static IReadOnlyList<MapNode> StandardLegNodes() => new[]
    {
        new MapNode("camp", NodeType.Skirmish, "a1", "a2"),
        new MapNode("a1", NodeType.ResourceHold, "b"),
        new MapNode("a2", NodeType.Skirmish, "b"),
        new MapNode("b", NodeType.Merchant, "c1", "c2"),
        new MapNode("c1", NodeType.Skirmish, "castle"),
        new MapNode("c2", NodeType.ResourceHold, "castle"),
        new MapNode("castle", NodeType.Castle),
    };

    // autoResolveCastle defaults true for standalone navigation; the Expedition driver passes false
    // so the castle is cracked by winning the siege, not merely arriving.
    public static RunMap StandardLeg(bool autoResolveCastle = true) =>
        new(StandardLegNodes(), startId: "camp", supplies: 8, marchLength: 6, autoResolveCastle);

    // N fresh legs for a campaign march — each a standalone standard leg the combat driver resolves.
    public static IReadOnlyList<Func<RunMap>> StandardLegs(int count)
    {
        var legs = new Func<RunMap>[count];
        for (var i = 0; i < count; i++) legs[i] = () => StandardLeg(autoResolveCastle: false);
        return legs;
    }

    // The combat a node hands the Expedition. Castle support is the bank earned from resource-holds.
    public static Encounter EncounterFor(MapNode node, int supportBank) => node.Type switch
    {
        NodeType.Skirmish => Sieges.ControlPoint(node.Id, 6, 6),
        NodeType.ResourceHold => Sieges.ControlPoint(node.Id, 8),
        NodeType.Castle => Sieges.Castle(supportBank),
        _ => Sieges.ControlPoint(node.Id, 6), // Unknown resolves to a light skirmish
    };
}
