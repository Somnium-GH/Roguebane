namespace Roguebane.Core.Content;

public static class Sessions
{
    // A ready-to-fight demo: a generously-pooled body with a live head and all six techniques,
    // dropped into the standard run. Balance is intentionally loose — see "play to feel it".
    public static Session Demo()
    {
        var player = new Entity(new AttributePool(new Dictionary<Attribute, int>
        {
            [Attribute.Power] = 30, [Attribute.Focus] = 30, [Attribute.Vigor] = 30,
        }));
        var head = new Part("head", new Dictionary<Attribute, int> { [Attribute.Vigor] = 2 }, PartRole.Head, 10);
        player.Add(head);
        player.Enable(head);

        var run = Sieges.StandardRun();
        var caster = new Caster(player, run.Current.Defenders, run.Current.CurrentTarget!);
        return new Session(player, caster, Techniques.All, run);
    }
}
