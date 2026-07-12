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

    // Item 2 (STATUS.md 2026-07-12 round 2, Doug: heals "don't work because they're not targeted"). A
    // powered Self heal (Bandage) must read as armed/charging the instant it's powered -- no target step.
    // Its StatusOf snapshot is the SAME shape as an attack's (cf. ActiveTimeredCountsDownThenResets): a
    // live Active + a counting-down Timered cooldown, with NO foe and NO aim involved. So nothing in the
    // status/render-signal layer makes a heal read as inert; the auto-fire path (Discharge's Heals branch,
    // MostDamagedPart) needs no target either. Any residual "reads as inert" is a card-skin/art detail.
    [Fact]
    public void PoweredSelfHealChargesLikeAnAttackWithNoTarget()
    {
        var c = new Caster(Body()); // no target at all
        c.Activate(Techniques.Bandage);

        var st = c.StatusOf(Techniques.Bandage);
        Assert.True(st.Active);                                 // powered -> armed
        Assert.False(st.Sustained);                            // a Timered heal, not a held shield
        Assert.Equal(Techniques.Bandage.Cooldown, st.Countdown); // charging from full, no aim needed
        c.Step();
        Assert.Equal(Techniques.Bandage.Cooldown - 1, c.StatusOf(Techniques.Bandage).Countdown); // ticks like an attack
    }
}
