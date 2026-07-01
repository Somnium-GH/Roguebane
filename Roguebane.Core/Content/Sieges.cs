namespace Roguebane.Core.Content;

// Encounters are content. Each is ONE enemy (single-foe canon): the old multi-layer HP folds into a
// single, tankier foe. Factories return fresh, stateful instances per call. Low HP scale.
public static class Sieges
{
    public static Encounter ControlPoint(string name, params int[] foeHp) =>
        new(name, new Foe(name, foeHp.Sum())); // one inert HP pool (control-point fodder)

    // The castle boss: one STRUCTURED, armed foe that MENDS itself through a real technique (§8, no free
    // HP tick). Its strikes chip the player on a whole-HP DPS race while banked support auto-fires back.
    // A fast, shielded loadout clears it before its attrition bites; a starved one-technique build takes
    // too many strikes and falls (the thesis the balance sim asserts). Numbers placeholder.
    public static Encounter Castle(int supportAmount = 2) =>
        new("castle", Foes.ArmedHealing("castle", hp: 40, arm: 4, figure: "ogre", aim: FoeAim.Smart),
            supportAmount: supportAmount, supportEvery: 20);

    private static readonly string[] RaiderFigures = { "bandit", "skeleton" };

    // Armed control point: one armed raider (Frame + Arsenal) so it fights back — the live-run skirmish.
    // §8 foe part-aim is LIVE: it erodes the player's PARTS (survivable because kits carry a part-heal;
    // a build without a defensive source pays the intended penalty).
    public static Encounter ArmedPoint(string name, params int[] foeHp) =>
        new(name, Foes.Armed(name, foeHp.Sum(), figure: RaiderFigures[0], aim: FoeAim.Random),
            foePartAim: true);

    // Armed castle (campaign): the same self-mending structured boss. SMART aim + a whole-HP strike race
    // so the boss thesis holds; the mend is a real technique in its Arsenal, not a free tick.
    public static Encounter ArmedCastle(int supportAmount = 2) =>
        new("castle", Foes.ArmedHealing("castle", hp: 40, arm: 4, figure: "ogre", aim: FoeAim.Smart),
            supportAmount: supportAmount, supportEvery: 20);

    public static Run StandardRun() =>
        new(new[] { ControlPoint("cp1", 12), ControlPoint("cp2", 10), Castle() });
}
