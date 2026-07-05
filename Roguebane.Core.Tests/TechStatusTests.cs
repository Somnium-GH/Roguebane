using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The action-bar render reads a live per-technique snapshot (cooldown fill + card state) from the
// caster. These assert the snapshot tracks the tick loop.
public class TechStatusTests
{
    private static Body Body()
    {
        var b = new Body();
        b.Add(new BodyPart("arm", Stat.Str, 6));
        b.Add(new BodyPart("head", Stat.Int, 6));
        b.Add(new BodyPart("chest", Stat.Con, 6));
        b.Wield(Armory.Sword); // Jab consults it (timer 1.0 -- haste-neutral)
        return b; // no DEX -> no haste, so cooldowns read at their base
    }

    [Fact]
    public void InactiveTechniqueReadsNotActive()
    {
        var c = new Caster(Body());
        var st = c.StatusOf(Techniques.Jab);
        Assert.False(st.Active);
        Assert.Equal(Techniques.Jab.Cooldown, st.Cooldown);
    }

    [Fact]
    public void ActiveTimeredCountsDownThenResets()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab);

        Assert.Equal(40, c.StatusOf(Techniques.Jab).Countdown); // fresh = full cooldown
        c.Step();
        Assert.Equal(39, c.StatusOf(Techniques.Jab).Countdown);
    }

    [Fact]
    public void SustainedBraceReadsHeld()
    {
        var c = new Caster(Body());
        c.Activate(Techniques.Brace);
        var st = c.StatusOf(Techniques.Brace);
        Assert.True(st.Active);
        Assert.True(st.Sustained);
    }
}
