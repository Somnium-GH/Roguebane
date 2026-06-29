namespace Roguebane.Core.Content;

// Encounters are content. Factories return fresh, stateful instances per call. Low HP scale.
public static class Sieges
{
    public static Encounter ControlPoint(string name, params int[] foeHp)
    {
        var foes = foeHp.Select((hp, i) => new Foe($"{name}-{i}", hp)).ToList();
        return new Encounter(name, foes, structural: false);
    }

    // Layered defenses: the boss restores its standing front while the player's rallied support
    // auto-fires on it — a DPS race between the two streams plus the player's own techniques. The
    // support amount is the bank earned from resource-holds on the way in.
    public static Encounter Castle(int supportAmount = 2)
    {
        var foes = new[] { new Foe("gate", 12), new Foe("wall", 16), new Foe("keep", 12) };
        return new Encounter("castle", foes, structural: true,
            restoreAmount: 2, restoreEvery: 1, supportAmount: supportAmount, supportEvery: 2);
    }

    public static Run StandardRun() =>
        new(new[] { ControlPoint("cp1", 6, 6), ControlPoint("cp2", 10), Castle() });
}
