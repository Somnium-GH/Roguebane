namespace Roguebane.Core;

// End-to-end assembly, one composable flow: socket a chassis, fold in whatever its allocated runes
// grant, and compose the minted body with a technique loadout and a run into a ready-to-fight
// Session. The pick-chassis -> allocate-runes -> build-body -> run -> siege path lives here.
public static class Forge
{
    public static Session Assemble(
        Chassis chassis,
        RuneLoadout runes,
        IReadOnlyList<Technique> loadout,
        Run run)
    {
        var body = chassis.NewBody(runes);
        var caster = new Caster(body, run.Current.CurrentTarget);
        return new Session(PlayerFighter(body), caster, loadout, run);
    }

    // The player's HP life total: a small CON-scaled pool, fixed at mint. (Whether chest damage
    // lowers MAX HP or only the available pool is parked — see "Needs human".)
    public static Fighter PlayerFighter(Body body) => new(body, 8 + body.Capacity(Stat.Con));
}
