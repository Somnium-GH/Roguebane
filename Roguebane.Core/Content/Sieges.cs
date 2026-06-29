namespace Roguebane.Core.Content;

// Encounters are content. Factories return fresh, stateful instances per call.
public static class Sieges
{
    private static readonly IReadOnlyDictionary<Attribute, int> NoDemand = new Dictionary<Attribute, int>();

    private static Part Defender(string id, int hp) => new(id, NoDemand, PartRole.Generic, hp);

    private static Entity Hold(IEnumerable<Part> parts)
    {
        var e = new Entity(new AttributePool(new Dictionary<Attribute, int>()));
        foreach (var p in parts) e.Add(p);
        return e;
    }

    public static Encounter ControlPoint(string name, params int[] enemyHealth)
    {
        var parts = enemyHealth.Select((hp, i) => Defender($"{name}-{i}", hp)).ToList();
        return new Encounter(name, Hold(parts), parts, structural: false);
    }

    // Layered defenses with a rallied-support stream that repairs the standing front.
    public static Encounter Castle()
    {
        var parts = new[] { Defender("gate", 20), Defender("wall", 30), Defender("keep", 25) };
        return new Encounter("castle", Hold(parts), parts, structural: true, repairAmount: 3, repairEvery: 2);
    }

    public static Run StandardRun() =>
        new(new[] { ControlPoint("cp1", 10, 10), ControlPoint("cp2", 15), Castle() });
}
