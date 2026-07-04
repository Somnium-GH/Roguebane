using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// FTL firing model: a Timered technique charges to ready then HOLDS — it discharges on command
// (Fire) or, with AUTO on, automatically on cadence. Aim picks which target a fired shot lands on.
public class CasterFiringTests
{
    private static Body Body()
    {
        var b = new Body();
        b.Add(new BodyPart("arm", Stat.Str, 6)); // powers Jab (reserve 1); no DEX => base cooldown
        return b;
    }

    [Fact]
    public void ChargedNonAutoTechniqueHoldsUntilFired()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab, auto: false);

        for (var i = 0; i < 200; i++) c.Step(); // long past its cooldown
        Assert.True(c.IsReady(Techniques.Jab)); // charged and holding
        Assert.Equal(1000, foe.Hp);             // never auto-fired

        Assert.True(c.Fire(Techniques.Jab));    // discharge on command
        Assert.True(foe.Hp < 1000);
        Assert.False(c.IsReady(Techniques.Jab)); // recharging again
    }

    [Fact]
    public void AutoTechniqueRepeatsWithoutCommand()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab, auto: true);

        for (var i = 0; i < 200; i++) c.Step();
        Assert.True(foe.Hp < 1000); // fired on its own, repeatedly
    }

    [Fact]
    public void FiringBeforeReadyIsANoOp()
    {
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe);
        c.Activate(Techniques.Jab, auto: false);

        Assert.False(c.IsReady(Techniques.Jab)); // fresh, still charging
        Assert.False(c.Fire(Techniques.Jab));    // can't fire mid-charge
        Assert.Equal(1000, foe.Hp);
    }

    [Fact]
    public void PlayerDoctrineChargesToReadyWhileUntargetedThenFiresCleanlyOnceAimed()
    {
        // Debt watch item: "firing after a weapon charges while UNTARGETED misbehaves" (no repro found
        // yet). This pins the one Core-side path that could produce it: a player technique (requireAim)
        // reaching Countdown<=0 with no aim must HOLD indefinitely (not skip/duplicate its cooldown), and
        // the very next Step after being aimed must discharge exactly once with a clean cooldown reset.
        var foe = new Foe("dummy", 1000);
        var c = new Caster(Body(), foe, requireAim: true);
        c.Activate(Techniques.Jab, auto: true); // player techs are always Auto:true; requireAim holds fire

        for (var i = 0; i < 200; i++) c.Step(); // charges well past its cooldown, no aim yet
        Assert.True(c.IsReady(Techniques.Jab));  // ready...
        Assert.Equal(1000, foe.Hp);              // ...but untargeted -> never auto-fired

        c.Aim(Techniques.Jab, foe);
        c.Step(); // first tick after aiming
        Assert.True(foe.Hp < 1000);              // fires cleanly, exactly once
        Assert.False(c.IsReady(Techniques.Jab)); // cooldown reset, recharging again
    }

    [Fact]
    public void FireLandsOnTheTechniquesOwnAimNotTheDefaultFront()
    {
        var front = new Foe("front", 100);
        var flank = new Foe("flank", 100);
        var c = new Caster(Body(), front);
        c.Activate(Techniques.Jab, auto: false);
        c.Aim(Techniques.Jab, flank);

        for (var i = 0; i < 60; i++) c.Step(); // charge to ready
        Assert.True(c.Fire(Techniques.Jab));

        Assert.Equal(100, front.Hp);   // default front untouched
        Assert.True(flank.Hp < 100);   // the aimed flank took the hit
    }
}
