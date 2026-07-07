namespace Roguebane.Core.Content;

// Run maps are content. A leg branches out of camp, rejoins, and ends at the castle — the player
// trades supplies and exposure for resource-holds while racing the war party home.
public static class Maps
{
    // Coords (Col = depth from camp, Row = lane) lay the chart out as a graph: camp -> a1/a2 -> b ->
    // c1/c2 -> castle. Row 1 is the centre lane; 0/2 are the upper/lower branches.
    public static IReadOnlyList<MapNode> StandardLegNodes() => new[]
    {
        new MapNode("camp", NodeType.Camp, "a1", "a2", "quest").At(0, 1),
        new MapNode("a1", NodeType.ResourceHold, "b").At(1, 0),
        new MapNode("a2", NodeType.Skirmish, "b").At(1, 2),
        // placeholder wiring (STATUS.md "Quests", 2026-07-07 Doug): one dead-end Quest slot off
        // camp -- real placement/frequency on the map graph is a separate design pass, not invented
        // here. Dead-ends are a supported chart shape (see MapNode's own doc comment).
        new MapNode("quest", NodeType.Quest).At(1, 1),
        new MapNode("b", NodeType.Merchant, "c1", "c2").At(2, 1),
        new MapNode("c1", NodeType.Skirmish, "castle").At(3, 0),
        new MapNode("c2", NodeType.ResourceHold, "castle").At(3, 2),
        new MapNode("castle", NodeType.Castle).At(4, 1),
    };

    // autoResolveCastle defaults true for standalone navigation; the Expedition driver passes false
    // so the castle is cracked by winning the siege, not merely arriving.
    public static CityMap StandardLeg(bool autoResolveCastle = true) =>
        new(StandardLegNodes(), startId: "camp", supplies: 8, marchLength: 6, autoResolveCastle);

    // N fresh legs for a campaign march — each a standalone standard leg the combat driver resolves.
    public static IReadOnlyList<Func<CityMap>> StandardLegs(int count)
    {
        var legs = new Func<CityMap>[count];
        for (var i = 0; i < count; i++) legs[i] = () => StandardLeg(autoResolveCastle: false);
        return legs;
    }

    // The combat a node hands the Expedition. Skirmish/resource-hold pull from the real FOES.md T1
    // roster (CHUNK D item 3, STATUS.md) via a seeded pick -- `seed` is the caller's own stable
    // per-node seed (Expedition.Seed), so the same (leg, node) always picks the same foe. Castle support
    // is the bank earned from resource-holds; the castle boss itself stays the fixed self-mending stand-
    // in (Sieges.ArmedCastle), unaffected by the roster pool.
    public static Encounter EncounterFor(MapNode node, int supportBank, ulong seed) => node.Type switch
    {
        NodeType.Skirmish => Sieges.SkirmishPoint(node.Id, seed),
        NodeType.ResourceHold => Sieges.ResourceHoldPoint(node.Id, seed),
        NodeType.Castle => Sieges.ArmedCastle(supportBank),
        _ => Sieges.SkirmishPoint(node.Id, seed), // Unknown resolves to a light skirmish
    };
}
