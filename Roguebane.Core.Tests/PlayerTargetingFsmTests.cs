using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The PLAYER targeting/firing doctrine (requireAim caster). A powered technique fires ONLY at its own
// explicit aim — no fallback to a default front, so untargeted holds. Firing is target-driven (no fire
// button): charged + aimed => it fires. AUTO off (the player default) is one-shot — it clears the
// target after the shot; AUTO on keeps the target so it fires each charge. Engine casters keep the
// old default-front auto-fire (covered by CasterFiringTests) — these pins are the player layer.
public class PlayerTargetingFsmTests
{
    private static Body Body()
    {
        var b = new Body();
        b.Add(new BodyPart("arm", Stat.Str, 6)); // powers Jab (reserve 1); no DEX => cooldown is exactly 50
        return b;
    }

    [Fact]
    public void UntargetedTechniqueNeverFires_NoFrontFallback()
    {
        var front = new Foe("front", 1000);
        var c = new Caster(Body(), front, requireAim: true); // a front EXISTS but requireAim ignores it
        c.Activate(Techniques.Jab, auto: true);

        for (var i = 0; i < 200; i++) c.Step();
        Assert.True(c.IsReady(Techniques.Jab)); // charged, holding
        Assert.Equal(1000, front.Hp);           // no aim => no fallback => never fired at the front
    }

    [Fact]
    public void TargetingDrivesTheShot_StandardIsOneShotThenClears()
    {
        var foe = new Foe("foe", 1000);
        var c = new Caster(Body(), null, requireAim: true);
        c.Activate(Techniques.Jab, auto: true);
        Assert.False(c.IsPersist(Techniques.Jab)); // player default: AUTO off / one-shot
        c.Aim(Techniques.Jab, foe);

        for (var i = 0; i < 200; i++) c.Step();    // 200 ticks = ~4 cooldowns of 50
        Assert.Equal(998, foe.Hp);                 // fired exactly ONCE (Power 2), then cleared the target
    }

    [Fact]
    public void ReAimingAfterAOneShotFiresAgain()
    {
        var foe = new Foe("foe", 1000);
        var c = new Caster(Body(), null, requireAim: true);
        c.Activate(Techniques.Jab, auto: true);

        c.Aim(Techniques.Jab, foe);
        for (var i = 0; i < 60; i++) c.Step(); // one shot -> 998, target cleared
        Assert.Equal(998, foe.Hp);

        c.Aim(Techniques.Jab, foe);            // re-target to fire again
        for (var i = 0; i < 60; i++) c.Step();
        Assert.Equal(996, foe.Hp);             // a second shot landed
    }

    [Fact]
    public void AutoOnPersistsTheTargetAndKeepsFiring()
    {
        var foe = new Foe("foe", 1000);
        var c = new Caster(Body(), null, requireAim: true);
        c.Activate(Techniques.Jab, auto: true);
        c.SetPersist(Techniques.Jab, true);    // AUTO on
        c.Aim(Techniques.Jab, foe);

        for (var i = 0; i < 200; i++) c.Step();
        Assert.True(foe.Hp <= 992);            // ~4 charges => >=4 shots at the same persisted target
    }

    [Fact]
    public void ClearingTheAimHoldsFire()
    {
        var foe = new Foe("foe", 1000);
        var c = new Caster(Body(), null, requireAim: true);
        c.Activate(Techniques.Jab, auto: true);
        c.SetPersist(Techniques.Jab, true);
        c.Aim(Techniques.Jab, foe);

        for (var i = 0; i < 60; i++) c.Step(); // one shot
        var hp = foe.Hp;
        Assert.True(hp < 1000);

        c.ClearAim(Techniques.Jab);            // right-click clears the target
        for (var i = 0; i < 200; i++) c.Step();
        Assert.Equal(hp, foe.Hp);              // no aim => holds, no fallback to a front
        Assert.True(c.IsActive(Techniques.Jab)); // still powered
    }
}
