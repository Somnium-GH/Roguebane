namespace Roguebane.Core.Content;

// Encounters are content. Each is ONE enemy (single-foe canon): the old multi-layer HP folds into a
// single, tankier foe. Factories return fresh, stateful instances per call. Low HP scale.
public static class Sieges
{
    public static Encounter ControlPoint(string name, params int[] foeHp) =>
        new(name, new Foe(name, foeHp.Sum())); // one inert HP pool (control-point fodder)

    // The castle boss: one tough foe that restores itself while the player's rallied support auto-fires
    // on it — a DPS race between the two streams plus the player's own techniques. Support is the bank
    // earned from resource-holds on the way in.
    public static Encounter Castle(int supportAmount = 2)
    {
        // Cadence in COMBAT TICKS at the 10/sec clock: the boss mends 2 every ~1s, support pings ~2s.
        // A full-loadout build wins this DPS race; a starved one-technique build cannot out-damage the
        // restore (the thesis the balance sim asserts).
        return new Encounter("castle", new Foe("castle", 40),
            restoreAmount: 2, restoreEvery: 10, supportAmount: supportAmount, supportEvery: 20);
    }

    private static readonly string[] RaiderFigures = { "bandit", "skeleton" };

    // Armed control point: one armed raider (Frame + Arsenal) so it fights back — the live-run skirmish.
    // §8 foe part-aim is LIVE: it erodes the player's PARTS (survivable because kits carry a part-heal;
    // a build without a defensive source pays the intended penalty).
    public static Encounter ArmedPoint(string name, params int[] foeHp) =>
        new(name, Foes.Armed(name, foeHp.Sum(), figure: RaiderFigures[0], aim: FoeAim.Random),
            foePartAim: true);

    // Armed castle: one heavy armed boss plus the boss-restore / rallied-support DPS race. SMART aim
    // (strips the largest live stat) — the castle stays a whole-HP DPS race so the boss thesis holds.
    public static Encounter ArmedCastle(int supportAmount = 2) =>
        new("castle", Foes.Armed("castle", 40, arm: 4, figure: "ogre", aim: FoeAim.Smart),
            restoreAmount: 2, restoreEvery: 10, supportAmount: supportAmount, supportEvery: 20);

    public static Run StandardRun() =>
        new(new[] { ControlPoint("cp1", 12), ControlPoint("cp2", 10), Castle() });
}
