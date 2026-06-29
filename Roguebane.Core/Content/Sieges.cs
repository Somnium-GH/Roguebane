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
        // Cadences are in COMBAT TICKS at the 10/sec clock: the boss mends its front 2 every ~1s and
        // the rallied support pings every ~2s. A full-loadout build wins this DPS race; a starved
        // one-technique build cannot out-damage the restore (the thesis the balance sim asserts).
        var foes = new[] { new Foe("gate", 12), new Foe("wall", 16), new Foe("keep", 12) };
        return new Encounter("castle", foes, structural: true,
            restoreAmount: 2, restoreEvery: 10, supportAmount: supportAmount, supportEvery: 20);
    }

    // Armed control point: the same fodder, but each foe carries a weak Frame+Arsenal so it fights
    // back (the live-run encounters). Threat stays low — runs remain winnable.
    public static Encounter ArmedPoint(string name, params int[] foeHp)
    {
        var foes = foeHp.Select((hp, i) => Foes.Armed($"{name}-{i}", hp)).ToList();
        return new Encounter(name, foes, structural: false);
    }

    // Armed castle: layered, armed defenders plus the boss-restore / rallied-support DPS race.
    public static Encounter ArmedCastle(int supportAmount = 2)
    {
        var foes = new[] { Foes.Armed("gate", 12), Foes.Armed("wall", 16), Foes.Armed("keep", 12) };
        return new Encounter("castle", foes, structural: true,
            restoreAmount: 2, restoreEvery: 10, supportAmount: supportAmount, supportEvery: 20);
    }

    public static Run StandardRun() =>
        new(new[] { ControlPoint("cp1", 6, 6), ControlPoint("cp2", 10), Castle() });
}
