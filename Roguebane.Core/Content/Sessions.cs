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
        var player = DemoBody();
        var run = Sieges.StandardRun();
        var caster = new Caster(player, run.Current.CurrentTarget);
        return new Session(player, caster, Techniques.All, run);
    }
}
