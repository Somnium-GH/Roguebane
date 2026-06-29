namespace Roguebane.Core.Content;

// Run maps are content. A leg branches out of camp, rejoins, and ends at the castle — the player
// trades supplies and exposure for resource-holds while racing the war party home.
public static class Maps
{
    public static RunMap StandardLeg()
    {
        var nodes = new[]
        {
            new MapNode("camp", NodeType.Skirmish, "a1", "a2"),
            new MapNode("a1", NodeType.ResourceHold, "b"),
            new MapNode("a2", NodeType.Skirmish, "b"),
            new MapNode("b", NodeType.Merchant, "c1", "c2"),
            new MapNode("c1", NodeType.Skirmish, "castle"),
            new MapNode("c2", NodeType.ResourceHold, "castle"),
            new MapNode("castle", NodeType.Castle),
        };
        return new RunMap(nodes, startId: "camp", supplies: 8, marchLength: 6);
    }
}
