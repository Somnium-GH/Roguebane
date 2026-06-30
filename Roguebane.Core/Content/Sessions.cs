namespace Roguebane.Core.Content;

public static class Sessions
{
    // A ready-to-fight demo body: generous stats so all six techniques fit at once, with a live
    // head (INT) for spells, dropped into the standard run. Balance is loose — see "play to feel it".
    public static Body DemoBody()
    {
        var body = new Body();
        body.Add(new BodyPart("arm-l", Stat.Str, 6));
        body.Add(new BodyPart("arm-r", Stat.Str, 6));
        body.Add(new BodyPart("leg-l", Stat.Dex, 4));
        body.Add(new BodyPart("leg-r", Stat.Dex, 4));
        body.Add(new BodyPart("head", Stat.Int, 12));
        body.Add(new BodyPart("chest", Stat.Con, 8));
        return body;
    }

    public static Session Demo()
    {
        var body = DemoBody();
        var run = Sieges.StandardRun();
        var caster = new Caster(body, run.Current.CurrentTarget); // Session = sim/legacy: default-front auto-fire
        return new Session(Forge.PlayerFighter(body), caster, Techniques.All, run);
    }

    // The real composable flow end to end: pick the Grunt chassis, climb its Vessel ladder to the
    // Hollow Vessel keystone (chassis extension it was never built for), mint the body with that
    // extra CON folded in, and drop it into the standard run with the full technique loadout.
    public static Session Forged()
    {
        var chassis = Chassrium.Grunt;
        var runes = chassis.NewLoadout();
        foreach (var rung in Paths.VesselLadder) runes.TryTake(rung);
        return Forge.Assemble(chassis, runes, Techniques.All, Sieges.StandardRun());
    }

    // The pre-run build screen's backbone: choose between the five chassis, climb either rune ladder,
    // pick from the six techniques. Launch() mints the body and drops it into the standard run.
    // The full run as one loop: a forged body marches the StandardLeg map, fighting at each node and
    // racing the war party to the castle. The castle's support is whatever holds were banked en route.
    public static Expedition Expedition()
    {
        var body = DemoBody();
        var caster = new Caster(body, maxCharge: Forge.MagicCapacity(body), requireAim: true);
        return new Expedition(
            Forge.PlayerFighter(body), caster, Techniques.All, Maps.StandardLeg(autoResolveCastle: false));
    }

    // The whole march: three legs to the Capital, one body and stash carrying through. Each leg is a
    // fresh map + war party. (Per-leg escalation of map/castle is content tuning — deferred.)
    public static Campaign NewCampaign()
    {
        var body = DemoBody();
        var caster = new Caster(body, requireAim: true);
        var legs = new Func<RunMap>[]
        {
            () => Maps.StandardLeg(autoResolveCastle: false),
            () => Maps.StandardLeg(autoResolveCastle: false),
            () => Maps.StandardLeg(autoResolveCastle: false),
        };
        return new Campaign(Forge.PlayerFighter(body), caster, Techniques.All, legs);
    }

    public static BuildSession NewBuild() => new(
        Chassrium.Roster,
        new[] { Paths.VesselLadder, Paths.ResonanceLadder },
        Techniques.All);
}
