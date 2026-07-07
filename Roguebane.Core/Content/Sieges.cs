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

    // CHUNK D item 3 (STATUS.md): skirmish/resource-hold pull from the real FOES.md T1 roster (gear +
    // Foe Effects wired, `Foes.Wraith`/`Foes.Ogre`) instead of the generic `Foes.Armed` stand-in above
    // (that helper stays -- FoeArmingTests/SiegeFigureTests still exercise it as a bare arming fixture,
    // unrelated to which content foe an encounter table picks). FOES.md doesn't pin node-type to a
    // specific foe, so which-foe-where is a seeded pick over the pool, not a hand-authored order --
    // FLAGGED as a placeholder ordering, not a design lock. The pool only lists T1 foes with real
    // gear/effects built so far; it grows as Skeleton/Gargoyle/Troll/Bandit clear their Needs-Doug
    // authoring blocks (STATUS.md CHUNK D item 2).
    private static readonly Func<string, int, Foe>[] T1Pool =
    {
        (id, hpBump) => Foes.Wraith(id, hp: 10 + hpBump),
        (id, hpBump) => Foes.Ogre(id, hp: 14 + hpBump),
    };

    private const ulong EncounterSalt = 0x454E4353; // decorrelates the foe-pick roll from the battle's own combat seed

    private static Foe PickT1(string name, ulong seed, int hpBump) =>
        T1Pool[new Rng(seed ^ EncounterSalt).Next(T1Pool.Length)](name, hpBump);

    // Skirmish: the T1 pool at its base HP.
    public static Encounter SkirmishPoint(string name, ulong seed) =>
        new(name, PickT1(name, seed, hpBump: 0), foePartAim: true);

    // Resource-hold: "tougher T1/T2" per CHUNK D item 3 -- T2 content (Dire Ogre) is still blocked on
    // the STR-budget reconciliation above, so this stays the SAME T1 pool at a bumped HP rather than
    // fabricating T2 stand-in content ahead of that call.
    public static Encounter ResourceHoldPoint(string name, ulong seed) =>
        new(name, PickT1(name, seed, hpBump: 6), foePartAim: true);

    public static Run StandardRun() =>
        new(new[] { ControlPoint("cp1", 12), ControlPoint("cp2", 10), Castle() });
}
